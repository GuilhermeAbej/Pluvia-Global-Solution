using Alaga.Api.Data;
using Alaga.Domain.Enums;
using Alaga.Domain.Geo;
using Alaga.Domain.Regras;
using Microsoft.EntityFrameworkCore;

namespace Alaga.Api.Services;

public class RiscoService : IRiscoService
{
    private readonly AlagaDbContext _db;
    private readonly IEstimadorDeRisco _estimador;
    private readonly TimeProvider _relogio;

    public RiscoService(AlagaDbContext db, IEstimadorDeRisco estimador, TimeProvider relogio)
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
