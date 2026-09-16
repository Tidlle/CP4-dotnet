using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Domain.Entities;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace GameCatalog.Tests.App
{
    public class EstudioControllerTest : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EstudioControllerTest(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Cada teste usa um IP proprio (X-Forwarded-For) porque o rate limit
        /// da API e particionado por cliente.
        /// </summary>
        private static HttpRequestMessage NovaRequisicao(HttpMethod metodo, string url, string ip)
        {
            var requisicao = new HttpRequestMessage(metodo, url);
            requisicao.Headers.Add("X-Forwarded-For", ip);

            return requisicao;
        }

        [Fact]
        [Trait("Controller", "Estudios")]
        public async Task Get_ComDados_DeveRetornar200()
        {
            // Arrange
            var estudios = new List<EstudioEntity> {
                new EstudioEntity { Id = 1, Nome = "Pixel Forge Studios", Pais = "Brasil", AnoFundacao = 2011 },
                new EstudioEntity { Id = 2, Nome = "Sakura Interactive", Pais = "Japao", AnoFundacao = 1987 }
            };

            _factory.EstudioUseCaseMock
                .Setup(x => x.ObterTodosEstudios(1, 10, null))
                .Returns(new PaginacaoDto<EstudioEntity>(estudios, 1, 10, 23));

            using var client = _factory.CreateClient();

            // Act
            var response = await client.SendAsync(NovaRequisicao(HttpMethod.Get, "/api/estudio", "200.0.0.1"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var paginacao = await response.Content.ReadFromJsonAsync<PaginacaoDto<EstudioEntity>>();

            Assert.NotNull(paginacao);
            Assert.Equal(2, paginacao!.Dados.Count());
            Assert.Equal(23, paginacao.TotalRegistros);
            Assert.Equal(3, paginacao.TotalPaginas);
        }

        [Fact]
        [Trait("Controller", "Estudios")]
        public async Task Get_SemDados_DeveRetornar204()
        {
            // Arrange
            _factory.EstudioUseCaseMock
                .Setup(x => x.ObterTodosEstudios(It.IsAny<int>(), It.IsAny<int>(), "vazio"))
                .Returns(new PaginacaoDto<EstudioEntity>(new List<EstudioEntity>(), 1, 10, 0));

            using var client = _factory.CreateClient();

            // Act
            var response = await client.SendAsync(NovaRequisicao(HttpMethod.Get, "/api/estudio?Busca=vazio", "200.0.0.2"));

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        [Trait("Controller", "Estudios")]
        public async Task GetPorId_ComIdInexistente_DeveRetornar404()
        {
            // Arrange
            _factory.EstudioUseCaseMock
                .Setup(x => x.ObterUmEstudio(9999))
                .Returns((EstudioEntity?)null);

            using var client = _factory.CreateClient();

            // Act
            var response = await client.SendAsync(NovaRequisicao(HttpMethod.Get, "/api/estudio/9999", "200.0.0.3"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [Trait("Controller", "Estudios")]
        public async Task Post_QuandoARegraDeNegocioFalha_DeveRetornar400()
        {
            // Arrange
            _factory.EstudioUseCaseMock
                .Setup(x => x.AdicionarEstudio(It.IsAny<PostEstudioDto>()))
                .Throws(new ArgumentException("O ano de fundacao deve estar entre 1958 e 2026."));

            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", "200.0.0.4");

            var model = new PostEstudioDto { Nome = "Estudio Invalido", Pais = "Brasil", AnoFundacao = 1800 };

            // Act
            var response = await client.PostAsJsonAsync("/api/estudio", model);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [Trait("Controller", "Estudios")]
        public async Task Post_ComPayloadInvalido_DeveRetornar400()
        {
            // Arrange - Nome com menos de 3 caracteres viola o DataAnnotation do DTO
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", "200.0.0.5");

            var model = new PostEstudioDto { Nome = "X", Pais = "Brasil", AnoFundacao = 2011 };

            // Act
            var response = await client.PostAsJsonAsync("/api/estudio", model);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
