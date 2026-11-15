using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Geo;

namespace Pluvia.Domain.Entities;

public class LeituraSensor
{
    public const int MaximoCm = 1000;

    private LeituraSensor() { }

    public LeituraSensor(int sensorId, int nivelCm, DateTime agoraUtc)
    {
        if (nivelCm < 0 || nivelCm > MaximoCm)
            throw new RegraDeNegocioException($"Nível fora do intervalo válido (0 a {MaximoCm} cm).");
        SensorId = sensorId;
        NivelCm = nivelCm;
        RegistradaEm = agoraUtc;
    }

    public int Id { get; private set; }
    public int SensorId { get; private set; }
    public int NivelCm { get; private set; }
    public DateTime RegistradaEm { get; private set; }
}
