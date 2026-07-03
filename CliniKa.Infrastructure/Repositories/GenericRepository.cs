using Microsoft.EntityFrameworkCore;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace CliniKa.Infrastructure.Repositories
{
    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> where TEntity : class
    {
        protected readonly AppDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            string? sortBy = null,
            bool isDescending = false,
            bool trackChanges = false,
            params Expression<Func<TEntity, object>>[] includes)
        {
            // will be updated to support pagination in the future [Fathi]

            IQueryable<TEntity> query = _dbSet;

            if (!trackChanges)
                query = query.AsNoTracking();

            if (filter != null)
                query = query.Where(filter);

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                var sortingDirection = isDescending ? "DESC" : "ASC";
                query = query.OrderBy($"{sortBy} {sortingDirection}");
            }

            return await query.ToListAsync();
        }

        public async Task<TEntity?> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<TEntity?> GetAsync(
            Expression<Func<TEntity, bool>> filter, 
            bool trackChanges = false,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            if (!trackChanges)
                query = query.AsNoTracking();

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(filter);
        }

        public void Add(TEntity entity)
        {
            _dbSet.Add(entity);
        }

        public void AddRange(IEnumerable<TEntity> entities)
        {
            _dbSet.AddRange(entities);
        }

     

        public void Delete(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public void DeleteRange(IEnumerable<TEntity> entities)
        {
            _dbSet.RemoveRange(entities);
        }
    }
}
