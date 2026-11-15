using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

/// <summary>"SemDadosRecentes" nunca significa segurança.</summary>
public record ListaDeOcorrenciasResponse(
    string StatusDosDados, string Aviso, DateTime? UltimaAtualizacaoNaRegiao,
    IReadOnlyList<OcorrenciaResponse> Ocorrencias);
