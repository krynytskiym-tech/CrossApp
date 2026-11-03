using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = Open(path);

        List<ProductDto>? dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<ProductDto>>(stream, Options, ct);
        }
        catch (JsonException ex)
        {
            throw new StorageException($"Файл {Path.GetFileName(path)} має некоректний формат JSON.", ex);
        }

        if (dtos is null)
            throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");

        var result = new List<Product>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(ToProduct(dtos[i], i + 1));
        }

        return result;
    }

    public async IAsyncEnumerable<Product> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = Open(path);
        await using var enumerator = JsonSerializer
            .DeserializeAsyncEnumerable<ProductDto>(stream, Options, ct)
            .GetAsyncEnumerator(ct);

        var number = 0;
        while (true)
        {
            bool hasNext;
            ProductDto? dto = null;
            try
            {
                hasNext = await enumerator.MoveNextAsync();
                if (hasNext)
                    dto = enumerator.Current;
            }
            catch (JsonException ex)
            {
                throw new StorageException($"Файл {Path.GetFileName(path)} має некоректний формат JSON.", ex);
            }

            if (!hasNext)
                yield break;

            ct.ThrowIfCancellationRequested();
            number++;
            yield return ToProduct(dto, number);
        }
    }

    private static FileStream Open(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

    private static Product ToProduct(ProductDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожній.");

        try
        {
            return Product.FromDto(dto);
        }
        catch (ArgumentException ex)
        {
            throw new DomainException($"Запис {number}: {ex.Message.ReplaceLineEndings(" ")}", ex);
        }
    }
}
