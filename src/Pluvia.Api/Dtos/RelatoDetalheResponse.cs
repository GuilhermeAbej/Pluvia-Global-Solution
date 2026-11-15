using System.ComponentModel.DataAnnotations;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;

namespace Pluvia.Api.Dtos;

public record RelatoDetalheResponse(
    int Id, StatusRelato Status, string Rotulo, TipoEvento Tipo, DateTime OcorridoEm, double Lat, double Lng,
    string Descricao, bool TemFoto, PrioridadeRelato Prioridade, ClassificacaoImagem? TriagemDaImagem,
    DateTime CriadoEm, DateTime? PublicadoEm);
