using System.Collections.Concurrent;
using TinyUrlSvc.Entity;

namespace TinyUrlSvc.Persistence;

/// <summary>
/// An in-memory repository storing <typeparamref name="T"/> entities by their UrlId Id.
/// </summary>
/// <typeparam name="T">An entity type that implements <see cref="IEntity"/>.</typeparam>
public class InMemoryRepository<T> : IRepository<T> where T : IEntity
{
    private readonly ConcurrentDictionary<UrlId, T> _storage = new();
    public Task<T?> GetAsync(UrlId id)
    {
        _storage.TryGetValue(id, out T? entity);
        return Task.FromResult(entity);
    }
    public Task<IEnumerable<T>> GetAllAsync()
    {
        // Return a snapshot of all values
        var entities = _storage.Values.ToArray();
        return Task.FromResult<IEnumerable<T>>(entities);
    }

    public Task<T> CreateAsync(T entity)
    {
        if (entity.Id == UrlId.Empty)
        {
            throw new InvalidOperationException("Entity must have a valid (non-empty) Id to be created.");
        }

        bool added = _storage.TryAdd(entity.Id, entity);
        if (!added)
        {
            throw new InvalidOperationException($"An entity with Id {entity.Id} already exists.");
        }

        return Task.FromResult(entity);
    }
    public Task UpdateAsync(T entity)
    {
        if (!_storage.ContainsKey(entity.Id))
        {
            throw new KeyNotFoundException($"No entity found with Id {entity.Id}.");
        }

        // Overwrite the existing entity
        _storage[entity.Id] = entity;
        return Task.CompletedTask;
    }
    public Task<bool> DeleteAsync(UrlId id)
    {
        bool removed = _storage.TryRemove(id, out _);
        return Task.FromResult(removed);
    }
}