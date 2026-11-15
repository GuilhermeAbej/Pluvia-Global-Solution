using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface IVeiculoService
{
    Task<VeiculoResponse> CriarAsync(int usuarioId, CriarVeiculoRequest req, CancellationToken ct = default);
    Task<PosicaoResponse> AtualizarPosicaoAsync(int usuarioId, int veiculoId, PosicaoRequest req, CancellationToken ct = default);
    Task<PosicaoResponse> ObterPosicaoAsync(int usuarioId, int veiculoId, CancellationToken ct = default);
    Task<VeiculoResponse> DefinirAvisosAsync(int usuarioId, int veiculoId, bool ativos, CancellationToken ct = default);
}
