using System.Diagnostics;
using System.Globalization;
using Core.Domain;
using Core.Reports;
using Core.Services;

namespace Cli;

// Підкоманди report-*: лише виклики ReportService і форматований вивід.
public static class ReportCommand
{
    public static int Run(string command, CatalogService service, List<JournalEntry> journal)
    {
        var items = service.All();

        switch (command)
        {
            case "report-top": Print(ReportService.TopByQuantity(items, 5)); break;
            case "report-low": Print(ReportService.LowStock(items, 10)); break;
            case "report-groups": PrintGroups(ReportService.ByUnit(items)); break;
            case "report-groups-query": PrintGroups(ReportService.ByUnitQuery(items)); break;
            case "report-top-units": PrintGroups(ReportService.TopUnits(items, 3)); break;
            case "report-sum":
                var t = ReportService.Totals(items);
                string average = t.Average.ToString("F2", CultureInfo.InvariantCulture);
                Console.WriteLine($" позицій {t.Items}, всього {t.Total}, середнє {average}, макс {t.Max}");
                break;
            case "report-defer": DeferredDemo(service); break;
            case "report-movements": MovementsDemo(service, journal); break;
            case "report-activity": ActivityDemo(service, journal); break;
            case "report-bench": Benchmark(); break;
            default:
                Console.WriteLine("report-top | report-low | report-groups | report-sum |");
                Console.WriteLine("report-groups-query | report-top-units | report-defer |");
                Console.WriteLine("report-movements | report-activity | report-bench");
                return 2;
        }

        return 0;
    }

    private static void Print(IReadOnlyList<StockLine> lines)
    {
        foreach (var l in lines)
            Console.WriteLine($" {l.Sku,-10} {l.Name,-22} {l.Quantity,6} {l.Unit}");
    }

    private static void PrintGroups(IReadOnlyList<GroupSummary> groups)
    {
        foreach (var g in groups)
            Console.WriteLine($" {g.Key,-6} позицій: {g.Count,3} сумарно: {g.TotalQuantity,6}");
    }

    // Крок 3 методички: відкладене виконання. Джерело — локальний список, бо All() дає новий знімок.
    private static void DeferredDemo(CatalogService service)
    {
        var source = service.All().ToList();
        var query = source.Where(p => p.Quantity < 10);          // запит побудовано, не виконано
        Console.WriteLine($"Дефіцитних до змін: {query.Count()}");

        source.Add(Product.Create("tmp00001", "SKU-999", "Скоба", "шт", 3));
        Console.WriteLine($"Дефіцитних після змін: {query.Count()}");   // число інше

        var snapshot = query.ToList();                           // результат зафіксовано
        source.Clear();
        Console.WriteLine($"У знімку лишилось: {snapshot.Count}");      // не змінюється
        Console.WriteLine($"Той самий запит після Clear: {query.Count()}");   // знову по джерелу
    }

    // Додаткове 1: Join і GroupJoin. Журнал наповнюється підписником на подію Changed.
    private static void MovementsDemo(CatalogService service, List<JournalEntry> journal)
    {
        if (!ApplySampleOperations(service)) return;

        Console.WriteLine("Join: записи журналу з даними товару");
        foreach (var m in ReportService.Movements(service.All(), journal))
            Console.WriteLine($" {m.Sku,-10} {m.Name,-22} {m.Kind,-9} {m.Delta,5:+0;-0}");
    }

    private static void ActivityDemo(CatalogService service, List<JournalEntry> journal)
    {
        if (!ApplySampleOperations(service)) return;

        var lines = ReportService.Activity(service.All(), journal);
        int idle = 0;
        Console.WriteLine("GroupJoin: рух по товарах");
        foreach (var a in lines)
        {
            if (a.Movements == 0)
            {
                idle++;
                continue;
            }

            Console.WriteLine($" {a.Sku,-10} {a.Name,-22} рухів: {a.Movements,2} разом: {a.NetDelta,5:+0;-0}");
        }

        Console.WriteLine($"Без руху: {idle} позицій");
    }

    private static bool ApplySampleOperations(CatalogService service)
    {
        if (service.Find("P-002") is null)
        {
            Console.WriteLine("Для цього звіту потрібні дані SampleData (запуск без --file).");
            return false;
        }

        service.Receive("P-002", 10);
        service.Receive("P-002", 5);
        service.Issue("P-001", 20);
        service.Issue("P-003", 100);
        service.Receive("P-016", 15);
        return true;
    }

    // Додаткове 4: Stopwatch на 100 000 записів, ToList усередині ланцюжка і без нього.
    private static void Benchmark()
    {
        const int count = 100_000;
        var source = Generate(count);
        Console.WriteLine($"Згенеровано записів: {count}");

        var query = source.Where(p => p.Quantity < 500)
                          .OrderBy(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal);

        var sw = Stopwatch.StartNew();
        int c1 = query.Count();
        int s1 = query.Sum(p => p.Quantity);
        int m1 = query.Max(p => p.Quantity);
        sw.Stop();
        long lazyMs = sw.ElapsedMilliseconds;

        sw.Restart();
        var list = query.ToList();
        int c2 = list.Count;
        int s2 = list.Sum(p => p.Quantity);
        int m2 = list.Max(p => p.Quantity);
        sw.Stop();
        long listMs = sw.ElapsedMilliseconds;

        Console.WriteLine($"Без ToList (запит виконано тричі): {lazyMs} мс");
        Console.WriteLine($"З ToList (запит виконано раз):     {listMs} мс");
        Console.WriteLine($"Результати збігаються: {c1 == c2 && s1 == s2 && m1 == m2} (позицій {c2}, сума {s2}, макс {m2})");

        sw.Restart();
        var groups = ReportService.ByUnit(source);
        var top = ReportService.TopByQuantity(source, 5);
        sw.Stop();
        Console.WriteLine($"ByUnit + TopByQuantity: {sw.ElapsedMilliseconds} мс (груп {groups.Count}, топ {top.Count})");
    }

    private static List<Product> Generate(int count)
    {
        string[] units = ["шт", "м", "кг", "м2", "уп"];
        var rnd = new Random(42);
        var list = new List<Product>(count);
        for (int i = 1; i <= count; i++)
            list.Add(Product.Create($"B-{i:D6}", $"SKU-{i:D6}", $"Товар {i}", units[i % units.Length], rnd.Next(0, 5000)));
        return list;
    }
}
