using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public static class CatalogJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,                          
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping       
    };

    
    public static void Write(string path, IEnumerable<Product> products)
    {
        var dtos = products.Select(product => product.ToDto()).ToList();
        string full = Path.GetFullPath(path);
        string tmp = full + ".tmp";

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(tmp, JsonSerializer.Serialize(dtos, Options));
        File.Move(tmp, full, overwrite: true);
    }

    public static IReadOnlyList<Product> Read(string path)
    {
        List<ProductDto?> dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<ProductDto?>>(File.ReadAllText(path), Options) ?? [];
        }
        catch (JsonException ex)
        {
            throw new StorageException($"Файл {Path.GetFileName(path)} має некоректний формат JSON.", ex);
        }

        var result = new List<Product>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i] ?? throw new DomainException($"Запис {i + 1}: порожній.");
            try
            {
                result.Add(Product.FromDto(dto));   // повторно перевіряє інваріанти
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message.ReplaceLineEndings(" ")}", ex);
            }
        }

        return result;
    }
}
