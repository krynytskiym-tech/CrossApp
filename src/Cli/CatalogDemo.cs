using Core.Abstractions;
using Core.Services;

namespace Cli;

// Сценарій працює лише з інтерфейсом і сервісом: конкретні сховища створює Program.cs.
public static class CatalogDemo
{
    public static int Run(string storeName, ICatalogStore store, CatalogService service)
    {
        Console.WriteLine($"Сховище: {storeName}");
        Console.WriteLine();

        Console.WriteLine("=== Сценарій 1: успіх ===");
        var created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
        Console.WriteLine($"Додано: {created}");

        service.Receive(created.Id, 25);
        service.Issue(created.Id, 10);
        Console.WriteLine($"Після приходу 25 і видачі 10: {service.Find(created.Id)}");

        var all = service.All();
        Console.WriteLine($"Усього записів: {all.Count}");
        foreach (var p in all)
            Console.WriteLine($" {p.Id} {p.Sku,-10} {p.Name,-22} {p.Quantity,5} {p.Unit}");
        Console.WriteLine();

        Console.WriteLine("=== Сценарій 2: відмови ===");
        TryDo("прихід для неіснуючого id", () => service.Receive("NOPE", 5));
        TryDo("видача більша за залишок", () => service.Issue(created.Id, 1000));
        TryDo("дубль id у сховищі", () => store.Add(created));
        TryDo("порожній SKU", () => service.Add(" ", "Без SKU", "шт", 1));

        return 0;
    }

    private static void TryDo(string title, Action action)
    {
        try
        {
            action();
            Console.WriteLine($" {title}: виняток НЕ спрацював!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message.ReplaceLineEndings(" ")}");
        }
    }
}
