using Compartilhado.Infrastructure.Repositories;
using Catalogo.Domain.Entities;
using Catalogo.Domain.Interfaces;
using Catalogo.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Infrastructure.Repositories
{
    public class PecaRepositorio : BaseRepository<Peca>, IPecaRepositorio
    {
        private readonly DbSet<ProdutoEstoque> _produtoEstoqueSet;

        public PecaRepositorio(AppDbContext context) : base(context)
        {
            _produtoEstoqueSet = context.Set<ProdutoEstoque>();
        }

        public async Task<Peca?> GetByIdComEstoqueAsync(int pecaId)
            => await _context.Set<Peca>()
                .Include(p => p.ProdutoEstoque)
                .FirstOrDefaultAsync(p => p.Id == pecaId);

        public async Task<IEnumerable<Peca>> GetAllComEstoqueAsync()
            => await _context.Set<Peca>()
                .Include(p => p.ProdutoEstoque)
                .ToListAsync();

        public async Task InsertProdutoEstoqueAsync(ProdutoEstoque produtoEstoque)
        {
            await _produtoEstoqueSet.AddAsync(produtoEstoque);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateProdutoEstoqueAsync(ProdutoEstoque produtoEstoque)
        {
            _produtoEstoqueSet.Update(produtoEstoque);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
