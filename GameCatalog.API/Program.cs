using GameCatalog.API.Infrastructure.IoC;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Filters;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

//Tracing e metricas no Application Insights
builder.Services.AddTelemetria(builder.Configuration);

//Log estruturado (console + arquivo)
builder.Services.AddLogApi();

//Injecao de dependencia das camadas (DbContext, repositorios e use cases)
Bootstrap.AddIoC(builder.Services, builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        //Expoe o enum de genero como texto (Acao, Rpg, ...) em vez de numero
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.ExampleFilters();
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

//Adicionar compressao de dados
builder.Services.AddResponseCompression(options =>
{
    //gzip
    //br - Brotli
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();

    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.SmallestSize;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.SmallestSize;
});

// Add Rate Limiter - janela fixa particionada por cliente (IP de origem)
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("politica_rate_limit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ObterIpDoCliente(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(20),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "20";

        await context.HttpContext.Response.WriteAsync(
            "Limite de 5 requisicoes a cada 20 segundos excedido. Tente novamente em instantes.",
            cancellationToken);
    };
});

builder.Services.AddHealthChecks()
    // Liveness
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("API em execucao"),
        tags: ["live"])
    // Readness
    .AddOracle(
        connectionString: builder.Configuration.GetConnectionString("Oracle") ?? "",
        name: "oracle",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});

app.UseAuthorization();

app.UseRateLimiter();
app.UseResponseCompression();

app.MapControllers();

app.Run();

//Usa o IP de origem como chave do rate limit e considera o X-Forwarded-For quando a API esta atras de proxy
static string ObterIpDoCliente(HttpContext httpContext)
{
    var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

    if (!string.IsNullOrWhiteSpace(forwardedFor))
        return forwardedFor.Split(',')[0].Trim();

    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
}

public partial class Program { }
