using System.Net;
using System.Text.Json;

namespace GameCatalog.Tests.App
{
    public class HealthControllerTest : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public HealthControllerTest(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        [Trait("Controller", "Health")]
        public async Task Live_DeveRetornar200ComOStatusDaApi()
        {
            // Arrange
            using var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/health/live");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var documento = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            Assert.Equal("Healthy", documento.RootElement.GetProperty("status").GetString());

            var checks = documento.RootElement.GetProperty("checks").EnumerateArray().ToList();

            Assert.Single(checks);
            Assert.Equal("self", checks[0].GetProperty("name").GetString());
        }

        [Fact]
        [Trait("HealthCheck", "Live")]
        public async Task HealthCheckLive_DeveResponderNoEndpointMapeado()
        {
            // Arrange
            using var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health/live");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        [Trait("Documentacao", "Swagger")]
        public async Task Swagger_DeveExporODocumentoOpenApi()
        {
            // Arrange
            using var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/v1/swagger.json");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var conteudo = await response.Content.ReadAsStringAsync();

            Assert.Contains("/api/estudio", conteudo);
            Assert.Contains("/api/jogo", conteudo);
            //Descricoes vindas das Swagger Annotations
            Assert.Contains("Lista os jogos de forma paginada", conteudo);
            Assert.Contains("Aplicar desconto no jogo", conteudo);
        }
    }
}
