using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Domain.Regras;

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
