using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public record RiscoCalculado(RiscoEstimado Risco, int OcorrenciasEm90Dias, int OcorrenciasAtivas);
