using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface IAnalisadorDeImagem
{
    /// <summary>Triagem da foto. Um modelo de visão real implementa esta interface sem mudar o resto.</summary>
    Task<AvaliacaoImagem> AnalisarAsync(byte[] imagem, CancellationToken ct = default);
}
