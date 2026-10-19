using Core.Abstractions;
using Core.Events;

namespace Cli.Sinks;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(CatalogChangedEventArgs e) =>
        Console.WriteLine(ChangeFormatter.Format(e));
}
