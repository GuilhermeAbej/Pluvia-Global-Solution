using Alaga.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Alaga.Api.Data;

public class AlagaDbContext : DbContext
{
    public AlagaDbContext(DbContextOptions<AlagaDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Fonte> Fontes => Set<Fonte>();
    public DbSet<Sensor> Sensores => Set<Sensor>();
    public DbSet<LeituraSensor> Leituras => Set<LeituraSensor>();
    public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();
    public DbSet<HistoricoStatus> Historicos => Set<HistoricoStatus>();
    public DbSet<Relato> Relatos => Set<Relato>();
    public DbSet<AreaMonitorada> Areas => Set<AreaMonitorada>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();

    // SQLite perde o Kind das datas; garantimos que tudo volta como UTC.
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<DateTime>().HaveConversion<DataUtc>();
        builder.Properties<DateTime?>().HaveConversion<DataUtcNula>();
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Usuario>(e => e.Property(u => u.Nome).HasMaxLength(120).IsRequired());

        m.Entity<Fonte>(e =>
        {
            e.Property(f => f.Nome).HasMaxLength(120).IsRequired();
            e.HasIndex(f => f.Nome).IsUnique();
        });

        m.Entity<Sensor>(e =>
        {
            e.Property(s => s.Codigo).HasMaxLength(50).IsRequired();
            e.HasIndex(s => s.Codigo).IsUnique();
            e.HasOne<Fonte>().WithMany().HasForeignKey(s => s.FonteId);
        });

        m.Entity<LeituraSensor>(e =>
        {
            e.HasOne<Sensor>().WithMany().HasForeignKey(l => l.SensorId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => new { l.SensorId, l.RegistradaEm });
        });

        m.Entity<Ocorrencia>(e =>
        {
            e.Property(o => o.Descricao).HasMaxLength(280);
            e.HasOne<Fonte>().WithMany().HasForeignKey(o => o.FonteId);
            e.HasIndex(o => new { o.Status, o.Lat, o.Lng });
        });

        m.Entity<HistoricoStatus>(e =>
        {
            e.HasOne<Ocorrencia>().WithMany().HasForeignKey(h => h.OcorrenciaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Fonte>().WithMany().HasForeignKey(h => h.FonteId);
            e.HasIndex(h => new { h.OcorrenciaId, h.Em });
        });

        m.Entity<Relato>(e =>
        {
            e.Property(r => r.Descricao).HasMaxLength(280);
            e.OwnsOne(r => r.Avaliacao, a => a.Property(x => x.Modelo).HasMaxLength(60));
            e.HasOne<Usuario>().WithMany().HasForeignKey(r => r.UsuarioId);
            e.HasIndex(r => new { r.UsuarioId, r.CriadoEm });
            e.HasIndex(r => new { r.Status, r.Lat, r.Lng });
        });

        m.Entity<AreaMonitorada>(e =>
        {
            e.Property(a => a.Nome).HasMaxLength(120).IsRequired();
            e.HasOne<Usuario>().WithMany().HasForeignKey(a => a.UsuarioId);
        });

        m.Entity<Alerta>(e =>
        {
            e.Property(a => a.Mensagem).HasMaxLength(600);
            e.HasOne<Ocorrencia>().WithMany().HasForeignKey(a => a.OcorrenciaId);
            // Notificação deduplicada: um aviso de cada tipo por pessoa e ocorrência.
            e.HasIndex(a => new { a.UsuarioId, a.OcorrenciaId, a.Tipo }).IsUnique();
        });

        m.Entity<Veiculo>(e =>
        {
            e.Property(v => v.Apelido).HasMaxLength(60).IsRequired();
            e.HasOne<Usuario>().WithMany().HasForeignKey(v => v.UsuarioId);
        });
    }

    private class DataUtc : ValueConverter<DateTime, DateTime>
    {
        public DataUtc() : base(d => d.ToUniversalTime(), d => DateTime.SpecifyKind(d, DateTimeKind.Utc)) { }
    }

    private class DataUtcNula : ValueConverter<DateTime?, DateTime?>
    {
        public DataUtcNula() : base(
            d => d.HasValue ? d.Value.ToUniversalTime() : d,
            d => d.HasValue ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc) : d) { }
    }
}
