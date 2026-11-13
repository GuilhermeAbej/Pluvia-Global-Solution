using Alaga.Api.Data;
using Alaga.Api.Options;
using Alaga.Domain.Entities;
using Alaga.Domain.Enums;
using Alaga.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Alaga.Api.Services;

public class GestorDeOcorrencias : IGestorDeOcorrencias
{
    private readonly AlagaDbContext _db;
    private readonly IGeradorDeAlertas _alertas;
    private readonly TimeProvider _relogio;
    private readonly AlagaOptions _opt;

    public GestorDeOcorrencias(AlagaDbContext db, IGeradorDeAlertas alertas, TimeProvider relogio, IOptions<AlagaOptions> opt)
    {
        _db = db; _alertas = alertas; _relogio = relogio; _opt = opt.Value;
    }

    public async Task<(Ocorrencia Ocorrencia, bool Criada)> AbrirAsync(
        int fonteId, double lat, double lng, string? descricao, CancellationToken ct = default)
    {
        if (!await _db.Fontes.AnyAsync(f => f.Id == fonteId, ct))
            throw new RecursoNaoEncontradoException("Fonte", fonteId);

        var agora = _relogio.GetUtcNow().UtcDateTime;

        // A mesma fonte não abre duas ocorrências no mesmo ponto: só confirma que continua.
        var existente = (await _db.OcorrenciasPerto(lat, lng, _opt.RaioAgrupamentoMetros,
            StatusOcorrencia.Ativa, fonteId, ct: ct)).FirstOrDefault();
        if (existente is not null)
        {
            existente.RegistrarAtualizacao(agora);
            await _db.SaveChangesAsync(ct);
            return (existente, false);
        }

        var nova = new Ocorrencia(fonteId, lat, lng, descricao, agora);
        _db.Ocorrencias.Add(nova);
        await _db.SaveChangesAsync(ct);

        _db.Historicos.Add(new HistoricoStatus(nova.Id, StatusOcorrencia.Ativa, fonteId, agora));
        await _db.SaveChangesAsync(ct);

        await _alertas.NotificarNovoEventoAsync(nova, ct);
        return (nova, true);
    }

    public async Task EncerrarAsync(Ocorrencia ocorrencia, int fonteId, CancellationToken ct = default)
    {
        var agora = _relogio.GetUtcNow().UtcDateTime;
        ocorrencia.Encerrar(fonteId, agora);   // regra de domínio: só a mesma fonte encerra
        _db.Historicos.Add(new HistoricoStatus(ocorrencia.Id, StatusOcorrencia.Encerrada, fonteId, agora));
        await _db.SaveChangesAsync(ct);

        await _alertas.NotificarEncerramentoAsync(ocorrencia, ct);
    }
}
