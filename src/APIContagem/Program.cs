using APIContagem;
using APIContagem.Data;
using APIContagem.Models;
using APIContagem.Tracing;
using Grafana.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

using var connectionRedis = ConnectionMultiplexer.Connect(
    builder.Configuration.GetConnectionString("Redis")!);
builder.Services.AddSingleton(connectionRedis);

builder.Services.AddDbContext<ContagemPostgresContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("BaseContagemPostgres"),
        o => o.UseNodaTime());
});

builder.Services.AddDbContext<ContagemMySqlContext>(options =>
{
    options.UseMySQL(
        builder.Configuration.GetConnectionString("BaseContagemMySql")!);
});

builder.Services.AddScoped<ContagemRepository>();
builder.Services.AddScoped<ContagemRegressivaRepository>();
builder.Services.AddSingleton<Contador>();

builder.Services.AddCors();

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
        serviceVersion: OpenTelemetryExtensions.ServiceVersion);
builder.Services.AddOpenTelemetry()
    .WithTracing((traceBuilder) =>
    {
        traceBuilder
            .AddSource(OpenTelemetryExtensions.ServiceName)
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddNpgsql() // PostgreSQL
            .AddConnectorNet() // MySQL
            .AddEntityFrameworkCoreInstrumentation(cfg =>
            {
                cfg.SetDbStatementForText = true;
            })
            .AddRedisInstrumentation(connectionRedis);

        if (Convert.ToBoolean(builder.Configuration["OtlpExporter:UseGrafana"]))
            traceBuilder.UseGrafana();
        else
            traceBuilder.AddOtlpExporter(cfg =>
            {
                cfg.Endpoint = new Uri(builder.Configuration["OtlpExporter:Endpoint"]!);
            });
        
        traceBuilder.AddConsoleExporter();
    });

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "API de Contagem de Acessos";
    options.Theme = ScalarTheme.BluePlanet;
    options.DarkMode = true;
});

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseHttpsRedirection();

Lock ContagemLock = new();

app.MapGet("/contador", (Contador contador,
    ContagemRepository repository) =>
{
    using var activity1 = OpenTelemetryExtensions.ActivitySource
        .StartActivity("GerarValorContagem")!;

    int valorAtualContador;
    using (ContagemLock.EnterScope())
    {
        contador.Incrementar();
        valorAtualContador = contador.ValorAtual;
    }
    activity1.SetTag("valorAtual", valorAtualContador);
    app.Logger.LogInformation($"Contador - Valor atual: {valorAtualContador}");

    var resultado = new ResultadoContador()
    {
        ValorAtual = valorAtualContador,
        Local = contador.Local,
        Kernel = contador.Kernel,
        Mensagem = "APIContagem - testes com PostgresSQL",
        Framework = contador.Framework
    };
    activity1.Stop();

    using var activity2 = OpenTelemetryExtensions.ActivitySource
        .StartActivity("RegistrarRetornarValorContagem")!;

    repository.Insert(resultado);
    app.Logger.LogInformation($"Registro inserido com sucesso! Valor: {valorAtualContador}");

    activity2.SetTag("valorAtual", valorAtualContador);
    activity2.SetTag("horario", $"{DateTime.UtcNow.AddHours(-3):HH:mm:ss}");

    return resultado;
})
.Produces<ResultadoContador>();

app.MapGet("/contador/regressivo", (Contador contador,
    ContagemRegressivaRepository repository) =>
{
        using var activity1 = OpenTelemetryExtensions.ActivitySource
            .StartActivity("GerarValorContagemRegressiva")!;

        int valorAtualContador;
        using (ContagemLock.EnterScope())
        {
            contador.Decrementar();
            valorAtualContador = contador.ValorAtualRegressivo;
        }
        activity1.SetTag("valorAtualRegressivo", valorAtualContador);
        app.Logger.LogInformation($"Contador Regressivo - Valor atual: {valorAtualContador}");

        var resultado = new ResultadoContador()
        {
            ValorAtual = valorAtualContador,
            Local = contador.Local,
            Kernel = contador.Kernel,
            Mensagem = "APIContagem - testes com MySQL",
            Framework = contador.Framework
        };
        activity1.Stop();

        using var activity2 = OpenTelemetryExtensions.ActivitySource
            .StartActivity("RegistrarRetornarValorContagemRegressiva")!;

        repository.Insert(resultado);
        app.Logger.LogInformation($"Registro inserido com sucesso! Valor regressivo: {valorAtualContador}");

        activity2.SetTag("valorAtualRegressivo", valorAtualContador);
        activity2.SetTag("horario", $"{DateTime.UtcNow.AddHours(-3):HH:mm:ss}");

        return resultado;
})
.Produces<ResultadoContador>();

app.MapGet("/contador/redis", (Contador contador,
    ConnectionMultiplexer connectionRedis) =>
{
        using var activity1 = OpenTelemetryExtensions.ActivitySource
            .StartActivity("GerarValorContagemRedis")!;

        int valorAtualContador = (int)connectionRedis.GetDatabase().StringIncrement("APIContagem");
        activity1.SetTag("valorAtualRedis", valorAtualContador);
        activity1.SetTag("horario", $"{DateTime.UtcNow.AddHours(-3):HH:mm:ss}");

        app.Logger.LogInformation($"Contador Redis - Valor atual: {valorAtualContador}");

        var resultado = new ResultadoContador()
        {
            ValorAtual = valorAtualContador,
            Local = contador.Local,
            Kernel = contador.Kernel,
            Mensagem = "APIContagem - testes com Redis",
            Framework = contador.Framework
        };
        activity1.Stop();

        return resultado;
})
.Produces<ResultadoContador>();

app.Run();