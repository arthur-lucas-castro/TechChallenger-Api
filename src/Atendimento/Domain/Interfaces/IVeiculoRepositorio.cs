using System.Linq.Expressions;
using Atendimento.Domain.Entities;

namespace Atendimento.Domain.Interfaces
{
    public interface IVeiculoRepositorio
    {
        Task<Veiculo?> GetByIdAsync(int id);
        Task<IEnumerable<Veiculo>> GetAllAsync();
        Task<IEnumerable<Veiculo>> GetByExpressionAsync(Expression<Func<Veiculo, bool>> predicate);
        Task<int> InsertAsync(Veiculo veiculo);
        Task<bool> UpdateAsync(Veiculo veiculo);
        Task<bool> DeleteAsync(int id);
        Task<Veiculo?> GetByPlacaAsync(string placa);
    }
}
