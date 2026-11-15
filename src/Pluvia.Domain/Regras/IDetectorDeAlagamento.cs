using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Domain.Regras;

// ---------- Detecção por sensor (Strategy) ----------

public interface IDetectorDeAlagamento
{
    /// <param name="leiturasRecentesCm">Da mais nova para a mais antiga.</param>
    bool Detectar(IReadOnlyList<int> leiturasRecentesCm, int limiteCm);

    /// <summary>Água voltou ao normal de forma sustentada?</summary>
    bool Normalizou(IReadOnlyList<int> leiturasRecentesCm, int limiteCm);
}
