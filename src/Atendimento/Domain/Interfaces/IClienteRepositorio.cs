using System.Linq.Expressions;
using ClienteEntity = Atendimento.Domain.Entities.Cliente;

namespace Atendimento.Domain.Interfaces
{
    public interface IClienteRepositorio
    {
        Task<ClienteEntity?> GetByIdAsync(int id);
        Task<IEnumerable<ClienteEntity>> GetAllAsync();
        Task<IEnumerable<ClienteEntity>> GetByExpressionAsync(Expression<Func<ClienteEntity, bool>> predicate);
        Task<int> InsertAsync(ClienteEntity cliente);
        Task<bool> UpdateAsync(ClienteEntity cliente);
        Task<bool> DeleteAsync(int id);
        Task<ClienteEntity?> ObterPorNumeroDocumentoAsync(string numeroDocumento);
    }
}
