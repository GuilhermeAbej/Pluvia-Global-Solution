using Pluvia.Api.Data;
using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Pluvia.Api.Services;

public class AlertaService : IAlertaService
{
    private readonly PluviaDbContext _db;
    public AlertaService(PluviaDbContext db) => _db = db;

    public async Task<IReadOnlyList<AlertaResponse>> ListarDoUsuarioAsync(int usuarioId, CancellationToken ct = default)
    {
        var linhas = await (
            from a in _db.Alertas.AsNoTracking()
            where a.UsuarioId == usuarioId
            join o in _db.Ocorrencias on a.OcorrenciaId equals o.Id
            join f in _db.Fontes on o.FonteId equals f.Id
            orderby a.CriadoEm descending
            select new { a.Id, a.Tipo, a.Mensagem, a.OcorrenciaId, Fonte = f.Nome, a.CriadoEm }
        ).ToListAsync(ct);

        // Cada aviso leva origem, horário e link para o evento.
        return linhas.Select(l => new AlertaResponse(
            l.Id, l.Tipo, l.Mensagem, l.OcorrenciaId, l.Fonte, $"/api/ocorrencias/{l.OcorrenciaId}", l.CriadoEm)).ToList();
    }
}
