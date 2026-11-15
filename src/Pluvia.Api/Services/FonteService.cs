using Pluvia.Api.Data;
using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Pluvia.Api.Services;

public class FonteService : IFonteService
{
    private readonly PluviaDbContext _db;
    public FonteService(PluviaDbContext db) => _db = db;

    public async Task<IReadOnlyList<FonteResponse>> ListarAsync(CancellationToken ct = default) =>
        await _db.Fontes.AsNoTracking().OrderBy(f => f.Id)
            .Select(f => new FonteResponse(f.Id, f.Nome, f.Tipo)).ToListAsync(ct);
}
