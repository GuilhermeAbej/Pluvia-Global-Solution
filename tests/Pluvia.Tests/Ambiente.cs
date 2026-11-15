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

/// <summary>Banco SQLite em memória + todos os serviços montados com dependências falsas.</summary>
public sealed class Ambiente : IDisposable
{
    private readonly SqliteConnection _conexao;
    public PluviaDbContext Db { get; }
    public RelogioFalso Relogio { get; } = new();
    public NotificadorFalso Notificador { get; } = new();
    public AnalisadorFalso Analisador { get; } = new();
    public ArmazenamentoFalso Fotos { get; } = new();
    public PluviaOptions Opcoes { get; } = new();

    public Sensor Sensor { get; }
    public int OficialId { get; }
    public int OutraOficialId { get; }
    public int FonteDoSensorId { get; }

    public SensorService Sensores { get; }
    public RelatoService Relatos { get; }
    public OcorrenciaService Ocorrencias { get; }
    public LocalService Locais { get; }
    public RiscoService Risco { get; }
    public UsuarioService Usuarios { get; }
    public VeiculoService Veiculos { get; }
    public AlertaService Alertas { get; }

    public Ambiente()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();
        Db = new PluviaDbContext(new DbContextOptionsBuilder<PluviaDbContext>().UseSqlite(_conexao).Options);
        Db.Database.EnsureCreated();

        var oficial = new Fonte("CGE-SP (teste)", TipoFonte.Oficial);
        var outra = new Fonte("Defesa Civil (teste)", TipoFonte.Oficial);
        var fonteSensor = new Fonte("Sensor ESP32-001", TipoFonte.Sensor);
        Db.Fontes.AddRange(oficial, outra, fonteSensor);
        Db.SaveChanges();
        OficialId = oficial.Id; OutraOficialId = outra.Id; FonteDoSensorId = fonteSensor.Id;

        Sensor = new Sensor(fonteSensor.Id, "ESP32-001", -23.5650, -46.6500, 80);
        Db.Sensores.Add(Sensor);
        Db.SaveChanges();

        var opt = Microsoft.Extensions.Options.Options.Create(Opcoes);
        Risco = new RiscoService(Db, new EstimadorPorHistorico(), Relogio);
        var geradorDeAlertas = new GeradorDeAlertas(Db, Notificador, Risco, Relogio);
        var gestor = new GestorDeOcorrencias(Db, geradorDeAlertas, Relogio, opt);

        Sensores = new SensorService(Db, new DetectorPorLimiteConfirmado(2), gestor, Relogio, opt);
        Relatos = new RelatoService(Db, Analisador, Fotos, Relogio, opt);
        Ocorrencias = new OcorrenciaService(Db, gestor, Relogio, opt);
        Locais = new LocalService(Db, Risco, Relogio);
        Usuarios = new UsuarioService(Db, Relogio);
        Veiculos = new VeiculoService(Db, Relogio);
        Alertas = new AlertaService(Db);
    }

    public async Task<int> NovoUsuario(string nome = "Ana") => (await Usuarios.CriarAsync(new(nome, true))).Id;

    public Task<LocalResponse> LocalPerto(int usuarioId, double lat = -23.5650, double lng = -46.6500, int raio = 500) =>
        Locais.CriarAsync(usuarioId, new CriarLocalRequest("Casa", lat, lng, raio));

    public async Task Leituras(params int[] niveis)
    {
        foreach (var n in niveis)
        {
            Relogio.Avancar(TimeSpan.FromMinutes(1));
            await Sensores.RegistrarLeituraAsync(new LeituraRequest("ESP32-001", n));
        }
    }

    public Task<RelatoResponse> Relatar(int usuarioId, double lat = -23.60, double lng = -46.70, byte[]? foto = null,
        bool fotoAtual = true, DateTime? ocorridoEm = null) =>
        Relatos.EnviarAsync(usuarioId, new NovoRelato(lat, lng, "agua na rua", TipoEvento.AguaSobreAVia, ocorridoEm, fotoAtual, foto));

    public void Dispose()
    {
        Db.Dispose();
        _conexao.Dispose();
    }
}
