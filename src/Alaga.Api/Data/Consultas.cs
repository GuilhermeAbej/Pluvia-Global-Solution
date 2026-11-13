using Alaga.Domain.Entities;
using Alaga.Domain.Enums;
using Alaga.Domain.Geo;
using Microsoft.EntityFrameworkCore;

namespace Alaga.Api.Data;

/// <summary>Consultas geográficas: caixa no banco (usa índice) e círculo exato em memória (Haversine).</summary>
public static class Consultas
{
    public static async Task<List<Ocorrencia>> OcorrenciasPerto(
        this AlagaDbContext db, double lat, double lng, double raioMetros,
        StatusOcorrencia? status = null, int? fonteId = null,
        DateTime? desde = null, DateTime? ate = null, CancellationToken ct = default)
    {
        var c = GeoCalculator.Caixa(lat, lng, raioMetros);

        var q = db.Ocorrencias
            .Where(o => o.Lat >= c.LatMin && o.Lat <= c.LatMax && o.Lng >= c.LngMin && o.Lng <= c.LngMax);
        if (status is not null) q = q.Where(o => o.Status == status);
        if (fonteId is not null) q = q.Where(o => o.FonteId == fonteId);
        if (desde is not null) q = q.Where(o => o.AbertaEm >= desde);
        if (ate is not null) q = q.Where(o => o.AbertaEm <= ate);

        var candidatas = await q.ToListAsync(ct);
        return candidatas
            .Where(o => GeoCalculator.DistanciaMetros(lat, lng, o.Lat, o.Lng) <= raioMetros)
            .OrderBy(o => GeoCalculator.DistanciaMetros(lat, lng, o.Lat, o.Lng))
            .ToList();
    }

    public static async Task<List<Relato>> RelatosPerto(
        this AlagaDbContext db, double lat, double lng, double raioMetros,
        StatusRelato[] status, DateTime? criadosDesde = null, CancellationToken ct = default)
    {
        var c = GeoCalculator.Caixa(lat, lng, raioMetros);

        var q = db.Relatos
            .Where(r => status.Contains(r.Status) &&
                        r.Lat >= c.LatMin && r.Lat <= c.LatMax && r.Lng >= c.LngMin && r.Lng <= c.LngMax);
        if (criadosDesde is not null) q = q.Where(r => r.CriadoEm >= criadosDesde);

        var candidatos = await q.ToListAsync(ct);
        return candidatos
            .Where(r => GeoCalculator.DistanciaMetros(lat, lng, r.Lat, r.Lng) <= raioMetros)
            .ToList();
    }

    public static async Task<List<AreaMonitorada>> AreasQueCobrem(
        this AlagaDbContext db, double lat, double lng, CancellationToken ct = default)
    {
        // Raio máximo de uma área é 2000 m: a caixa usa esse teto e o Haversine refina por área.
        var c = GeoCalculator.Caixa(lat, lng, AreaMonitorada.RaioMaximo);

        var candidatas = await db.Areas
            .Where(a => a.Lat >= c.LatMin && a.Lat <= c.LatMax && a.Lng >= c.LngMin && a.Lng <= c.LngMax)
            .ToListAsync(ct);

        return candidatas
            .Where(a => GeoCalculator.DistanciaMetros(lat, lng, a.Lat, a.Lng) <= a.RaioMetros)
            .ToList();
    }

    public static async Task<List<Veiculo>> VeiculosQueCobrem(
        this AlagaDbContext db, double lat, double lng, CancellationToken ct = default)
    {
        var c = GeoCalculator.Caixa(lat, lng, AreaMonitorada.RaioMaximo);

        var candidatos = await db.Veiculos
            .Where(v => v.Lat != null && v.Lng != null &&
                        v.Lat >= c.LatMin && v.Lat <= c.LatMax && v.Lng >= c.LngMin && v.Lng <= c.LngMax)
            .ToListAsync(ct);

        return candidatos
            .Where(v => GeoCalculator.DistanciaMetros(lat, lng, v.Lat!.Value, v.Lng!.Value) <= v.RaioMetros)
            .ToList();
    }
}
