using Alaga.Domain.Entities;
using Alaga.Domain.Enums;

namespace Alaga.Domain.Regras;

// ---------- Detecção por sensor (Strategy) ----------

public interface IDetectorDeAlagamento
{
    /// <param name="leiturasRecentesCm">Da mais nova para a mais antiga.</param>
    bool Detectar(IReadOnlyList<int> leiturasRecentesCm, int limiteCm);

    /// <summary>Água voltou ao normal de forma sustentada?</summary>
    bool Normalizou(IReadOnlyList<int> leiturasRecentesCm, int limiteCm);
}

/// <summary>Exige N leituras seguidas acima do limite, para um pico isolado não disparar alerta.</summary>
public class DetectorPorLimiteConfirmado : IDetectorDeAlagamento
{
    private readonly int _leiturasNecessarias;

    public DetectorPorLimiteConfirmado(int leiturasNecessarias = 2)
    {
        if (leiturasNecessarias < 1) throw new ArgumentOutOfRangeException(nameof(leiturasNecessarias));
        _leiturasNecessarias = leiturasNecessarias;
    }

    public bool Detectar(IReadOnlyList<int> leituras, int limiteCm) =>
        leituras.Count >= _leiturasNecessarias &&
        leituras.Take(_leiturasNecessarias).All(l => l >= limiteCm);

    public bool Normalizou(IReadOnlyList<int> leituras, int limiteCm) =>
        leituras.Count >= _leiturasNecessarias &&
        leituras.Take(_leiturasNecessarias).All(l => l < limiteCm);
}

// ---------- Triagem e validação do relato ----------

public static class PoliticaDeTriagem
{
    /// <summary>
    /// A IA só ajuda a ordenar a fila de análise. Nunca descarta nem rebaixa um relato,
    /// e nunca o publica: "não vi água na foto" não significa "não alaga ali".
    /// </summary>
    public static PrioridadeRelato Priorizar(AvaliacaoImagem? avaliacao) =>
        avaliacao?.Classificacao == ClassificacaoImagem.IndicioDeAguaSobreAVia
            ? PrioridadeRelato.Alta
            : PrioridadeRelato.Normal;
}

public static class PoliticaDeValidacao
{
    /// <summary>
    /// Critérios para um relato sair de "em análise" (a IA não entra aqui):
    /// uma fonte identificada já tem ocorrência ativa por perto, ou pessoas diferentes relataram o mesmo ponto.
    /// </summary>
    public static bool DevePublicar(int pessoasDistintasNoPonto, bool fonteAtivaPerto, int minimoDePessoas) =>
        fonteAtivaPerto || pessoasDistintasNoPonto >= minimoDePessoas;
}

// ---------- Estimativa de risco (Strategy) ----------

public interface IEstimadorDeRisco
{
    RiscoEstimado Estimar(int ocorrenciasNoPeriodo, int diasDeHistorico);
}

/// <summary>Estimativa simples por histórico. Sem histórico suficiente devolve Indeterminado, nunca Baixo.</summary>
public class EstimadorPorHistorico : IEstimadorDeRisco
{
    public const int DiasMinimosDeHistorico = 30;

    public RiscoEstimado Estimar(int ocorrencias, int diasDeHistorico)
    {
        if (diasDeHistorico < DiasMinimosDeHistorico) return RiscoEstimado.Indeterminado;
        return ocorrencias switch
        {
            0 => RiscoEstimado.Baixo,
            <= 2 => RiscoEstimado.Moderado,
            _ => RiscoEstimado.Alto
        };
    }
}

// ---------- Mensagens ----------

public static class MensagemDeAlerta
{
    /// <summary>Horário de Brasília (UTC-3, sem horário de verão desde 2019).</summary>
    public static string Hora(DateTime utc) => utc.AddHours(-3).ToString("HH:mm");

    private const string Emergencia = "Em risco imediato, ligue 199 (Defesa Civil) ou 193 (Bombeiros).";

    public static string NovoEvento(string local, string fonte, DateTime em) =>
        $"Alagamento ativo registrado por {fonte} às {Hora(em)} perto de '{local}'. Evite entrar em áreas alagadas. {Emergencia}";

    public static string NovoEventoVeiculo(string apelido, string fonte, DateTime em) =>
        $"Alagamento ativo registrado por {fonte} às {Hora(em)} perto do seu veículo '{apelido}'. " +
        $"Não entre em área alagada para retirar o carro. {Emergencia}";

    /// <summary>Encerrar nunca é garantia de trafegabilidade nem de segurança.</summary>
    public static string Encerramento(string local, string fonte, DateTime em) =>
        $"Ocorrência perto de '{local}' encerrada pela fonte {fonte} às {Hora(em)}. " +
        "Isso não garante que a via esteja livre ou segura: a condição física precisa ser reavaliada.";

    public static string MudancaDeRisco(string local, RiscoEstimado risco) =>
        $"O risco estimado perto de '{local}' agora é {risco}. É uma estimativa por histórico, " +
        "pode mudar rapidamente e não confirma água na pista.";
}
