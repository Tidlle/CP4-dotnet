using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Enums;

namespace GameCatalog.API.Application.Interfaces
{
    public interface IJogoUseCase
    {
        PaginacaoDto<JogoEntity> ObterTodosJogos(int PageNumber, int PageSize, GeneroJogo? Genero, int? EstudioId);
        JogoEntity? ObterUmJogo(int Id);
        JogoEntity? AdicionarJogo(PostJogoDto entity);
        JogoEntity? EditarJogo(int Id, PostJogoDto entity);
        JogoEntity? DeletarJogo(int Id);
        JogoEntity? AplicarDesconto(int Id, double Percentual);
        PlataformaEntity? AdicionarPlataformaNoJogo(PostPlataformaDto entity, int jogoId);
    }
}
