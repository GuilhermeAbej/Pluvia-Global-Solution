using Pluvia.Api.Data;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Regras;
using Microsoft.EntityFrameworkCore;

namespace Pluvia.Api.Services;

/// <summary>
/// Decide quem recebe aviso: quem monitora uma área ou um veículo perto da ocorrência, fora do silêncio,
/// uma única vez por pessoa e tipo de aviso (deduplicação).
/// </summary>
public class GeradorDeAlertas : IGeradorDeAlertas
{
    private record Contexto(int UsuarioId, string Nome, bool EhVeiculo);

    private readonly PluviaDbContext _db;
    private readonly INotificador _notificador;
    private readonly IRiscoService _risco;
    private readonly TimeProvider _relogio;

    public GeradorDeAlertas(PluviaDbContext db, INotificador notificador, IRiscoService risco, TimeProvider relogio)
    {
        _db = db; _notificador = notificador; _risco = risco; _relogio = relogio;
    }

    public async Task NotificarNovoEventoAsync(Ocorrencia o, CancellationToken ct = default)
    {
        var agora = _relogio.GetUtcNow().UtcDateTime;
        var fonte = await NomeDaFonte(o, ct);

        var jaAvisados = await _db.Alertas
            .Where(a => a.OcorrenciaId == o.Id && a.Tipo == TipoAlerta.NovoEvento)
            .Select(a => a.UsuarioId).ToListAsync(ct);

        var areas = await _db.AreasQueCobrem(o.Lat, o.Lng, ct);
        var novos = new List<Alerta>();

        foreach (var c in await Contextos(o, areas, agora, ct))
        {
            if (jaAvisados.Contains(c.UsuarioId)) continue;
            var msg = c.EhVeiculo
                ? MensagemDeAlerta.NovoEventoVeiculo(c.Nome, fonte, o.AbertaEm)
                : MensagemDeAlerta.NovoEvento(c.Nome, fonte, o.AbertaEm);
            novos.Add(new Alerta(c.UsuarioId, o.Id, TipoAlerta.NovoEvento, msg, agora));
        }

        // Mudança de risco: avisa só quando a estimativa da área sobe para Moderado ou Alto.
        var avisadosDeRisco = new HashSet<int>();
        foreach (var area in areas)
        {
            var calculado = await _risco.CalcularAsync(area.Lat, area.Lng, area.RaioMetros, ct);
            if (calculado.Risco == area.RiscoAtual) continue;
            area.AtualizarRisco(calculado.Risco);

            var subiu = calculado.Risco is RiscoEstimado.Moderado or RiscoEstimado.Alto;
            if (subiu && !area.EstaSilenciada(agora) && avisadosDeRisco.Add(area.UsuarioId))
                novos.Add(new Alerta(area.UsuarioId, o.Id, TipoAlerta.MudancaDeRisco,
                    MensagemDeAlerta.MudancaDeRisco(area.Nome, calculado.Risco), agora));
        }

        await Gravar(novos, ct);
    }

    public async Task NotificarEncerramentoAsync(Ocorrencia o, CancellationToken ct = default)
    {
        var agora = _relogio.GetUtcNow().UtcDateTime;
        var fonte = await NomeDaFonte(o, ct);

        // Só quem recebeu o aviso de novo evento precisa saber que encerrou.
        var avisados = await _db.Alertas
            .Where(a => a.OcorrenciaId == o.Id && a.Tipo == TipoAlerta.NovoEvento &&
                        !_db.Alertas.Any(e => e.OcorrenciaId == o.Id && e.UsuarioId == a.UsuarioId &&
                                              e.Tipo == TipoAlerta.Encerramento))
            .Select(a => a.UsuarioId).ToListAsync(ct);
        if (avisados.Count == 0) return;

        var areas = await _db.AreasQueCobrem(o.Lat, o.Lng, ct);
        var quando = o.EncerradaEm ?? agora;

        var novos = (await Contextos(o, areas, agora, ct))
            .Where(c => avisados.Contains(c.UsuarioId))
            .Select(c => new Alerta(c.UsuarioId, o.Id, TipoAlerta.Encerramento,
                MensagemDeAlerta.Encerramento(c.Nome, fonte, quando), agora))
            .ToList();

        await Gravar(novos, ct);
    }

    /// <summary>
    /// Um contexto por pessoa. Se a pessoa tem veículo e área na região, vale o veículo
    /// (mensagem mais específica). Áreas silenciadas e veículos com avisos desligados ficam de fora.
    /// </summary>
    private async Task<List<Contexto>> Contextos(Ocorrencia o, List<AreaMonitorada> areas, DateTime agora, CancellationToken ct)
    {
        var veiculos = (await _db.VeiculosQueCobrem(o.Lat, o.Lng, ct)).Where(v => v.AvisosAtivos);

        var deVeiculo = veiculos.Select(v => new Contexto(v.UsuarioId, v.Apelido, true));
        var deArea = areas.Where(a => !a.EstaSilenciada(agora)).Select(a => new Contexto(a.UsuarioId, a.Nome, false));

        return deVeiculo.Concat(deArea)
            .GroupBy(c => c.UsuarioId)
            .Select(g => g.OrderByDescending(c => c.EhVeiculo).First())
            .ToList();
    }

    private async Task<string> NomeDaFonte(Ocorrencia o, CancellationToken ct) =>
        await _db.Fontes.Where(f => f.Id == o.FonteId).Select(f => f.Nome).FirstOrDefaultAsync(ct) ?? "fonte identificada";

    private async Task Gravar(List<Alerta> alertas, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);   // também grava o risco atualizado das áreas
        if (alertas.Count == 0) return;
        _db.Alertas.AddRange(alertas);
        await _db.SaveChangesAsync(ct);
        foreach (var a in alertas) await _notificador.NotificarAsync(a, ct);
    }
}
