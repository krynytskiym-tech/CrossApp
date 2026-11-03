using Core;
using Core.Abstractions;
using Core.Services;

namespace Cli;

// dotnet run --project src/Cli -- import-json [шлях] [--stream] [--progress] [--slow] [--timeout=N]
public static class AsyncImportCommand
{
    public static async Task<int> RunAsync(CatalogService service, ICatalogImporter importer, string[] args)
    {
        string path = args.Where(a => !a.StartsWith("--")).Skip(1).FirstOrDefault()
                      ?? Path.Combine("data", "import.json");
        int? timeoutSeconds = ParseTimeout(args);
        bool stream = args.Contains("--stream");
        IProgress<int>? progress = args.Contains("--progress") ? new ConsoleProgress(percent: !stream) : null;

        int events = 0;
        service.Changed += (_, _) => events++;

        // Джерела скасування: Ctrl+C і таймаут. Зв'язаний токен спрацьовує від будь-якого з них.
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = timeoutSeconds is { } seconds
            ? new CancellationTokenSource(TimeSpan.FromSeconds(seconds))
            : new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);

        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true;      // не завершувати процес одразу
            userCts.Cancel();
        };
        Console.CancelKeyPress += onCancel;

        try
        {
            int added = stream
                ? await service.ImportStreamAsync(importer, path, linked.Token, progress)
                : await service.ImportAsync(importer, path, linked.Token, progress);

            if (progress is not null)
                Console.WriteLine();
            Console.WriteLine($"Імпортовано записів: {added}");
            Console.WriteLine($"Подій отримано: {events}");
            return 0;
        }
        catch (OperationCanceledException)   // має стояти до загальних обробників
        {
            if (progress is not null)
                Console.WriteLine();
            Console.WriteLine("Імпорт скасовано.");
            if (timeoutCts.IsCancellationRequested)
                Console.WriteLine($"Причина: перевищено таймаут {timeoutSeconds} с.");
            return 3;
        }
        catch (DomainException ex)
        {
            Console.WriteLine($"Дані відхилено: {ex.Message}");
            return 4;
        }
        catch (StorageException ex)
        {
            Console.WriteLine($"Помилка читання даних: {ex.Message}");
            return 5;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
            return 1;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Помилка файлу: {ex.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }
    }

    private static int? ParseTimeout(string[] args)
    {
        string? option = args.FirstOrDefault(a => a.StartsWith("--timeout="));
        return option is not null && int.TryParse(option["--timeout=".Length..], out int value) && value > 0
            ? value
            : null;
    }
}
