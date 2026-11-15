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

public class Alerta
{
    private Alerta() { Mensagem = string.Empty; }

    public Alerta(int usuarioId, int ocorrenciaId, TipoAlerta tipo, string mensagem, DateTime agoraUtc)
    {
        UsuarioId = usuarioId;
        OcorrenciaId = ocorrenciaId;
        Tipo = tipo;
        Mensagem = mensagem;
        CriadoEm = agoraUtc;
    }

    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public int OcorrenciaId { get; private set; }
    public TipoAlerta Tipo { get; private set; }
    public string Mensagem { get; private set; }
    public DateTime CriadoEm { get; private set; }
}

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
