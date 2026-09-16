using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace GameCatalog.API.Doc.Samples
{
    public class JogoRequestSample : IExamplesProvider<PostJogoDto>
    {
        public PostJogoDto GetExamples()
        {
            return new PostJogoDto
            {
                Titulo = "Neon Drift",
                Genero = GeneroJogo.Acao,
                Preco = 149.90,
                DataLancamento = new DateTime(2021, 5, 10),
                Nota = 8.5,
                EstudioId = 1,
                Plataformas = new List<PostPlataformaDto>
                {
                    new PostPlataformaDto { Nome = "PC" },
                    new PostPlataformaDto { Nome = "Xbox Series X" }
                }
            };
        }
    }
}
