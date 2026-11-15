using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Controllers;

[Route("api/riscos")]
public class RiscosController : BaseController
{
    private readonly IRiscoService _risco;
    public RiscosController(IRiscoService risco) => _risco = risco;

    /// <summary>
    /// Exposição histórica/estimada de um ponto pesquisado (apoia "Onde estacionar").
    /// Não recomenda vagas e não confirma água na pista.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<RiscoResponse>> Consultar(
        [FromQuery] double lat, [FromQuery] double lng, [FromQuery] int raioMetros = 500, CancellationToken ct = default)
    {
        if (raioMetros is < 100 or > 2000) throw new DominioException("Raio deve estar entre 100 e 2000 metros.");
        return Ok(LocalService.ParaResposta(await _risco.CalcularAsync(lat, lng, raioMetros, ct)));
    }
}
