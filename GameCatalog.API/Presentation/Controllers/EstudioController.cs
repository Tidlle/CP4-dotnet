using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Application.Interfaces;
using GameCatalog.API.Doc.Samples;
using GameCatalog.API.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace GameCatalog.API.Presentation.Controllers
{
    [Route("api/estudio")]
    [ApiController]
    [EnableRateLimiting("politica_rate_limit")]
    public class EstudioController : ControllerBase
    {
        private readonly ILogger<EstudioController> _logger;
        private readonly IEstudioUseCase _estudioUseCase;

        public EstudioController(IEstudioUseCase estudioUseCase, ILogger<EstudioController> logger)
        {
            _estudioUseCase = estudioUseCase;
            _logger = logger;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lista os estudios de forma paginada",
            Description = """
            ## 📋 Informações do Retorno:

            * **Status 200 (OK):** Retorna a página solicitada com os metadados da paginação.
            * **Status 204 (No Content):** Executado com sucesso, porém a base não possui registros para o filtro informado.
            * **Status 400 (Bad Request):** Ocorreu uma falha durante a consulta (ex: erro de conexão com o banco).
            * **Status 429 (Too Many Requests):** Limite de requisições por cliente excedido.

            ## 💡 Observações:
            * **PageNumber:** número da página desejada (mínimo 1).
            * **PageSize:** quantidade de registros por página (máximo 100).
            * **Busca:** filtro opcional aplicado sobre o nome e o país do estúdio.
            * A consulta é apoiada pelo índice **IDX_ESTUDIO_NOME**.
            """
        )]
        [SwaggerResponse(statusCode: 200, description: "Estudios listados com sucesso", type: typeof(PaginacaoDto<EstudioEntity>))]
        [SwaggerResponse(statusCode: 204, description: "Nao possui estudios cadastrados")]
        [SwaggerResponse(statusCode: 400, description: "Ocorreu um erro ao listar os estudios", type: typeof(string))]
        [SwaggerResponse(statusCode: 429, description: "Limite de requisicoes excedido")]
        [SwaggerResponseExample(statusCode: 200, typeof(EstudioResponseListSample))]
        public IActionResult Get(
            [SwaggerParameter("Numero da pagina (minimo 1)")] int PageNumber = 1,
            [SwaggerParameter("Registros por pagina (maximo 100)")] int PageSize = 10,
            [SwaggerParameter("Filtro por nome ou pais")] string? Busca = null)
        {
            try
            {
                _logger.LogInformation("Inicio metodo ObterTodos dos estudios");

                var resultado = _estudioUseCase.ObterTodosEstudios(PageNumber, PageSize, Busca);

                if (!resultado.Dados.Any())
                {
                    _logger.LogWarning(
                        "Nao retornou nenhuma informacao do ObterTodos - Parametros: P: {0} - T: {1}",
                        PageNumber, PageSize);

                    return NoContent();
                }

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ERROR] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Obter um estudio com os seus jogos"
        )]
        [SwaggerResponse(statusCode: 200, description: "Dados do estudio retornado com sucesso", type: typeof(EstudioEntity))]
        [SwaggerResponse(statusCode: 404, description: "Nao encontrado dados para o estudio")]
        [SwaggerResponseExample(statusCode: 200, typeof(EstudioResponseSample))]
        public IActionResult Get(int id)
        {
            try
            {
                var estudio = _estudioUseCase.ObterUmEstudio(id);

                if (estudio is null)
                    return NotFound();

                return Ok(estudio);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ERROR] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Adicionar estudio",
            Description = "O ano de fundacao precisa estar entre 1958 e o ano atual."
        )]
        [SwaggerRequestExample(typeof(PostEstudioDto), typeof(EstudioRequestSample))]
        [SwaggerResponse(statusCode: 201, description: "Estudio cadastrado com sucesso", type: typeof(EstudioEntity))]
        [SwaggerResponse(statusCode: 400, description: "Dados invalidos", type: typeof(string))]
        [SwaggerResponseExample(statusCode: 201, typeof(EstudioResponseSample))]
        public IActionResult Post(PostEstudioDto model)
        {
            try
            {
                var estudio = _estudioUseCase.AdicionarEstudio(model);

                return CreatedAtAction(nameof(Get), new { id = estudio?.Id ?? 0 }, estudio);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ERROR] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        [SwaggerOperation(
            Summary = "Editar estudio"
        )]
        [SwaggerResponse(statusCode: 200, description: "Estudio atualizado com sucesso", type: typeof(EstudioEntity))]
        [SwaggerResponse(statusCode: 404, description: "Nao encontrado dados para o estudio")]
        public IActionResult Put([FromRoute, SwaggerParameter("Id do estudio!")] int id, PostEstudioDto model)
        {
            try
            {
                var estudio = _estudioUseCase.EditarEstudio(id, model);

                if (estudio is null)
                    return NotFound();

                return Ok(estudio);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ERROR] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Deletar estudio",
            Description = "Um estudio que possui jogos vinculados nao pode ser excluido."
        )]
        [SwaggerResponse(statusCode: 200, description: "Estudio removido com sucesso", type: typeof(EstudioEntity))]
        [SwaggerResponse(statusCode: 400, description: "O estudio possui jogos vinculados", type: typeof(string))]
        [SwaggerResponse(statusCode: 404, description: "Nao encontrado dados para o estudio")]
        public IActionResult Delete(int id)
        {
            try
            {
                var estudio = _estudioUseCase.DeletarEstudio(id);

                if (estudio is null)
                    return NotFound();

                return Ok(estudio);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ERROR] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}
