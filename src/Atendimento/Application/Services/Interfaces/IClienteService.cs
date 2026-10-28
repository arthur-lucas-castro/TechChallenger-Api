using Atendimento.Application.DTOs;

namespace Atendimento.Application.Services.Interfaces
{
    public interface IClienteService
    {
        Task<ClienteResponseDto?> ObterPorIdAsync(int id);
        Task<ClienteResponseDto?> ObterPorNumeroDocumentoAsync(string numeroDocumento);
        Task<IEnumerable<ClienteResponseDto>> ObterTodosAsync();
        Task<IEnumerable<ClienteResponseDto>> ObterPorIdsAsync(IEnumerable<int> ids);
        Task<int> CriarAsync(ClienteRequestDto dto);
        Task<bool> AtualizarAsync(int id, ClienteRequestDto dto);
        Task<bool> ExcluirAsync(int id);
        Task<bool> ResponderOrcamentoAsync(int clienteId, int ordemServicoId, ResponderOrcamentoDto dto);
    }
}
