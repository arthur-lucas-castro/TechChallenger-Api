using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Catalogo.Application.DTOs;
using Catalogo.Application.Services.Interfaces;

namespace Catalogo.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class EstoqueController : ControllerBase
    {
        private readonly IPecaService _service;

        public EstoqueController(IPecaService service) => _service = service;

        /// <summary>Lista o estoque atual de todas as peças com seus dados e quantidades.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.ObterEstoqueTodosAsync());

        /// <summary>Registra uma entrada de estoque para uma peça.</summary>
        /// <param name="dto">
        /// Dados da entrada:
        /// <list type="bullet">
        ///   <item><c>pecaId</c> — ID da peça que terá estoque reposto.</item>
        ///   <item><c>quantidade</c> — quantidade de unidades a adicionar.</item>
        ///   <item><c>precoCusto</c> — preço de custo unitário desta entrada, usado para calcular o custo médio ponderado.</item>
        /// </list>
        /// </param>
        [HttpPost("entrada")]
        public async Task<IActionResult> AdicionarProduto([FromBody] EntradaEstoqueRequestDto dto)
        {
            try
            {
                var resultado = await _service.AdicionarEstoqueAsync(dto);
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>Registra uma saída (baixa) de estoque para uma peça.</summary>
        /// <param name="dto">
        /// Dados da baixa:
        /// <list type="bullet">
        ///   <item><c>pecaId</c> — ID da peça a ter estoque reduzido.</item>
        ///   <item><c>quantidade</c> — quantidade de unidades a retirar do estoque.</item>
        /// </list>
        /// </param>
        [HttpPost("baixa")]
        public async Task<IActionResult> DarBaixa([FromBody] BaixaEstoqueRequestDto dto)
        {
            try
            {
                var resultado = await _service.DarBaixaAsync(dto);
                if (resultado is null)
                    return NotFound($"Estoque não encontrado para a peça {dto.PecaId}.");
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
