using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record NovoRelato(double Lat, double Lng, string? Descricao, TipoEvento Tipo,
    DateTime? OcorridoEm, bool FotoAtualConfirmada, byte[]? Foto);

// ---------- Respostas (nunca expõem dados pessoais de outros usuários) ----------
