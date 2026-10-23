using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Atendimento.Application.DTOs;
using Atendimento.Application.Services.Interfaces;

namespace Atendimento.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class VeiculoController : ControllerBase
    {
        private readonly IVeiculoService _service;

        public VeiculoController(IVeiculoService service) => _service = service;

        /// <summary>Lista todos os veículos cadastrados.</summary>
        [HttpGet]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> GetAll() => Ok(await _service.ObterTodosAsync());

        /// <summary>Retorna os dados de um veículo pelo ID.</summary>
        /// <param name="id">ID do veículo.</param>
        [HttpGet("{id:int}")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> GetById(int id)
        {
            var veiculo = await _service.ObterPorIdAsync(id);
            if (veiculo is null) return NotFound();
            return Ok(veiculo);
        }

        /// <summary>Busca um veículo pela placa.</summary>
        /// <param name="placa">
        /// Placa do veículo nos formatos antigo (<c>ABC1234</c>) ou Mercosul (<c>ABC1D23</c>).
        /// O hífen é opcional e ignorado na busca.
        /// </param>
        [HttpGet("placa/{placa}")]
        public async Task<IActionResult> GetByPlaca(string placa)
        {
            var veiculo = await _service.ObterPorPlacaAsync(placa);
            if (veiculo is null) return NotFound();
            return Ok(veiculo);
        }

        /// <summary>Cadastra um novo veículo.</summary>
        /// <param name="dto">
        /// Dados do veículo:
        /// <list type="bullet">
        ///   <item><c>modelo</c> — modelo do veículo (ex.: <c>Gol</c>).</item>
        ///   <item><c>marca</c> — fabricante (ex.: <c>Volkswagen</c>).</item>
        ///   <item><c>placa</c> — placa no formato antigo (<c>ABC1234</c>) ou Mercosul (<c>ABC1D23</c>).</item>
        ///   <item><c>ano</c> — ano de fabricação (ex.: <c>2022</c>).</item>
        /// </list>
        /// </param>
        [HttpPost]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> Create([FromBody] VeiculoRequestDto dto)
        {
            var id = await _service.CriarAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        /// <summary>Atualiza os dados de um veículo existente.</summary>
        /// <param name="id">ID do veículo a ser atualizado.</param>
        /// <param name="dto">Novos dados do veículo (mesmos campos do cadastro).</param>
        [HttpPut("{id:int}")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> Update(int id, [FromBody] VeiculoRequestDto dto)
        {
            if (!await _service.AtualizarAsync(id, dto)) return NotFound();
            return Ok(dto);
        }

        /// <summary>Remove um veículo do sistema.</summary>
        /// <param name="id">ID do veículo a ser removido.</param>
        [HttpDelete("{id:int}")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.ExcluirAsync(id)) return NotFound();
            return NoContent();
        }
    }
}
