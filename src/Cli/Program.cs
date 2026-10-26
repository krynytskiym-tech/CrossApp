using Cli;
using Cli.Sinks;
using Core;
using Core.Abstractions;
using Core.Events;
using Core.Reports;
using Core.Services;
using Core.Storage;

// dotnet run --project src/Cli                         — сервіс + сховище в пам'яті (лаба 5), потім події (лаба 6)
// dotnet run --project src/Cli -- --file               — файлове сховище
// dotnet run --project src/Cli -- report-top           — звіти LINQ (лаба 7): report-top | report-low | report-groups | report-sum | ...
// dotnet run --project src/Cli -- --cache | --factory | --sinks   — додаткові режими лаб 5 і 6
// dotnet run --project src/Cli -- --domain             — демонстрація доменної моделі (лаба 4)
// dotnet run --project src/Cli -- data/sample.csv [--verbose]  — імпорт CSV/JSON/TXT (лаба 3)
if (args.Contains("--domain"))
    return DomainDemo.Run();

// Перший аргумент без "--": підкоманда звіту або шлях до файлу для імпорту.
string? command = args.FirstOrDefault(a => !a.StartsWith("--"));
bool isReport = command is not null && command.StartsWith("report-");

if (command is not null && !isReport)
    return ImportCommand.Run(command, args.Contains("--verbose"));

// ---- Composition root: єдине місце, де створюються конкретні класи ----
bool useFile = args.Contains("--file");
bool useSinks = args.Contains("--sinks");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");
string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");

ICatalogStore store;
if (args.Contains("--factory"))
{
    store = StoreFactory.Create(args, dataPath);
}
else
{
    store = useFile
        ? new FileCatalogStore(dataPath)
        : new InMemoryCatalogStore(SampleData.Products());

    if (args.Contains("--cache"))
        store = new CachingCatalogStore(store);
}

INotificationSink? sink = useSinks
    ? new CompositeSink(new ConsoleSink(), new FileSink(logPath))
    : null;

var service = new CatalogService(store, sink);
// -----------------------------------------------------------------------

// ---- Лабораторна 7: звіти. Журнал змін збирається підпискою на подію (лаба 6) ----
if (isReport)
{
    var journal = new List<JournalEntry>();
    service.Changed += (_, e) => journal.Add(new JournalEntry(e.Id, e.Kind, e.Delta));
    return ReportCommand.Run(command!, service, journal);
}

if (useFile)
    Console.WriteLine($"Файл даних: {dataPath}");

string storeName = store is CachingCatalogStore caching
    ? $"{nameof(CachingCatalogStore)} → {caching.InnerName}"
    : store.GetType().Name;

// Сценарії лабораторної 5 виконуються до підписки: подій ще ніхто не слухає.
CatalogDemo.Run(storeName, store, service);

if (store is CachingCatalogStore stats)
    Console.WriteLine($"Кеш: влучань {stats.Hits}, промахів {stats.Misses}");
Console.WriteLine();

// ---- Лабораторна 6: підписка на подію ----
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
int changes = 0;

// Локальний метод: його можна передати і в +=, і в -=.
void OnCatalogChanged(object? sender, CatalogChangedEventArgs e)
{
    string line = ChangeFormatter.Format(e);
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

if (!useSinks)                                   // у режимі --sinks виводять приймачі, подвійного рядка не потрібно
    service.Changed += OnCatalogChanged;         // підписник 1: консоль + файл
service.Changed += (_, _) => changes++;          // підписник 2: лямбда із замиканням над changes

Console.WriteLine($"Журнал: {logPath}");
Console.WriteLine();

return EventsDemo.Run(service, OnCatalogChanged, () => changes);
