using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_core.DataAccessLayer
{
    public interface IRepository<TEntity> where TEntity : class
    {
        IQueryable<TEntity> GetAll();
        IQueryable<TEntity> ReadAll();
        TEntity Get(object id);
        void Create(TEntity entity);
        void Update(TEntity entity);
        void Delete(object id);
        void Save();
        Task<int> SaveAsync();
    }
}
