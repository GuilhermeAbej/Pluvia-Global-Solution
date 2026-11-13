using Alaga.Api.Dtos;
using Alaga.Api.Options;
using Alaga.Api.Services;
using Alaga.Domain.Enums;
using Alaga.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Alaga.Api.Controllers;

[Route("api/usuarios")]
public class UsuariosController : BaseController
{
    private readonly IUsuarioService _servico;
    public UsuariosController(IUsuarioService servico) => _servico = servico;

    /// <summary>Cadastra um usuário (exige consentimento LGPD). Use o id no cabeçalho X-Usuario-Id dos demais pedidos.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarUsuarioRequest req, CancellationToken ct)
    {
        var r = await _servico.CriarAsync(req, ct);
        return Created($"/api/usuarios/{r.Id}", r);
    }
}

[Route("api/fontes")]
public class FontesController : BaseController
{
    private readonly IFonteService _servico;
    public FontesController(IFonteService servico) => _servico = servico;

    /// <summary>Fontes que registram ocorrências (oficiais e sensores).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FonteResponse>>> Listar(CancellationToken ct) =>
        Ok(await _servico.ListarAsync(ct));
}

[Route("api/sensores")]
public class SensoresController : BaseController
{
    private readonly ISensorService _servico;
    public SensoresController(ISensorService servico) => _servico = servico;

