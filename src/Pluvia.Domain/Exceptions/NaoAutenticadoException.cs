namespace Pluvia.Domain.Exceptions;

/// <summary>Requisição sem identificação do usuário (HTTP 401).</summary>
public class NaoAutenticadoException : DominioException
{
    public NaoAutenticadoException(string mensagem) : base(mensagem) { }
}
