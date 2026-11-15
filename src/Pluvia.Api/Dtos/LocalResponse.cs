using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record LocalResponse(int Id, string Nome, double Lat, double Lng, int RaioMetros, DateTime? SilenciadaAte, RiscoEstimado RiscoAtual);
