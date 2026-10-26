using Core.Domain;

namespace Core.Reports;

// Чисті функції: приймають IEnumerable<Product>, повертають IReadOnlyList<DTO>.
// Жодної залежності від сховища, консолі чи файлів.
public static class ReportService
{
    // 1) фільтр + сортування + проєкція
    public static IReadOnlyList<StockLine> LowStock(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold)
              .OrderBy(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal)
              .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
              .ToList();

    // 2) топ-N зі стабільним порядком (метод-розширення TopBy: ключ + другий ключ Sku)
    public static IReadOnlyList<StockLine> TopByQuantity(IEnumerable<Product> source, int n) =>
        source.TopBy(p => p.Quantity, p => p.Sku, n)
              .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
              .ToList();

    // 3) групування з підрахунком і сумою
    public static IReadOnlyList<GroupSummary> ByUnit(IEnumerable<Product> source) =>
        source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
              .OrderByDescending(s => s.TotalQuantity).ThenBy(s => s.Key, StringComparer.Ordinal)
              .ToList();

    // 3б) те саме в синтаксисі запитів (додаткове завдання 3).
    // group ... by не приймає компаратор, тому ключ нормалізовано ToLowerInvariant.
    public static IReadOnlyList<GroupSummary> ByUnitQuery(IEnumerable<Product> source) =>
        (from p in source
         group p by p.Unit.ToLowerInvariant() into g
         orderby g.Sum(p => p.Quantity) descending, g.Key
         select new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity))).ToList();

    // 3в) топ-N груп: TopBy застосовано до груп, а не до товарів
    public static IReadOnlyList<GroupSummary> TopUnits(IEnumerable<Product> source, int n) =>
        ByUnit(source).TopBy(s => s.TotalQuantity, s => s.Key, n).ToList();

    // 4) агрегати з обробкою порожньої послідовності
    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<Product> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count, list.Sum(p => p.Quantity),
               list.Average(p => p.Quantity), list.Max(p => p.Quantity));
    }

    // 5) Join: кожен запис журналу доповнюється даними товару
    public static IReadOnlyList<MovementLine> Movements(
        IEnumerable<Product> products, IEnumerable<JournalEntry> journal) =>
        products.Join(journal,
                      product => product.Id,
                      entry => entry.ProductId,
                      (product, entry) => new MovementLine(product.Sku, product.Name, entry.Kind, entry.Delta))
                .OrderBy(l => l.Sku, StringComparer.Ordinal)
                .ToList();

    // 6) GroupJoin: усі товари, навіть без жодного запису в журналі
    public static IReadOnlyList<ActivityLine> Activity(
        IEnumerable<Product> products, IEnumerable<JournalEntry> journal) =>
        products.GroupJoin(journal,
                           product => product.Id,
                           entry => entry.ProductId,
                           (product, entries) => new ActivityLine(
                               product.Sku, product.Name, entries.Count(), entries.Sum(e => e.Delta)))
                .OrderByDescending(a => a.Movements).ThenBy(a => a.Sku, StringComparer.Ordinal)
                .ToList();
}
