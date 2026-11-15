using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record LeituraRequest(
    [Required, StringLength(50)] string Codigo,
    [Range(0, LeituraSensor.MaximoCm)] int NivelCm);
