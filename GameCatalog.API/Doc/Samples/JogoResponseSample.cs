using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace GameCatalog.API.Doc.Samples
{
    public class JogoResponseSample : IExamplesProvider<JogoEntity>
    {
        public JogoEntity GetExamples()
        {
            return new JogoEntity
            {
                Id = 1,
                Titulo = "Neon Drift",
                Genero = GeneroJogo.Acao,
                Preco = 149.90,
                DataLancamento = new DateTime(2021, 5, 10),
                Nota = 8.5,
                EstudioId = 1,
                Plataformas = new List<PlataformaEntity>
                {
                    new PlataformaEntity { Id = 1, Nome = "PC" },
                    new PlataformaEntity { Id = 2, Nome = "PlayStation 5" }
                }
            };
        }
    }
}
