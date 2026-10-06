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
