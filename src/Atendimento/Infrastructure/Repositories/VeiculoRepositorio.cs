using Atendimento.Domain.Entities;
using Atendimento.Domain.Interfaces;
using Atendimento.Domain.ValueObjects;
using Compartilhado.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Atendimento.Infrastructure.Repositories
{
    public class VeiculoRepositorio : BaseRepository<Veiculo>, IVeiculoRepositorio
    {
        public VeiculoRepositorio(AppDbContext context) : base(context) { }

        public async Task<Veiculo?> GetByPlacaAsync(string placa)
        {
            Placa placaVO = placa;
            return await _dbSet.FirstOrDefaultAsync(v => v.Placa == placaVO);
        }
    }
}
