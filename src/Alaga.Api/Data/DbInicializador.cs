using Alaga.Domain.Entities;
using Alaga.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Alaga.Api.Data;

public static class DbInicializador
{
    public const string FonteOficialSimulada = "CGE-SP (simulada)";

    /// <summary>Dados de exemplo, todos fictícios. Troque pelo ponto real escolhido pelo grupo.</summary>
    public static async Task PopularAsync(AlagaDbContext db)
    {
        if (await db.Fontes.AnyAsync()) return;

        var oficial = new Fonte(FonteOficialSimulada, TipoFonte.Oficial);
        var fonteSensor = new Fonte("Sensor ESP32-001", TipoFonte.Sensor);
        db.Fontes.AddRange(oficial, fonteSensor);
        await db.SaveChangesAsync();

        db.Sensores.Add(new Sensor(fonteSensor.Id, "ESP32-001", lat: -23.5650, lng: -46.6500, limiteCm: 80));
        await db.SaveChangesAsync();
    }
}
