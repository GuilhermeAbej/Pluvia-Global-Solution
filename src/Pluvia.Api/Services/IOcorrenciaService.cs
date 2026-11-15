using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public interface IOcorrenciaService
{
    Task<ListaDeOcorrenciasResponse> ListarAsync(FiltroOcorrencias filtro, CancellationToken ct = default);
    Task<OcorrenciaResponse> BuscarAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<HistoricoResponse>> HistoricoAsync(int id, CancellationToken ct = default);
    Task<OcorrenciaResponse> RegistrarOficialAsync(RegistrarOcorrenciaRequest req, CancellationToken ct = default);
    Task<OcorrenciaResponse> EncerrarOficialAsync(int id, int fonteId, CancellationToken ct = default);
}
