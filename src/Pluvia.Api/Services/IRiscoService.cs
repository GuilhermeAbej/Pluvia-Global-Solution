using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface IRiscoService
{
    Task<RiscoCalculado> CalcularAsync(double lat, double lng, double raioMetros, CancellationToken ct = default);
}
