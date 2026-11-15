using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record PosicaoRequest([Range(-90, 90)] double Lat, [Range(-180, 180)] double Lng);
