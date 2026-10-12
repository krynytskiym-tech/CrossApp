using Cli;
using Core;
using Core.Abstractions;
using Core.Services;
using Core.Storage;

// dotnet run --project src/Cli                     — сервіс + сховище в пам'яті (лаба 5)
// dotnet run --project src/Cli -- --file           — сервіс + файлове сховище (лаба 5)
// dotnet run --project src/Cli -- --domain         — демонстрація доменної моделі (лаба 4)
// dotnet run --project src/Cli -- data/sample.csv  — імпорт CSV/JSON/TXT (лаба 3)
if (args.Contains("--domain"))
    return DomainDemo.Run();

if (args.Length > 0 && !args[0].StartsWith("--"))
    return ImportCommand.Run(args[0]);

// ---- Composition root: єдине місце, де створюються конкретні класи ----
bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

ICatalogStore store = useFile
    ? new FileCatalogStore(dataPath)
    : new InMemoryCatalogStore(SampleData.Products());

var service = new CatalogService(store);
// -----------------------------------------------------------------------

if (useFile)
    Console.WriteLine($"Файл даних: {dataPath}");

return CatalogDemo.Run(store.GetType().Name, store, service);
