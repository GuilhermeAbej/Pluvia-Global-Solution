using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Controllers;

[Route("api/alertas")]
public class AlertasController : BaseController
{
    private readonly IAlertaService _servico;
    public AlertasController(IAlertaService servico) => _servico = servico;

    /// <summary>Avisos recebidos pelo usuário, com origem, horário e link do evento.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertaResponse>>> Listar(CancellationToken ct) =>
        Ok(await _servico.ListarDoUsuarioAsync(UsuarioAtual, ct));
}
