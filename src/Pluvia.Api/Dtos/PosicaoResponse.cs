using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record PosicaoResponse(int VeiculoId, double? Lat, double? Lng, DateTime? AtualizadoEm);
