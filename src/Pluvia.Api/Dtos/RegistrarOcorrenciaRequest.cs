using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

/// <summary>Registro feito pela fonte oficial (simulada pela chave oficial).</summary>
public record RegistrarOcorrenciaRequest(
    [Range(1, int.MaxValue)] int FonteId,
    [Range(-90, 90)] double Lat,
    [Range(-180, 180)] double Lng,
    [StringLength(280)] string? Descricao);
