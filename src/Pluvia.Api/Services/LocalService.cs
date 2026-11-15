using Pluvia.Api.Data;
using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Pluvia.Api.Services;

public class LocalService : ILocalService
{
    private readonly PluviaDbContext _db;
    private readonly IRiscoService _risco;
    private readonly TimeProvider _relogio;

    public LocalService(PluviaDbContext db, IRiscoService risco, TimeProvider relogio)
    {
        _db = db; _risco = risco; _relogio = relogio;
    }

    public async Task<LocalResponse> CriarAsync(int usuarioId, CriarLocalRequest req, CancellationToken ct = default)
    {
        if (!await _db.Usuarios.AnyAsync(u => u.Id == usuarioId, ct))
            throw new RecursoNaoEncontradoException("Usuário", usuarioId);

        var area = new AreaMonitorada(usuarioId, req.Nome, req.Lat, req.Lng, req.RaioMetros);
        // Guarda o risco de partida: só mudanças a partir daqui geram aviso.
        area.AtualizarRisco((await _risco.CalcularAsync(area.Lat, area.Lng, area.RaioMetros, ct)).Risco);
        _db.Areas.Add(area);
        await _db.SaveChangesAsync(ct);
        return Mapear(area);
    }

    public async Task<IReadOnlyList<LocalResponse>> ListarAsync(int usuarioId, CancellationToken ct = default)
    {
        var areas = await _db.Areas.AsNoTracking().Where(a => a.UsuarioId == usuarioId).OrderBy(a => a.Nome).ToListAsync(ct);
        return areas.Select(Mapear).ToList();
    }

    public async Task<RiscoResponse> RiscoAsync(int usuarioId, int localId, CancellationToken ct = default)
    {
        var area = await ObterDoDono(usuarioId, localId, ct);
        var r = await _risco.CalcularAsync(area.Lat, area.Lng, area.RaioMetros, ct);
        return ParaResposta(r);
    }

    public async Task<LocalResponse> SilenciarAsync(int usuarioId, int localId, int minutos, CancellationToken ct = default)
    {
        var area = await ObterDoDono(usuarioId, localId, ct);
        area.Silenciar(TimeSpan.FromMinutes(minutos), _relogio.GetUtcNow().UtcDateTime);
        await _db.SaveChangesAsync(ct);
        return Mapear(area);
    }

    public async Task<LocalResponse> ReativarAsync(int usuarioId, int localId, CancellationToken ct = default)
    {
        var area = await ObterDoDono(usuarioId, localId, ct);
        area.ReativarAvisos();
        await _db.SaveChangesAsync(ct);
        return Mapear(area);
    }

    public static RiscoResponse ParaResposta(RiscoCalculado r) => new(
        r.Risco, r.OcorrenciasEm90Dias, r.OcorrenciasAtivas,
        r.Risco == RiscoEstimado.Indeterminado
            ? "Ainda não há histórico suficiente. Isso NÃO significa que o local seja seguro."
            : "Risco estimado por histórico do Pluvia. Não confirma água na pista, pode mudar rapidamente e não substitui os avisos oficiais.");

    private async Task<AreaMonitorada> ObterDoDono(int usuarioId, int localId, CancellationToken ct)
    {
        var area = await _db.Areas.FirstOrDefaultAsync(a => a.Id == localId, ct)
            ?? throw new RecursoNaoEncontradoException("Local monitorado", localId);
        if (area.UsuarioId != usuarioId) throw new AcessoNegadoException("Este local pertence a outro usuário.");
        return area;
    }

    private static LocalResponse Mapear(AreaMonitorada a) =>
        new(a.Id, a.Nome, a.Lat, a.Lng, a.RaioMetros, a.SilenciadaAte, a.RiscoAtual);
}
