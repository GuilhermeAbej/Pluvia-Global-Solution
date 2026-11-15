using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record RiscoResponse(
    RiscoEstimado Risco, int OcorrenciasEm90Dias, int OcorrenciasAtivasAgora, string Observacao);
