using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Controllers;

[Route("api/sensores")]
public class SensoresController : BaseController
{
    private readonly ISensorService _servico;
    public SensoresController(ISensorService servico) => _servico = servico;

    /// <summary>Recebe a medição de nível de água do sensor (ESP32). Pode abrir, atualizar ou encerrar uma ocorrência.</summary>
    [HttpPost("leituras")]
    [ProducesResponseType(typeof(LeituraResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegistrarLeitura(LeituraRequest req, CancellationToken ct)
    {
        var r = await _servico.RegistrarLeituraAsync(req, ct);
        return Created(r.OcorrenciaId is null ? "/api/ocorrencias" : $"/api/ocorrencias/{r.OcorrenciaId}", r);
    }
}
