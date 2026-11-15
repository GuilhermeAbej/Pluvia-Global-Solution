using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Controllers;

[Route("api/veiculos")]
public class VeiculosController : BaseController
{
    private readonly IVeiculoService _servico;
    public VeiculosController(IVeiculoService servico) => _servico = servico;

    /// <summary>Cadastra o veículo e o raio de monitoramento ao redor de onde ele está estacionado.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VeiculoResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarVeiculoRequest req, CancellationToken ct)
    {
        var r = await _servico.CriarAsync(UsuarioAtual, req, ct);
        return Created($"/api/veiculos/{r.Id}", r);
    }

    /// <summary>Salva onde o carro está estacionado.</summary>
    [HttpPut("{id:int}/posicao")]
    public async Task<ActionResult<PosicaoResponse>> AtualizarPosicao(int id, PosicaoRequest req, CancellationToken ct) =>
        Ok(await _servico.AtualizarPosicaoAsync(UsuarioAtual, id, req, ct));

    /// <summary>Posição do veículo. Visível somente ao dono.</summary>
    [HttpGet("{id:int}/posicao")]
    public async Task<ActionResult<PosicaoResponse>> ObterPosicao(int id, CancellationToken ct) =>
        Ok(await _servico.ObterPosicaoAsync(UsuarioAtual, id, ct));

    /// <summary>Liga ou desliga os avisos do veículo.</summary>
    [HttpPut("{id:int}/avisos")]
    public async Task<ActionResult<VeiculoResponse>> Avisos(int id, AvisosRequest req, CancellationToken ct) =>
        Ok(await _servico.DefinirAvisosAsync(UsuarioAtual, id, req.Ativos, ct));
}
