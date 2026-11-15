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

public class ArmazenamentoLocalDeFotos : IArmazenamentoDeFotos
{
    private readonly string _pasta;

    public ArmazenamentoLocalDeFotos(IHostEnvironment env, IOptions<PluviaOptions> opt)
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
