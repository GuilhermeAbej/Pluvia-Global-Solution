using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Services;

public record FiltroOcorrencias(
    double Lat, double Lng, int RaioMetros, StatusOcorrencia? Status, int? FonteId, DateTime? Desde, DateTime? Ate);
