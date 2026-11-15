using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record OcorrenciaResponse(
    int Id, int FonteId, string Fonte, StatusOcorrencia Status, string Rotulo, double Lat, double Lng,
    string? Descricao, DateTime AbertaEm, DateTime AtualizadaEm, DateTime? EncerradaEm,
    bool SemAtualizacaoRecente, double? DistanciaMetros);
