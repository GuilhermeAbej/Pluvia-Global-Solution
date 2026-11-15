using System.Security.Cryptography;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Infra;

/// <summary>Traduz exceções de domínio em respostas HTTP padronizadas (ProblemDetails).</summary>
public class TratadorDeExcecoes : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excecao, CancellationToken ct)
    {
        var (status, titulo) = excecao switch
        {
            NaoAutenticadoException => (StatusCodes.Status401Unauthorized, "Usuário não identificado"),
            AcessoNegadoException => (StatusCodes.Status403Forbidden, "Acesso negado"),
            RecursoNaoEncontradoException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ImagemInvalidaException => (StatusCodes.Status415UnsupportedMediaType, "Imagem inválida"),
            LimiteExcedidoException => (StatusCodes.Status429TooManyRequests, "Limite excedido"),
            RegraDeNegocioException => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada"),
            DominioException => (StatusCodes.Status400BadRequest, "Dados inválidos"),
            _ => (0, string.Empty)
        };

        if (status == 0) return false;

        contexto.Response.StatusCode = status;
        await contexto.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = titulo, Detail = excecao.Message }, ct);
        return true;
    }
}
