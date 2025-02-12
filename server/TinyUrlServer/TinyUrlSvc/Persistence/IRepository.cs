using TinyUrlSvc.Entity;

namespace TinyUrlSvc.Persistence
{
    public interface IRepository<T>
    {
        Task<T?> GetAsync(UrlId id);

        Task<IEnumerable<T>> GetAllAsync();

        Task<T> CreateAsync(T item);

        Task UpdateAsync(T item);

        Task<bool> DeleteAsync(UrlId id);
    }
}
