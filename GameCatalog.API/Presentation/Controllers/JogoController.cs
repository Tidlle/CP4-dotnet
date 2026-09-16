using GameCatalog.API.Application.Dtos;
using GameCatalog.API.Application.Interfaces;
using GameCatalog.API.Doc.Samples;
using GameCatalog.API.Domain.Entities;
using GameCatalog.API.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace GameCatalog.API.Presentation.Controllers
{
    [Route("api/jogo")]
    [ApiController]
    [EnableRateLimiting("politica_rate_limit")]
    public class JogoController : ControllerBase
    {
        private readonly ILogger<JogoController> _logger;
        private readonly IJogoUseCase _jogoUseCase;

        public JogoController(IJogoUseCase jogoUseCase, ILogger<JogoController> logger)
        {
            _jogoUseCase = jogoUseCase;
            _logger = logger;
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lista os jogos de forma paginada",
            Description = """
            ## 📋 Informações do Retorno:

            * **Status 200 (OK):** Retorna a página solicitada com os metadados da paginação.
            * **Status 204 (No Content):** Executado com sucesso, porém a base não possui registros para o filtro informado.
            * **Status 400 (Bad Request):** Ocorreu uma falha durante a consulta.
            * **Status 429 (Too Many Requests):** Limite de requisições por cliente excedido.

            ## 💡 Observações:
            * **PageNumber / PageSize:** controlam a paginação (máximo de 100 registros por página).
            * **Genero:** Acao, Aventura, Rpg, Estrategia, Simulacao, Puzzle, Plataforma ou Terror.
            * **EstudioId:** retorna apenas os jogos do estúdio informado.
            * Os filtros são apoiados pelos índices **IDX_JOGO_GENERO_PRECO** e **IDX_JOGO_ESTUDIO_DATA**.
            """
        )]
        [SwaggerResponse(statusCode: 200, description: "Jogos listados com sucesso", type: typeof(PaginacaoDto<JogoEntity>))]
        [SwaggerResponse(statusCode: 204, description: "Nao possui jogos cadastrados")]
        [SwaggerResponse(statusCode: 400, description: "Ocorreu um erro ao listar os jogos", type: typeof(string))]
        [SwaggerResponse(statusCode: 429, description: "Limite de requisicoes excedido")]
        [SwaggerResponseExample(statusCode: 200, typeof(JogoResponseListSample))]
        public IActionResult Get(
            [SwaggerParameter("Numero da pagina (minimo 1)")] int PageNumber = 1,
            [SwaggerParameter("Registros por pagina (maximo 100)")] int PageSize = 10,
            [SwaggerParameter("Filtro por genero")] GeneroJogo? Genero = null,
            [SwaggerParameter("Filtro pelos jogos de um estudio")] int? EstudioId = null)
        {
            try
            {
                _logger.LogInformation("Inicio metodo ObterTodos dos jogos");

                var resultado = _jogoUseCase.ObterTodosJogos(PageNumber, PageSize, Genero, EstudioId);

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
            Summary = "Obter um jogo"
        )]
        [SwaggerResponse(statusCode: 200, description: "Dados do jogo retornado com sucesso", type: typeof(JogoEntity))]
        [SwaggerResponse(statusCode: 404, description: "Nao encontrado dados para o jogo")]
        [SwaggerResponseExample(statusCode: 200, typeof(JogoResponseSample))]
        public IActionResult Get(int id)
        {
            try
            {
                var jogo = _jogoUseCase.ObterUmJogo(id);

                if (jogo is null)
                    return NotFound();

                return Ok(jogo);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ERROR] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [SwaggerOperation(
            Summary = "Adicionar jogo",
            Description = "O jogo precisa estar vinculado a um estudio ja cadastrado."
        )]
        [SwaggerRequestExample(typeof(PostJogoDto), typeof(JogoRequestSample))]
        [SwaggerResponse(statusCode: 201, description: "Jogo cadastrado com sucesso", type: typeof(JogoEntity))]
        [SwaggerResponse(statusCode: 404, description: "O estudio informado nao existe", type: typeof(string))]
        [SwaggerResponseExample(statusCode: 201, typeof(JogoResponseSample))]
        public IActionResult Post(PostJogoDto model)
        {
            try
            {
                var jogo = _jogoUseCase.AdicionarJogo(model);

                if (jogo is null)
                    return NotFound("O estudio informado nao existe.");

                return CreatedAtAction(nameof(Get), new { id = jogo.Id }, jogo);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ERROR] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        [SwaggerOperation(
            Summary = "Editar jogo"
        )]
        [SwaggerResponse(statusCode: 200, description: "Jogo atualizado com sucesso", type: typeof(JogoEntity))]
        [SwaggerResponse(statusCode: 404, description: "Jogo ou estudio nao encontrado")]
        public IActionResult Put([FromRoute, SwaggerParameter("Id do jogo!")] int id, PostJogoDto model)
        {
            try
            {
                var jogo = _jogoUseCase.EditarJogo(id, model);

                if (jogo is null)
                    return NotFound();

                return Ok(jogo);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ERROR] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{id}/desconto")]
        [SwaggerOperation(
            Summary = "Aplicar desconto no jogo",
            Description = "Regra de negocio: o percentual precisa ser maior que zero e no maximo 90%."
        )]
        [SwaggerResponse(statusCode: 200, description: "Desconto aplicado com sucesso", type: typeof(JogoEntity))]
        [SwaggerResponse(statusCode: 400, description: "Percentual de desconto invalido", type: typeof(string))]
        [SwaggerResponse(statusCode: 404, description: "Nao encontrado dados para o jogo")]
        public IActionResult PatchDesconto(int id, PostDescontoDto model)
        {
            try
            {
                var jogo = _jogoUseCase.AplicarDesconto(id, model.Percentual);

                if (jogo is null)
                    return NotFound();

                return Ok(jogo);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("[REGRA DE NEGOCIO] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ERROR] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(
            Summary = "Deletar jogo"
        )]
        [SwaggerResponse(statusCode: 200, description: "Jogo removido com sucesso", type: typeof(JogoEntity))]
        [SwaggerResponse(statusCode: 404, description: "Nao encontrado dados para o jogo")]
        public IActionResult Delete(int id)
        {
            try
            {
                var jogo = _jogoUseCase.DeletarJogo(id);

                if (jogo is null)
                    return NotFound();

                return Ok(jogo);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ERROR] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{jogoId}/plataforma")]
        [SwaggerOperation(
            Summary = "Adicionar plataforma no jogo",
            Description = "Relacionamento N:N entre jogo e plataforma."
        )]
        [SwaggerResponse(statusCode: 201, description: "Plataforma vinculada com sucesso", type: typeof(PlataformaEntity))]
        [SwaggerResponse(statusCode: 404, description: "Nao encontrado dados para o jogo")]
        public IActionResult PostPlataforma(int jogoId, PostPlataformaDto model)
        {
            try
            {
                var plataforma = _jogoUseCase.AdicionarPlataformaNoJogo(model, jogoId);

                if (plataforma is null)
                    return NotFound();

                return CreatedAtAction(nameof(Get), new { id = jogoId }, plataforma);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ERROR] - {0}", ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}
