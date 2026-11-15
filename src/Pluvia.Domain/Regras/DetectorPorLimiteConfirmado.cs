using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Domain.Regras;

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
