using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Application.Interfaces;
using GameCatalog.API.Application.Mappers;
using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Interfaces;

namespace GameCatalog.API.Application.UseCases
{
    public class EstudioUseCase : IEstudioUseCase
    {
        public const int PageSizeMaximo = 100;
        public const int AnoFundacaoMinimo = 1958;

        private readonly ILogger<EstudioUseCase>? _logger;

        private readonly IEstudioRepository _estudioRepository;

        public EstudioUseCase(IEstudioRepository estudioRepository, ILogger<EstudioUseCase>? logger)
        {
            _estudioRepository = estudioRepository;
            _logger = logger;
        }

        public PaginacaoDto<EstudioEntity> ObterTodosEstudios(int PageNumber, int PageSize, string? Busca)
        {
            //Normaliza a paginacao para proteger o banco de consultas abusivas
            if (PageNumber < 1) PageNumber = 1;

            if (PageSize <= 0) PageSize = 10;

            if (PageSize > PageSizeMaximo) PageSize = PageSizeMaximo;

            _logger?.LogInformation(
                "ObterTodosEstudios iniciado na application - PageNumber: {0} - PageSize: {1} - Busca: {2}",
                PageNumber, PageSize, Busca);

            var registros = _estudioRepository.ObterTodos(PageNumber, PageSize, Busca);
            var total = _estudioRepository.ObterTotal(Busca);

            return new PaginacaoDto<EstudioEntity>(registros, PageNumber, PageSize, total);
        }

        public EstudioEntity? ObterUmEstudio(int Id)
        {
            return _estudioRepository.ObterUm(Id);
        }

        public EstudioEntity? AdicionarEstudio(PostEstudioDto entity)
        {
            ValidarAnoFundacao(entity.AnoFundacao);

            _logger?.LogInformation("Adicionando o estudio {0}", entity.Nome);

            return _estudioRepository.Adicionar(entity.ToEstudioEntity());
        }

        public EstudioEntity? EditarEstudio(int Id, PostEstudioDto entity)
        {
            ValidarAnoFundacao(entity.AnoFundacao);

            return _estudioRepository.Editar(Id, entity.ToEstudioEntity());
        }

        public EstudioEntity? DeletarEstudio(int Id)
        {
            var estudio = _estudioRepository.ObterUm(Id);

            if (estudio is null)
                return null;

            //Regra de negocio: o historico de jogos nao pode ficar orfao
            if (estudio.Jogos is not null && estudio.Jogos.Any())
            {
                _logger?.LogWarning("Exclusao do estudio {0} bloqueada: possui jogos vinculados", Id);

                throw new ArgumentException("Nao e possivel excluir um estudio que possui jogos vinculados.");
            }

            return _estudioRepository.Deletar(Id);
        }

        /// <summary>
        /// Regra de negocio: o estudio nao pode ter sido fundado antes do primeiro
        /// videogame comercial (1958) nem em uma data futura.
        /// </summary>
        private static void ValidarAnoFundacao(int anoFundacao)
        {
            if (anoFundacao < AnoFundacaoMinimo || anoFundacao > DateTime.Now.Year)
                throw new ArgumentException($"O ano de fundacao deve estar entre {AnoFundacaoMinimo} e {DateTime.Now.Year}.");
        }
    }
}
