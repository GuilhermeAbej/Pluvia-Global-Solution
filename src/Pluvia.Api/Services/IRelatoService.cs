using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface IRelatoService
{
    Task<RelatoResponse> EnviarAsync(int usuarioId, NovoRelato novo, CancellationToken ct = default);
    Task<FotoAnaliseResponse> AnalisarFotoAsync(byte[] foto, CancellationToken ct = default);
    Task<RelatoDetalheResponse> ObterDoAutorAsync(int usuarioId, int relatoId, CancellationToken ct = default);
    Task<IReadOnlyList<RelatoDetalheResponse>> ListarDoAutorAsync(int usuarioId, CancellationToken ct = default);
    Task<IReadOnlyList<RelatoPublicoResponse>> ListarPublicosAsync(double lat, double lng, int raioMetros, CancellationToken ct = default);
    Task ExcluirAsync(int usuarioId, int relatoId, CancellationToken ct = default);
    Task<(Stream Conteudo, string ContentType)> ObterFotoAsync(int relatoId, int? usuarioId, bool oficial, CancellationToken ct = default);
    Task<RelatoDetalheResponse> PublicarAsync(int relatoId, CancellationToken ct = default);
    Task<RelatoDetalheResponse> RejeitarAsync(int relatoId, CancellationToken ct = default);
}
