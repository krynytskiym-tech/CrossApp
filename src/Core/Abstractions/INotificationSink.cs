using Core.Events;

namespace Core.Abstractions;

// Додаткове завдання 1: контракт приймача сповіщень, який передають сервісу через конструктор.
public interface INotificationSink
{
    void Notify(CatalogChangedEventArgs e);
}
