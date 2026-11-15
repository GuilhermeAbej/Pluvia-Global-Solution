using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Controllers;

[Route("api/relatos")]
public class RelatosController : BaseController
{
    private readonly IRelatoService _servico;
    private readonly IOptions<PluviaOptions> _opt;

    public RelatosController(IRelatoService servico, IOptions<PluviaOptions> opt) { _servico = servico; _opt = opt; }

    /// <summary>
    /// Envia um relato (multipart/form-data) com foto opcional. O relato nasce "em análise" e só é publicado
    /// pelos critérios de validação. A foto tem metadados removidos e passa por triagem de IA que só define prioridade.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(8_000_000)]
    [ProducesResponseType(typeof(RelatoResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Enviar([FromForm] EnviarRelatoForm form, CancellationToken ct)
    {
        var usuarioId = UsuarioAtual;
        var bytes = await LerFoto(form.Foto, _opt.Value.FotoMaxBytes, ct);
        var r = await _servico.EnviarAsync(usuarioId,
            new NovoRelato(form.Lat, form.Lng, form.Descricao, form.Tipo, form.OcorridoEm, form.FotoAtualConfirmada, bytes), ct);
        return Created($"/api/relatos/{r.RelatoId}", r);
    }

    /// <summary>Relatos do próprio usuário, com o status de cada um.</summary>
    [HttpGet("meus")]
    public async Task<ActionResult<IReadOnlyList<RelatoDetalheResponse>>> Meus(CancellationToken ct) =>
        Ok(await _servico.ListarDoAutorAsync(UsuarioAtual, ct));

    /// <summary>Acompanha o status de um relato. Só o autor.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RelatoDetalheResponse>> Buscar(int id, CancellationToken ct) =>
        Ok(await _servico.ObterDoAutorAsync(UsuarioAtual, id, ct));

    /// <summary>Exclui o relato e apaga a foto. Só o autor.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await _servico.ExcluirAsync(UsuarioAtual, id, ct);
        return NoContent();
    }

    /// <summary>Relatos publicados perto de um ponto, separados das ocorrências oficiais e sem dados do autor.</summary>
    [HttpGet("comunitarios")]
    public async Task<ActionResult<IReadOnlyList<RelatoPublicoResponse>>> Comunitarios(
        [FromQuery] double lat, [FromQuery] double lng, [FromQuery] int raioMetros = 1000, CancellationToken ct = default) =>
        Ok(await _servico.ListarPublicosAsync(lat, lng, raioMetros, ct));

    /// <summary>Foto de um relato. Só o autor (X-Usuario-Id) ou a fonte oficial (X-Chave-Oficial).</summary>
    [HttpGet("{id:int}/foto")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Foto(int id, CancellationToken ct)
    {
        var oficial = EhOficial(_opt);
        var usuario = UsuarioOpcional;
        if (!oficial && usuario is null)
            throw new NaoAutenticadoException($"Envie {CabecalhoUsuario} ou {CabecalhoOficial}.");

        var (conteudo, tipo) = await _servico.ObterFotoAsync(id, usuario, oficial, ct);
        return File(conteudo, tipo);
    }

    /// <summary>Moderação: publica um relato em análise. Exige X-Chave-Oficial.</summary>
    [HttpPost("{id:int}/publicacao")]
    public async Task<ActionResult<RelatoDetalheResponse>> Publicar(int id, CancellationToken ct)
    {
        ExigirOficial(_opt);
        return Ok(await _servico.PublicarAsync(id, ct));
    }

    /// <summary>Moderação: rejeita um relato em análise. Exige X-Chave-Oficial.</summary>
    [HttpPost("{id:int}/rejeicao")]
    public async Task<ActionResult<RelatoDetalheResponse>> Rejeitar(int id, CancellationToken ct)
    {
        ExigirOficial(_opt);
        return Ok(await _servico.RejeitarAsync(id, ct));
    }

    internal static async Task<byte[]?> LerFoto(IFormFile? foto, int maximo, CancellationToken ct)
    {
        if (foto is not { Length: > 0 }) return null;
        if (foto.Length > maximo)
            throw new ImagemInvalidaException($"A foto passa de {maximo / 1_000_000} MB.");
        using var ms = new MemoryStream();
        await foto.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
