using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Geo;

namespace Pluvia.Domain.Entities;

/// <summary>Resultado da triagem de imagem (objeto de valor). É só uma sugestão de prioridade.</summary>
public record AvaliacaoImagem(ClassificacaoImagem Classificacao, double Confianca, string Modelo);

/// <summary>Relato da comunidade. Nasce "em análise" e nunca é um registro oficial.</summary>
public class Relato
{
    public const int IdadeMaximaHoras = 12;

    private Relato() { Descricao = string.Empty; }

    public Relato(int usuarioId, double lat, double lng, string? descricao, TipoEvento tipo,
        DateTime ocorridoEmUtc, string? fotoNome, bool fotoAtualConfirmada, DateTime agoraUtc)
    {
        GeoCalculator.Validar(lat, lng);
        if (descricao is { Length: > 280 }) throw new DominioException("Descrição deve ter até 280 caracteres.");
        if (ocorridoEmUtc > agoraUtc.AddMinutes(5)) throw new RegraDeNegocioException("O horário do evento está no futuro.");
        if (ocorridoEmUtc < agoraUtc.AddHours(-IdadeMaximaHoras))
            throw new RegraDeNegocioException($"O evento tem mais de {IdadeMaximaHoras} horas. Relatos antigos não entram, para não confundir com eventos atuais.");
        if (fotoNome is not null && !fotoAtualConfirmada)
            throw new RegraDeNegocioException("Confirme que a foto é atual e do ponto indicado.");

        UsuarioId = usuarioId;
        Lat = lat;
        Lng = lng;
        Descricao = descricao?.Trim() ?? string.Empty;
        Tipo = tipo;
        OcorridoEm = ocorridoEmUtc;
        FotoNome = fotoNome;
        Status = StatusRelato.EmAnalise;
        Prioridade = PrioridadeRelato.Normal;
        CriadoEm = agoraUtc;
    }

    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public string Descricao { get; private set; }
    public TipoEvento Tipo { get; private set; }
    public DateTime OcorridoEm { get; private set; }
    public string? FotoNome { get; private set; }
    public AvaliacaoImagem? Avaliacao { get; private set; }
    public PrioridadeRelato Prioridade { get; private set; }
    public StatusRelato Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? PublicadoEm { get; private set; }

    public void DefinirTriagem(AvaliacaoImagem avaliacao, PrioridadeRelato prioridade)
    {
        Avaliacao = avaliacao;
        Prioridade = prioridade;
    }

    public void Publicar(DateTime agoraUtc)
    {
        if (Status != StatusRelato.EmAnalise) throw new RegraDeNegocioException("Só relatos em análise podem ser publicados.");
        Status = StatusRelato.Publicado;
        PublicadoEm = agoraUtc;
    }

    public void Rejeitar()
    {
        if (Status != StatusRelato.EmAnalise) throw new RegraDeNegocioException("Só relatos em análise podem ser rejeitados.");
        Status = StatusRelato.Rejeitado;
    }

    /// <summary>Exclusão pedida pelo autor: apaga texto e referência da foto, mantém só o registro mínimo.</summary>
    public void Excluir()
    {
        if (Status == StatusRelato.Excluido) throw new RegraDeNegocioException("Relato já excluído.");
        Status = StatusRelato.Excluido;
        Descricao = string.Empty;
        FotoNome = null;
    }
}
