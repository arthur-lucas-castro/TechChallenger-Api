using System.Linq.Expressions;
using Catalogo.Domain.Entities;

namespace Catalogo.Domain.Interfaces
{
    public interface IServicoRepositorio
    {
        Task<Servico?> GetByIdAsync(int id);
        Task<IEnumerable<Servico>> GetAllAsync();
        Task<IEnumerable<Servico>> GetByExpressionAsync(Expression<Func<Servico, bool>> predicate);
        Task<int> InsertAsync(Servico servico);
        Task<bool> UpdateAsync(Servico servico);
        Task<bool> DeleteAsync(int id);
    }
}
