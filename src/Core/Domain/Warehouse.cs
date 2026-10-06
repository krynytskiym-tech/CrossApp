namespace Core.Domain;

// Корінь агрегату: правило «сума залишків не перевищує місткість» охоплює
// склад і кілька товарів, тому жоден окремий Product його гарантувати не може.
public sealed class Warehouse
{
    private readonly List<Product> _products = [];

    public string Id { get; }
    public string Name { get; }
    public int Capacity { get; }

    public IReadOnlyList<Product> Products => _products.AsReadOnly();
    public int TotalQuantity => _products.Sum(p => p.Quantity);
    public int FreeSpace => Capacity - TotalQuantity;

    private Warehouse(string id, string name, int capacity)
    {
        Id = id;
        Name = name;
        Capacity = capacity;
    }

    public static Warehouse Create(string id, string name, int capacity)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор складу обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва складу не може бути порожньою", nameof(name));
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
                "Місткість складу має бути більшою за нуль");

        return new Warehouse(id.Trim(), name.Trim(), capacity);
    }

    public void AddProduct(Product product)
    {
        if (product is null)
            throw new ArgumentNullException(nameof(product), "Товар обов'язковий");
        if (_products.Any(p => p.Sku == product.Sku))
            throw new InvalidOperationException(
                $"Товар {product.Sku} уже є на складі {Id}");
        if (product.Quantity > FreeSpace)
            throw new InvalidOperationException(
                $"Не можна додати {product.Sku}: кількість {product.Quantity} перевищує вільне місце {FreeSpace} на складі {Id}");

        _products.Add(product);
    }

    public void Receive(string sku, int amount)
    {
        Product product = Find(sku);

        if (amount > FreeSpace)
            throw new InvalidOperationException(
                $"Не можна прийняти {amount}: на складі {Id} вільно лише {FreeSpace} із {Capacity}");

        product.RegisterArrival(amount);
    }

    public void Issue(string sku, int amount) => Find(sku).Issue(amount);

    private Product Find(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU не може бути порожнім", nameof(sku));

        string key = sku.Trim().ToUpperInvariant();
        return _products.FirstOrDefault(p => p.Sku == key)
            ?? throw new InvalidOperationException($"Товар {key} не знайдено на складі {Id}");
    }
}
