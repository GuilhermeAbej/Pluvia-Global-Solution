using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record VeiculoResponse(int Id, string Apelido, int RaioMetros, bool AvisosAtivos);
