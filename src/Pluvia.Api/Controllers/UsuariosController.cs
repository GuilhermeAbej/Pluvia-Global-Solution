using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Controllers;

[Route("api/usuarios")]
public class UsuariosController : BaseController
{
    private readonly IUsuarioService _servico;
    public UsuariosController(IUsuarioService servico) => _servico = servico;

    /// <summary>Cadastra um usuário (exige consentimento LGPD). Use o id no cabeçalho X-Usuario-Id dos demais pedidos.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarUsuarioRequest req, CancellationToken ct)
    {
        var r = await _servico.CriarAsync(req, ct);
        return Created($"/api/usuarios/{r.Id}", r);
    }
}
