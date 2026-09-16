using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Domain.Entities;

namespace GameCatalog.API.Application.Mappers
{
    public static class JogoMapper
    {
        public static JogoEntity ToJogoEntity(this PostJogoDto obj)
        {
            return new JogoEntity
            {
                Titulo = obj.Titulo,
                Genero = obj.Genero,
                Preco = obj.Preco,
                DataLancamento = obj.DataLancamento,
                Nota = obj.Nota,
                EstudioId = obj.EstudioId,
                Plataformas = obj.Plataformas?.Select(x => new PlataformaEntity
                {
                    Nome = x.Nome

                }).ToList() ?? Enumerable.Empty<PlataformaEntity>().ToList()
            };
        }
    }
}
