using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

/// <summary>Relato publicado, exibido separado das ocorrências oficiais e sem dados do autor.</summary>
public record RelatoPublicoResponse(
    int Id, double Lat, double Lng, TipoEvento Tipo, DateTime OcorridoEm, string Rotulo, double? DistanciaMetros);
