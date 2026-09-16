using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Domain.Entities;

namespace GameCatalog.API.Application.Interfaces
{
    public interface IEstudioUseCase
    {
        PaginacaoDto<EstudioEntity> ObterTodosEstudios(int PageNumber, int PageSize, string? Busca);
        EstudioEntity? ObterUmEstudio(int Id);
        EstudioEntity? AdicionarEstudio(PostEstudioDto entity);
        EstudioEntity? EditarEstudio(int Id, PostEstudioDto entity);
        EstudioEntity? DeletarEstudio(int Id);
    }
}
