using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record CriarVeiculoRequest(
    [Required, StringLength(60)] string Apelido,
    [Range(AreaMonitorada.RaioMinimo, AreaMonitorada.RaioMaximo)] int RaioMetros = Veiculo.RaioPadrao);
