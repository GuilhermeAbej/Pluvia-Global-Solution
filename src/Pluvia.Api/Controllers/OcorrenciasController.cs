using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Controllers;

[Route("api/ocorrencias")]
public class OcorrenciasController : BaseController
{
    private readonly IOcorrenciaService _servico;
    private readonly IOptions<PluviaOptions> _opt;

    public OcorrenciasController(IOcorrenciaService servico, IOptions<PluviaOptions> opt) { _servico = servico; _opt = opt; }

    /// <summary>
    /// Ocorrências registradas por fontes identificadas, perto de um ponto. Por padrão só as ativas.
    /// Filtros: status, fonteId, desde, ate (data de abertura), ou todos=true para incluir encerradas.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ListaDeOcorrenciasResponse>> Listar(
        [FromQuery] double lat, [FromQuery] double lng, [FromQuery] int raioMetros = 1000,
        [FromQuery] StatusOcorrencia? status = null, [FromQuery] int? fonteId = null,
        [FromQuery] DateTime? desde = null, [FromQuery] DateTime? ate = null, [FromQuery] bool todos = false,
        CancellationToken ct = default)
    {
        var filtro = new FiltroOcorrencias(lat, lng, raioMetros,
            todos ? null : status ?? StatusOcorrencia.Ativa, fonteId, desde?.ToUniversalTime(), ate?.ToUniversalTime());
        return Ok(await _servico.ListarAsync(filtro, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OcorrenciaResponse>> Buscar(int id, CancellationToken ct) =>
        Ok(await _servico.BuscarAsync(id, ct));

    /// <summary>Histórico de mudanças de status da ocorrência, com a fonte de cada mudança.</summary>
    [HttpGet("{id:int}/historico")]
    public async Task<ActionResult<IReadOnlyList<HistoricoResponse>>> Historico(int id, CancellationToken ct) =>
        Ok(await _servico.HistoricoAsync(id, ct));

    /// <summary>Fonte oficial registra uma ocorrência ativa. Exige X-Chave-Oficial.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OcorrenciaResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Registrar(RegistrarOcorrenciaRequest req, CancellationToken ct)
    {
        ExigirOficial(_opt);
        var r = await _servico.RegistrarOficialAsync(req, ct);
        return Created($"/api/ocorrencias/{r.Id}", r);
    }

    /// <summary>A mesma fonte oficial que registrou marca o encerramento. Exige X-Chave-Oficial.</summary>
    [HttpPost("{id:int}/encerramento")]
    public async Task<ActionResult<OcorrenciaResponse>> Encerrar(int id, EncerrarOcorrenciaRequest req, CancellationToken ct)
    {
        ExigirOficial(_opt);
        return Ok(await _servico.EncerrarOficialAsync(id, req.FonteId, ct));
    }
}
