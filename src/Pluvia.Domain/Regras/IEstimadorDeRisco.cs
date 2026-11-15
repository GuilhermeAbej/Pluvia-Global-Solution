using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Domain.Regras;

public interface IEstimadorDeRisco
{
    RiscoEstimado Estimar(int ocorrenciasNoPeriodo, int diasDeHistorico);
}
