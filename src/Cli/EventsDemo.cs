using Core.Events;
using Core.Services;

namespace Cli;

public static class EventsDemo
{
    public static int Run(
        CatalogService service,
        EventHandler<CatalogChangedEventArgs> logHandler,
        Func<int> getChanges)
    {
        Console.WriteLine("=== Сценарій 4: події (лабораторна 6) ===");
        var created = service.Add("SKU-201", "Мережевий кабель", "м", 50);
        service.Receive(created.Id, 25);
        service.Issue(created.Id, 10);

        var temp = service.Add("SKU-202", "Тимчасовий товар", "шт", 30);
        service.Remove(temp.Id);
        Console.WriteLine($"Подій отримано: {getChanges()}");
        Console.WriteLine();

        Console.WriteLine("--- Відмови: виняток є, події немає ---");
        TryDo("прихід від'ємної кількості", () => service.Receive(created.Id, -5));
        TryDo("прихід для неіснуючого id", () => service.Receive("NOPE", 5));
        TryDo("видача більша за залишок", () => service.Issue(created.Id, 1000));
        Console.WriteLine($"Подій отримано: {getChanges()}");
        Console.WriteLine();

        Console.WriteLine("--- Після відписки логера (-=) ---");
        service.Changed -= logHandler;
        service.Receive(created.Id, 5);   // тиха операція: лог мовчить, лічильник рахує
        Console.WriteLine($"Подій отримано: {getChanges()}");
        Console.WriteLine($"Залишок після тихої операції: {service.Find(created.Id)?.Quantity}");
        Console.WriteLine();

        Console.WriteLine("--- Func замість події: FindAll(p => p.Quantity < 20) ---");
        var low = service.FindAll(p => p.Quantity < 20);
        Console.WriteLine($"Знайдено: {low.Count}");
        foreach (var p in low)
            Console.WriteLine($" {p.Id} {p.Sku,-10} {p.Name,-22} {p.Quantity,5} {p.Unit}");

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
