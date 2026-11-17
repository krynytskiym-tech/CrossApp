using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class FileCatalogStore(string path) : ICatalogStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    // Усі методи нижче виконуються під lock: сховище можна зареєструвати як Singleton в Api.
    private void EnsureLoaded()
    {
        if (_loaded) return;

        if (File.Exists(_path))
        {
            _cache.Clear();
            foreach (var product in CatalogJson.Read(_path))
                _cache[product.Id] = product;
        }

        _loaded = true;
    }

    private void Flush() => CatalogJson.Write(_path, _cache.Values);

    public IReadOnlyList<Product> List()
    {
        lock (_gate)
        {
            EnsureLoaded();
            return _cache.Values.ToList();
        }
    }

    public Product? GetById(string id)
    {
        lock (_gate)
        {
            EnsureLoaded();
            return _cache.GetValueOrDefault(id);
        }
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_gate)
        {
            EnsureLoaded();
            if (_cache.ContainsKey(item.Id))
                throw new DomainException($"Запис з id={item.Id} уже існує.");
            _cache.Add(item.Id, item);
            Flush();
        }
    }

    public void Update(Product item)
    {
        lock (_gate)
        {
            EnsureLoaded();
            _cache[item.Id] = item;
            Flush();
        }
    }

    public bool Remove(string id)
    {
        lock (_gate)
        {
            EnsureLoaded();
            if (!_cache.Remove(id)) return false;
            Flush();
            return true;
        }
    }
}
