using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CliniKa.Domain.Repositories
{
    public interface IGenericRepository<TEntity, TKey>
    {
        Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            string? sortBy = null,
            bool isDescending = false,
            bool trackChanges = false,
            params Expression<Func<TEntity, object>>[] includes);
       
        Task<TEntity?> GetByIdAsync(TKey id);

        Task<TEntity?> GetAsync(
            Expression<Func<TEntity, bool>> filter, 
            bool trackChanges = false,
            params Expression<Func<TEntity, object>>[] includes);

        void Add(TEntity entity);
        void AddRange(IEnumerable<TEntity> entities);
        void Delete(TEntity entity);
        void DeleteRange(IEnumerable<TEntity> entities);
    }
}
