namespace Pluvia.Domain.Exceptions;

/// <summary>Arquivo de imagem inválido (HTTP 415).</summary>
public class ImagemInvalidaException : DominioException
{
    public ImagemInvalidaException(string mensagem) : base(mensagem) { }
}
