namespace Cli;

// Синхронний IProgress<int>: на відміну від Progress<T>, не відкладає виклик у пул потоків,
// тому рядок прогресу не з'являється після підсумку.
public sealed class ConsoleProgress(bool percent) : IProgress<int>
{
    public void Report(int value) =>
        Console.Write(percent ? $"\r  Прогрес: {value,3}%" : $"\r  Опрацьовано записів: {value}");
}
