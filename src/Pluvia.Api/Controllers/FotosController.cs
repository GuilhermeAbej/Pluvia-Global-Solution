using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Controllers;

[Route("api/fotos")]
public class FotosController : BaseController
{
    private readonly IRelatoService _servico;
    private readonly IOptions<PluviaOptions> _opt;

    public FotosController(IRelatoService servico, IOptions<PluviaOptions> opt) { _servico = servico; _opt = opt; }

    /// <summary>
    /// Tela de resultado da IA: devolve a categoria sugerida e a confiança, sem criar relato.
    /// É triagem, não confirmação.
    /// </summary>
    [HttpPost("analise")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(8_000_000)]
    public async Task<ActionResult<FotoAnaliseResponse>> Analisar([FromForm] AnalisarFotoForm form, CancellationToken ct)
    {
        _ = UsuarioAtual;
        var bytes = await RelatosController.LerFoto(form.Foto, _opt.Value.FotoMaxBytes, ct)
            ?? throw new ImagemInvalidaException("Envie uma foto.");
        return Ok(await _servico.AnalisarFotoAsync(bytes, ct));
    }
}
