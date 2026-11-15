using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Geo;

namespace Pluvia.Domain.Entities;

/// <summary>
/// Alagamento registrado por uma fonte identificada (oficial ou sensor).
/// Só a mesma fonte que registrou pode marcar o encerramento.
/// </summary>
public class Ocorrencia
{
    private Ocorrencia() { }

    public Ocorrencia(int fonteId, double lat, double lng, string? descricao, DateTime agoraUtc)
    {
        GeoCalculator.Validar(lat, lng);
        if (descricao is { Length: > 280 }) throw new DominioException("Descrição deve ter até 280 caracteres.");
        FonteId = fonteId;
        Lat = lat;
        Lng = lng;
        Descricao = descricao?.Trim();
        Status = StatusOcorrencia.Ativa;
        AbertaEm = agoraUtc;
        AtualizadaEm = agoraUtc;
    }

    public int Id { get; private set; }
    public int FonteId { get; private set; }
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public string? Descricao { get; private set; }
    public StatusOcorrencia Status { get; private set; }
    public DateTime AbertaEm { get; private set; }
    /// <summary>Última vez que a fonte confirmou que o evento continua.</summary>
    public DateTime AtualizadaEm { get; private set; }
    public DateTime? EncerradaEm { get; private set; }

    public bool Ativa => Status == StatusOcorrencia.Ativa;

    public bool SemAtualizacaoRecente(DateTime agoraUtc, TimeSpan limite) => Ativa && agoraUtc - AtualizadaEm > limite;

    public void RegistrarAtualizacao(DateTime agoraUtc)
    {
        if (!Ativa) throw new RegraDeNegocioException("Ocorrência já encerrada.");
        AtualizadaEm = agoraUtc;
    }

    public void Encerrar(int fonteId, DateTime agoraUtc)
    {
        if (!Ativa) throw new RegraDeNegocioException("Ocorrência já encerrada.");
        if (fonteId != FonteId)
            throw new RegraDeNegocioException("Só a mesma fonte que registrou a ocorrência pode marcá-la como encerrada.");
        Status = StatusOcorrencia.Encerrada;
        EncerradaEm = agoraUtc;
        AtualizadaEm = agoraUtc;
    }
}

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
