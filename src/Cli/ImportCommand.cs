using Core.Domain;
using Core.Dto;
using Core.Import;

namespace Cli;

public static class ImportCommand
{
    public static int Run(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
            return 1;
        }

        string extension = Path.GetExtension(path).ToLowerInvariant();

        return extension switch
        {
            ".csv" => ShowProducts(ProductCsvImporter.Load(path), "рядків"),
            ".json" => ShowProducts(ProductJsonImporter.Load(path), "записів"),
            ".txt" => ShowMixed(MixedCsvImporter.Load(path)),
            _ => ShowUnsupported(extension)
        };
    }

    private static int ShowProducts(ImportResult<ProductDto> result, string unit)
    {
        Console.WriteLine($"Завантажено записів: {result.Items.Count}");
        PrintProducts(result.Items);
        PrintTail(ImportStats.From(result), result.Errors, unit);

        ImportResult<Product> domain = result.ToDomain();
        int rejected = domain.Errors.Count - result.Errors.Count;
        Console.WriteLine($"Доменна перевірка: створено {domain.Items.Count} сутностей Product, відхилено {rejected}");
        foreach (string e in domain.Errors.Skip(result.Errors.Count))
            Console.WriteLine($" ! {e}");

        return 0;
    }

    private static int ShowMixed(MixedImportResult result)
    {
        Console.WriteLine($"Товарів: {result.Products.Count}");
        PrintProducts(result.Products);

        Console.WriteLine($"Складів: {result.Warehouses.Count}");
        foreach (WarehouseDto w in result.Warehouses.Take(5))
            Console.WriteLine($" {w.Id,-6} {w.Name,-26} {w.City}");

        var stats = new ImportStats(result.Products.Count + result.Warehouses.Count, result.Errors.Count);
        PrintTail(stats, result.Errors, "рядків");
        return 0;
    }

    private static int ShowUnsupported(string extension)
    {
        Console.WriteLine($"Непідтримуване розширення '{extension}'. Очікую .csv, .json або .txt");
        return 2;
    }

    private static void PrintProducts(IReadOnlyList<ProductDto> products)
    {
        foreach (ProductDto p in products.Take(5))
            Console.WriteLine($" {p.Id,-6} {p.Sku,-10} {p.Name,-26} {p.Quantity,5} {p.Unit}");
    }

    private static void PrintTail(ImportStats stats, IReadOnlyList<string> errors, string unit)
    {
        if (errors.Count > 0)
        {
            Console.WriteLine($"Пропущено {unit}: {errors.Count}");
            foreach (string e in errors)
                Console.WriteLine($" ! {e}");
        }

        Console.WriteLine($"Статистика: {stats.Summary()}");
    }
}
