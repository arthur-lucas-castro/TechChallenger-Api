using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Catalogo.Application.DTOs;
using Catalogo.Application.Services.Interfaces;

namespace Catalogo.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ServicoController : ControllerBase
    {
        private readonly IServicoService _service;

        public ServicoController(IServicoService service) => _service = service;

        /// <summary>Lista todos os serviços do catálogo.</summary>
        [HttpGet]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> GetAll() => Ok(await _service.ObterTodosAsync());

        /// <summary>Retorna os dados de um serviço pelo ID.</summary>
        /// <param name="id">ID do serviço.</param>
        [HttpGet("{id:int}")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> GetById(int id)
        {
            var servico = await _service.ObterPorIdAsync(id);
            if (servico is null) return NotFound();
            return Ok(servico);
        }

        /// <summary>Cadastra um novo serviço no catálogo. Requer perfil <c>Adm</c>.</summary>
        /// <param name="dto">
        /// Dados do serviço:
        /// <list type="bullet">
        ///   <item><c>nome</c> — nome do serviço (ex.: <c>Alinhamento</c>).</item>
        ///   <item><c>precoVenda</c> — valor cobrado pelo serviço em reais (ex.: <c>120.00</c>).</item>
        ///   <item><c>tempoEstimadoEmMinutos</c> — tempo previsto de execução em minutos (ex.: <c>60</c>).</item>
        /// </list>
        /// </param>
        [HttpPost]
        [Authorize(Roles = "Adm")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> Create([FromBody] ServicoRequestDto dto)
        {
            var id = await _service.CriarAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        /// <summary>Atualiza os dados de um serviço existente. Requer perfil <c>Adm</c>.</summary>
        /// <param name="id">ID do serviço a ser atualizado.</param>
        /// <param name="dto">Novos dados do serviço (mesmos campos do cadastro).</param>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Adm")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> Update(int id, [FromBody] ServicoRequestDto dto)
        {
            if (!await _service.AtualizarAsync(id, dto)) return NotFound();
            return Ok(dto);
        }

        /// <summary>Remove um serviço do catálogo. Requer perfil <c>Adm</c>.</summary>
        /// <param name="id">ID do serviço a ser removido.</param>
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
