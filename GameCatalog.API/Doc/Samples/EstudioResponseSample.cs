using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace GameCatalog.API.Doc.Samples
{
    public class EstudioResponseSample : IExamplesProvider<EstudioEntity>
    {
        public EstudioEntity GetExamples()
        {
            return new EstudioEntity
            {
                Id = 1,
                Nome = "Pixel Forge Studios",
                Pais = "Brasil",
                AnoFundacao = 2011,
                Ativo = true,
                Jogos = new List<JogoEntity>
                {
                    new JogoEntity
                    {
                        Id = 1,
                        Titulo = "Neon Drift",
                        Genero = GeneroJogo.Acao,
                        Preco = 149.90,
                        DataLancamento = new DateTime(2021, 5, 10),
                        Nota = 8.5,
                        EstudioId = 1
                    }
                }
            };
        }
    }
}
