using Core.Events;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class CatalogServiceTests
{
    [Fact] // 1) інваріант: відмова і незмінний стан
    public void Receive_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var p = service.Add("SKU-1", "Кабель", "м", 10);

        // ArgumentOutOfRangeException є нащадком ArgumentException, тому ThrowsAny.
        Assert.ThrowsAny<ArgumentException>(() => service.Receive(p.Id, -5));
        Assert.Equal(10, store.GetById(p.Id)!.Quantity);
    }

    [Fact] // 2) подія з тижня 6 як спосіб перевірки
    public void Add_RaisesChangedExactlyOnce()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.Add("SKU-2", "Гвинт", "шт", 7);

        Assert.Single(events);
        Assert.Equal(CatalogChangeKind.Added, events[0].Kind);
    }

    [Fact] // бізнес-правило: DomainException і незмінний стан
    public void Issue_MoreThanStock_Throws_DomainException_AndKeepsState()
    {
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var p = service.Add("SKU-3", "Кабель", "м", 10);

        Assert.Throws<DomainException>(() => service.Issue(p.Id, 50));
        Assert.Equal(10, store.GetById(p.Id)!.Quantity);
    }

    [Fact] // помилка не породжує подію
    public void FailedIssue_DoesNotRaiseChanged()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var p = service.Add("SKU-4", "Гвинт", "шт", 3);
        var events = 0;
        service.Changed += (_, _) => events++;

        Assert.Throws<DomainException>(() => service.Issue(p.Id, 50));

        Assert.Equal(0, events);
    }

    [Fact] // відсутній запис: бізнес-помилка, а не NullReferenceException
    public void Receive_UnknownId_Throws_DomainException()
    {
        var service = new CatalogService(new InMemoryCatalogStore());

        Assert.Throws<DomainException>(() => service.Receive("NOPE", 5));
    }
}
