using Compartilhado.Domain.ValueObjects;
using Operacao.Domain.Entities;

namespace Operacao.Domain.Interfaces
{
    public interface IOrdemServicoRepositorio
    {
        Task<OrdemServico?> GetByIdAsync(int id);
        Task<IEnumerable<OrdemServico>> GetAllAsync(IReadOnlyList<StatusOrdemServico> status);
        Task<int> InsertAsync(OrdemServico ordemServico);
        Task<bool> UpdateAsync(OrdemServico ordemServico);
        Task<bool> DeleteAsync(int id);
        Task<OrdemServico?> GetByIdComServicosAsync(int id);
        Task<OrdemServico?> GetByIdComPecasAsync(int id);
        Task<OrdemServico?> GetByIdComItensAsync(int id);
        Task<OrdemServico?> GetByIdComServicosEExecucaoAsync(int id);
        Task<OrdemServico?> GetByIdDetalhadoAsync(int id);
        Task<bool> CommitAsync();
        Task<IEnumerable<(int ServicoId, IEnumerable<double> Tempos)>> ObterTemposExecucaoPorServicoAsync();
    }
}
