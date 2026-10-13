using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Декоратор: той самий контракт ICatalogStore, нова поведінка — кеш читання.
// Будь-яка зміна (Add/Update/Remove) скидає кеш, тому застарілих даних не буде.
public sealed class CachingCatalogStore(ICatalogStore inner) : ICatalogStore
{
    private readonly ICatalogStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Product?> _byId = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Product>? _list;

    public int Hits { get; private set; }
    public int Misses { get; private set; }
    public string InnerName => _inner.GetType().Name;

    public IReadOnlyList<Product> List()
    {
        if (_list is not null)
        {
            Hits++;
            return _list;
        }

        Misses++;
        _list = _inner.List();
        return _list;
    }

    public Product? GetById(string id)
    {
        if (_byId.TryGetValue(id, out Product? cached))
        {
            Hits++;
            return cached;
        }

        Misses++;
        Product? found = _inner.GetById(id);
        _byId[id] = found;
        return found;
    }

    public void Add(Product item)
    {
        _inner.Add(item);
        Invalidate();
    }

    public void Update(Product item)
    {
        _inner.Update(item);
        Invalidate();
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        Invalidate();
        return removed;
    }

    private void Invalidate()
    {
        _byId.Clear();
        _list = null;
    }
}
