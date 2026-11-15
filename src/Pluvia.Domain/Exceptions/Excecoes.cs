namespace Pluvia.Domain.Exceptions;

public class DominioException : Exception
{
    public DominioException(string mensagem) : base(mensagem) { }
}

/// <summary>Regra de negócio violada (HTTP 422).</summary>
public class RegraDeNegocioException : DominioException
{
    public RegraDeNegocioException(string mensagem) : base(mensagem) { }
}

/// <summary>Recurso inexistente (HTTP 404).</summary>
public class RecursoNaoEncontradoException : DominioException
{
    public RecursoNaoEncontradoException(string recurso, object id)
        : base($"{recurso} '{id}' não encontrado.") { }
}

/// <summary>Usuário sem permissão sobre o recurso (HTTP 403).</summary>
public class AcessoNegadoException : DominioException
{
    public AcessoNegadoException(string mensagem) : base(mensagem) { }
}

/// <summary>Limite de envios excedido (HTTP 429).</summary>
public class LimiteExcedidoException : DominioException
{
    public LimiteExcedidoException(string mensagem) : base(mensagem) { }
}

/// <summary>Arquivo de imagem inválido (HTTP 415).</summary>
public class ImagemInvalidaException : DominioException
{
    public ImagemInvalidaException(string mensagem) : base(mensagem) { }
}

/// <summary>Requisição sem identificação do usuário (HTTP 401).</summary>
public class NaoAutenticadoException : DominioException
{
    public NaoAutenticadoException(string mensagem) : base(mensagem) { }
}
