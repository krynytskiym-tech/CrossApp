using System.Runtime.CompilerServices;
using Core.Abstractions;
using Core.Domain;

namespace Cli;

// Декоратор над ICatalogImporter для демонстрації скасування: додає затримки, які реагують на токен.
public sealed class SlowCatalogImporter(ICatalogImporter inner, TimeSpan loadDelay, TimeSpan itemDelay)
    : ICatalogImporter
{
    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        await Task.Delay(loadDelay, ct);
        return await inner.LoadAsync(path, ct);
    }

    public async IAsyncEnumerable<Product> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in inner.StreamAsync(path, ct).WithCancellation(ct))
        {
            await Task.Delay(itemDelay, ct);
            yield return item;
        }
    }
}
