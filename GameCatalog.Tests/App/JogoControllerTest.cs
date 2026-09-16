using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Enums;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameCatalog.Tests.App
{
    public class JogoControllerTest : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public JogoControllerTest(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        //A API expoe o enum de genero como texto, entao a desserializacao do teste precisa do mesmo conversor
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private static HttpRequestMessage NovaRequisicao(HttpMethod metodo, string url, string ip)
        {
            var requisicao = new HttpRequestMessage(metodo, url);
            requisicao.Headers.Add("X-Forwarded-For", ip);

            return requisicao;
        }

        private static PaginacaoDto<JogoEntity> PaginaDeJogos() => new(
            new List<JogoEntity>
            {
                new JogoEntity { Id = 1, Titulo = "Neon Drift", Genero = GeneroJogo.Acao, Preco = 149.90, EstudioId = 1 },
                new JogoEntity { Id = 2, Titulo = "Runes of Valoria", Genero = GeneroJogo.Rpg, Preco = 219.90, EstudioId = 2 }
            },
            pageNumber: 1,
            pageSize: 10,
            totalRegistros: 47);

        [Fact]
        [Trait("Controller", "Jogos")]
        public async Task Get_ComDados_DeveRetornar200ComOsMetadadosDaPaginacao()
        {
            // Arrange
            _factory.JogoUseCaseMock
                .Setup(x => x.ObterTodosJogos(1, 10, null, null))
                .Returns(PaginaDeJogos());

            using var client = _factory.CreateClient();

            // Act
            var response = await client.SendAsync(NovaRequisicao(HttpMethod.Get, "/api/jogo", "201.0.0.1"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var paginacao = await response.Content.ReadFromJsonAsync<PaginacaoDto<JogoEntity>>(_jsonOptions);

            Assert.NotNull(paginacao);
            Assert.Equal(47, paginacao!.TotalRegistros);
            Assert.Equal(5, paginacao.TotalPaginas);
            Assert.False(paginacao.TemPaginaAnterior);
            Assert.True(paginacao.TemProximaPagina);
        }

        [Fact]
        [Trait("Controller", "Jogos")]
        public async Task Get_ComFiltroDeGenero_DeveRepassarOFiltroParaOUseCase()
        {
            // Arrange
            _factory.JogoUseCaseMock
                .Setup(x => x.ObterTodosJogos(2, 5, GeneroJogo.Rpg, 3))
                .Returns(PaginaDeJogos());

            using var client = _factory.CreateClient();

            // Act
            var response = await client.SendAsync(
                NovaRequisicao(HttpMethod.Get, "/api/jogo?PageNumber=2&PageSize=5&Genero=Rpg&EstudioId=3", "201.0.0.2"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            _factory.JogoUseCaseMock.Verify(x => x.ObterTodosJogos(2, 5, GeneroJogo.Rpg, 3), Times.Once);
        }

        [Fact]
        [Trait("Controller", "Jogos")]
        public async Task Get_SemDados_DeveRetornar204()
        {
            // Arrange
            _factory.JogoUseCaseMock
                .Setup(x => x.ObterTodosJogos(It.IsAny<int>(), It.IsAny<int>(), GeneroJogo.Terror, null))
                .Returns(new PaginacaoDto<JogoEntity>(new List<JogoEntity>(), 1, 10, 0));

            using var client = _factory.CreateClient();

            // Act
            var response = await client.SendAsync(NovaRequisicao(HttpMethod.Get, "/api/jogo?Genero=Terror", "201.0.0.3"));

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        [Trait("Controller", "Jogos")]
        public async Task Post_ComEstudioInexistente_DeveRetornar404()
        {
            // Arrange
            _factory.JogoUseCaseMock
                .Setup(x => x.AdicionarJogo(It.IsAny<PostJogoDto>()))
                .Returns((JogoEntity?)null);

            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", "201.0.0.4");

            var model = new PostJogoDto
            {
                Titulo = "Jogo Sem Estudio",
                Genero = GeneroJogo.Acao,
                Preco = 100,
                DataLancamento = new DateTime(2023, 1, 1),
                EstudioId = 9999
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/jogo", model);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [Trait("Controller", "Jogos")]
        public async Task PatchDesconto_ComPercentualInvalido_DeveRetornar400()
        {
            // Arrange
            _factory.JogoUseCaseMock
                .Setup(x => x.AplicarDesconto(1, 95))
                .Throws(new ArgumentException("O percentual de desconto nao pode ultrapassar 90%."));

            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", "201.0.0.5");

            // Act
            var response = await client.PatchAsJsonAsync("/api/jogo/1/desconto", new PostDescontoDto { Percentual = 95 });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var mensagem = await response.Content.ReadAsStringAsync();
            Assert.Contains("90", mensagem);
        }

        [Fact]
        [Trait("Controller", "Jogos")]
        public async Task PatchDesconto_ComPercentualValido_DeveRetornar200()
        {
            // Arrange
            _factory.JogoUseCaseMock
                .Setup(x => x.AplicarDesconto(1, 25))
                .Returns(new JogoEntity { Id = 1, Titulo = "Neon Drift", Preco = 150 });

            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", "201.0.0.6");

            // Act
            var response = await client.PatchAsJsonAsync("/api/jogo/1/desconto", new PostDescontoDto { Percentual = 25 });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var jogo = await response.Content.ReadFromJsonAsync<JogoEntity>(_jsonOptions);

            Assert.NotNull(jogo);
            Assert.Equal(150, jogo!.Preco);
        }

        [Fact]
        [Trait("RateLimit", "Jogos")]
        public async Task RateLimit_AoUltrapassarOLimite_DeveRetornar429()
        {
            // Arrange - a politica permite 5 requisicoes a cada 20 segundos por cliente
            _factory.JogoUseCaseMock
                .Setup(x => x.ObterTodosJogos(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<GeneroJogo?>(), It.IsAny<int?>()))
                .Returns(PaginaDeJogos());

            using var client = _factory.CreateClient();

            var statusCodes = new List<HttpStatusCode>();

            // Act
            for (var i = 0; i < 7; i++)
            {
                var response = await client.SendAsync(NovaRequisicao(HttpMethod.Get, "/api/jogo", "201.0.0.7"));
                statusCodes.Add(response.StatusCode);
            }

            // Assert
            Assert.Equal(5, statusCodes.Count(x => x == HttpStatusCode.OK));
            Assert.Equal(2, statusCodes.Count(x => x == HttpStatusCode.TooManyRequests));
        }

        [Fact]
        [Trait("RateLimit", "Jogos")]
        public async Task RateLimit_DeveSerIndependentePorCliente()
        {
            // Arrange
            _factory.JogoUseCaseMock
                .Setup(x => x.ObterTodosJogos(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<GeneroJogo?>(), It.IsAny<int?>()))
                .Returns(PaginaDeJogos());

            using var client = _factory.CreateClient();

            // Act - o primeiro cliente estoura a janela
            for (var i = 0; i < 6; i++)
                await client.SendAsync(NovaRequisicao(HttpMethod.Get, "/api/jogo", "201.0.0.8"));

            var clienteBloqueado = await client.SendAsync(NovaRequisicao(HttpMethod.Get, "/api/jogo", "201.0.0.8"));
            var outroCliente = await client.SendAsync(NovaRequisicao(HttpMethod.Get, "/api/jogo", "201.0.0.9"));

            // Assert
            Assert.Equal(HttpStatusCode.TooManyRequests, clienteBloqueado.StatusCode);
            Assert.Equal(HttpStatusCode.OK, outroCliente.StatusCode);
        }

        [Fact]
        [Trait("Compressao", "Jogos")]
        public async Task Compressao_ComAcceptEncoding_DeveRetornarRespostaComprimida()
        {
            // Arrange
            _factory.JogoUseCaseMock
                .Setup(x => x.ObterTodosJogos(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<GeneroJogo?>(), It.IsAny<int?>()))
                .Returns(PaginaDeJogos());

            using var client = _factory.CreateClient();

            var requisicao = NovaRequisicao(HttpMethod.Get, "/api/jogo", "202.0.0.1");
            requisicao.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

            // Act
            var response = await client.SendAsync(requisicao);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("br", response.Content.Headers.ContentEncoding);
        }
    }
}
