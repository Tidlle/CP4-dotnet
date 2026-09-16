using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace GameCatalog.API.Doc.Samples
{
    public class JogoResponseListSample : IExamplesProvider<PaginacaoDto<JogoEntity>>
    {
        public PaginacaoDto<JogoEntity> GetExamples()
        {
            var jogos = new List<JogoEntity>
            {
                new JogoEntity { Id = 1, Titulo = "Neon Drift", Genero = GeneroJogo.Acao, Preco = 149.90, DataLancamento = new DateTime(2021, 5, 10), Nota = 8.5, EstudioId = 1 },
                new JogoEntity { Id = 2, Titulo = "Chronicles of Aether", Genero = GeneroJogo.Rpg, Preco = 199.90, DataLancamento = new DateTime(2020, 2, 15), Nota = 9.1, EstudioId = 2 },
                new JogoEntity { Id = 3, Titulo = "Deep Harbor", Genero = GeneroJogo.Aventura, Preco = 89.90, DataLancamento = new DateTime(2019, 8, 1), Nota = 7.8, EstudioId = 1 }
            };

            return new PaginacaoDto<JogoEntity>(jogos, pageNumber: 1, pageSize: 10, totalRegistros: 47);
        }
    }
}
