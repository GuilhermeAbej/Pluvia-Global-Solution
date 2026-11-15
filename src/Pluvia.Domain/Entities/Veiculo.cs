using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Geo;

namespace Pluvia.Domain.Entities;

/// <summary>Onde o carro está estacionado. Só o dono vê a posição; avisos podem ser desligados.</summary>
public class Veiculo
{
    public const int RaioPadrao = 300;

    private Veiculo() { Apelido = string.Empty; }

    public Veiculo(int usuarioId, string apelido, int raioMetros = RaioPadrao)
    {
        if (string.IsNullOrWhiteSpace(apelido)) throw new DominioException("Apelido do veículo é obrigatório.");
        if (raioMetros < AreaMonitorada.RaioMinimo || raioMetros > AreaMonitorada.RaioMaximo)
            throw new DominioException($"Raio deve estar entre {AreaMonitorada.RaioMinimo} e {AreaMonitorada.RaioMaximo} metros.");
        UsuarioId = usuarioId;
        Apelido = apelido.Trim();
        RaioMetros = raioMetros;
        AvisosAtivos = true;
    }

    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public string Apelido { get; private set; }
    public int RaioMetros { get; private set; }
    public bool AvisosAtivos { get; private set; }
    public double? Lat { get; private set; }
    public double? Lng { get; private set; }
    public DateTime? AtualizadoEm { get; private set; }

    public void AtualizarPosicao(double lat, double lng, DateTime agoraUtc)
    {
        GeoCalculator.Validar(lat, lng);
        Lat = lat;
        Lng = lng;
        AtualizadoEm = agoraUtc;
    }

    public void DefinirAvisos(bool ativos) => AvisosAtivos = ativos;
}
