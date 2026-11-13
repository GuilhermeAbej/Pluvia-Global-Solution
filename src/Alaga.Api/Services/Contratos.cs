using Alaga.Api.Dtos;
using Alaga.Domain.Entities;
using Alaga.Domain.Enums;

namespace Alaga.Api.Services;

public interface IAnalisadorDeImagem
{
    /// <summary>Triagem da foto. Um modelo de visão real implementa esta interface sem mudar o resto.</summary>
    Task<AvaliacaoImagem> AnalisarAsync(byte[] imagem, CancellationToken ct = default);
}

public interface INotificador
{
    Task NotificarAsync(Alerta alerta, CancellationToken ct = default);
}

public interface IArmazenamentoDeFotos
{
    Task<string> SalvarAsync(byte[] conteudo, string extensao, CancellationToken ct = default);
    Task<(Stream Conteudo, string ContentType)?> AbrirAsync(string nome, CancellationToken ct = default);
    Task ExcluirAsync(string nome, CancellationToken ct = default);
}

public record RiscoCalculado(RiscoEstimado Risco, int OcorrenciasEm90Dias, int OcorrenciasAtivas);

public interface IRiscoService
{
    Task<RiscoCalculado> CalcularAsync(double lat, double lng, double raioMetros, CancellationToken ct = default);
}

public interface IGeradorDeAlertas
{
    Task NotificarNovoEventoAsync(Ocorrencia ocorrencia, CancellationToken ct = default);
    Task NotificarEncerramentoAsync(Ocorrencia ocorrencia, CancellationToken ct = default);
}

/// <summary>Ciclo de vida da ocorrência: abrir, atualizar, encerrar, registrar histórico e avisar.</summary>
public interface IGestorDeOcorrencias
{
    Task<(Ocorrencia Ocorrencia, bool Criada)> AbrirAsync(int fonteId, double lat, double lng, string? descricao, CancellationToken ct = default);
    Task EncerrarAsync(Ocorrencia ocorrencia, int fonteId, CancellationToken ct = default);
}

public interface IUsuarioService
{
    Task<UsuarioResponse> CriarAsync(CriarUsuarioRequest req, CancellationToken ct = default);
}

public interface IFonteService
{
    Task<IReadOnlyList<FonteResponse>> ListarAsync(CancellationToken ct = default);
}

public interface ISensorService
{
    Task<LeituraResponse> RegistrarLeituraAsync(LeituraRequest req, CancellationToken ct = default);
}

public interface IRelatoService
{
    Task<RelatoResponse> EnviarAsync(int usuarioId, NovoRelato novo, CancellationToken ct = default);
    Task<FotoAnaliseResponse> AnalisarFotoAsync(byte[] foto, CancellationToken ct = default);
    Task<RelatoDetalheResponse> ObterDoAutorAsync(int usuarioId, int relatoId, CancellationToken ct = default);
    Task<IReadOnlyList<RelatoDetalheResponse>> ListarDoAutorAsync(int usuarioId, CancellationToken ct = default);
    Task<IReadOnlyList<RelatoPublicoResponse>> ListarPublicosAsync(double lat, double lng, int raioMetros, CancellationToken ct = default);
    Task ExcluirAsync(int usuarioId, int relatoId, CancellationToken ct = default);
    Task<(Stream Conteudo, string ContentType)> ObterFotoAsync(int relatoId, int? usuarioId, bool oficial, CancellationToken ct = default);
    Task<RelatoDetalheResponse> PublicarAsync(int relatoId, CancellationToken ct = default);
    Task<RelatoDetalheResponse> RejeitarAsync(int relatoId, CancellationToken ct = default);
}

public record FiltroOcorrencias(
    double Lat, double Lng, int RaioMetros, StatusOcorrencia? Status, int? FonteId, DateTime? Desde, DateTime? Ate);

public interface IOcorrenciaService
{
    Task<ListaDeOcorrenciasResponse> ListarAsync(FiltroOcorrencias filtro, CancellationToken ct = default);
    Task<OcorrenciaResponse> BuscarAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<HistoricoResponse>> HistoricoAsync(int id, CancellationToken ct = default);
    Task<OcorrenciaResponse> RegistrarOficialAsync(RegistrarOcorrenciaRequest req, CancellationToken ct = default);
    Task<OcorrenciaResponse> EncerrarOficialAsync(int id, int fonteId, CancellationToken ct = default);
}

public interface ILocalService
{
    Task<LocalResponse> CriarAsync(int usuarioId, CriarLocalRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<LocalResponse>> ListarAsync(int usuarioId, CancellationToken ct = default);
    Task<RiscoResponse> RiscoAsync(int usuarioId, int localId, CancellationToken ct = default);
    Task<LocalResponse> SilenciarAsync(int usuarioId, int localId, int minutos, CancellationToken ct = default);
    Task<LocalResponse> ReativarAsync(int usuarioId, int localId, CancellationToken ct = default);
}

public interface IAlertaService
{
    Task<IReadOnlyList<AlertaResponse>> ListarDoUsuarioAsync(int usuarioId, CancellationToken ct = default);
}

public interface IVeiculoService
{
    Task<VeiculoResponse> CriarAsync(int usuarioId, CriarVeiculoRequest req, CancellationToken ct = default);
    Task<PosicaoResponse> AtualizarPosicaoAsync(int usuarioId, int veiculoId, PosicaoRequest req, CancellationToken ct = default);
    Task<PosicaoResponse> ObterPosicaoAsync(int usuarioId, int veiculoId, CancellationToken ct = default);
    Task<VeiculoResponse> DefinirAvisosAsync(int usuarioId, int veiculoId, bool ativos, CancellationToken ct = default);
}
