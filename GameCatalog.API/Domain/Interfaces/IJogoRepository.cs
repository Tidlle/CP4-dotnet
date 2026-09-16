using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Enums;

namespace GameCatalog.API.Domain.Interfaces
{
    public interface IJogoRepository
    {
        IEnumerable<JogoEntity> ObterTodos(int PageNumber, int PageSize, GeneroJogo? Genero, int? EstudioId);
        int ObterTotal(GeneroJogo? Genero, int? EstudioId);
        JogoEntity? ObterUm(int Id);
        JogoEntity? Adicionar(JogoEntity entity);
        JogoEntity? Editar(int Id, JogoEntity entity);
        JogoEntity? Deletar(int Id);
        JogoEntity? AtualizarPreco(int Id, double NovoPreco);
        PlataformaEntity? AdicionarPlataforma(PlataformaEntity entity, int jogoId);
    }
}
