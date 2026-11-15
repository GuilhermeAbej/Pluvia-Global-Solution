using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

/// <summary>Ciclo de vida da ocorrência: abrir, atualizar, encerrar, registrar histórico e avisar.</summary>
public interface IGestorDeOcorrencias
{
    Task<(Ocorrencia Ocorrencia, bool Criada)> AbrirAsync(int fonteId, double lat, double lng, string? descricao, CancellationToken ct = default);
    Task EncerrarAsync(Ocorrencia ocorrencia, int fonteId, CancellationToken ct = default);
}
