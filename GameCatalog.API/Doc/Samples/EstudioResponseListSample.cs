using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Domain.Entities;
using Swashbuckle.AspNetCore.Filters;

namespace GameCatalog.API.Doc.Samples
{
    public class EstudioResponseListSample : IExamplesProvider<PaginacaoDto<EstudioEntity>>
    {
        public PaginacaoDto<EstudioEntity> GetExamples()
        {
            var estudios = new List<EstudioEntity>
            {
                new EstudioEntity { Id = 1, Nome = "Pixel Forge Studios", Pais = "Brasil", AnoFundacao = 2011, Ativo = true },
                new EstudioEntity { Id = 2, Nome = "Northern Lights Games", Pais = "Suecia", AnoFundacao = 1998, Ativo = true },
                new EstudioEntity { Id = 3, Nome = "Sakura Interactive", Pais = "Japao", AnoFundacao = 1987, Ativo = false }
            };

            return new PaginacaoDto<EstudioEntity>(estudios, pageNumber: 1, pageSize: 10, totalRegistros: 23);
        }
    }
}
