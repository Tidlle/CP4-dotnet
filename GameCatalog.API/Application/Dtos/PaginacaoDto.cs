namespace GameCatalog.API.Application.Dtos
{
    /// <summary>
    /// Envelope devolvido pelos endpoints de listagem com os metadados da paginacao.
    /// </summary>
    public class PaginacaoDto<T>
    {
        public PaginacaoDto()
        {
            Dados = Enumerable.Empty<T>();
        }

        public PaginacaoDto(IEnumerable<T> dados, int pageNumber, int pageSize, int totalRegistros)
        {
            Dados = dados;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalRegistros = totalRegistros;
        }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalRegistros { get; set; }

        public int TotalPaginas => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRegistros / (double)PageSize);

        public bool TemPaginaAnterior => PageNumber > 1;

        public bool TemProximaPagina => PageNumber < TotalPaginas;

        public IEnumerable<T> Dados { get; set; }
    }
}
