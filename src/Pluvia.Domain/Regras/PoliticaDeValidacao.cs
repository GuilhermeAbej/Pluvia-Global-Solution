using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Domain.Regras;

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
