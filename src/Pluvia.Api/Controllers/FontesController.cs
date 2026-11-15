using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Controllers;

[Route("api/fontes")]
public class FontesController : BaseController
{
    private readonly IFonteService _servico;
    public FontesController(IFonteService servico) => _servico = servico;

    /// <summary>Fontes que registram ocorrências (oficiais e sensores).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FonteResponse>>> Listar(CancellationToken ct) =>
        Ok(await _servico.ListarAsync(ct));
}
