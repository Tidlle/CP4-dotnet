using GameCatalog.API.Domain.Entities;

namespace GameCatalog.API.Domain.Interfaces
{
    public interface IEstudioRepository
    {
        IEnumerable<EstudioEntity> ObterTodos(int PageNumber, int PageSize, string? Busca);
        int ObterTotal(string? Busca);
        EstudioEntity? ObterUm(int Id);
        EstudioEntity? Adicionar(EstudioEntity entity);
        EstudioEntity? Editar(int Id, EstudioEntity entity);
        EstudioEntity? Deletar(int Id);
        bool Existe(int Id);
    }
}
