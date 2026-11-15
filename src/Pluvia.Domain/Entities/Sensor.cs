using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Geo;

namespace Pluvia.Domain.Entities;

public class Sensor
{
    private Sensor() { Codigo = string.Empty; }

    public Sensor(int fonteId, string codigo, double lat, double lng, int limiteCm)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new DominioException("Código do sensor é obrigatório.");
        if (limiteCm <= 0) throw new DominioException("Limite deve ser positivo.");
        GeoCalculator.Validar(lat, lng);
        FonteId = fonteId;
        Codigo = codigo.Trim();
        Lat = lat;
        Lng = lng;
        LimiteCm = limiteCm;
        Status = StatusSensor.Ativo;
    }

    public int Id { get; private set; }
    public int FonteId { get; private set; }
    public string Codigo { get; private set; }
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public int LimiteCm { get; private set; }
    public StatusSensor Status { get; private set; }

    public bool AceitaLeituras => Status == StatusSensor.Ativo;

    public void AlterarStatus(StatusSensor status) => Status = status;
}
