using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storage.DataManager.Data
{
    public interface IRepository<TEntity>
    {
        Task<List<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(int id);
        IQueryable<TEntity> GetQueryable();
        void Insert(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        Task SaveChangesAsync();

    }
}
