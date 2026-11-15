using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface IAlertaService
{
    Task<IReadOnlyList<AlertaResponse>> ListarDoUsuarioAsync(int usuarioId, CancellationToken ct = default);
}
