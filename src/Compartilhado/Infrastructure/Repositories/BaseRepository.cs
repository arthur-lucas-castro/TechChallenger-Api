using System.Linq.Expressions;
using Compartilhado.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Compartilhado.Infrastructure.Repositories
{
    public abstract class BaseRepository<TEntidade> where TEntidade : class, IAggregateRoot
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntidade> _dbSet;

        protected BaseRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntidade>();
        }

        public async Task<TEntidade?> GetByIdAsync(int id)
            => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<TEntidade>> GetAllAsync()
            => await _dbSet.ToListAsync();

        public async Task<IEnumerable<TEntidade>> GetByExpressionAsync(Expression<Func<TEntidade, bool>> predicate)
            => await _dbSet.Where(predicate).ToListAsync();

        public async Task<int> InsertAsync(TEntidade entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            var idProp = typeof(TEntidade).GetProperty("Id");
            return idProp is not null ? (int)idProp.GetValue(entity)! : 0;
        }

        public async Task<bool> UpdateAsync(TEntidade entity)
        {
            _dbSet.Update(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity is null) return false;
            _dbSet.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
