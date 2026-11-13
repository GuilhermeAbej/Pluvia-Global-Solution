using Alaga.Api.Data;
using Alaga.Api.Dtos;
using Alaga.Domain.Entities;
using Alaga.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Alaga.Api.Services;

public class UsuarioService : IUsuarioService
{
    private readonly AlagaDbContext _db;
    private readonly TimeProvider _relogio;

    public UsuarioService(AlagaDbContext db, TimeProvider relogio) { _db = db; _relogio = relogio; }

    public async Task<UsuarioResponse> CriarAsync(CriarUsuarioRequest req, CancellationToken ct = default)
    {
        var usuario = new Usuario(req.Nome, req.ConsentimentoLgpd, _relogio.GetUtcNow().UtcDateTime);
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);
        return new UsuarioResponse(usuario.Id, usuario.Nome);
    }
}

public class FonteService : IFonteService
{
    private readonly AlagaDbContext _db;
    public FonteService(AlagaDbContext db) => _db = db;

    public async Task<IReadOnlyList<FonteResponse>> ListarAsync(CancellationToken ct = default) =>
        await _db.Fontes.AsNoTracking().OrderBy(f => f.Id)
            .Select(f => new FonteResponse(f.Id, f.Nome, f.Tipo)).ToListAsync(ct);
}

public class VeiculoService : IVeiculoService
{
    private readonly AlagaDbContext _db;
    private readonly TimeProvider _relogio;

    public VeiculoService(AlagaDbContext db, TimeProvider relogio) { _db = db; _relogio = relogio; }

    public async Task<VeiculoResponse> CriarAsync(int usuarioId, CriarVeiculoRequest req, CancellationToken ct = default)
    {
        if (!await _db.Usuarios.AnyAsync(u => u.Id == usuarioId, ct))
            throw new RecursoNaoEncontradoException("Usuário", usuarioId);

        var veiculo = new Veiculo(usuarioId, req.Apelido, req.RaioMetros);
        _db.Veiculos.Add(veiculo);
        await _db.SaveChangesAsync(ct);
        return Mapear(veiculo);
    }

    public async Task<PosicaoResponse> AtualizarPosicaoAsync(int usuarioId, int veiculoId, PosicaoRequest req, CancellationToken ct = default)
    {
        var veiculo = await ObterDoDono(usuarioId, veiculoId, ct);
        veiculo.AtualizarPosicao(req.Lat, req.Lng, _relogio.GetUtcNow().UtcDateTime);
        await _db.SaveChangesAsync(ct);
        return new PosicaoResponse(veiculo.Id, veiculo.Lat, veiculo.Lng, veiculo.AtualizadoEm);
    }

    public async Task<PosicaoResponse> ObterPosicaoAsync(int usuarioId, int veiculoId, CancellationToken ct = default)
    {
        var veiculo = await ObterDoDono(usuarioId, veiculoId, ct);
        return new PosicaoResponse(veiculo.Id, veiculo.Lat, veiculo.Lng, veiculo.AtualizadoEm);
    }

    public async Task<VeiculoResponse> DefinirAvisosAsync(int usuarioId, int veiculoId, bool ativos, CancellationToken ct = default)
    {
        var veiculo = await ObterDoDono(usuarioId, veiculoId, ct);
        veiculo.DefinirAvisos(ativos);
        await _db.SaveChangesAsync(ct);
        return Mapear(veiculo);
    }

    // Privacidade: a posição do veículo só é visível para o dono.
    private async Task<Veiculo> ObterDoDono(int usuarioId, int veiculoId, CancellationToken ct)
    {
        var veiculo = await _db.Veiculos.FirstOrDefaultAsync(v => v.Id == veiculoId, ct)
            ?? throw new RecursoNaoEncontradoException("Veículo", veiculoId);
        if (veiculo.UsuarioId != usuarioId)
            throw new AcessoNegadoException("Este veículo pertence a outro usuário.");
        return veiculo;
    }

    private static VeiculoResponse Mapear(Veiculo v) => new(v.Id, v.Apelido, v.RaioMetros, v.AvisosAtivos);
}

public class AlertaService : IAlertaService
{
    private readonly AlagaDbContext _db;
    public AlertaService(AlagaDbContext db) => _db = db;

    public async Task<IReadOnlyList<AlertaResponse>> ListarDoUsuarioAsync(int usuarioId, CancellationToken ct = default)
    {
        var linhas = await (
            from a in _db.Alertas.AsNoTracking()
            where a.UsuarioId == usuarioId
            join o in _db.Ocorrencias on a.OcorrenciaId equals o.Id
            join f in _db.Fontes on o.FonteId equals f.Id
            orderby a.CriadoEm descending
            select new { a.Id, a.Tipo, a.Mensagem, a.OcorrenciaId, Fonte = f.Nome, a.CriadoEm }
        ).ToListAsync(ct);

        // Cada aviso leva origem, horário e link para o evento.
        return linhas.Select(l => new AlertaResponse(
            l.Id, l.Tipo, l.Mensagem, l.OcorrenciaId, l.Fonte, $"/api/ocorrencias/{l.OcorrenciaId}", l.CriadoEm)).ToList();
    }
}
