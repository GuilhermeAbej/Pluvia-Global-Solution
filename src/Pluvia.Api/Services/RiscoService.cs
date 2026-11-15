using Pluvia.Api.Data;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Geo;
using Pluvia.Domain.Regras;
using Microsoft.EntityFrameworkCore;

namespace Pluvia.Api.Services;

public class RiscoService : IRiscoService
{
    private readonly PluviaDbContext _db;
    private readonly IEstimadorDeRisco _estimador;
    private readonly TimeProvider _relogio;

    public RiscoService(PluviaDbContext db, IEstimadorDeRisco estimador, TimeProvider relogio)
    {
        _db = db; _estimador = estimador; _relogio = relogio;
    }

    public async Task<RiscoCalculado> CalcularAsync(double lat, double lng, double raioMetros, CancellationToken ct = default)
    {
        GeoCalculator.Validar(lat, lng);
        var agora = _relogio.GetUtcNow().UtcDateTime;
        var naArea = await _db.OcorrenciasPerto(lat, lng, raioMetros, ct: ct);

        var recentes = naArea.Count(o => o.AbertaEm >= agora.AddDays(-90));
        var ativas = naArea.Count(o => o.Ativa);

        // Dias de histórico = desde a primeira ocorrência registrada no sistema (simplificação documentada).
        var primeira = await _db.Ocorrencias.AsNoTracking()
            .OrderBy(o => o.AbertaEm).Select(o => (DateTime?)o.AbertaEm).FirstOrDefaultAsync(ct);
        var dias = primeira is null ? 0 : (int)(agora - primeira.Value).TotalDays;

        return new RiscoCalculado(_estimador.Estimar(recentes, dias), recentes, ativas);
    }
}
