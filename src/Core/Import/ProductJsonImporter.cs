using System.Text;
using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        string json = File.ReadAllText(path, Encoding.UTF8);

        try
        {
            using JsonDocument document = JsonDocument.Parse(json, DocumentOptions);
            ReadItems(document.RootElement, items, errors);
        }
        catch (JsonException)
        {
            errors.Add("файл не є коректним JSON");
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    private static void ReadItems(JsonElement root, List<ProductDto> items, List<string> errors)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            errors.Add("очікую масив записів на верхньому рівні");
            return;
        }

        int number = 0;
        foreach (JsonElement element in root.EnumerateArray())
        {
            number++;

            ProductDto? dto;
            try
            {
                dto = element.Deserialize<ProductDto>(Options);
            }
            catch (JsonException ex)
            {
                errors.Add($"запис {number}: некоректне значення в {ex.Path ?? "записі"}");
                continue;
            }

            string? problem = dto switch
            {
                null => "запис порожній (null)",
                { Quantity: < 0 } d => $"кількість {d.Quantity} від'ємна",
                { } d when string.IsNullOrWhiteSpace(d.Sku) || string.IsNullOrWhiteSpace(d.Name)
                    => "SKU або назва порожні",
                { } d when string.IsNullOrWhiteSpace(d.Id)
                    => "id порожній",
                _ => null
            };

            if (problem is null)
                items.Add(dto!);
            else
                errors.Add($"запис {number}: {problem}");
        }
    }
}
