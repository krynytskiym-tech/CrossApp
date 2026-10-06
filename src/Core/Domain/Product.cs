using Core.Dto;

namespace Core.Domain;

public sealed class Product
{
    private int _quantity;
    private readonly List<Movement> _movements = [];

    public string Id { get; }
    public string Sku { get; }
    public string Name { get; }
    public string Unit { get; }
    public int Quantity => _quantity;
    public ProductStatus Status { get; private set; } = ProductStatus.Active;

    // Назовні — лише читання: Add/Clear недоступні, приведення до List неможливе.
    public IReadOnlyList<Movement> Movements => _movements.AsReadOnly();

    private Product(string id, string sku, string name, string unit, int quantity)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Unit = unit;
        _quantity = quantity;
    }

    // Єдиний спосіб створити товар: усі перевірки тут, до виклику конструктора.
    public static Product Create(string id, string sku, string name, string unit, int quantity)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU не може бути порожнім", nameof(sku));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва не може бути порожньою", nameof(name));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Одиниця виміру не може бути порожньою", nameof(unit));
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity,
                "Початковий залишок не може бути від'ємним");

        return new Product(id.Trim(), sku.Trim().ToUpperInvariant(),
            name.Trim(), unit.Trim(), quantity);
    }

    public void RegisterArrival(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість приходу має бути більшою за нуль");
        if (Status != ProductStatus.Active)
            throw new InvalidOperationException(
                $"Не можна оформити прихід: товар {Sku} має статус {Status}");

        _quantity += amount;
        _movements.Add(new Movement(MovementKind.Arrival, amount));
    }

    public void Issue(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount,
                "Кількість видачі має бути більшою за нуль");
        if (Status == ProductStatus.Archived)
            throw new InvalidOperationException(
                $"Не можна оформити видачу: товар {Sku} має статус {Status}");
        if (amount > _quantity)
            throw new InvalidOperationException(
                $"Не можна видати {amount}: залишок {Sku} = {_quantity}");

        _quantity -= amount;
        _movements.Add(new Movement(MovementKind.Issue, amount));
    }

    public void Discontinue() => ChangeStatus(ProductStatus.Discontinued);

    public void Reactivate() => ChangeStatus(ProductStatus.Active);

    public void Archive() => ChangeStatus(ProductStatus.Archived);

    // Допустимі переходи статусу описані таблицею правил (switch expression).
    private void ChangeStatus(ProductStatus target)
    {
        bool allowed = (Status, target) switch
        {
            (ProductStatus.Active, ProductStatus.Discontinued) => true,
            (ProductStatus.Discontinued, ProductStatus.Active) => true,
            (ProductStatus.Discontinued, ProductStatus.Archived) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"Недопустимий перехід статусу товару {Sku}: {Status} → {target}");
        if (target == ProductStatus.Archived && _quantity > 0)
            throw new InvalidOperationException(
                $"Не можна архівувати {Sku}: залишок {_quantity} {Unit} не нульовий");

        Status = target;
    }

    // Мапінг у формат тижня 3 і назад — знадобиться сховищу тижня 5.
    // Історія руху і статус у DTO не зберігаються: FromDto відновлює активний товар
    // із поточним залишком (формат DTO розширять на тижні 5).
    public ProductDto ToDto() => new(Id, Sku, Name, Unit, Quantity);

    public static Product FromDto(ProductDto dto) =>
        Create(dto.Id, dto.Sku, dto.Name, dto.Unit, dto.Quantity);

    public override string ToString() => $"{Id} [{Sku}] {Name} — {Quantity} {Unit}";
}
