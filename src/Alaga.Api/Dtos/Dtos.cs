using System.ComponentModel.DataAnnotations;
using Alaga.Domain.Entities;
using Alaga.Domain.Enums;

namespace Alaga.Api.Dtos;

// Em records, as anotações de validação ficam direto no parâmetro (nunca [property: ...]).

public record CriarUsuarioRequest([Required, StringLength(120)] string Nome, bool ConsentimentoLgpd);

public record LeituraRequest(
    [Required, StringLength(50)] string Codigo,
    [Range(0, LeituraSensor.MaximoCm)] int NivelCm);

public record CriarLocalRequest(
    [Required, StringLength(120)] string Nome,
    [Range(-90, 90)] double Lat,
    [Range(-180, 180)] double Lng,
    [Range(AreaMonitorada.RaioMinimo, AreaMonitorada.RaioMaximo)] int RaioMetros);

public record SilenciarRequest([Range(1, 1440)] int Minutos);

public record CriarVeiculoRequest(
    [Required, StringLength(60)] string Apelido,
    [Range(AreaMonitorada.RaioMinimo, AreaMonitorada.RaioMaximo)] int RaioMetros = Veiculo.RaioPadrao);

public record PosicaoRequest([Range(-90, 90)] double Lat, [Range(-180, 180)] double Lng);

public record AvisosRequest(bool Ativos);

/// <summary>Registro feito pela fonte oficial (simulada pela chave oficial).</summary>
public record RegistrarOcorrenciaRequest(
    [Range(1, int.MaxValue)] int FonteId,
    [Range(-90, 90)] double Lat,
    [Range(-180, 180)] double Lng,
    [StringLength(280)] string? Descricao);

public record EncerrarOcorrenciaRequest([Range(1, int.MaxValue)] int FonteId);

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

public class AnalisarFotoForm
{
    [Required] public IFormFile? Foto { get; set; }
}

public record NovoRelato(double Lat, double Lng, string? Descricao, TipoEvento Tipo,
    DateTime? OcorridoEm, bool FotoAtualConfirmada, byte[]? Foto);

// ---------- Respostas (nunca expõem dados pessoais de outros usuários) ----------

public record UsuarioResponse(int Id, string Nome);

public record FonteResponse(int Id, string Nome, TipoFonte Tipo);

public record LeituraResponse(int LeituraId, string Acao, int? OcorrenciaId);

public record RelatoResponse(
    int RelatoId, StatusRelato Status, PrioridadeRelato Prioridade,
    ClassificacaoImagem? TriagemDaImagem, bool MetadadosRemovidos, string Aviso);

public record RelatoDetalheResponse(
    int Id, StatusRelato Status, string Rotulo, TipoEvento Tipo, DateTime OcorridoEm, double Lat, double Lng,
    string Descricao, bool TemFoto, PrioridadeRelato Prioridade, ClassificacaoImagem? TriagemDaImagem,
    DateTime CriadoEm, DateTime? PublicadoEm);

/// <summary>Relato publicado, exibido separado das ocorrências oficiais e sem dados do autor.</summary>
public record RelatoPublicoResponse(
    int Id, double Lat, double Lng, TipoEvento Tipo, DateTime OcorridoEm, string Rotulo, double? DistanciaMetros);

public record FotoAnaliseResponse(ClassificacaoImagem Classificacao, double Confianca, bool MetadadosRemovidos, string Aviso);

public record OcorrenciaResponse(
    int Id, int FonteId, string Fonte, StatusOcorrencia Status, string Rotulo, double Lat, double Lng,
    string? Descricao, DateTime AbertaEm, DateTime AtualizadaEm, DateTime? EncerradaEm,
    bool SemAtualizacaoRecente, double? DistanciaMetros);

public record HistoricoResponse(StatusOcorrencia Status, string Fonte, DateTime Em);

/// <summary>"SemDadosRecentes" nunca significa segurança.</summary>
public record ListaDeOcorrenciasResponse(
    string StatusDosDados, string Aviso, DateTime? UltimaAtualizacaoNaRegiao,
    IReadOnlyList<OcorrenciaResponse> Ocorrencias);

public record LocalResponse(int Id, string Nome, double Lat, double Lng, int RaioMetros, DateTime? SilenciadaAte, RiscoEstimado RiscoAtual);

public record RiscoResponse(
    RiscoEstimado Risco, int OcorrenciasEm90Dias, int OcorrenciasAtivasAgora, string Observacao);

public record AlertaResponse(
    int Id, TipoAlerta Tipo, string Mensagem, int OcorrenciaId, string Fonte, string Link, DateTime CriadoEm);

public record VeiculoResponse(int Id, string Apelido, int RaioMetros, bool AvisosAtivos);

public record PosicaoResponse(int VeiculoId, double? Lat, double? Lng, DateTime? AtualizadoEm);
