using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface ILocalService
{
    Task<LocalResponse> CriarAsync(int usuarioId, CriarLocalRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<LocalResponse>> ListarAsync(int usuarioId, CancellationToken ct = default);
    Task<RiscoResponse> RiscoAsync(int usuarioId, int localId, CancellationToken ct = default);
    Task<LocalResponse> SilenciarAsync(int usuarioId, int localId, int minutos, CancellationToken ct = default);
    Task<LocalResponse> ReativarAsync(int usuarioId, int localId, CancellationToken ct = default);
}
