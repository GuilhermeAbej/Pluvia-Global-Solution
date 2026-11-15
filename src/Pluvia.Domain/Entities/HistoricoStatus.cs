using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Geo;

namespace Pluvia.Domain.Entities;

public class HistoricoStatus
{
    private HistoricoStatus() { }

    public HistoricoStatus(int ocorrenciaId, StatusOcorrencia status, int fonteId, DateTime agoraUtc)
    {
        OcorrenciaId = ocorrenciaId;
        Status = status;
        FonteId = fonteId;
        Em = agoraUtc;
    }

    public int Id { get; private set; }
    public int OcorrenciaId { get; private set; }
    public StatusOcorrencia Status { get; private set; }
    public int FonteId { get; private set; }
    public DateTime Em { get; private set; }
}
