using Alaga.Api.Data;
using Alaga.Api.Dtos;
using Alaga.Api.Options;
using Alaga.Domain.Entities;
using Alaga.Domain.Enums;
using Alaga.Domain.Exceptions;
using Alaga.Domain.Geo;
using Alaga.Domain.Regras;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Alaga.Api.Services;

public class RelatoService : IRelatoService
{
    private const string AvisoDaIa =
        "Triagem automática. Uma foto isolada não comprova profundidade, extensão, bloqueio de rua nem risco para atravessar.";

    private readonly AlagaDbContext _db;
    private readonly IAnalisadorDeImagem _analisador;
    private readonly IArmazenamentoDeFotos _fotos;
    private readonly TimeProvider _relogio;
    private readonly AlagaOptions _opt;

    public RelatoService(AlagaDbContext db, IAnalisadorDeImagem analisador, IArmazenamentoDeFotos fotos,
        TimeProvider relogio, IOptions<AlagaOptions> opt)
    {
        _db = db; _analisador = analisador; _fotos = fotos; _relogio = relogio; _opt = opt.Value;
    }

    public async Task<RelatoResponse> EnviarAsync(int usuarioId, NovoRelato novo, CancellationToken ct = default)
    {
        if (!await _db.Usuarios.AnyAsync(u => u.Id == usuarioId, ct))
            throw new RecursoNaoEncontradoException("Usuário", usuarioId);

        GeoCalculator.Validar(novo.Lat, novo.Lng);
        var agora = _relogio.GetUtcNow().UtcDateTime;
        var janela = agora.AddMinutes(-_opt.JanelaAgrupamentoMinutos);

        await ExigirLimiteDeEnvioAsync(usuarioId, agora, ct);

        // Duplicidade: a mesma pessoa não relata o mesmo ponto de novo dentro da janela.
        var proprios = (await _db.RelatosPerto(novo.Lat, novo.Lng, _opt.RaioAgrupamentoMetros,
            new[] { StatusRelato.EmAnalise, StatusRelato.Publicado }, janela, ct)).Any(r => r.UsuarioId == usuarioId);
        if (proprios) throw new RegraDeNegocioException("Você já enviou um relato deste ponto recentemente.");

        string? nomeFoto = null;
        ImagemSanitizada? imagem = null;
        AvaliacaoImagem? avaliacao = null;
        if (novo.Foto is { Length: > 0 })
        {
            imagem = ValidarEPrepararFoto(novo.Foto);
            avaliacao = await _analisador.AnalisarAsync(imagem.Bytes, ct);
            nomeFoto = await _fotos.SalvarAsync(imagem.Bytes, imagem.Extensao, ct);
        }

        Relato relato;
        try
        {
            relato = new Relato(usuarioId, novo.Lat, novo.Lng, novo.Descricao, novo.Tipo,
                NormalizarData(novo.OcorridoEm ?? agora), nomeFoto, novo.FotoAtualConfirmada, agora);
        }
        catch
        {
            if (nomeFoto is not null) await _fotos.ExcluirAsync(nomeFoto, ct);   // não deixa foto órfã
            throw;
        }

        if (avaliacao is not null) relato.DefinirTriagem(avaliacao, PoliticaDeTriagem.Priorizar(avaliacao));
        _db.Relatos.Add(relato);
        await _db.SaveChangesAsync(ct);

        await AvaliarPublicacaoAsync(relato, agora, janela, ct);

        var aviso = relato.Status == StatusRelato.Publicado
            ? "Publicado como possível alagamento, relato comunitário. Não é registro oficial."
            : "Relato em análise. Ele só aparece no mapa depois de passar pelos critérios de validação.";

        return new RelatoResponse(relato.Id, relato.Status, relato.Prioridade, relato.Avaliacao?.Classificacao,
            imagem?.MetadadosRemovidos ?? false, aviso);
    }

    public async Task<FotoAnaliseResponse> AnalisarFotoAsync(byte[] foto, CancellationToken ct = default)
    {
        var imagem = ValidarEPrepararFoto(foto);
        var a = await _analisador.AnalisarAsync(imagem.Bytes, ct);
        return new FotoAnaliseResponse(a.Classificacao, a.Confianca, imagem.MetadadosRemovidos, AvisoDaIa);
    }

