using Core;
using Core.Abstractions;
using Core.Storage;

namespace Cli;

// Альтернатива «усе в Program.cs»: вибір і складання сховища винесено в одне місце.
// Фабрика лежить у Cli, тож конкретні класи так само не потрапляють у Core.
public static class StoreFactory
{
    public static ICatalogStore Create(string[] args, string dataPath)
    {
        ICatalogStore store = args.Contains("--file")
            ? new FileCatalogStore(dataPath)
            : new InMemoryCatalogStore(SampleData.Products());

        return args.Contains("--cache") ? new CachingCatalogStore(store) : store;
    }
}
