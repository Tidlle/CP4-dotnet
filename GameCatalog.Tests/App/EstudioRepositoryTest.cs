using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Infrastructure.Data;
using GameCatalog.API.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GameCatalog.Tests.App
{
    public class EstudioRepositoryTest
    {
        private readonly DbContextOptions<ApplicationContext> _options;
        private readonly ApplicationContext _applicationContext;
        private readonly EstudioRepository _estudioRepository;

        public EstudioRepositoryTest()
        {
            _options = new DbContextOptionsBuilder<ApplicationContext>()
                .UseInMemoryDatabase(databaseName: $"EstudioTestDatabase_{Guid.NewGuid()}")
                .Options;

            _applicationContext = new ApplicationContext(_options);

            _applicationContext.Database.EnsureDeleted();
            _applicationContext.Database.EnsureCreated();

            _estudioRepository = new EstudioRepository(_applicationContext);
        }

        private void PopularBase()
        {
            var estudios = new List<EstudioEntity> {
                new EstudioEntity { Nome = "Atlantic Bytes", Pais = "Portugal", AnoFundacao = 2016 },
                new EstudioEntity { Nome = "Northern Lights Games", Pais = "Suecia", AnoFundacao = 1998 },
                new EstudioEntity { Nome = "Pixel Forge Studios", Pais = "Brasil", AnoFundacao = 2011 },
                new EstudioEntity { Nome = "Sakura Interactive", Pais = "Japao", AnoFundacao = 1987 }
            };

            _applicationContext.Estudio.AddRange(estudios);
            _applicationContext.SaveChanges();
        }

        [Fact]
        [Trait("Repository", "Estudios")]
        public void ObterTodos_DeveRetornarEstudios()
        {
            // Arrange
            PopularBase();

            // Act
            var resultado = _estudioRepository.ObterTodos(1, 10, null);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(4, resultado.Count());
        }

        [Fact]
        [Trait("Repository", "Estudios")]
        public void ObterTodos_DeveRespeitarOPageSize()
        {
            // Arrange
            PopularBase();

            // Act
            var primeiraPagina = _estudioRepository.ObterTodos(1, 2, null);

            // Assert
            Assert.Equal(2, primeiraPagina.Count());
            Assert.Equal("Atlantic Bytes", primeiraPagina.First().Nome);
        }

        [Fact]
        [Trait("Repository", "Estudios")]
        public void ObterTodos_SegundaPagina_DeveRetornarRegistrosDiferentes()
        {
            // Arrange
            PopularBase();

            // Act
            var primeiraPagina = _estudioRepository.ObterTodos(1, 2, null).ToList();
            var segundaPagina = _estudioRepository.ObterTodos(2, 2, null).ToList();

            // Assert
            Assert.Equal(2, segundaPagina.Count);
            Assert.Empty(primeiraPagina.Select(x => x.Id).Intersect(segundaPagina.Select(x => x.Id)));
        }

        [Fact]
        [Trait("Repository", "Estudios")]
        public void ObterTodos_ComBusca_DeveFiltrarOResultado()
        {
            // Arrange
            PopularBase();

            // Act
            var resultado = _estudioRepository.ObterTodos(1, 10, "Sakura");

            // Assert
            Assert.Single(resultado);
            Assert.Equal("Sakura Interactive", resultado.First().Nome);
        }

        [Fact]
        [Trait("Repository", "Estudios")]
        public void ObterTotal_DeveRetornarAQuantidadeDeRegistros()
        {
            // Arrange
            PopularBase();

            // Act
            var total = _estudioRepository.ObterTotal(null);
            var totalFiltrado = _estudioRepository.ObterTotal("Brasil");

            // Assert
            Assert.Equal(4, total);
            Assert.Equal(1, totalFiltrado);
        }

        [Fact]
        [Trait("Repository", "Estudios")]
        public void ObterUm_DeveRetornarUmEstudio()
        {
            // Arrange
            var estudio = new EstudioEntity { Nome = "Red Canyon", Pais = "Estados Unidos", AnoFundacao = 2004 };

            _applicationContext.Estudio.Add(estudio);
            _applicationContext.SaveChanges();

            // Act
            var resultado = _estudioRepository.ObterUm(estudio.Id);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(estudio.Nome, resultado.Nome);
            Assert.Equal(estudio.Pais, resultado.Pais);
            Assert.Equal(estudio.AnoFundacao, resultado.AnoFundacao);
        }

        [Fact]
        [Trait("Repository", "Estudios")]
        public void Adicionar_DeveAdicionarEstudio()
        {
            // Arrange
            var estudio = new EstudioEntity { Nome = "Pixel Forge Studios", Pais = "Brasil", AnoFundacao = 2011 };

            // Act
            var resultado = _estudioRepository.Adicionar(estudio);

            // Assert
            var idEstudio = resultado?.Id ?? -1;

            var estudioNoDb = _applicationContext.Estudio.FirstOrDefault(x => x.Id == idEstudio);

            Assert.NotNull(estudioNoDb);
            Assert.Equal(estudio.Nome, estudioNoDb.Nome);
            Assert.True(estudioNoDb.Ativo);
        }

        [Fact]
        [Trait("Repository", "Estudios")]
        public void Deletar_DeveRemoverOEstudio()
        {
            // Arrange
            var estudio = new EstudioEntity { Nome = "Estudio Temporario", Pais = "Chile", AnoFundacao = 2020 };

            _applicationContext.Estudio.Add(estudio);
            _applicationContext.SaveChanges();

            // Act
            var resultado = _estudioRepository.Deletar(estudio.Id);

            // Assert
            Assert.NotNull(resultado);
            Assert.Null(_applicationContext.Estudio.FirstOrDefault(x => x.Id == estudio.Id));
        }

        [Fact]
        [Trait("Repository", "Estudios")]
        public void Deletar_ComIdInexistente_DeveRetornarNull()
        {
            // Act
            var resultado = _estudioRepository.Deletar(9999);

            // Assert
            Assert.Null(resultado);
        }
    }
}
