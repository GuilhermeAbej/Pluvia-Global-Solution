using Pluvia.Api.Data;
using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Exceptions;
using Pluvia.Domain.Geo;
using Pluvia.Domain.Regras;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Pluvia.Api.Services;

public class OcorrenciaService : IOcorrenciaService
{
    private readonly PluviaDbContext _db;
    private readonly IGestorDeOcorrencias _gestor;
    private readonly TimeProvider _relogio;
    private readonly PluviaOptions _opt;

    public OcorrenciaService(PluviaDbContext db, IGestorDeOcorrencias gestor, TimeProvider relogio, IOptions<PluviaOptions> opt)
    {
        _db = db; _gestor = gestor; _relogio = relogio; _opt = opt.Value;
    }

    public async Task<ListaDeOcorrenciasResponse> ListarAsync(FiltroOcorrencias f, CancellationToken ct = default)
    {
        GeoCalculator.Validar(f.Lat, f.Lng);
        if (f.RaioMetros is < 100 or > 10_000) throw new DominioException("Raio deve estar entre 100 e 10000 metros.");

        var agora = _relogio.GetUtcNow().UtcDateTime;
        var encontradas = await _db.OcorrenciasPerto(f.Lat, f.Lng, f.RaioMetros, f.Status, f.FonteId, f.Desde, f.Ate, ct);
        var fontes = await NomesDasFontes(ct);

        var itens = encontradas.Select(o => Mapear(o, fontes, agora, GeoCalculator.DistanciaMetros(f.Lat, f.Lng, o.Lat, o.Lng))).ToList();

        // Frescor dos dados da região: última leitura de sensor ou atualização de qualquer ocorrência por perto.
        var ultima = await UltimaAtualizacaoDaRegiao(f.Lat, f.Lng, f.RaioMetros, ct);
        var recente = ultima is not null && agora - ultima.Value <= TimeSpan.FromMinutes(_opt.JanelaDadosRecentesMinutos);

        string status, aviso;
        if (!recente)
        {
            status = "SemDadosRecentes";
            aviso = ultima is null
                ? "Não há dados de nenhuma fonte para esta região. Isso não significa que esteja segura."
                : $"Sem atualização suficiente: o último dado da região é das {MensagemDeAlerta.Hora(ultima.Value)} (Brasília). Isso não significa que esteja segura.";
        }
        else
        {
            status = "Atualizado";
            aviso = itens.Count == 0
                ? "Nenhum alagamento registrado pelas fontes neste filtro. A ausência de ponto no mapa não significa que a via esteja segura."
                : $"Dados da região atualizados às {MensagemDeAlerta.Hora(ultima!.Value)} (Brasília).";
        }

        return new ListaDeOcorrenciasResponse(status, aviso, ultima, itens);
    }

    public async Task<OcorrenciaResponse> BuscarAsync(int id, CancellationToken ct = default)
    {
        var o = await _db.Ocorrencias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new RecursoNaoEncontradoException("Ocorrência", id);
        return Mapear(o, await NomesDasFontes(ct), _relogio.GetUtcNow().UtcDateTime, null);
    }

    public async Task<IReadOnlyList<HistoricoResponse>> HistoricoAsync(int id, CancellationToken ct = default)
    {
        if (!await _db.Ocorrencias.AnyAsync(o => o.Id == id, ct)) throw new RecursoNaoEncontradoException("Ocorrência", id);

        return await (
            from h in _db.Historicos.AsNoTracking()
            where h.OcorrenciaId == id
            join f in _db.Fontes on h.FonteId equals f.Id
            orderby h.Em, h.Id
            select new HistoricoResponse(h.Status, f.Nome, h.Em)
        ).ToListAsync(ct);
    }

