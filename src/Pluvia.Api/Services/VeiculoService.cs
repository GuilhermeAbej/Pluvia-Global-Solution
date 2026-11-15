using Pluvia.Api.Data;
using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Pluvia.Api.Services;

public class VeiculoService : IVeiculoService
{
    private readonly PluviaDbContext _db;
    private readonly TimeProvider _relogio;

    public VeiculoService(PluviaDbContext db, TimeProvider relogio) { _db = db; _relogio = relogio; }

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
