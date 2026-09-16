using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Enums;
using GameCatalog.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameCatalog.API.Infrastructure.Data.Repositories
{
    public class JogoRepository : IJogoRepository
    {
        private readonly ApplicationContext _context;

        public JogoRepository(ApplicationContext context)
        {
            _context = context;
        }

        public JogoEntity? Adicionar(JogoEntity entity)
        {
            try
            {
                _context.Jogo.Add(entity);
                _context.SaveChanges();

                return entity;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public JogoEntity? Deletar(int Id)
        {
            try
            {
                var jogo = _context.Jogo.FirstOrDefault(x => x.Id == Id);

                if (jogo is null)
                    return null;

                _context.Jogo.Remove(jogo);
                _context.SaveChanges();

                return jogo;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public JogoEntity? Editar(int Id, JogoEntity entity)
        {
            try
            {
                var jogo = _context
                    .Jogo
                    .Include(x => x.Plataformas)
                    .FirstOrDefault(x => x.Id == Id);

                if (jogo is null)
                    return null;

                jogo.Titulo = entity.Titulo;
                jogo.Genero = entity.Genero;
                jogo.Preco = entity.Preco;
                jogo.DataLancamento = entity.DataLancamento;
                jogo.Nota = entity.Nota;
                jogo.EstudioId = entity.EstudioId;

                _context.Jogo.Update(jogo);
                _context.SaveChanges();

                return jogo;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Paginacao feita no banco (OFFSET/FETCH). Os filtros de genero e estudio
        /// sao apoiados pelos indices IDX_JOGO_GENERO_PRECO e IDX_JOGO_ESTUDIO_DATA.
        /// </summary>
        public IEnumerable<JogoEntity> ObterTodos(int PageNumber = 1, int PageSize = 10, GeneroJogo? Genero = null, int? EstudioId = null)
        {
            try
            {
                if (PageNumber < 1) PageNumber = 1;

                if (PageSize <= 0) PageSize = 10;

                var consulta = MontarConsulta(Genero, EstudioId)
                    .Include(x => x.Plataformas);

                var resultado = consulta
                    .OrderBy(x => x.Titulo)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                if (!resultado.Any())
                    return Enumerable.Empty<JogoEntity>();

                return resultado;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public int ObterTotal(GeneroJogo? Genero, int? EstudioId)
        {
            return MontarConsulta(Genero, EstudioId).Count();
        }

        public JogoEntity? ObterUm(int Id)
        {
            try
            {
                var jogo = _context
                    .Jogo
                    .Include(x => x.Plataformas)
                    .FirstOrDefault(x => x.Id == Id);

                if (jogo is null)
                    return null;

                return jogo;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public JogoEntity? AtualizarPreco(int Id, double NovoPreco)
        {
            try
            {
                var jogo = _context.Jogo.FirstOrDefault(x => x.Id == Id);

                if (jogo is null)
                    return null;

                jogo.Preco = NovoPreco;

                _context.Jogo.Update(jogo);
                _context.SaveChanges();

                return jogo;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public PlataformaEntity? AdicionarPlataforma(PlataformaEntity entity, int jogoId)
        {
            var jogo = _context.Jogo.FirstOrDefault(x => x.Id == jogoId);

            if (jogo is null)
                return null;

            entity.Jogos = [jogo];

            _context.Plataforma.Add(entity);
            _context.SaveChanges();

            return entity;
        }

        private IQueryable<JogoEntity> MontarConsulta(GeneroJogo? Genero, int? EstudioId)
        {
            var consulta = _context.Jogo.AsQueryable();

            if (Genero.HasValue)
                consulta = consulta.Where(x => x.Genero == Genero.Value);

            if (EstudioId.HasValue)
                consulta = consulta.Where(x => x.EstudioId == EstudioId.Value);

            return consulta;
        }
    }
}
