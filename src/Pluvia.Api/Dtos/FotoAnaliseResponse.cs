using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record FotoAnaliseResponse(ClassificacaoImagem Classificacao, double Confianca, bool MetadadosRemovidos, string Aviso);
