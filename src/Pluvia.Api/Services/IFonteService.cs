using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface IFonteService
{
    Task<IReadOnlyList<FonteResponse>> ListarAsync(CancellationToken ct = default);
}
