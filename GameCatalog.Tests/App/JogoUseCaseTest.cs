using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Application.Mappers;
using GameCatalog.API.Application.UseCases;
using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Enums;
using GameCatalog.API.Domain.Interfaces;
using Moq;

namespace GameCatalog.Tests.App
{
    public class JogoUseCaseTest
    {
        private readonly Mock<IJogoRepository> _jogoRepository;
        private readonly Mock<IEstudioRepository> _estudioRepository;
        private readonly JogoUseCase _jogoUseCase;

        public JogoUseCaseTest()
        {
            _jogoRepository = new Mock<IJogoRepository>();
            _estudioRepository = new Mock<IEstudioRepository>();

            _jogoUseCase = new JogoUseCase(_jogoRepository.Object, _estudioRepository.Object, null);
        }

        private static PostJogoDto NovoJogoDto(int estudioId = 1) => new PostJogoDto
        {
            Titulo = "Neon Drift",
            Genero = GeneroJogo.Acao,
            Preco = 149.90,
            DataLancamento = new DateTime(2021, 5, 10),
            Nota = 8.5,
            EstudioId = estudioId
        };

        [Fact]
        [Trait("UseCase", "Jogos")]
        public void Adicionar_ComEstudioValido_DevePersistir()
        {
            //Arrange
            var jogoDto = NovoJogoDto();
            var entity = jogoDto.ToJogoEntity();

            _estudioRepository.Setup(obj => obj.Existe(1)).Returns(true);
            _jogoRepository.Setup(obj => obj.Adicionar(It.IsAny<JogoEntity>())).Returns(entity);

            //Act
            var resultado = _jogoUseCase.AdicionarJogo(jogoDto);

            //Assert
            Assert.NotNull(resultado);
            Assert.Equal(jogoDto.Titulo, resultado!.Titulo);
            Assert.Equal(jogoDto.Genero, resultado!.Genero);
            Assert.Equal(jogoDto.Preco, resultado!.Preco);
        }

        [Fact]
        [Trait("UseCase", "Jogos")]
        public void Adicionar_ComEstudioInexistente_NaoDevePersistir()
        {
            //Arrange
            var jogoDto = NovoJogoDto(estudioId: 99);

            _estudioRepository.Setup(obj => obj.Existe(99)).Returns(false);

            //Act
            var resultado = _jogoUseCase.AdicionarJogo(jogoDto);

            //Assert
            Assert.Null(resultado);

            _jogoRepository.Verify(obj => obj.Adicionar(It.IsAny<JogoEntity>()), Times.Never);
        }

        [Fact]
        [Trait("UseCase", "Jogos")]
        public void ObterTodos_DeveNormalizarAPaginacao()
        {
            //Arrange
            int pageSizeUtilizado = 0;

            _jogoRepository
                .Setup(obj => obj.ObterTodos(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<GeneroJogo?>(), It.IsAny<int?>()))
                .Callback<int, int, GeneroJogo?, int?>((pageNumber, pageSize, genero, estudioId) => pageSizeUtilizado = pageSize)
                .Returns(new List<JogoEntity>());

            //Act
            _jogoUseCase.ObterTodosJogos(1, 999, null, null);

            //Assert
            Assert.Equal(JogoUseCase.PageSizeMaximo, pageSizeUtilizado);
        }

        [Fact]
        [Trait("UseCase", "Jogos")]
        public void AplicarDesconto_DeveCalcularONovoPreco()
        {
            //Arrange
            var jogo = new JogoEntity { Id = 1, Titulo = "Neon Drift", Preco = 200 };

            _jogoRepository.Setup(obj => obj.ObterUm(1)).Returns(jogo);
            _jogoRepository
                .Setup(obj => obj.AtualizarPreco(1, It.IsAny<double>()))
                .Returns<int, double>((id, preco) => new JogoEntity { Id = id, Titulo = jogo.Titulo, Preco = preco });

            //Act - 25% de desconto sobre 200
            var resultado = _jogoUseCase.AplicarDesconto(1, 25);

            //Assert
            Assert.NotNull(resultado);
            Assert.Equal(150, resultado!.Preco);

            _jogoRepository.Verify(obj => obj.AtualizarPreco(1, 150), Times.Once);
        }

        [Fact]
        [Trait("UseCase", "Jogos")]
        public void AplicarDesconto_NoLimiteMaximo_DeveSerPermitido()
        {
            //Arrange
            var jogo = new JogoEntity { Id = 1, Titulo = "Neon Drift", Preco = 100 };

            _jogoRepository.Setup(obj => obj.ObterUm(1)).Returns(jogo);
            _jogoRepository
                .Setup(obj => obj.AtualizarPreco(1, It.IsAny<double>()))
                .Returns<int, double>((id, preco) => new JogoEntity { Id = id, Preco = preco });

            //Act
            var resultado = _jogoUseCase.AplicarDesconto(1, JogoUseCase.PercentualMaximoDesconto);

            //Assert
            Assert.NotNull(resultado);
            Assert.Equal(10, resultado!.Preco);
        }

        [Theory]
        [Trait("UseCase", "Jogos")]
        [InlineData(0)]
        [InlineData(-10)]
        [InlineData(90.01)]
        [InlineData(150)]
        public void AplicarDesconto_ComPercentualInvalido_DeveLancarExcecao(double percentual)
        {
            //Act + Assert
            Assert.Throws<ArgumentException>(() => _jogoUseCase.AplicarDesconto(1, percentual));

            _jogoRepository.Verify(obj => obj.AtualizarPreco(It.IsAny<int>(), It.IsAny<double>()), Times.Never);
        }

        [Fact]
        [Trait("UseCase", "Jogos")]
        public void AplicarDesconto_ComJogoInexistente_DeveRetornarNull()
        {
            //Arrange
            _jogoRepository.Setup(obj => obj.ObterUm(It.IsAny<int>())).Returns((JogoEntity?)null);

            //Act
            var resultado = _jogoUseCase.AplicarDesconto(9999, 10);

            //Assert
            Assert.Null(resultado);

            _jogoRepository.Verify(obj => obj.AtualizarPreco(It.IsAny<int>(), It.IsAny<double>()), Times.Never);
        }

        [Fact]
        [Trait("UseCase", "Jogos")]
        public void AdicionarPlataforma_DeveMapearODtoParaAEntidade()
        {
            //Arrange
            _jogoRepository
                .Setup(obj => obj.AdicionarPlataforma(It.IsAny<PlataformaEntity>(), 1))
                .Returns<PlataformaEntity, int>((plataforma, jogoId) => plataforma);

            //Act
            var resultado = _jogoUseCase.AdicionarPlataformaNoJogo(new PostPlataformaDto { Nome = "PC" }, 1);

            //Assert
            Assert.NotNull(resultado);
            Assert.Equal("PC", resultado!.Nome);
        }
    }
}
