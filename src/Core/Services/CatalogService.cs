using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class CatalogService(ICatalogStore store)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Product Add(string sku, string name, string unit, int quantity)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product);   // інваріанти вже перевірив Product.Create
        return product;
    }

    public void Receive(string id, int quantity)
    {
        var product = Get(id);
        product.RegisterArrival(quantity);   // метод сутності з тижня 4
        _store.Update(product);
    }

    public void Issue(string id, int quantity)
    {
        var product = Get(id);
        product.Issue(quantity);
        _store.Update(product);
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    // Пошук за довільною умовою: підготовка до LINQ-звітів тижнів 6-7.
    public IReadOnlyList<Product> Search(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    private Product Get(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає запису з id={id}.");
}
