using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameCatalog.API.Infrastructure.Data.Repositories
{
    public class EstudioRepository : IEstudioRepository
    {
        private readonly ApplicationContext _context;

        public EstudioRepository(ApplicationContext context)
        {
            _context = context;
        }

        public EstudioEntity? Adicionar(EstudioEntity entity)
        {
            try
            {
                _context.Estudio.Add(entity);
                _context.SaveChanges();

                return entity;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public EstudioEntity? Deletar(int Id)
        {
            try
            {
                var estudio = _context.Estudio.FirstOrDefault(x => x.Id == Id);

                if (estudio is null)
                    return null;

                _context.Estudio.Remove(estudio);
                _context.SaveChanges();

                return estudio;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public EstudioEntity? Editar(int Id, EstudioEntity entity)
        {
            try
            {
                var estudio = _context
                    .Estudio
                    .FirstOrDefault(x => x.Id == Id);

                if (estudio is null)
                    return null;

                estudio.Nome = entity.Nome;
                estudio.Pais = entity.Pais;
                estudio.AnoFundacao = entity.AnoFundacao;
                estudio.Ativo = entity.Ativo;

                _context.Estudio.Update(estudio);
                _context.SaveChanges();

                return estudio;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public bool Existe(int Id)
        {
            return _context.Estudio.Any(x => x.Id == Id);
        }

        /// <summary>
        /// Paginacao feita no banco (OFFSET/FETCH), nunca em memoria.
        /// A busca e a ordenacao sao apoiadas pelo indice IDX_ESTUDIO_NOME.
        /// </summary>
        public IEnumerable<EstudioEntity> ObterTodos(int PageNumber = 1, int PageSize = 10, string? Busca = null)
        {
            try
            {
                if (PageNumber < 1) PageNumber = 1;

                if (PageSize <= 0) PageSize = 10;

                var consulta = _context
                    .Estudio
                    .Include(x => x.Jogos)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(Busca))
                    consulta = consulta.Where(x => x.Nome.Contains(Busca) || x.Pais.Contains(Busca));

                var resultado = consulta
                    .OrderBy(x => x.Nome)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                if (!resultado.Any())
                    return Enumerable.Empty<EstudioEntity>();

                return resultado;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public int ObterTotal(string? Busca)
        {
            var consulta = _context.Estudio.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Busca))
                consulta = consulta.Where(x => x.Nome.Contains(Busca) || x.Pais.Contains(Busca));

            return consulta.Count();
        }

        public EstudioEntity? ObterUm(int Id)
        {
            try
            {
                var estudio = _context
                    .Estudio
                    .Include(x => x.Jogos) //Select * from tb_estudio e inner join tb_jogo j on j.i_estudio_id = e.id
                    .FirstOrDefault(x => x.Id == Id);

                if (estudio is null)
                    return null;

                return estudio;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }
    }
}
