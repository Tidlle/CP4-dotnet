using GameCatalog.API.Application.Interfaces;
using GameCatalog.API.Application.UseCases;
using GameCatalog.API.Domain.Interfaces;
using GameCatalog.API.Infrastructure.Data;
using GameCatalog.API.Infrastructure.Data.Repositories;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

namespace GameCatalog.API.Infrastructure.IoC
{
    public static class Bootstrap
    {
        /// <summary>
        /// Tracing e metricas exportados para o Application Insights (Azure Monitor).
        /// So e ativado quando existe connection string configurada, assim a API
        /// continua subindo normalmente em ambiente local e durante os testes.
        /// </summary>
        public static void AddTelemetria(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["ApplicationInsights:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
                return;

            services.AddOpenTelemetry()
                .UseAzureMonitor(options =>
                {
                    options.ConnectionString = connectionString;
                });
        }

        /// <summary>
        /// Log estruturado em console e em arquivo com rotacao diaria.
        /// </summary>
        public static void AddLogApi(this IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(AppContext.BaseDirectory, "logs", "api-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}{NewLine}    {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            services.AddSerilog();
        }

        public static void AddIoC(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationContext>(options =>
            {
                options.UseOracle(configuration.GetConnectionString("Oracle"));
            });

            services.AddTransient<IEstudioRepository, EstudioRepository>();
            services.AddTransient<IJogoRepository, JogoRepository>();

            services.AddTransient<IEstudioUseCase, EstudioUseCase>();
            services.AddTransient<IJogoUseCase, JogoUseCase>();
        }
    }
}
