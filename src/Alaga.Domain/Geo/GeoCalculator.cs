using Alaga.Domain.Exceptions;

namespace Alaga.Domain.Geo;

public readonly record struct CaixaGeografica(double LatMin, double LatMax, double LngMin, double LngMax);

public static class GeoCalculator
{
    private const double RaioTerraMetros = 6_371_000;

    public static void Validar(double lat, double lng)
    {
        if (double.IsNaN(lat) || lat < -90 || lat > 90)
            throw new DominioException("Latitude deve estar entre -90 e 90.");
        if (double.IsNaN(lng) || lng < -180 || lng > 180)
            throw new DominioException("Longitude deve estar entre -180 e 180.");
    }

    /// <summary>Distância em metros pela fórmula de Haversine.</summary>
    public static double DistanciaMetros(double lat1, double lng1, double lat2, double lng2)
    {
        static double Rad(double g) => g * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLng = Rad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * RaioTerraMetros * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>
    /// Caixa que contém o círculo. Serve para o banco filtrar barato (índice em lat/lng);
    /// o círculo exato é refinado em memória com Haversine.
    /// </summary>
    public static CaixaGeografica Caixa(double lat, double lng, double raioMetros)
    {
        var dLat = raioMetros / 111_320.0;
        var cos = Math.Max(0.01, Math.Cos(lat * Math.PI / 180));
        var dLng = raioMetros / (111_320.0 * cos);
        return new CaixaGeografica(lat - dLat, lat + dLat, lng - dLng, lng + dLng);
    }
}
