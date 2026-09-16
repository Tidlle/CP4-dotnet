using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Application.Mappers;
using GameCatalog.API.Application.UseCases;
using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Interfaces;
using Moq;

namespace GameCatalog.Tests.App
{
    public class EstudioUseCaseTest
    {
        private readonly Mock<IEstudioRepository> _estudioRepository;
        private readonly EstudioUseCase _estudioUseCase;

        public EstudioUseCaseTest()
        {
            _estudioRepository = new Mock<IEstudioRepository>();
            _estudioUseCase = new EstudioUseCase(_estudioRepository.Object, null);
        }

        [Fact]
        [Trait("UseCase", "Estudios")]
        public void ObterUm_DeveRetornarUmEstudio()
        {
            //Arrange
            int idEstudio = 1;
            var estudio = new EstudioEntity { Id = 1, Nome = "Pixel Forge Studios", Pais = "Brasil", AnoFundacao = 2011 };

            _estudioRepository.Setup(obj => obj.ObterUm(idEstudio)).Returns(estudio);

            //Act
            var resultado = _estudioUseCase.ObterUmEstudio(idEstudio);

            //Assert
            Assert.NotNull(resultado);
            Assert.Equal(idEstudio, resultado.Id);
            Assert.Equal(estudio.Nome, resultado.Nome);
        }

        [Fact]
        [Trait("UseCase", "Estudios")]
        public void ObterTodos_DeveNormalizarAPaginacao()
        {
            //Arrange
            int pageNumberUtilizado = 0;
            int pageSizeUtilizado = 0;

            _estudioRepository
                .Setup(obj => obj.ObterTodos(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
                .Callback<int, int, string?>((pageNumber, pageSize, busca) =>
                {
                    pageNumberUtilizado = pageNumber;
                    pageSizeUtilizado = pageSize;
                })
                .Returns(new List<EstudioEntity>());

            _estudioRepository.Setup(obj => obj.ObterTotal(It.IsAny<string?>())).Returns(0);

            //Act - valores invalidos enviados pelo cliente
            _estudioUseCase.ObterTodosEstudios(0, 500, null);

            //Assert
            Assert.Equal(1, pageNumberUtilizado);
            Assert.Equal(EstudioUseCase.PageSizeMaximo, pageSizeUtilizado);
        }

        [Fact]
        [Trait("UseCase", "Estudios")]
        public void ObterTodos_DeveMontarOsMetadadosDaPaginacao()
        {
            //Arrange
            var estudios = new List<EstudioEntity> {
                new EstudioEntity { Id = 1, Nome = "Pixel Forge Studios", Pais = "Brasil", AnoFundacao = 2011 }
            };

            _estudioRepository
                .Setup(obj => obj.ObterTodos(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
                .Returns(estudios);

            _estudioRepository.Setup(obj => obj.ObterTotal(It.IsAny<string?>())).Returns(23);

            //Act
            var resultado = _estudioUseCase.ObterTodosEstudios(2, 10, null);

            //Assert
            Assert.Equal(2, resultado.PageNumber);
            Assert.Equal(10, resultado.PageSize);
            Assert.Equal(23, resultado.TotalRegistros);
            Assert.Equal(3, resultado.TotalPaginas);
            Assert.True(resultado.TemPaginaAnterior);
            Assert.True(resultado.TemProximaPagina);
        }

        [Fact]
        [Trait("UseCase", "Estudios")]
        public void Adicionar_DeveMapearODtoEPersistir()
        {
            //Arrange
            var estudioDto = new PostEstudioDto { Nome = "Sakura Interactive", Pais = "Japao", AnoFundacao = 1987 };

            var entity = estudioDto.ToEstudioEntity();

            _estudioRepository.Setup(obj => obj.Adicionar(It.IsAny<EstudioEntity>())).Returns(entity);

            //Act
            var resultado = _estudioUseCase.AdicionarEstudio(estudioDto);

            //Assert
            Assert.NotNull(resultado);
            Assert.Equal(estudioDto.Nome, resultado!.Nome);
            Assert.Equal(estudioDto.Pais, resultado!.Pais);
            Assert.Equal(estudioDto.AnoFundacao, resultado!.AnoFundacao);
        }

        [Theory]
        [Trait("UseCase", "Estudios")]
        [InlineData(1900)]
        [InlineData(1957)]
        [InlineData(2500)]
        public void Adicionar_ComAnoDeFundacaoInvalido_DeveLancarExcecao(int anoFundacao)
        {
            //Arrange
            var estudioDto = new PostEstudioDto { Nome = "Estudio Invalido", Pais = "Brasil", AnoFundacao = anoFundacao };

            //Act + Assert
            var excecao = Assert.Throws<ArgumentException>(() => _estudioUseCase.AdicionarEstudio(estudioDto));

            Assert.Contains("ano de fundacao", excecao.Message);

            _estudioRepository.Verify(obj => obj.Adicionar(It.IsAny<EstudioEntity>()), Times.Never);
        }

        [Fact]
        [Trait("UseCase", "Estudios")]
        public void Deletar_ComJogosVinculados_DeveLancarExcecao()
        {
            //Arrange
            var estudio = new EstudioEntity
            {
                Id = 1,
                Nome = "Pixel Forge Studios",
                Pais = "Brasil",
                AnoFundacao = 2011,
                Jogos = new List<JogoEntity> { new JogoEntity { Id = 1, Titulo = "Neon Drift" } }
            };

            _estudioRepository.Setup(obj => obj.ObterUm(1)).Returns(estudio);

            //Act + Assert
            var excecao = Assert.Throws<ArgumentException>(() => _estudioUseCase.DeletarEstudio(1));

            Assert.Contains("jogos vinculados", excecao.Message);

            _estudioRepository.Verify(obj => obj.Deletar(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        [Trait("UseCase", "Estudios")]
        public void Deletar_SemJogosVinculados_DeveRemover()
        {
            //Arrange
            var estudio = new EstudioEntity { Id = 1, Nome = "Atlantic Bytes", Pais = "Portugal", AnoFundacao = 2016 };

            _estudioRepository.Setup(obj => obj.ObterUm(1)).Returns(estudio);
            _estudioRepository.Setup(obj => obj.Deletar(1)).Returns(estudio);

            //Act
            var resultado = _estudioUseCase.DeletarEstudio(1);

            //Assert
            Assert.NotNull(resultado);
            _estudioRepository.Verify(obj => obj.Deletar(1), Times.Once);
        }

        [Fact]
        [Trait("UseCase", "Estudios")]
        public void Deletar_ComIdInexistente_DeveRetornarNull()
        {
            //Arrange
            _estudioRepository.Setup(obj => obj.ObterUm(It.IsAny<int>())).Returns((EstudioEntity?)null);

            //Act
            var resultado = _estudioUseCase.DeletarEstudio(9999);

            //Assert
            Assert.Null(resultado);
        }
    }
}
