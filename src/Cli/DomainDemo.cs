using Core.Domain;
using Core.Dto;

namespace Cli;

public static class DomainDemo
{
    public static int Run()
    {
        Console.WriteLine("=== Сценарій 1: успіх ===");
        Product product = Product.Create("P-001", "sku-001", "Цемент М400 25кг", "шт", 100);
        Console.WriteLine(product);
        product.RegisterArrival(50);
        product.Issue(30);
        Console.WriteLine(product);
        Console.WriteLine();

        Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
        TryDo("видача більша за залишок", () => product.Issue(1000));
        TryDo("видача нуля", () => product.Issue(0));
        TryDo("від'ємний прихід", () => product.RegisterArrival(-10));
        TryDo("порожній id", () => Product.Create("", "SKU-002", "Пісок", "т", 10));
        TryDo("порожній SKU", () => Product.Create("P-002", " ", "Пісок", "т", 10));
        TryDo("порожня одиниця виміру", () => Product.Create("P-002", "SKU-002", "Пісок", " ", 10));
        TryDo("від'ємний залишок", () => Product.Create("P-003", "SKU-003", "Цегла", "шт", -5));
        Console.WriteLine();

        Console.WriteLine($"Стан після відмов: {product}");
        Console.WriteLine("Історія руху:");
        foreach (Movement m in product.Movements)
            Console.WriteLine($" {m.Kind,-8} {m.Amount,5}");
        string castResult = product.Movements is List<Movement> ? "можливе" : "неможливе";
        Console.WriteLine($"Приведення Movements до List<Movement>: {castResult}");
        Console.WriteLine();

        Console.WriteLine("=== Сценарій 3: ToDto / FromDto ===");
        ProductDto dto = product.ToDto();
        Console.WriteLine($"ToDto: Id={dto.Id}, Sku={dto.Sku}, Quantity={dto.Quantity}");
        Product restored = Product.FromDto(dto);
        Console.WriteLine($"FromDto: {restored}");
        Console.WriteLine($"DTO після циклу ToDto → FromDto → ToDto рівний вихідному: {restored.ToDto() == dto}");
        TryDo("FromDto з порожнім SKU", () => Product.FromDto(new ProductDto("P-009", "", "Без SKU", "шт", 5)));
        Console.WriteLine();

        Console.WriteLine("=== Сценарій 4: імпорт → доменні сутності ===");
        var import = new ImportResult<ProductDto>(
            new List<ProductDto>
            {
                new ProductDto("P-010", "SKU-010", "Клей", "кг", 40),
                new ProductDto("", "SKU-011", "Без id", "шт", 5),
                new ProductDto("P-012", "SKU-012", "Профіль", "м", -3)
            },
            new List<string> { "рядок 7: очікую 5 колонок, отримав 4" });

        ImportResult<Product> domain = import.ToDomain();
        Console.WriteLine($"Створено сутностей: {domain.Items.Count}");
        foreach (Product p in domain.Items)
            Console.WriteLine($" {p}");
        Console.WriteLine($"Відхилено: {domain.Errors.Count}");
        foreach (string e in domain.Errors)
            Console.WriteLine($" ! {e}");
        Console.WriteLine();

        Console.WriteLine("=== Сценарій 5: статуси товару (додаткове завдання 3) ===");
        Product tile = Product.Create("P-020", "SKU-020", "Плитка керамічна", "м2", 10);
        Console.WriteLine($"Статус: {tile.Status}");
        TryDo("архівація з Active", () => tile.Archive());
        tile.Discontinue();
        Console.WriteLine($"Статус: {tile.Status}");
        TryDo("прихід у знятий із продажу", () => tile.RegisterArrival(5));
        TryDo("архівація з ненульовим залишком", () => tile.Archive());
        tile.Issue(10);
        tile.Archive();
        Console.WriteLine($"Статус: {tile.Status}, залишок {tile.Quantity}");
        TryDo("видача з архівного", () => tile.Issue(1));
        TryDo("повернення з архіву", () => tile.Reactivate());
        Console.WriteLine();

        Console.WriteLine("=== Сценарій 6: правило між двома сутностями (додаткове завдання 2) ===");
        Warehouse wh = Warehouse.Create("W-01", "Головний склад", 200);
        wh.AddProduct(Product.Create("P-030", "SKU-030", "Цемент", "шт", 120));
        wh.AddProduct(Product.Create("P-031", "SKU-031", "Пісок", "т", 50));
        Console.WriteLine($"Склад {wh.Name}: зайнято {wh.TotalQuantity} із {wh.Capacity}, вільно {wh.FreeSpace}");
        TryDo("прихід понад місткість", () => wh.Receive("sku-030", 50));
        TryDo("дублікат SKU", () => wh.AddProduct(Product.Create("P-032", "SKU-030", "Цемент 2", "шт", 1)));
        TryDo("невідомий SKU", () => wh.Issue("SKU-999", 1));
        wh.Receive("sku-030", 30);
        Console.WriteLine($"Після приходу: зайнято {wh.TotalQuantity} із {wh.Capacity}, вільно {wh.FreeSpace}");
        TryDo("товар більший за вільне місце", () => wh.AddProduct(Product.Create("P-033", "SKU-033", "Клей", "кг", 5)));

        return 0;
    }

    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($" {title}: виняток НЕ спрацював — інваріант відсутній!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message.ReplaceLineEndings(" ")}");
        }
    }
}
