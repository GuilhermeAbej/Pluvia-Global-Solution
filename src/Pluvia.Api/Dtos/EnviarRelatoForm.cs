using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

/// <summary>Formulário multipart do relato. A foto é opcional.</summary>
public class EnviarRelatoForm
{
    [Range(-90, 90)] public double Lat { get; set; }
    [Range(-180, 180)] public double Lng { get; set; }
    [StringLength(280)] public string? Descricao { get; set; }
    public TipoEvento Tipo { get; set; } = TipoEvento.AguaSobreAVia;
    /// <summary>Quando viu o evento (UTC). Vazio = agora.</summary>
    public DateTime? OcorridoEm { get; set; }
    /// <summary>Obrigatório quando há foto: "a foto é atual e do ponto indicado".</summary>
    public bool FotoAtualConfirmada { get; set; }
    public IFormFile? Foto { get; set; }
}
