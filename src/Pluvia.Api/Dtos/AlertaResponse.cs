using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record AlertaResponse(
    int Id, TipoAlerta Tipo, string Mensagem, int OcorrenciaId, string Fonte, string Link, DateTime CriadoEm);
