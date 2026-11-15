using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

// Em records, as anotações de validação ficam direto no parâmetro (nunca [property: ...]).

public record CriarUsuarioRequest([Required, StringLength(120)] string Nome, bool ConsentimentoLgpd);
