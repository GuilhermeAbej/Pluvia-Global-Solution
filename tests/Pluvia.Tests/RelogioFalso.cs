using Pluvia.Api.Data;
using Pluvia.Api.Dtos;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Entities;
using Pluvia.Domain.Enums;
using Pluvia.Domain.Regras;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Pluvia.Tests;

public class RelogioFalso : TimeProvider
{
    private DateTimeOffset _agora = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => _agora;
    public void Avancar(TimeSpan t) => _agora += t;
}
