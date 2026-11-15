using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface IArmazenamentoDeFotos
{
    Task<string> SalvarAsync(byte[] conteudo, string extensao, CancellationToken ct = default);
    Task<(Stream Conteudo, string ContentType)?> AbrirAsync(string nome, CancellationToken ct = default);
    Task ExcluirAsync(string nome, CancellationToken ct = default);
}
