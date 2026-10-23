using Atendimento.Domain.Interfaces;
using Atendimento.Domain.ValueObjects;
using Compartilhado.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using ClienteEntity = Atendimento.Domain.Entities.Cliente;

namespace Atendimento.Infrastructure.Repositories
{
    public class ClienteRepositorio : BaseRepository<ClienteEntity>, IClienteRepositorio
    {
        public ClienteRepositorio(AppDbContext context) : base(context) { }

        public async Task<ClienteEntity?> ObterPorNumeroDocumentoAsync(string numeroDocumento)
        {
            Documento doc = numeroDocumento;
            return await _dbSet.FirstOrDefaultAsync(c => c.NumeroDocumento == doc);
        }
    }
}
