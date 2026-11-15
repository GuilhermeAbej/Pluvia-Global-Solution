using Pluvia.Api.Data;
using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Regras;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Pluvia.Tests;

public class ArmazenamentoFalso : IArmazenamentoDeFotos
{
    public int Salvas { get; private set; }
    public int Excluidas { get; private set; }
    public byte[]? UltimoConteudo { get; private set; }

    public Task<string> SalvarAsync(byte[] conteudo, string extensao, CancellationToken ct = default)
    {
        Salvas++;
        UltimoConteudo = conteudo;
        return Task.FromResult($"foto{Salvas}{extensao}");
    }

    public Task<(Stream Conteudo, string ContentType)?> AbrirAsync(string nome, CancellationToken ct = default) =>
        Task.FromResult<(Stream, string)?>((new MemoryStream(new byte[] { 1 }), "image/png"));

    public Task ExcluirAsync(string nome, CancellationToken ct = default)
    {
        Excluidas++;
        return Task.CompletedTask;
    }
}
