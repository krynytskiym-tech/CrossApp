using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Singleton у Api може обслуговувати паралельні запити, тому доступ до словника під lock.
public sealed class InMemoryCatalogStore(IEnumerable<Product>? seed = null) : ICatalogStore
{
    private readonly object _gate = new();

    private readonly Dictionary<string, Product> _items =
        (seed ?? []).ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Product> List()
    {
        lock (_gate)
            return _items.Values.ToList();
    }

    public Product? GetById(string id)
    {
        lock (_gate)
            return _items.GetValueOrDefault(id);
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_gate)
        {
            if (_items.ContainsKey(item.Id))
                throw new DomainException($"Запис з id={item.Id} уже існує.");
            _items.Add(item.Id, item);
        }
    }

    public void Update(Product item)
    {
        lock (_gate)
            _items[item.Id] = item;
    }

    public bool Remove(string id)
    {
        lock (_gate)
            return _items.Remove(id);
    }
}
