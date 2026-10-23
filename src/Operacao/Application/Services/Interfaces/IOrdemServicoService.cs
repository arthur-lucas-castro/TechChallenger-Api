using Compartilhado.Domain.ValueObjects;
using Operacao.Application.DTOs;

namespace Operacao.Application.Services.Interfaces
{
    public interface IOrdemServicoService
    {
        Task<OrdemServicoResponseDto?> ObterPorIdAsync(int id);
        Task<OrdemServicoDetalhadaResponseDto?> ObterDetalhadoPorIdAsync(int id);
        Task<IEnumerable<OrdemServicoResponseDto>> ObterTodosAsync(List<StatusOrdemServico>? status = null);
        Task<int> CriarAsync(OrdemServicoRequestDto dto);
        Task<bool> ExcluirAsync(int id);
        Task<bool> AlterarStatusAsync(int id, AlterarStatusOrdemServicoDto dto);
        Task<bool> AdicionarServicoAsync(int ordemServicoId, ServicoSolicitadoRequestDto dto);
        Task<bool> RemoverServicoAsync(int ordemServicoId, int servicoId);
        Task<bool> AdicionarPecaAsync(int ordemServicoId, PecaSolicitadaRequestDto dto);
        Task<bool> RemoverPecaAsync(int ordemServicoId, int pecaId);
        Task<bool> IniciarDiagnosticoAsync(int id);
        Task<bool> FinalizarDiagnosticoAsync(int id);
        Task<bool> ConfirmarPagamentoAsync(int id);
        Task<bool> IniciarServicoAsync(int ordemServicoId, int servicoSolicitadoId);
        Task<bool> FinalizarServicoAsync(int ordemServicoId, int servicoSolicitadoId);
        Task<IEnumerable<TempoExecucaoServicoResponseDto>> ObterTemposExecucaoPorServicoAsync();
    }
}