    /// <summary>Recebe a medição de nível de água do sensor (ESP32). Pode abrir, atualizar ou encerrar uma ocorrência.</summary>
    [HttpPost("leituras")]
    [ProducesResponseType(typeof(LeituraResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegistrarLeitura(LeituraRequest req, CancellationToken ct)
    {
        var r = await _servico.RegistrarLeituraAsync(req, ct);
        return Created(r.OcorrenciaId is null ? "/api/ocorrencias" : $"/api/ocorrencias/{r.OcorrenciaId}", r);
    }
}

[Route("api/relatos")]
public class RelatosController : BaseController
{
    private readonly IRelatoService _servico;
    private readonly IOptions<AlagaOptions> _opt;

    public RelatosController(IRelatoService servico, IOptions<AlagaOptions> opt) { _servico = servico; _opt = opt; }

    /// <summary>
    /// Envia um relato (multipart/form-data) com foto opcional. O relato nasce "em análise" e só é publicado
    /// pelos critérios de validação. A foto tem metadados removidos e passa por triagem de IA que só define prioridade.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(8_000_000)]
    [ProducesResponseType(typeof(RelatoResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Enviar([FromForm] EnviarRelatoForm form, CancellationToken ct)
    {
        var usuarioId = UsuarioAtual;
        var bytes = await LerFoto(form.Foto, _opt.Value.FotoMaxBytes, ct);
        var r = await _servico.EnviarAsync(usuarioId,
            new NovoRelato(form.Lat, form.Lng, form.Descricao, form.Tipo, form.OcorridoEm, form.FotoAtualConfirmada, bytes), ct);
        return Created($"/api/relatos/{r.RelatoId}", r);
    }

    /// <summary>Relatos do próprio usuário, com o status de cada um.</summary>
    [HttpGet("meus")]
    public async Task<ActionResult<IReadOnlyList<RelatoDetalheResponse>>> Meus(CancellationToken ct) =>
        Ok(await _servico.ListarDoAutorAsync(UsuarioAtual, ct));

    /// <summary>Acompanha o status de um relato. Só o autor.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RelatoDetalheResponse>> Buscar(int id, CancellationToken ct) =>
        Ok(await _servico.ObterDoAutorAsync(UsuarioAtual, id, ct));

    /// <summary>Exclui o relato e apaga a foto. Só o autor.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await _servico.ExcluirAsync(UsuarioAtual, id, ct);
        return NoContent();
    }

    /// <summary>Relatos publicados perto de um ponto, separados das ocorrências oficiais e sem dados do autor.</summary>
    [HttpGet("comunitarios")]
    public async Task<ActionResult<IReadOnlyList<RelatoPublicoResponse>>> Comunitarios(
        [FromQuery] double lat, [FromQuery] double lng, [FromQuery] int raioMetros = 1000, CancellationToken ct = default) =>
        Ok(await _servico.ListarPublicosAsync(lat, lng, raioMetros, ct));

    /// <summary>Foto de um relato. Só o autor (X-Usuario-Id) ou a fonte oficial (X-Chave-Oficial).</summary>
    [HttpGet("{id:int}/foto")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Foto(int id, CancellationToken ct)
    {
        var oficial = EhOficial(_opt);
        var usuario = UsuarioOpcional;
        if (!oficial && usuario is null)
            throw new NaoAutenticadoException($"Envie {CabecalhoUsuario} ou {CabecalhoOficial}.");

        var (conteudo, tipo) = await _servico.ObterFotoAsync(id, usuario, oficial, ct);
        return File(conteudo, tipo);
    }

    /// <summary>Moderação: publica um relato em análise. Exige X-Chave-Oficial.</summary>
    [HttpPost("{id:int}/publicacao")]
    public async Task<ActionResult<RelatoDetalheResponse>> Publicar(int id, CancellationToken ct)
    {
        ExigirOficial(_opt);
        return Ok(await _servico.PublicarAsync(id, ct));
    }

    /// <summary>Moderação: rejeita um relato em análise. Exige X-Chave-Oficial.</summary>
    [HttpPost("{id:int}/rejeicao")]
    public async Task<ActionResult<RelatoDetalheResponse>> Rejeitar(int id, CancellationToken ct)
    {
        ExigirOficial(_opt);
        return Ok(await _servico.RejeitarAsync(id, ct));
    }

    internal static async Task<byte[]?> LerFoto(IFormFile? foto, int maximo, CancellationToken ct)
    {
        if (foto is not { Length: > 0 }) return null;
        if (foto.Length > maximo)
            throw new ImagemInvalidaException($"A foto passa de {maximo / 1_000_000} MB.");
        using var ms = new MemoryStream();
        await foto.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}

[Route("api/fotos")]
public class FotosController : BaseController
{
    private readonly IRelatoService _servico;
    private readonly IOptions<AlagaOptions> _opt;

    public FotosController(IRelatoService servico, IOptions<AlagaOptions> opt) { _servico = servico; _opt = opt; }

    /// <summary>
    /// Tela de resultado da IA: devolve a categoria sugerida e a confiança, sem criar relato.
    /// É triagem, não confirmação.
    /// </summary>
    [HttpPost("analise")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(8_000_000)]
    public async Task<ActionResult<FotoAnaliseResponse>> Analisar([FromForm] AnalisarFotoForm form, CancellationToken ct)
    {
        _ = UsuarioAtual;
        var bytes = await RelatosController.LerFoto(form.Foto, _opt.Value.FotoMaxBytes, ct)
            ?? throw new ImagemInvalidaException("Envie uma foto.");
        return Ok(await _servico.AnalisarFotoAsync(bytes, ct));
    }
}

[Route("api/ocorrencias")]
public class OcorrenciasController : BaseController
{
    private readonly IOcorrenciaService _servico;
    private readonly IOptions<AlagaOptions> _opt;

    public OcorrenciasController(IOcorrenciaService servico, IOptions<AlagaOptions> opt) { _servico = servico; _opt = opt; }

    /// <summary>
    /// Ocorrências registradas por fontes identificadas, perto de um ponto. Por padrão só as ativas.
    /// Filtros: status, fonteId, desde, ate (data de abertura), ou todos=true para incluir encerradas.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ListaDeOcorrenciasResponse>> Listar(
        [FromQuery] double lat, [FromQuery] double lng, [FromQuery] int raioMetros = 1000,
        [FromQuery] StatusOcorrencia? status = null, [FromQuery] int? fonteId = null,
        [FromQuery] DateTime? desde = null, [FromQuery] DateTime? ate = null, [FromQuery] bool todos = false,
        CancellationToken ct = default)
    {
        var filtro = new FiltroOcorrencias(lat, lng, raioMetros,
            todos ? null : status ?? StatusOcorrencia.Ativa, fonteId, desde?.ToUniversalTime(), ate?.ToUniversalTime());
        return Ok(await _servico.ListarAsync(filtro, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OcorrenciaResponse>> Buscar(int id, CancellationToken ct) =>
        Ok(await _servico.BuscarAsync(id, ct));

    /// <summary>Histórico de mudanças de status da ocorrência, com a fonte de cada mudança.</summary>
    [HttpGet("{id:int}/historico")]
    public async Task<ActionResult<IReadOnlyList<HistoricoResponse>>> Historico(int id, CancellationToken ct) =>
        Ok(await _servico.HistoricoAsync(id, ct));

    /// <summary>Fonte oficial registra uma ocorrência ativa. Exige X-Chave-Oficial.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OcorrenciaResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Registrar(RegistrarOcorrenciaRequest req, CancellationToken ct)
    {
        ExigirOficial(_opt);
        var r = await _servico.RegistrarOficialAsync(req, ct);
        return Created($"/api/ocorrencias/{r.Id}", r);
    }

    /// <summary>A mesma fonte oficial que registrou marca o encerramento. Exige X-Chave-Oficial.</summary>
    [HttpPost("{id:int}/encerramento")]
    public async Task<ActionResult<OcorrenciaResponse>> Encerrar(int id, EncerrarOcorrenciaRequest req, CancellationToken ct)
    {
        ExigirOficial(_opt);
        return Ok(await _servico.EncerrarOficialAsync(id, req.FonteId, ct));
    }
}

[Route("api/locais-monitorados")]
public class LocaisMonitoradosController : BaseController
{
    private readonly ILocalService _servico;
    public LocaisMonitoradosController(ILocalService servico) => _servico = servico;

    /// <summary>Cadastra um lugar para monitorar (casa, trabalho, escola).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LocalResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarLocalRequest req, CancellationToken ct)
    {
        var r = await _servico.CriarAsync(UsuarioAtual, req, ct);
        return Created($"/api/locais-monitorados/{r.Id}", r);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocalResponse>>> Listar(CancellationToken ct) =>
        Ok(await _servico.ListarAsync(UsuarioAtual, ct));

    /// <summary>Risco estimado do local. "Indeterminado" quando falta histórico, nunca "seguro".</summary>
    [HttpGet("{id:int}/risco")]
    public async Task<ActionResult<RiscoResponse>> Risco(int id, CancellationToken ct) =>
        Ok(await _servico.RiscoAsync(UsuarioAtual, id, ct));

    /// <summary>Silencia os avisos do local por até 24 h.</summary>
    [HttpPost("{id:int}/silencio")]
    public async Task<ActionResult<LocalResponse>> Silenciar(int id, SilenciarRequest req, CancellationToken ct) =>
        Ok(await _servico.SilenciarAsync(UsuarioAtual, id, req.Minutos, ct));

    [HttpDelete("{id:int}/silencio")]
    public async Task<ActionResult<LocalResponse>> Reativar(int id, CancellationToken ct) =>
        Ok(await _servico.ReativarAsync(UsuarioAtual, id, ct));
}

[Route("api/riscos")]
public class RiscosController : BaseController
{
    private readonly IRiscoService _risco;
    public RiscosController(IRiscoService risco) => _risco = risco;

    /// <summary>
    /// Exposição histórica/estimada de um ponto pesquisado (apoia "Onde estacionar").
    /// Não recomenda vagas e não confirma água na pista.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<RiscoResponse>> Consultar(
        [FromQuery] double lat, [FromQuery] double lng, [FromQuery] int raioMetros = 500, CancellationToken ct = default)
    {
        if (raioMetros is < 100 or > 2000) throw new DominioException("Raio deve estar entre 100 e 2000 metros.");
        return Ok(LocalService.ParaResposta(await _risco.CalcularAsync(lat, lng, raioMetros, ct)));
    }
}

[Route("api/alertas")]
public class AlertasController : BaseController
{
    private readonly IAlertaService _servico;
    public AlertasController(IAlertaService servico) => _servico = servico;

    /// <summary>Avisos recebidos pelo usuário, com origem, horário e link do evento.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertaResponse>>> Listar(CancellationToken ct) =>
        Ok(await _servico.ListarDoUsuarioAsync(UsuarioAtual, ct));
}

[Route("api/veiculos")]
public class VeiculosController : BaseController
{
    private readonly IVeiculoService _servico;
    public VeiculosController(IVeiculoService servico) => _servico = servico;

    /// <summary>Cadastra o veículo e o raio de monitoramento ao redor de onde ele está estacionado.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VeiculoResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarVeiculoRequest req, CancellationToken ct)
    {
        var r = await _servico.CriarAsync(UsuarioAtual, req, ct);
        return Created($"/api/veiculos/{r.Id}", r);
    }

    /// <summary>Salva onde o carro está estacionado.</summary>
    [HttpPut("{id:int}/posicao")]
    public async Task<ActionResult<PosicaoResponse>> AtualizarPosicao(int id, PosicaoRequest req, CancellationToken ct) =>
        Ok(await _servico.AtualizarPosicaoAsync(UsuarioAtual, id, req, ct));

    /// <summary>Posição do veículo. Visível somente ao dono.</summary>
    [HttpGet("{id:int}/posicao")]
    public async Task<ActionResult<PosicaoResponse>> ObterPosicao(int id, CancellationToken ct) =>
        Ok(await _servico.ObterPosicaoAsync(UsuarioAtual, id, ct));

    /// <summary>Liga ou desliga os avisos do veículo.</summary>
    [HttpPut("{id:int}/avisos")]
    public async Task<ActionResult<VeiculoResponse>> Avisos(int id, AvisosRequest req, CancellationToken ct) =>
        Ok(await _servico.DefinirAvisosAsync(UsuarioAtual, id, req.Ativos, ct));
}
