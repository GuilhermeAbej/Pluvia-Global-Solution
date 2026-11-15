using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface INotificador
{
    Task NotificarAsync(Alerta alerta, CancellationToken ct = default);
}
