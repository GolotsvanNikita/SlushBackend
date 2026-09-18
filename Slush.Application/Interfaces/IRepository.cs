using System.Linq.Expressions;

namespace Slush.Application.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        IQueryable<T> AsQueryable();
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}