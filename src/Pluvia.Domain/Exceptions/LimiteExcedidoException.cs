namespace Pluvia.Domain.Exceptions;

/// <summary>Limite de envios excedido (HTTP 429).</summary>
public class LimiteExcedidoException : DominioException
{
    public LimiteExcedidoException(string mensagem) : base(mensagem) { }
}
