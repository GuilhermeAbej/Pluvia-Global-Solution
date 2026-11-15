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

public class AnalisadorFalso : IAnalisadorDeImagem
{
    public ClassificacaoImagem Resultado { get; set; } = ClassificacaoImagem.IndicioDeAguaSobreAVia;
    public Task<AvaliacaoImagem> AnalisarAsync(byte[] imagem, CancellationToken ct = default) =>
        Task.FromResult(new AvaliacaoImagem(Resultado, 0.9, "falso"));
}
