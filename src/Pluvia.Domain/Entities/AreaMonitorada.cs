using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Geo;

namespace Pluvia.Domain.Entities;

public class AreaMonitorada
{
    public const int RaioMinimo = 100;
    public const int RaioMaximo = 2000;

    private AreaMonitorada() { Nome = string.Empty; }

    public AreaMonitorada(int usuarioId, string nome, double lat, double lng, int raioMetros)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DominioException("Nome da área é obrigatório.");
        if (raioMetros < RaioMinimo || raioMetros > RaioMaximo)
            throw new DominioException($"Raio deve estar entre {RaioMinimo} e {RaioMaximo} metros.");
        GeoCalculator.Validar(lat, lng);
        UsuarioId = usuarioId;
        Nome = nome.Trim();
        Lat = lat;
        Lng = lng;
        RaioMetros = raioMetros;
        RiscoAtual = RiscoEstimado.Indeterminado;
    }

    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public string Nome { get; private set; }
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public int RaioMetros { get; private set; }
    public DateTime? SilenciadaAte { get; private set; }
    /// <summary>Último risco estimado já comunicado, para só avisar quando mudar.</summary>
    public RiscoEstimado RiscoAtual { get; private set; }

    public bool EstaSilenciada(DateTime agoraUtc) => SilenciadaAte.HasValue && SilenciadaAte > agoraUtc;

    public void Silenciar(TimeSpan duracao, DateTime agoraUtc)
    {
        if (duracao <= TimeSpan.Zero || duracao > TimeSpan.FromHours(24))
            throw new RegraDeNegocioException("A duração do silêncio deve ficar entre 1 minuto e 24 horas.");
        SilenciadaAte = agoraUtc + duracao;
    }

    public void ReativarAvisos() => SilenciadaAte = null;

    public void AtualizarRisco(RiscoEstimado risco) => RiscoAtual = risco;
}
