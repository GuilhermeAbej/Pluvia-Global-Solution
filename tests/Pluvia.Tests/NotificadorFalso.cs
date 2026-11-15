using Pluvia.Api.Data;
using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Regras;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Pluvia.Tests;

public class NotificadorFalso : INotificador
{
    public List<Alerta> Enviados { get; } = new();
    public Task NotificarAsync(Alerta alerta, CancellationToken ct = default)
    {
        Enviados.Add(alerta);
        return Task.CompletedTask;
    }
}
