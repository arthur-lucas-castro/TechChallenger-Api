using Operacao.Application.DTOs;
using Operacao.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Operacao.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class StatusOrdemServicoController : ControllerBase
    {
        private readonly IOrdemServicoService _service;
        public StatusOrdemServicoController(IOrdemServicoService service) => _service = service;

        /// <summary>
        /// Inicia o diagnóstico do veículo, avançando o status de <c>Recebida</c> para <c>EmDiagnostico</c>.
        /// </summary>
        /// <param name="id">ID da ordem de serviço.</param>
        [HttpPatch("{id:int}/iniciar-diagnostico")]
        public async Task<IActionResult> IniciarDiagnostico(int id)
        {
            if (!await _service.IniciarDiagnosticoAsync(id)) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Finaliza o diagnóstico e gera o orçamento para aprovação do cliente,
        /// avançando o status para <c>AguardandoAprovacao</c>.
        /// </summary>
        /// <remarks>
        /// Pode ser chamado a partir dos status <c>Recebida</c> ou <c>EmDiagnostico</c>.
        /// O orçamento é calculado com base nos serviços e peças já adicionados à ordem.
        /// </remarks>
        /// <param name="id">ID da ordem de serviço.</param>
        [HttpPatch("{id:int}/finalizar-diagnostico")]
        public async Task<IActionResult> FinalizarDiagnostico(int id)
        {
            if (!await _service.FinalizarDiagnosticoAsync(id)) return NotFound();
            return NoContent();
        }

        /// <summary>Altera o status de uma ordem de serviço para qualquer transição válida do fluxo.</summary>
        /// <remarks>
        /// Transições permitidas:
        /// <list type="bullet">
        ///   <item><c>Recebida</c> → <c>EmDiagnostico</c></item>
        ///   <item><c>Recebida</c> ou <c>EmDiagnostico</c> → <c>AguardandoAprovacao</c></item>
        ///   <item><c>AguardandoAprovacao</c> → <c>EmExecucao</c> (exige orçamento aprovado)</item>
        ///   <item><c>EmExecucao</c> → <c>Finalizada</c></item>
        ///   <item><c>Finalizada</c> → <c>Entregue</c></item>
        /// </list>
        /// </remarks>
        /// <param name="id">ID da ordem de serviço.</param>
        /// <param name="dto">
        /// Novo status:
        /// <list type="bullet">
        ///   <item><c>status</c> — nome do status destino (ex.: <c>EmDiagnostico</c>, <c>Finalizada</c>).</item>
        /// </list>
        /// </param>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> AlterarStatus(int id, [FromBody] AlterarStatusOrdemServicoDto dto)
        {
            if (!await _service.AlterarStatusAsync(id, dto)) return NotFound();
            return NoContent();
        }

        /// <summary>Marca um serviço solicitado como em execução.</summary>
        /// <param name="ordemServicoId">ID da ordem de serviço.</param>
        /// <param name="servicoSolicitadoId">ID do item de serviço solicitado (não é o ID do serviço do catálogo).</param>
        [HttpPatch("{ordemServicoId:int}/iniciar-servico/{servicoSolicitadoId:int}")]
        public async Task<IActionResult> IniciarServico(int ordemServicoId, int servicoSolicitadoId)
        {
            if (!await _service.IniciarServicoAsync(ordemServicoId, servicoSolicitadoId)) return NotFound();
            return NoContent();
        }

        /// <summary>Marca um serviço solicitado como executado (concluído).</summary>
        /// <param name="ordemServicoId">ID da ordem de serviço.</param>
        /// <param name="servicoSolicitadoId">ID do item de serviço solicitado (não é o ID do serviço do catálogo).</param>
        [HttpPatch("{ordemServicoId:int}/finalizar-servico/{servicoSolicitadoId:int}")]
        public async Task<IActionResult> FinalizarServico(int ordemServicoId, int servicoSolicitadoId)
        {
            if (!await _service.FinalizarServicoAsync(ordemServicoId, servicoSolicitadoId)) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Retorna métricas de tempo de execução por tipo de serviço:
        /// tempo médio e pior tempo registrados nas ordens finalizadas.
        /// </summary>
        [HttpGet("tempos-servico")]
        public async Task<IActionResult> GetTemposServico()
            => Ok(await _service.ObterTemposExecucaoPorServicoAsync());
    }
}
