namespace Pluvia.Domain.Exceptions;

/// <summary>Recurso inexistente (HTTP 404).</summary>
public class RecursoNaoEncontradoException : DominioException
{
    public RecursoNaoEncontradoException(string recurso, object id)
        : base($"{recurso} '{id}' não encontrado.") { }
}
