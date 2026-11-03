using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class CatalogService(ICatalogStore store, INotificationSink? sink = null)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public event EventHandler<CatalogChangedEventArgs>? Changed;

    // Єдине місце підняття події. Викликається лише ПІСЛЯ успішної зміни стану.
    private void OnChanged(Product p, CatalogChangeKind kind, int delta, int? quantityAfter = null)
    {
        var args = new CatalogChangedEventArgs(p.Id, kind, delta, quantityAfter ?? p.Quantity);
        Changed?.Invoke(this, args);
        sink?.Notify(args);
    }

    public Product Add(string sku, string name, string unit, int quantity)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product);   // інваріанти вже перевірив Product.Create
        OnChanged(product, CatalogChangeKind.Added, quantity);   // після зміни, не до
        return product;
    }

    public void Receive(string id, int quantity)
    {
        var product = Get(id);
        product.RegisterArrival(quantity);   // якщо тут виняток — події не буде
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public void Issue(string id, int quantity)
    {
        var product = Get(id);
        product.Issue(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Issued, -quantity);
    }

    public void Remove(string id)
    {
        var product = Get(id);
        int before = product.Quantity;
        _store.Remove(id);
        OnChanged(product, CatalogChangeKind.Removed, -before, 0);
    }

    // Асинхронний імпорт: імпортер приходить параметром (ручний DI), події піднімаються для кожного доданого запису.
    // progress (додаткове 3): відсотки виконання.
    public async Task<int> ImportAsync(
        ICatalogImporter importer, string path, CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();

        var items = await importer.LoadAsync(path, ct);
        var added = 0;
        var processed = 0;

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            processed++;

            if (_store.GetById(item.Id) is null)   // ідемпотентність імпорту
            {
                _store.Add(item);
                OnChanged(item, CatalogChangeKind.Added, item.Quantity);
                added++;
            }

            progress?.Report(processed * 100 / items.Count);
        }

        return added;
    }

    // Додаткове 1: потоковий імпорт (await foreach). progress звітує кількістю опрацьованих записів.
    public async Task<int> ImportStreamAsync(
        ICatalogImporter importer, string path, CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();

        var added = 0;
        var processed = 0;

        await foreach (var item in importer.StreamAsync(path, ct).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            processed++;

            if (_store.GetById(item.Id) is null)
            {
                _store.Add(item);
                OnChanged(item, CatalogChangeKind.Added, item.Quantity);
                added++;
            }

            progress?.Report(processed);
        }

        return added;
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    // Критерій задає той, хто викликає: Func повертає значення, подія для цього не годиться.
    public IReadOnlyList<Product> FindAll(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    private Product Get(string id) =>
        _store.GetById(id) ?? throw new DomainException($"Немає запису з id={id}.");
}
