namespace Pluvia.Domain.Exceptions;

/// <summary>Usuário sem permissão sobre o recurso (HTTP 403).</summary>
public class AcessoNegadoException : DominioException
{
    public AcessoNegadoException(string mensagem) : base(mensagem) { }
}
