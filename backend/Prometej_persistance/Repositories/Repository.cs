using Microsoft.EntityFrameworkCore;
using Prometej_core.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prometej_persistance.Repositories
{
   public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly DataContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public Repository(DataContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public void Create(TEntity entity)
        {
            _dbSet.Add(entity);
        }
        public void Update(TEntity entity)
        {
            if (entity != null)
            {
                _dbSet.Update(entity);
            }
        }
        public void Delete(object id)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            TEntity entityToDelete = _dbSet.Find(id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (entityToDelete != null)
            {
                _dbSet.Remove(entityToDelete);
            }
        }
#pragma warning disable CS8603 // Possible null reference return.
        public TEntity Get(object id) => _dbSet.Find(id);
#pragma warning restore CS8603 // Possible null reference return.
        public IQueryable<TEntity> ReadAll()
        {
            return _dbSet.AsNoTracking();
        }
        public virtual void Save()
        {
            _context.SaveChanges();
        }
        public virtual async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }
        public IQueryable<TEntity> GetAll()
        {
            return _dbSet;
        }
    }
}
