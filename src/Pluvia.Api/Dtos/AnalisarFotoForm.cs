using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public class AnalisarFotoForm
{
    [Required] public IFormFile? Foto { get; set; }
}
