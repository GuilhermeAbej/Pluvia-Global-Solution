using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record RelatoResponse(
    int RelatoId, StatusRelato Status, PrioridadeRelato Prioridade,
    ClassificacaoImagem? TriagemDaImagem, bool MetadadosRemovidos, string Aviso);
