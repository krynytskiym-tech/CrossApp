using System.Runtime.CompilerServices;
using Core.Abstractions;
using Core.Domain;
using Xunit;

namespace Core.Tests;

// Маленький фіксований набір записів: не залежить від SampleData і від диска.
internal static class TestData
{
    public static IReadOnlyList<Product> Items() =>
    [
        Product.Create("T-1", "SKU-BIG", "Великий запас", "шт", 100),
        Product.Create("T-2", "SKU-MID", "Середній запас", "шт", 50),
        Product.Create("T-3", "SKU-LOW", "Малий запас", "м", 5),
        Product.Create("T-4", "SKU-TINY", "Мізерний запас", "м", 1)
    ];
}

// Підміна імпортера: жодного диска.
internal sealed class FakeImporter(IEnumerable<Product> items) : ICatalogImporter
{
    private readonly IReadOnlyList<Product> _items = items.ToList();

    public Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_items);
    }

    public async IAsyncEnumerable<Product> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in _items)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return item;
        }
    }
}

// Синхронний IProgress<int>: зручно перевіряти послідовність значень.
internal sealed class ListProgress : IProgress<int>
{
    public List<int> Values { get; } = [];

    public void Report(int value) => Values.Add(value);
}

// Тимчасовий файл, який тест сам видаляє у finally.
internal static class TempFile
{
    public static string Write(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"coretests-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }
}

// Тимчасовий каталог для тестів сховища; тест видаляє його у finally.
internal static class TempDir
{
    public static string Create()
    {
        var path = Path.Combine(Path.GetTempPath(), $"coretests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    public static void Delete(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
