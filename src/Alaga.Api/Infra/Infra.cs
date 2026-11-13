using System.Security.Cryptography;
using Alaga.Api.Options;
using Alaga.Api.Services;
using Alaga.Domain.Entities;
using Alaga.Domain.Enums;
using Alaga.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Alaga.Api.Infra;

/// <summary>Notificação simulada: só registra no log. Troque por push/SMS sem mexer nas regras.</summary>
public class NotificadorEmLog : INotificador
{
    private readonly ILogger<NotificadorEmLog> _log;
    public NotificadorEmLog(ILogger<NotificadorEmLog> log) => _log = log;

    public Task NotificarAsync(Alerta alerta, CancellationToken ct = default)
    {
        _log.LogInformation("[NOTIFICAÇÃO SIMULADA] usuário {Usuario} | {Tipo} | {Mensagem}",
            alerta.UsuarioId, alerta.Tipo, alerta.Mensagem);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Triagem simulada: resultado determinístico a partir do hash da imagem, sem olhar o conteúdo visual.
/// Existe para demonstrar o fluxo; um modelo de visão real implementa a mesma interface.
/// </summary>
public class AnalisadorDeImagemSimulado : IAnalisadorDeImagem
{
    public Task<AvaliacaoImagem> AnalisarAsync(byte[] imagem, CancellationToken ct = default)
    {
        var hash = SHA256.HashData(imagem);
        var classe = (ClassificacaoImagem)(hash[0] % 3);
        var confianca = Math.Round(0.5 + hash[1] / 255.0 * 0.4, 2);
        return Task.FromResult(new AvaliacaoImagem(classe, confianca, "simulado-v1"));
    }
}

public class ArmazenamentoLocalDeFotos : IArmazenamentoDeFotos
{
    private readonly string _pasta;

    public ArmazenamentoLocalDeFotos(IHostEnvironment env, IOptions<AlagaOptions> opt)
    {
        _pasta = Path.Combine(env.ContentRootPath, opt.Value.PastaFotos);
        Directory.CreateDirectory(_pasta);
    }

    public async Task<string> SalvarAsync(byte[] conteudo, string extensao, CancellationToken ct = default)
    {
        // Nome aleatório: não revela autor, local nem nome original do arquivo.
        var nome = $"{Guid.NewGuid():N}{extensao}";
        await File.WriteAllBytesAsync(Caminho(nome), conteudo, ct);
        return nome;
    }

    public Task<(Stream Conteudo, string ContentType)?> AbrirAsync(string nome, CancellationToken ct = default)
    {
        var caminho = Caminho(nome);
        if (!File.Exists(caminho)) return Task.FromResult<(Stream, string)?>(null);
        var tipo = Path.GetExtension(caminho) == ".png" ? "image/png" : "image/jpeg";
        return Task.FromResult<(Stream, string)?>((File.OpenRead(caminho), tipo));
    }

    public Task ExcluirAsync(string nome, CancellationToken ct = default)
    {
        var caminho = Caminho(nome);
        if (File.Exists(caminho)) File.Delete(caminho);
        return Task.CompletedTask;
    }

    private string Caminho(string nome) => Path.Combine(_pasta, Path.GetFileName(nome)); // GetFileName bloqueia path traversal
}

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