    public async Task<RelatoDetalheResponse> ObterDoAutorAsync(int usuarioId, int relatoId, CancellationToken ct = default)
    {
        var r = await _db.Relatos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == relatoId, ct)
            ?? throw new RecursoNaoEncontradoException("Relato", relatoId);
        if (r.UsuarioId != usuarioId) throw new AcessoNegadoException("Este relato pertence a outro usuário.");
        return Detalhe(r);
    }

    public async Task<IReadOnlyList<RelatoDetalheResponse>> ListarDoAutorAsync(int usuarioId, CancellationToken ct = default)
    {
        var lista = await _db.Relatos.AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId && r.Status != StatusRelato.Excluido)
            .OrderByDescending(r => r.CriadoEm).ToListAsync(ct);
        return lista.Select(Detalhe).ToList();
    }

    public async Task<IReadOnlyList<RelatoPublicoResponse>> ListarPublicosAsync(
        double lat, double lng, int raioMetros, CancellationToken ct = default)
    {
        GeoCalculator.Validar(lat, lng);
        if (raioMetros is < 100 or > 10_000) throw new DominioException("Raio deve estar entre 100 e 10000 metros.");

        var lista = await _db.RelatosPerto(lat, lng, raioMetros, new[] { StatusRelato.Publicado }, ct: ct);
        return lista
            .Select(r => (Relato: r, Dist: GeoCalculator.DistanciaMetros(lat, lng, r.Lat, r.Lng)))
            .OrderBy(x => x.Dist)
            .Select(x => new RelatoPublicoResponse(x.Relato.Id, x.Relato.Lat, x.Relato.Lng, x.Relato.Tipo,
                x.Relato.OcorridoEm, "Possível alagamento • relato comunitário (não é registro oficial)", Math.Round(x.Dist)))
            .ToList();
    }

    public async Task ExcluirAsync(int usuarioId, int relatoId, CancellationToken ct = default)
    {
        var r = await _db.Relatos.FirstOrDefaultAsync(x => x.Id == relatoId, ct)
            ?? throw new RecursoNaoEncontradoException("Relato", relatoId);
        if (r.UsuarioId != usuarioId) throw new AcessoNegadoException("Este relato pertence a outro usuário.");

        var foto = r.FotoNome;
        r.Excluir();
        await _db.SaveChangesAsync(ct);
        if (foto is not null) await _fotos.ExcluirAsync(foto, ct);
    }

    public async Task<(Stream Conteudo, string ContentType)> ObterFotoAsync(
        int relatoId, int? usuarioId, bool oficial, CancellationToken ct = default)
    {
        var relato = await _db.Relatos.AsNoTracking().FirstOrDefaultAsync(r => r.Id == relatoId, ct)
            ?? throw new RecursoNaoEncontradoException("Relato", relatoId);

        // Fotos são protegidas: só o autor ou a fonte oficial.
        if (!oficial && relato.UsuarioId != usuarioId)
            throw new AcessoNegadoException("Apenas o autor do relato ou a fonte oficial podem ver a foto.");

        if (relato.FotoNome is null) throw new RecursoNaoEncontradoException("Foto do relato", relatoId);

        return await _fotos.AbrirAsync(relato.FotoNome, ct)
            ?? throw new RecursoNaoEncontradoException("Foto do relato", relatoId);
    }

    public async Task<RelatoDetalheResponse> PublicarAsync(int relatoId, CancellationToken ct = default)
    {
        var r = await Obter(relatoId, ct);
        r.Publicar(_relogio.GetUtcNow().UtcDateTime);
        await _db.SaveChangesAsync(ct);
        return Detalhe(r);
    }

    public async Task<RelatoDetalheResponse> RejeitarAsync(int relatoId, CancellationToken ct = default)
    {
        var r = await Obter(relatoId, ct);
        r.Rejeitar();
        await _db.SaveChangesAsync(ct);
        return Detalhe(r);
    }

    // ---------- internos ----------

    /// <summary>
    /// Critérios de validação (a IA não decide): fonte identificada com ocorrência ativa perto,
    /// ou pessoas diferentes relatando o mesmo ponto. Quando atende, publica todos os relatos em análise do ponto.
    /// </summary>
    private async Task AvaliarPublicacaoAsync(Relato novo, DateTime agora, DateTime janela, CancellationToken ct)
    {
        var doPonto = await _db.RelatosPerto(novo.Lat, novo.Lng, _opt.RaioAgrupamentoMetros,
            new[] { StatusRelato.EmAnalise, StatusRelato.Publicado }, janela, ct);
        var pessoas = doPonto.Select(r => r.UsuarioId).Distinct().Count();
        var fonteAtiva = (await _db.OcorrenciasPerto(novo.Lat, novo.Lng, _opt.RaioAgrupamentoMetros,
            StatusOcorrencia.Ativa, ct: ct)).Count > 0;

        if (!PoliticaDeValidacao.DevePublicar(pessoas, fonteAtiva, _opt.RelatosParaPublicar)) return;

        foreach (var r in doPonto.Where(r => r.Status == StatusRelato.EmAnalise)) r.Publicar(agora);
        await _db.SaveChangesAsync(ct);
    }

    private ImagemSanitizada ValidarEPrepararFoto(byte[] foto)
    {
        if (foto.Length > _opt.FotoMaxBytes)
            throw new ImagemInvalidaException($"A foto passa de {_opt.FotoMaxBytes / 1_000_000} MB.");
        return SanitizadorDeImagem.Sanitizar(foto);
    }

    private async Task ExigirLimiteDeEnvioAsync(int usuarioId, DateTime agora, CancellationToken ct)
    {
        var desde = agora.AddHours(-1);
        var enviados = await _db.Relatos.CountAsync(r => r.UsuarioId == usuarioId && r.CriadoEm >= desde, ct);
        if (enviados >= _opt.RelatosPorHora)
            throw new LimiteExcedidoException($"Limite de {_opt.RelatosPorHora} relatos por hora atingido.");
    }

    private async Task<Relato> Obter(int id, CancellationToken ct) =>
        await _db.Relatos.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new RecursoNaoEncontradoException("Relato", id);

    private static DateTime NormalizarData(DateTime d) => d.Kind switch
    {
        DateTimeKind.Local => d.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(d, DateTimeKind.Utc),
        _ => d
    };

    private static RelatoDetalheResponse Detalhe(Relato r) => new(
        r.Id, r.Status,
        r.Status switch
        {
            StatusRelato.EmAnalise => "Relato em análise",
            StatusRelato.Publicado => "Possível alagamento • relato comunitário (não é registro oficial)",
            StatusRelato.Rejeitado => "Relato não publicado",
            _ => "Relato excluído"
        },
        r.Tipo, r.OcorridoEm, r.Lat, r.Lng, r.Descricao, r.FotoNome is not null, r.Prioridade,
        r.Avaliacao?.Classificacao, r.CriadoEm, r.PublicadoEm);
}
