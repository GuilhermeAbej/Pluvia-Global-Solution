using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface IGeradorDeAlertas
{
    Task NotificarNovoEventoAsync(Ocorrencia ocorrencia, CancellationToken ct = default);
    Task NotificarEncerramentoAsync(Ocorrencia ocorrencia, CancellationToken ct = default);
}
