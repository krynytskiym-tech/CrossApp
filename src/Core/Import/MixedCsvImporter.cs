using System.Globalization;
using System.Text;
using Core.Dto;

namespace Core.Import;

// Формат: перша колонка — префікс типу запису.
//   P;id;sku;name;unit;quantity   — товар
//   W;id;name;city                — склад
public static class MixedCsvImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var warehouses = new List<WarehouseDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseLine(line))
            {
                case ProductOk p:
                    products.Add(p.Value);
                    break;
                case WarehouseOk w:
                    warehouses.Add(w.Value);
                    break;
                case Failed f:
                    errors.Add($"рядок {number}: {f.Reason}");
                    break;
            }
        }

        return new MixedImportResult(products, warehouses, errors);
    }

    private static Outcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", "", _, _, _, _] or ["P", _, "", _, _, _] or ["P", _, _, "", _, _]
                => new Failed("id, SKU або назва товару порожні"),

            ["P", var id, var sku, var name, var unit, var qty]
                when int.TryParse(qty, NumberStyles.Integer, CultureInfo.InvariantCulture, out int q) && q >= 0
                => new ProductOk(new ProductDto(id, sku, name, unit, q)),

            ["P", _, _, _, _, var qty]
                => new Failed($"кількість '{qty}' не є невід'ємним числом"),

            ["P", ..]
                => new Failed($"товар (P) має містити 6 колонок, отримав {parts.Length}"),

            ["W", "", _, _] or ["W", _, "", _]
                => new Failed("id або назва складу порожні"),

            ["W", var id, var name, var city]
                => new WarehouseOk(new WarehouseDto(id, name, city)),

            ["W", ..]
                => new Failed($"склад (W) має містити 4 колонки, отримав {parts.Length}"),

            [var prefix, ..]
                => new Failed($"невідомий префікс '{prefix}' (очікую P або W)"),

            _ => new Failed("порожній рядок")
        };
    }

    private abstract record Outcome;
    private sealed record ProductOk(ProductDto Value) : Outcome;
    private sealed record WarehouseOk(WarehouseDto Value) : Outcome;
    private sealed record Failed(string Reason) : Outcome;
}
