using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface ISensorService
{
    Task<LeituraResponse> RegistrarLeituraAsync(LeituraRequest req, CancellationToken ct = default);
}
