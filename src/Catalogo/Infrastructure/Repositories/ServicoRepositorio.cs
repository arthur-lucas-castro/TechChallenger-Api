using Compartilhado.Infrastructure.Repositories;
using Catalogo.Domain.Entities;
using Catalogo.Domain.Interfaces;

namespace Catalogo.Infrastructure.Repositories
{
    public class ServicoRepositorio : BaseRepository<Servico>, IServicoRepositorio
    {
        public ServicoRepositorio(AppDbContext context) : base(context) { }
    }
}
