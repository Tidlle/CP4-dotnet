using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Enums;
using GameCatalog.API.Infrastructure.Data;
using GameCatalog.API.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GameCatalog.Tests.App
{
    public class JogoRepositoryTest
    {
        private readonly DbContextOptions<ApplicationContext> _options;
        private readonly ApplicationContext _applicationContext;
        private readonly JogoRepository _jogoRepository;

        public JogoRepositoryTest()
        {
            _options = new DbContextOptionsBuilder<ApplicationContext>()
                .UseInMemoryDatabase(databaseName: $"JogoTestDatabase_{Guid.NewGuid()}")
                .Options;

            _applicationContext = new ApplicationContext(_options);

            _applicationContext.Database.EnsureDeleted();
            _applicationContext.Database.EnsureCreated();

            _jogoRepository = new JogoRepository(_applicationContext);
        }

        private void PopularBase()
        {
            var jogos = new List<JogoEntity> {
                new JogoEntity { Titulo = "Chronicles of Aether", Genero = GeneroJogo.Rpg, Preco = 199.90, Nota = 9.1, EstudioId = 1, DataLancamento = new DateTime(2020, 2, 15) },
                new JogoEntity { Titulo = "Deep Harbor", Genero = GeneroJogo.Aventura, Preco = 89.90, Nota = 7.8, EstudioId = 1, DataLancamento = new DateTime(2019, 8, 1) },
                new JogoEntity { Titulo = "Neon Drift", Genero = GeneroJogo.Acao, Preco = 149.90, Nota = 8.5, EstudioId = 2, DataLancamento = new DateTime(2021, 5, 10) },
                new JogoEntity { Titulo = "Runes of Valoria", Genero = GeneroJogo.Rpg, Preco = 219.90, Nota = 9.4, EstudioId = 2, DataLancamento = new DateTime(2024, 10, 15) }
            };

            _applicationContext.Jogo.AddRange(jogos);
            _applicationContext.SaveChanges();
        }

        [Fact]
        [Trait("Repository", "Jogos")]
        public void ObterTodos_DeveRetornarJogosOrdenadosPorTitulo()
        {
            // Arrange
            PopularBase();

            // Act
            var resultado = _jogoRepository.ObterTodos(1, 10, null, null).ToList();

            // Assert
            Assert.Equal(4, resultado.Count);
            Assert.Equal("Chronicles of Aether", resultado.First().Titulo);
        }

        [Fact]
        [Trait("Repository", "Jogos")]
        public void ObterTodos_DeveRespeitarAPaginacao()
        {
            // Arrange
            PopularBase();

            // Act
            var primeiraPagina = _jogoRepository.ObterTodos(1, 2, null, null).ToList();
            var segundaPagina = _jogoRepository.ObterTodos(2, 2, null, null).ToList();

            // Assert
            Assert.Equal(2, primeiraPagina.Count);
            Assert.Equal(2, segundaPagina.Count);
            Assert.Empty(primeiraPagina.Select(x => x.Id).Intersect(segundaPagina.Select(x => x.Id)));
        }

        [Fact]
        [Trait("Repository", "Jogos")]
        public void ObterTodos_ComFiltroDeGenero_DeveRetornarSomenteOGenero()
        {
            // Arrange
            PopularBase();

            // Act
            var resultado = _jogoRepository.ObterTodos(1, 10, GeneroJogo.Rpg, null).ToList();

            // Assert
            Assert.Equal(2, resultado.Count);
            Assert.All(resultado, jogo => Assert.Equal(GeneroJogo.Rpg, jogo.Genero));
        }

        [Fact]
        [Trait("Repository", "Jogos")]
        public void ObterTodos_ComFiltroDeEstudio_DeveRetornarSomenteOsJogosDoEstudio()
        {
            // Arrange
            PopularBase();

            // Act
            var resultado = _jogoRepository.ObterTodos(1, 10, null, 2).ToList();

            // Assert
            Assert.Equal(2, resultado.Count);
            Assert.All(resultado, jogo => Assert.Equal(2, jogo.EstudioId));
        }

        [Fact]
        [Trait("Repository", "Jogos")]
        public void ObterTotal_DeveConsiderarOsFiltros()
        {
            // Arrange
            PopularBase();

            // Act
            var total = _jogoRepository.ObterTotal(null, null);
            var totalRpg = _jogoRepository.ObterTotal(GeneroJogo.Rpg, null);

            // Assert
            Assert.Equal(4, total);
            Assert.Equal(2, totalRpg);
        }

        [Fact]
        [Trait("Repository", "Jogos")]
        public void AtualizarPreco_DeveAlterarOPrecoDoJogo()
        {
            // Arrange
            var jogo = new JogoEntity { Titulo = "Neon Drift", Genero = GeneroJogo.Acao, Preco = 200, EstudioId = 1 };

            _applicationContext.Jogo.Add(jogo);
            _applicationContext.SaveChanges();

            // Act
            var resultado = _jogoRepository.AtualizarPreco(jogo.Id, 150);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(150, resultado.Preco);
            Assert.Equal(150, _applicationContext.Jogo.First(x => x.Id == jogo.Id).Preco);
        }

        [Fact]
        [Trait("Repository", "Jogos")]
        public void AdicionarPlataforma_ComJogoInexistente_DeveRetornarNull()
        {
            // Act
            var resultado = _jogoRepository.AdicionarPlataforma(new PlataformaEntity { Nome = "PC" }, 9999);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        [Trait("Repository", "Jogos")]
        public void AdicionarPlataforma_DeveVincularAPlataformaNoJogo()
        {
            // Arrange
            var jogo = new JogoEntity { Titulo = "Neon Drift", Genero = GeneroJogo.Acao, Preco = 100, EstudioId = 1 };

            _applicationContext.Jogo.Add(jogo);
            _applicationContext.SaveChanges();

            // Act
            var resultado = _jogoRepository.AdicionarPlataforma(new PlataformaEntity { Nome = "PlayStation 5" }, jogo.Id);

            // Assert
            Assert.NotNull(resultado);

            var jogoNoDb = _applicationContext
                .Jogo
                .Include(x => x.Plataformas)
                .First(x => x.Id == jogo.Id);

            Assert.Single(jogoNoDb.Plataformas!);
            Assert.Equal("PlayStation 5", jogoNoDb.Plataformas!.First().Nome);
        }
    }
}
