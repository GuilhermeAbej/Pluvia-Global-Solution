using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Domain.Regras;

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