    public async Task<OcorrenciaResponse> RegistrarOficialAsync(RegistrarOcorrenciaRequest req, CancellationToken ct = default)
    {
        await ExigirFonteOficial(req.FonteId, ct);

        var (o, criada) = await _gestor.AbrirAsync(req.FonteId, req.Lat, req.Lng, req.Descricao, ct);
        if (!criada) throw new RegraDeNegocioException($"Esta fonte já tem a ocorrência ativa {o.Id} neste ponto.");

        return Mapear(o, await NomesDasFontes(ct), _relogio.GetUtcNow().UtcDateTime, null);
    }

    public async Task<OcorrenciaResponse> EncerrarOficialAsync(int id, int fonteId, CancellationToken ct = default)
    {
        await ExigirFonteOficial(fonteId, ct);
        var o = await _db.Ocorrencias.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new RecursoNaoEncontradoException("Ocorrência", id);

        await _gestor.EncerrarAsync(o, fonteId, ct);
        return Mapear(o, await NomesDasFontes(ct), _relogio.GetUtcNow().UtcDateTime, null);
    }

    // A chave oficial só fala em nome de fontes do tipo Oficial; sensores encerram apenas as próprias ocorrências.
    private async Task ExigirFonteOficial(int fonteId, CancellationToken ct)
    {
        var fonte = await _db.Fontes.AsNoTracking().FirstOrDefaultAsync(f => f.Id == fonteId, ct)
            ?? throw new RecursoNaoEncontradoException("Fonte", fonteId);
        if (fonte.Tipo != TipoFonte.Oficial)
            throw new RegraDeNegocioException("A chave oficial só atua em nome de fontes oficiais.");
    }

    private async Task<DateTime?> UltimaAtualizacaoDaRegiao(double lat, double lng, double raio, CancellationToken ct)
    {
        var c = GeoCalculator.Caixa(lat, lng, raio);
        var sensores = (await _db.Sensores.AsNoTracking()
                .Where(s => s.Lat >= c.LatMin && s.Lat <= c.LatMax && s.Lng >= c.LngMin && s.Lng <= c.LngMax)
                .ToListAsync(ct))
            .Where(s => GeoCalculator.DistanciaMetros(lat, lng, s.Lat, s.Lng) <= raio)
            .Select(s => s.Id).ToList();

        DateTime? deSensor = sensores.Count == 0 ? null : await _db.Leituras.AsNoTracking()
            .Where(l => sensores.Contains(l.SensorId))
            .OrderByDescending(l => l.RegistradaEm)
            .Select(l => (DateTime?)l.RegistradaEm).FirstOrDefaultAsync(ct);

        var todas = await _db.OcorrenciasPerto(lat, lng, raio, ct: ct);
        DateTime? deOcorrencia = todas.Count == 0 ? null : todas.Max(o => o.AtualizadaEm);

        return new[] { deSensor, deOcorrencia }.Where(d => d.HasValue).Max();
    }

    private async Task<Dictionary<int, string>> NomesDasFontes(CancellationToken ct) =>
        await _db.Fontes.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.Nome, ct);

    private OcorrenciaResponse Mapear(Ocorrencia o, Dictionary<int, string> fontes, DateTime agora, double? distancia)
    {
        var semAtualizacao = o.SemAtualizacaoRecente(agora, TimeSpan.FromMinutes(_opt.JanelaDadosRecentesMinutos));
        var rotulo = o.Status switch
        {
            StatusOcorrencia.Ativa when semAtualizacao => "Ativo, sem atualização recente da fonte",
            StatusOcorrencia.Ativa => "Confirmado • ativo",
            _ => "Encerrado pela fonte (a condição física precisa ser reavaliada)"
        };
        return new OcorrenciaResponse(o.Id, o.FonteId, fontes.GetValueOrDefault(o.FonteId, "?"), o.Status, rotulo,
            o.Lat, o.Lng, o.Descricao, o.AbertaEm, o.AtualizadaEm, o.EncerradaEm, semAtualizacao,
            distancia is null ? null : Math.Round(distancia.Value));
    }
}
