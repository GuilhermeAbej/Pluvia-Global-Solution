using Alaga.Api.Options;
using Alaga.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Alaga.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    public const string CabecalhoUsuario = "X-Usuario-Id";
    public const string CabecalhoOficial = "X-Chave-Oficial";

    /// <summary>Identificação simulada por cabeçalho. Em produção seria autenticação real (JWT).</summary>
    protected int UsuarioAtual =>
        Request.Headers.TryGetValue(CabecalhoUsuario, out var v) && int.TryParse(v, out var id) && id > 0
            ? id
            : throw new NaoAutenticadoException($"Envie o cabeçalho {CabecalhoUsuario} com o id do usuário.");

    protected int? UsuarioOpcional =>
        Request.Headers.TryGetValue(CabecalhoUsuario, out var v) && int.TryParse(v, out var id) && id > 0 ? id : null;

    protected bool EhOficial(IOptions<AlagaOptions> opt) =>
        Request.Headers.TryGetValue(CabecalhoOficial, out var v) && v == opt.Value.ChaveOficial;

    protected void ExigirOficial(IOptions<AlagaOptions> opt)
    {
        if (!EhOficial(opt)) throw new AcessoNegadoException($"Ação restrita à fonte oficial (cabeçalho {CabecalhoOficial}).");
    }
}
