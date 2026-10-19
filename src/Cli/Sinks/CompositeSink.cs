using Core.Abstractions;
using Core.Events;

namespace Cli.Sinks;

public sealed class CompositeSink(params INotificationSink[] sinks) : INotificationSink
{
    public void Notify(CatalogChangedEventArgs e)
    {
        foreach (INotificationSink sink in sinks)
            sink.Notify(e);
    }
}
