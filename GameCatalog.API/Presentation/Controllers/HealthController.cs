using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Annotations;

namespace GameCatalog.API.Presentation.Controllers
{
    [Route("api/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        [HttpGet("live")]
        [SwaggerOperation(
            Summary = "Liveness da API",
            Description = "Responde se o processo da API esta de pe. Nao consulta dependencias externas."
        )]
        [SwaggerResponse(statusCode: 200, description: "API em execucao")]
        [SwaggerResponse(statusCode: 503, description: "API indisponivel")]
        public async Task<IActionResult> Live(CancellationToken ct)
        {
            var report = await _healthCheckService.CheckHealthAsync(r => r.Tags.Contains("live"), ct);

            var result = new
            {
                status = report.Status.ToString(),

                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    error = e.Value.Exception?.Message
                })
            };

            return report.Status == HealthStatus.Healthy
                ? Ok(result)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }

        [HttpGet("db")]
        [SwaggerOperation(
            Summary = "Readiness da API",
            Description = "Verifica se o banco de dados Oracle esta acessivel."
        )]
        [SwaggerResponse(statusCode: 200, description: "Banco de dados disponivel")]
        [SwaggerResponse(statusCode: 503, description: "Banco de dados indisponivel")]
        public async Task<IActionResult> DbReady(CancellationToken ct)
        {
            var report = await _healthCheckService.CheckHealthAsync(r => r.Tags.Contains("db"), ct);

            var result = new
            {
                status = report.Status.ToString(),

                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    error = e.Value.Exception?.Message
                })
            };

            return report.Status == HealthStatus.Healthy
                ? Ok(result)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }
    }
}
