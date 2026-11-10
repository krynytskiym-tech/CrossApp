using Core.Services;
using Core.Storage;

namespace Cli;

// export [шлях]: зберегти всю колекцію у JSON; config: показати ефективні налаштування.
public static class DataCommands
{
    public static int Export(CatalogService service, AppSettings settings, string[] args)
    {
        string target = args.Where(a => !a.StartsWith("--")).Skip(1).FirstOrDefault() ?? settings.DataPath;
        var items = service.All();

        CatalogJson.Write(target, items);

        PrintHeader(settings, Path.GetFullPath(target));
        Console.WriteLine($"Експортовано {items.Count} {Plural(items.Count)}.");
        return 0;
    }

    public static int ShowConfig(AppSettings settings)
    {
        var o = settings.Options;
        Console.WriteLine($"Профіль            : {o.EnvironmentName}");
        Console.WriteLine($"Логування          : MinimumLevel = {o.Logging.MinimumLevel}");
        Console.WriteLine($"Warehouse          : DefaultUnit = {o.Warehouse.DefaultUnit}");
        Console.WriteLine($"Каталог користувача : UseUserDataFolder = {o.Storage.UseUserDataFolder}");
        PrintHeader(settings, settings.DataPath);
        return 0;
    }

    // Рядок "Файл даних" і факт наявності секрету. Саме значення ключа не друкується.
    public static void PrintHeader(AppSettings settings, string dataPath)
    {
        Console.WriteLine($"Файл даних : {dataPath}");

        var apiKey = settings.Config["Integration:ApiKey"];
        Console.WriteLine(apiKey is null ? "ApiKey: не заданий" : $"ApiKey: присутній, довжина {apiKey.Length}");
    }

    private static string Plural(int n) => (n % 100, n % 10) switch
    {
        (>= 11 and <= 14, _) => "записів",
        (_, 1) => "запис",
        (_, >= 2 and <= 4) => "записи",
        _ => "записів"
    };
}
