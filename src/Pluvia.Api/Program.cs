using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using Pluvia.Api.Data;
using Pluvia.Api.Infra;
using Pluvia.Api.Options;
using Pluvia.Api.Services;
using Pluvia.Domain.Regras;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PluviaOptions>(builder.Configuration.GetSection(PluviaOptions.Secao));

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "Pluvia API", Version = "v1",
        Description = "Alertas de alagamento colaborativos. Identificação simulada pelo cabeçalho X-Usuario-Id; " +
                      "ações oficiais exigem X-Chave-Oficial." });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) o.IncludeXmlComments(xml);

    // Botão "Authorize": permite informar os cabeçalhos simulados direto no Swagger.
    foreach (var (nome, descricao) in new[]
    {
        ("X-Usuario-Id", "Id devolvido por POST /api/usuarios."),
        ("X-Chave-Oficial", "Só para ações oficiais e moderação (chave de demonstração em appsettings.json)."),
    })
    {
        o.AddSecurityDefinition(nome, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Name = nome, Description = descricao,
        });
        o.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = nome } }] = Array.Empty<string>(),
        });
    }
});

// O banco fica ao lado do código, não no diretório de onde o comando foi executado.
var conexao = builder.Configuration.GetConnectionString("Pluvia") ?? "Data Source=pluvia.db";
var arquivo = conexao.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();
if (!Path.IsPathRooted(arquivo)) conexao = $"Data Source={Path.Combine(builder.Environment.ContentRootPath, arquivo)}";
builder.Services.AddDbContext<PluviaDbContext>(o => o.UseSqlite(conexao));

// Injeção de dependência: regras e serviços só conhecem interfaces.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDetectorDeAlagamento>(sp =>
    new DetectorPorLimiteConfirmado(sp.GetRequiredService<IOptions<PluviaOptions>>().Value.LeiturasParaConfirmar));
builder.Services.AddSingleton<IEstimadorDeRisco, EstimadorPorHistorico>();
builder.Services.AddSingleton<IAnalisadorDeImagem, AnalisadorDeImagemSimulado>();
builder.Services.AddSingleton<IArmazenamentoDeFotos, ArmazenamentoLocalDeFotos>();
builder.Services.AddScoped<INotificador, NotificadorEmLog>();

builder.Services.AddScoped<IRiscoService, RiscoService>();
builder.Services.AddScoped<IGeradorDeAlertas, GeradorDeAlertas>();
builder.Services.AddScoped<IGestorDeOcorrencias, GestorDeOcorrencias>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IFonteService, FonteService>();
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IRelatoService, RelatoService>();
builder.Services.AddScoped<IOcorrenciaService, OcorrenciaService>();
builder.Services.AddScoped<ILocalService, LocalService>();
builder.Services.AddScoped<IAlertaService, AlertaService>();
builder.Services.AddScoped<IVeiculoService, VeiculoService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorDeExcecoes>();

var app = builder.Build();

using (var escopo = app.Services.CreateScope())
{
    var db = escopo.ServiceProvider.GetRequiredService<PluviaDbContext>();
    await db.Database.MigrateAsync();
    await DbInicializador.PopularAsync(db);
}

// Números com ponto decimal (-23.5651) em qualquer máquina. Sem isso, num Windows em pt-BR
// os campos de formulário (multipart) leem "-23.5651" como -235651 e o relato volta 400.
var invariante = CultureInfo.InvariantCulture;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(invariante),
    SupportedCultures = new[] { invariante },
    SupportedUICultures = new[] { invariante },
});

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();

public partial class Program { }
