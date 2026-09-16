using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Application.Interfaces;
using GameCatalog.API.Application.Mappers;
using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Enums;
using GameCatalog.API.Domain.Interfaces;

namespace GameCatalog.API.Application.UseCases
{
    public class JogoUseCase : IJogoUseCase
    {
        public const int PageSizeMaximo = 100;
        public const double PercentualMaximoDesconto = 90;

        private readonly ILogger<JogoUseCase>? _logger;

        private readonly IJogoRepository _jogoRepository;
        private readonly IEstudioRepository _estudioRepository;

        public JogoUseCase(
            IJogoRepository jogoRepository,
            IEstudioRepository estudioRepository,
            ILogger<JogoUseCase>? logger)
        {
            _jogoRepository = jogoRepository;
            _estudioRepository = estudioRepository;
            _logger = logger;
        }

        public PaginacaoDto<JogoEntity> ObterTodosJogos(int PageNumber, int PageSize, GeneroJogo? Genero, int? EstudioId)
        {
            //Normaliza a paginacao para proteger o banco de consultas abusivas
            if (PageNumber < 1) PageNumber = 1;

            if (PageSize <= 0) PageSize = 10;

            if (PageSize > PageSizeMaximo) PageSize = PageSizeMaximo;

            _logger?.LogInformation(
                "ObterTodosJogos iniciado na application - PageNumber: {0} - PageSize: {1} - Genero: {2} - EstudioId: {3}",
                PageNumber, PageSize, Genero, EstudioId);

            var registros = _jogoRepository.ObterTodos(PageNumber, PageSize, Genero, EstudioId);
            var total = _jogoRepository.ObterTotal(Genero, EstudioId);

            return new PaginacaoDto<JogoEntity>(registros, PageNumber, PageSize, total);
        }

        public JogoEntity? ObterUmJogo(int Id)
        {
            return _jogoRepository.ObterUm(Id);
        }

        public JogoEntity? AdicionarJogo(PostJogoDto entity)
        {
            //Regra de negocio: o jogo sempre pertence a um estudio existente
            if (!_estudioRepository.Existe(entity.EstudioId))
            {
                _logger?.LogWarning("Estudio {0} informado no jogo nao existe", entity.EstudioId);

                return null;
            }

            _logger?.LogInformation("Adicionando o jogo {0} no estudio {1}", entity.Titulo, entity.EstudioId);

            return _jogoRepository.Adicionar(entity.ToJogoEntity());
        }

        public JogoEntity? EditarJogo(int Id, PostJogoDto entity)
        {
            if (!_estudioRepository.Existe(entity.EstudioId))
                return null;

            return _jogoRepository.Editar(Id, entity.ToJogoEntity());
        }

        public JogoEntity? DeletarJogo(int Id)
        {
            return _jogoRepository.Deletar(Id);
        }

        /// <summary>
        /// Regra de negocio: o desconto precisa ser maior que zero, no maximo 90%,
        /// e nunca pode deixar o preco do jogo negativo.
        /// </summary>
        public JogoEntity? AplicarDesconto(int Id, double Percentual)
        {
            if (Percentual <= 0)
                throw new ArgumentException("O percentual de desconto deve ser maior que zero.");

            if (Percentual > PercentualMaximoDesconto)
                throw new ArgumentException($"O percentual de desconto nao pode ultrapassar {PercentualMaximoDesconto}%.");

            var jogo = _jogoRepository.ObterUm(Id);

            if (jogo is null)
                return null;

            var novoPreco = Math.Round(jogo.Preco - (jogo.Preco * Percentual / 100), 2);

            _logger?.LogInformation(
                "Desconto de {0}% aplicado no jogo {1} - Preco {2} para {3}",
                Percentual, Id, jogo.Preco, novoPreco);

            return _jogoRepository.AtualizarPreco(Id, novoPreco);
        }

        public PlataformaEntity? AdicionarPlataformaNoJogo(PostPlataformaDto entity, int jogoId)
        {
            var plataforma = new PlataformaEntity
            {
                Nome = entity.Nome
            };

            return _jogoRepository.AdicionarPlataforma(plataforma, jogoId);
        }
    }
}
