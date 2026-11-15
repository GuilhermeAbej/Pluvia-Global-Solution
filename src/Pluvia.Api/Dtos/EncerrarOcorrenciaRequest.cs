using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record EncerrarOcorrenciaRequest([Range(1, int.MaxValue)] int FonteId);
