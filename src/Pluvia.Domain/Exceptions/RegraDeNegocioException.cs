namespace Pluvia.Domain.Exceptions;

/// <summary>Regra de negócio violada (HTTP 422).</summary>
public class RegraDeNegocioException : DominioException
{
    public RegraDeNegocioException(string mensagem) : base(mensagem) { }
}
