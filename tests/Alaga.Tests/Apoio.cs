using Alaga.Api.Data;
using Alaga.Api.Dtos;
using Alaga.Api.Options;
using Alaga.Api.Services;
using Alaga.Domain.Entities;
using Alaga.Domain.Enums;
using Alaga.Domain.Regras;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Alaga.Tests;

public class RelogioFalso : TimeProvider
{
    private DateTimeOffset _agora = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => _agora;
    public void Avancar(TimeSpan t) => _agora += t;
}

public class NotificadorFalso : INotificador
{
    public List<Alerta> Enviados { get; } = new();
    public Task NotificarAsync(Alerta alerta, CancellationToken ct = default)
    {
        Enviados.Add(alerta);
        return Task.CompletedTask;
    }
}

public class AnalisadorFalso : IAnalisadorDeImagem
{
    public ClassificacaoImagem Resultado { get; set; } = ClassificacaoImagem.IndicioDeAguaSobreAVia;
    public Task<AvaliacaoImagem> AnalisarAsync(byte[] imagem, CancellationToken ct = default) =>
        Task.FromResult(new AvaliacaoImagem(Resultado, 0.9, "falso"));
}

public class ArmazenamentoFalso : IArmazenamentoDeFotos
{
    public int Salvas { get; private set; }
    public int Excluidas { get; private set; }
    public byte[]? UltimoConteudo { get; private set; }

    public Task<string> SalvarAsync(byte[] conteudo, string extensao, CancellationToken ct = default)
    {
        Salvas++;
        UltimoConteudo = conteudo;
        return Task.FromResult($"foto{Salvas}{extensao}");
    }

    public Task<(Stream Conteudo, string ContentType)?> AbrirAsync(string nome, CancellationToken ct = default) =>
        Task.FromResult<(Stream, string)?>((new MemoryStream(new byte[] { 1 }), "image/png"));

    public Task ExcluirAsync(string nome, CancellationToken ct = default)
    {
        Excluidas++;
        return Task.CompletedTask;
    }
}

/// <summary>Imagens mínimas, mas estruturalmente válidas, com metadados para testar a remoção.</summary>
public static class Imagens
{
    public const string SegredoGps = "GPS=-23.5,-46.6";

    public static byte[] Png(bool comMetadados = true)
    {
        var o = new List<byte> { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        Chunk(o, "IHDR", new byte[13]);
        if (comMetadados) Chunk(o, "tEXt", System.Text.Encoding.ASCII.GetBytes(SegredoGps));
        Chunk(o, "IDAT", new byte[20]);
        Chunk(o, "IEND", Array.Empty<byte>());
        return o.ToArray();
    }

    public static byte[] Jpeg(bool comExif = true)
    {
        var o = new List<byte> { 0xFF, 0xD8 };
        Segmento(o, 0xE0, new byte[14]);
        if (comExif) Segmento(o, 0xE1, System.Text.Encoding.ASCII.GetBytes("Exif\0\0" + SegredoGps));
        Segmento(o, 0xDB, new byte[67]);
        Segmento(o, 0xDA, new byte[10]);
        o.AddRange(new byte[40]);   // dados da imagem
        o.AddRange(new byte[] { 0xFF, 0xD9 });
        return o.ToArray();
    }

    private static void Chunk(List<byte> o, string tipo, byte[] dados)
    {
        o.AddRange(new[] { (byte)(dados.Length >> 24), (byte)(dados.Length >> 16), (byte)(dados.Length >> 8), (byte)dados.Length });
        o.AddRange(System.Text.Encoding.ASCII.GetBytes(tipo));
        o.AddRange(dados);
        o.AddRange(new byte[4]);   // CRC (não verificado pelo sanitizador)
    }

    private static void Segmento(List<byte> o, byte marcador, byte[] dados)
    {
        var tam = dados.Length + 2;
        o.AddRange(new[] { (byte)0xFF, marcador, (byte)(tam >> 8), (byte)tam });
        o.AddRange(dados);
    }

    public static bool Contem(byte[] bytes, string texto) =>
        System.Text.Encoding.ASCII.GetString(bytes).Contains(texto);
}

/// <summary>Banco SQLite em memória + todos os serviços montados com dependências falsas.</summary>
public sealed class Ambiente : IDisposable
{
    private readonly SqliteConnection _conexao;
    public AlagaDbContext Db { get; }
    public RelogioFalso Relogio { get; } = new();
    public NotificadorFalso Notificador { get; } = new();
    public AnalisadorFalso Analisador { get; } = new();
    public ArmazenamentoFalso Fotos { get; } = new();
    public AlagaOptions Opcoes { get; } = new();

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
        Db = new AlagaDbContext(new DbContextOptionsBuilder<AlagaDbContext>().UseSqlite(_conexao).Options);
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
