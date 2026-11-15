using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Controllers;

[Route("api/locais-monitorados")]
public class LocaisMonitoradosController : BaseController
{
    private readonly ILocalService _servico;
    public LocaisMonitoradosController(ILocalService servico) => _servico = servico;

    /// <summary>Cadastra um lugar para monitorar (casa, trabalho, escola).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LocalResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarLocalRequest req, CancellationToken ct)
    {
        var r = await _servico.CriarAsync(UsuarioAtual, req, ct);
        return Created($"/api/locais-monitorados/{r.Id}", r);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocalResponse>>> Listar(CancellationToken ct) =>
        Ok(await _servico.ListarAsync(UsuarioAtual, ct));

    /// <summary>Risco estimado do local. "Indeterminado" quando falta histórico, nunca "seguro".</summary>
    [HttpGet("{id:int}/risco")]
    public async Task<ActionResult<RiscoResponse>> Risco(int id, CancellationToken ct) =>
        Ok(await _servico.RiscoAsync(UsuarioAtual, id, ct));

    /// <summary>Silencia os avisos do local por até 24 h.</summary>
    [HttpPost("{id:int}/silencio")]
    public async Task<ActionResult<LocalResponse>> Silenciar(int id, SilenciarRequest req, CancellationToken ct) =>
        Ok(await _servico.SilenciarAsync(UsuarioAtual, id, req.Minutos, ct));

    [HttpDelete("{id:int}/silencio")]
    public async Task<ActionResult<LocalResponse>> Reativar(int id, CancellationToken ct) =>
        Ok(await _servico.ReativarAsync(UsuarioAtual, id, ct));
}
