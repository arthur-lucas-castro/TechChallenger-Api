using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Catalogo.Application.DTOs;
using Catalogo.Application.Services.Interfaces;

namespace Catalogo.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PecaController : ControllerBase
    {
        private readonly IPecaService _service;

        public PecaController(IPecaService service) => _service = service;

        /// <summary>Lista todas as peças do catálogo.</summary>
        [HttpGet]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.ObterTodosAsync());

        /// <summary>Retorna os dados de uma peça pelo ID.</summary>
        /// <param name="id">ID da peça.</param>
        [HttpGet("{id:int}")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> GetById(int id)
        {
            var peca = await _service.ObterPorIdAsync(id);
            if (peca is null) return NotFound();
            return Ok(peca);
        }

        /// <summary>Cadastra uma nova peça no catálogo. Requer perfil <c>Adm</c>.</summary>
        /// <param name="dto">
        /// Dados da peça:
        /// <list type="bullet">
        ///   <item><c>nome</c> — nome da peça (ex.: <c>Filtro de Óleo</c>).</item>
        ///   <item><c>descricao</c> — descrição técnica da peça.</item>
        ///   <item><c>custo</c> — preço de custo em reais (ex.: <c>25.50</c>).</item>
        ///   <item><c>precoVenda</c> — preço de venda ao cliente em reais (ex.: <c>45.00</c>).</item>
        /// </list>
        /// </param>
        [HttpPost]
        [Authorize(Roles = "Adm")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> Create([FromBody] PecaRequestDto dto)
        {
            var id = await _service.CriarAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        /// <summary>Atualiza os dados de uma peça existente. Requer perfil <c>Adm</c>.</summary>
        /// <param name="id">ID da peça a ser atualizada.</param>
        /// <param name="dto">Novos dados da peça (mesmos campos do cadastro).</param>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Adm")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> Update(int id, [FromBody] PecaRequestDto dto)
        {
            if (!await _service.AtualizarAsync(id, dto)) return NotFound();
            return Ok(dto);
        }

        /// <summary>Remove uma peça do catálogo. Requer perfil <c>Adm</c>.</summary>
        /// <param name="id">ID da peça a ser removida.</param>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Adm")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.ExcluirAsync(id)) return NotFound();
            return NoContent();
        }
    }
}
