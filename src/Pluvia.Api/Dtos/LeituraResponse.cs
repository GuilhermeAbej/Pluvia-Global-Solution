using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record LeituraResponse(int LeituraId, string Acao, int? OcorrenciaId);
