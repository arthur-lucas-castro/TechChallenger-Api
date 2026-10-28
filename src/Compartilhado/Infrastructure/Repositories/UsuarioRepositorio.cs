using Compartilhado.Domain.Entities;
using Compartilhado.Domain.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Compartilhado.Infrastructure.Repositories
{
    public class UsuarioRepositorio : BaseRepository<Usuario>, IUsuarioRepositorio
    {
        public UsuarioRepositorio(AppDbContext context) : base(context) { }

        public async Task<Usuario?> ObterPorEmailAsync(string email)
            => await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
}
