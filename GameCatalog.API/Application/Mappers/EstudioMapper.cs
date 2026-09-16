using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Domain.Entities;

namespace GameCatalog.API.Application.Mappers
{
    public static class EstudioMapper
    {
        public static EstudioEntity ToEstudioEntity(this PostEstudioDto obj)
        {
            return new EstudioEntity
            {
                Nome = obj.Nome,
                Pais = obj.Pais,
                AnoFundacao = obj.AnoFundacao,
                Ativo = obj.Ativo,
                Jogos = obj.Jogos?.Select(x => x.ToJogoEntity()).ToList()
                        ?? Enumerable.Empty<JogoEntity>().ToList()
            };
        }
    }
}
