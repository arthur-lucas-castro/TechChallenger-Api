using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Atendimento.Application.DTOs;
using Atendimento.Application.Services.Interfaces;

namespace Atendimento.Presentation.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _service;

        public ClienteController(IClienteService service) => _service = service;

        /// <summary>Lista todos os clientes cadastrados.</summary>
        [HttpGet]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> GetAll() => Ok(await _service.ObterTodosAsync());

        /// <summary>Retorna os dados de um cliente pelo ID.</summary>
        /// <param name="id">ID do cliente.</param>
        [HttpGet("{id:int}")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> GetById(int id)
        {
            var cliente = await _service.ObterPorIdAsync(id);
            if (cliente is null) return NotFound();
            return Ok(cliente);
        }

        /// <summary>Busca um cliente pelo número do documento (CPF ou CNPJ).</summary>
        /// <param name="documentNumber">Número do documento sem formatação (ex.: <c>12345678901</c>).</param>
        [HttpGet("{documentNumber}")]
        public async Task<IActionResult> GetByDocument(string documentNumber)
        {
            var cliente = await _service.ObterPorNumeroDocumentoAsync(documentNumber);
            if (cliente is null) return NotFound();
            return Ok(cliente);
        }

        /// <summary>Cadastra um novo cliente.</summary>
        /// <param name="dto">
        /// Dados do cliente:
        /// <list type="bullet">
        ///   <item><c>nome</c> — primeiro nome.</item>
        ///   <item><c>sobrenome</c> — sobrenome.</item>
        ///   <item><c>telefone</c> — número de telefone com DDD (ex.: <c>11999999999</c>).</item>
        ///   <item><c>email</c> — endereço de e-mail válido.</item>
        ///   <item><c>numeroDocumento</c> — CPF ou CNPJ sem formatação.</item>
        ///   <item><c>tipoPessoa</c> — <c>0</c> = Física, <c>1</c> = Jurídica.</item>
        /// </list>
        /// </param>
        [HttpPost]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> Create([FromBody] ClienteRequestDto dto)
        {
            var id = await _service.CriarAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        /// <summary>Atualiza os dados de um cliente existente.</summary>
        /// <param name="id">ID do cliente a ser atualizado.</param>
        /// <param name="dto">Novos dados do cliente (mesmos campos do cadastro).</param>
        [HttpPut("{id:int}")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> Update(int id, [FromBody] ClienteRequestDto dto)
        {
            if (!await _service.AtualizarAsync(id, dto)) return NotFound();
            return Ok(dto);
        }

        /// <summary>Remove um cliente do sistema.</summary>
        /// <param name="id">ID do cliente a ser removido.</param>
        [HttpDelete("{id:int}")]
        [ApiExplorerSettings(GroupName = "GestaoAdministrativa")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _service.ExcluirAsync(id)) return NotFound();
            return NoContent();
        }

        /// <summary>Registra a resposta do cliente a um orçamento de ordem de serviço.</summary>
        /// <param name="clienteId">ID do cliente que está respondendo.</param>
        /// <param name="ordemServicoId">ID da ordem de serviço cujo orçamento está sendo respondido.</param>
        /// <param name="dto">
        /// Resposta ao orçamento:
        /// <list type="bullet">
        ///   <item><c>aprovado</c> — <c>true</c> aprova o orçamento e inicia a execução; <c>false</c> recusa.</item>
        /// </list>
        /// </param>
        [HttpPost("{clienteId:int}/ordens-servico/{ordemServicoId:int}/responder-orcamento")]
        public async Task<IActionResult> ResponderOrcamento(int clienteId, int ordemServicoId, [FromBody] ResponderOrcamentoDto dto)
        {
            if (!await _service.ResponderOrcamentoAsync(clienteId, ordemServicoId, dto)) return NotFound();
            return NoContent();
        }
    }
}
