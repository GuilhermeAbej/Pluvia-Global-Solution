using Pluvia.Api.Data;
using Pluvia.Api.Dtos;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Pluvia.Api.Services;

public class UsuarioService : IUsuarioService
{
    private readonly PluviaDbContext _db;
    private readonly TimeProvider _relogio;

    public UsuarioService(PluviaDbContext db, TimeProvider relogio) { _db = db; _relogio = relogio; }

    public async Task<UsuarioResponse> CriarAsync(CriarUsuarioRequest req, CancellationToken ct = default)
    {
        var usuario = new Usuario(req.Nome, req.ConsentimentoLgpd, _relogio.GetUtcNow().UtcDateTime);
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(ct);
        return new UsuarioResponse(usuario.Id, usuario.Nome);
    }
}
