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
