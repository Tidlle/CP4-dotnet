using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace GameCatalog.API.Doc.Samples
{
    public class EstudioRequestSample : IExamplesProvider<PostEstudioDto>
    {
        public PostEstudioDto GetExamples()
        {
            return new PostEstudioDto
            {
                Nome = "Pixel Forge Studios",
                Pais = "Brasil",
                AnoFundacao = 2011,
                Ativo = true,
                Jogos = new List<PostJogoDto>
                {
                    new PostJogoDto
                    {
                        Titulo = "Neon Drift",
                        Genero = GeneroJogo.Acao,
                        Preco = 149.90,
                        DataLancamento = new DateTime(2021, 5, 10),
                        Nota = 8.5,
                        Plataformas = new List<PostPlataformaDto>
                        {
                            new PostPlataformaDto { Nome = "PC" },
                            new PostPlataformaDto { Nome = "PlayStation 5" }
                        }
                    }
                }
            };
        }
    }
}
