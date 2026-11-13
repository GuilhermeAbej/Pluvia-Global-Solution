namespace Alaga.Api.Options;

/// <summary>Parâmetros ajustáveis do produto. Valores de exemplo, não medidos em campo.</summary>
public class AlagaOptions
{
    public const string Secao = "Alaga";

    /// <summary>Pessoas diferentes relatando o mesmo ponto para um relato sair de "em análise".</summary>
    public int RelatosParaPublicar { get; set; } = 3;
    public int RaioAgrupamentoMetros { get; set; } = 200;
    public int JanelaAgrupamentoMinutos { get; set; } = 120;
    public int RelatosPorHora { get; set; } = 5;
    public int FotoMaxBytes { get; set; } = 5_000_000;
    public int LeiturasParaConfirmar { get; set; } = 2;
    /// <summary>Acima disso sem nova informação da fonte, o dado é "sem atualização recente".</summary>
    public int JanelaDadosRecentesMinutos { get; set; } = 60;
    /// <summary>Simula a credencial da fonte oficial / moderação. Em produção seria autenticação real.</summary>
    public string ChaveOficial { get; set; } = "chave-oficial-demo";
    public string PastaFotos { get; set; } = "fotos";
}
