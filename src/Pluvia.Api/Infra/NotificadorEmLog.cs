using System.Security.Cryptography;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Infra;

/// <summary>Notificação simulada: só registra no log. Troque por push/SMS sem mexer nas regras.</summary>
public class NotificadorEmLog : INotificador
{
    private readonly ILogger<NotificadorEmLog> _log;
    public NotificadorEmLog(ILogger<NotificadorEmLog> log) => _log = log;

    public Task NotificarAsync(Alerta alerta, CancellationToken ct = default)
    {
        _log.LogInformation("[NOTIFICAÇÃO SIMULADA] usuário {Usuario} | {Tipo} | {Mensagem}",
            alerta.UsuarioId, alerta.Tipo, alerta.Mensagem);
        return Task.CompletedTask;
    }
}
