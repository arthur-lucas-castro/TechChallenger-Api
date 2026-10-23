using Compartilhado.Domain.ValueObjects;
using Operacao.Application.DTOs;
using Operacao.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Operacao.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class OrdemServicoController : ControllerBase
    {
        private readonly IOrdemServicoService _service;
        public OrdemServicoController(IOrdemServicoService service) => _service = service;

        /// <summary>
        /// Lista as ordens de serviço em andamento (exclui Finalizada e Entregue), ordenadas por prioridade
        /// de status (Em Execução > Aguardando Aprovação > Diagnóstico > Recebida) e, dentro do mesmo status,
        /// pelas mais antigas primeiro.
        /// </summary>
        /// <param name="status">Filtra por uma lista de status (opcional, repetir o parâmetro para múltiplos valores, ex.: <c>?status=EmExecucao&amp;status=Recebida</c>). Se omitido, lista Em Execução, Aguardando Aprovação, Diagnóstico e Recebida.</param>
        [HttpGet]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> GetAll([FromQuery] List<StatusOrdemServico>? status = null) => Ok(await _service.ObterTodosAsync(status));

        /// <summary>Retorna os detalhes completos de uma ordem de serviço, incluindo serviços, peças e orçamento.</summary>
        /// <param name="id">ID da ordem de serviço.</param>
        [HttpGet("{id:int}")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> GetById(int id)
        {
            var os = await _service.ObterDetalhadoPorIdAsync(id);
            if (os is null) return NotFound();
            return Ok(os);
        }

        /// <summary>Abre uma nova ordem de serviço para um veículo e cliente.</summary>
        /// <param name="dto">
        /// Dados da ordem de serviço:
        /// <list type="bullet">
        ///   <item><c>veiculoId</c> — ID do veículo a ser atendido.</item>
        ///   <item><c>clienteId</c> — ID do cliente proprietário do veículo.</item>
        ///   <item><c>servicos</c> — lista de serviços a incluir: <c>servicoId</c> e <c>quantidade</c>.</item>
        ///   <item><c>pecas</c> — lista de peças a incluir: <c>pecaId</c> e <c>quantidade</c>.</item>
        /// </list>
        /// </param>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrdemServicoRequestDto dto)
        {
            var id = await _service.CriarAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        /// <summary>Adiciona um serviço a uma ordem de serviço existente.</summary>
        /// <param name="id">ID da ordem de serviço.</param>
        /// <param name="dto">
        /// Serviço a adicionar:
        /// <list type="bullet">
        ///   <item><c>servicoId</c> — ID do serviço do catálogo.</item>
        ///   <item><c>quantidade</c> — quantidade de execuções do serviço.</item>
        /// </list>
        /// </param>
        [HttpPost("{id:int}/servicos")]
        public async Task<IActionResult> AdicionarServico(int id, [FromBody] ServicoSolicitadoRequestDto dto)
        {
            try
            {
                if (!await _service.AdicionarServicoAsync(id, dto)) return NotFound();
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>Remove um serviço de uma ordem de serviço.</summary>
        /// <param name="id">ID da ordem de serviço.</param>
        /// <param name="servicoId">ID do serviço a ser removido da ordem.</param>
        [HttpDelete("{id:int}/servicos/{servicoId:int}")]
        public async Task<IActionResult> RemoverServico(int id, int servicoId)
        {
            if (!await _service.RemoverServicoAsync(id, servicoId)) return NotFound();
            return NoContent();
        }

        /// <summary>Adiciona uma peça a uma ordem de serviço existente.</summary>
        /// <param name="id">ID da ordem de serviço.</param>
        /// <param name="dto">
        /// Peça a adicionar:
        /// <list type="bullet">
        ///   <item><c>pecaId</c> — ID da peça do catálogo.</item>
        ///   <item><c>quantidade</c> — quantidade de unidades da peça.</item>
        /// </list>
        /// </param>
        [HttpPost("{id:int}/pecas")]
        public async Task<IActionResult> AdicionarPeca(int id, [FromBody] PecaSolicitadaRequestDto dto)
        {
            try
            {
                if (!await _service.AdicionarPecaAsync(id, dto)) return NotFound();
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>Remove uma peça de uma ordem de serviço.</summary>
        /// <param name="id">ID da ordem de serviço.</param>
        /// <param name="pecaId">ID da peça a ser removida da ordem.</param>
        [HttpDelete("{id:int}/pecas/{pecaId:int}")]
        public async Task<IActionResult> RemoverPeca(int id, int pecaId)
        {
            if (!await _service.RemoverPecaAsync(id, pecaId)) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Confirma o pagamento e registra a entrega do veículo ao cliente,
        /// alterando o status da ordem para <c>Entregue</c>.
        /// </summary>
        /// <remarks>A ordem deve estar com status <c>Finalizada</c> para que esta operação seja permitida.</remarks>
        /// <param name="id">ID da ordem de serviço.</param>
        [HttpPost("{id:int}/confirmar-pagamento")]
        public async Task<IActionResult> ConfirmarPagamento(int id)
        {
            if (!await _service.ConfirmarPagamentoAsync(id)) return NotFound();
            return NoContent();
        }

        /// <summary>Remove uma ordem de serviço do sistema.</summary>
        /// <param name="id">ID da ordem de serviço a ser removida.</param>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.ExcluirAsync(id)) return NotFound();
            return NoContent();
        }
    }
}
