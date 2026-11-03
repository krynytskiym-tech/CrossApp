using Core.Events;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class ImportTests
{
    [Fact] // 5) скасування
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonCatalogImporter(), "any.json", cts.Token));
    }

    [Fact] // 6) тимчасовий файл із некоректним записом -> DomainException; файл видаляється у finally
    public async Task ImportAsync_WithBadRecord_Throws_DomainException_WithRecordNumber()
    {
        var path = TempFile.Write("""
            [
              { "id": "F-1", "sku": "SKU-F1", "name": "Добрий", "unit": "шт", "quantity": 1 },
              { "id": "F-2", "sku": "",       "name": "Поганий", "unit": "шт", "quantity": 2 }
            ]
            """);
        try
        {
            var service = new CatalogService(new InMemoryCatalogStore());

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonCatalogImporter(), path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);   // початковий виняток збережено
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // технічна помилка не маскується під доменну
    public async Task ImportAsync_WithMalformedJson_Throws_StorageException()
    {
        var path = TempFile.Write("[ { \"id\": ");
        try
        {
            var service = new CatalogService(new InMemoryCatalogStore());

            await Assert.ThrowsAsync<StorageException>(
                () => service.ImportAsync(new JsonCatalogImporter(), path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // ідемпотентність: повторний імпорт нічого не додає
    public async Task ImportAsync_IsIdempotent()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var importer = new FakeImporter(TestData.Items());

        var first = await service.ImportAsync(importer, "fake.json");
        var second = await service.ImportAsync(importer, "fake.json");

        Assert.Equal(4, first);
        Assert.Equal(0, second);
        Assert.Equal(4, service.All().Count);
    }

    [Fact] // подія піднімається для кожного доданого запису
    public async Task ImportAsync_RaisesAddedEvent_ForEachNewItem()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var kinds = new List<CatalogChangeKind>();
        service.Changed += (_, e) => kinds.Add(e.Kind);

        var added = await service.ImportAsync(new FakeImporter(TestData.Items()), "fake.json");

        Assert.Equal(added, kinds.Count);
        Assert.All(kinds, k => Assert.Equal(CatalogChangeKind.Added, k));
    }

    [Fact] // додаткове 3: IProgress<int> (відсотки)
    public async Task ImportAsync_ReportsProgressInPercent()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var progress = new ListProgress();

        await service.ImportAsync(new FakeImporter(TestData.Items()), "fake.json", progress: progress);

        Assert.Equal(new[] { 25, 50, 75, 100 }, progress.Values);
    }

    [Fact] // додаткове 1: потоковий імпорт з файлу (await foreach) + прогрес кількістю
    public async Task ImportStreamAsync_FromFile_ImportsAll_AndReportsCount()
    {
        var path = TempFile.Write("""
            [
              { "id": "S-1", "sku": "SKU-S1", "name": "Перший", "unit": "шт", "quantity": 1 },
              { "id": "S-2", "sku": "SKU-S2", "name": "Другий", "unit": "шт", "quantity": 2 },
              { "id": "S-3", "sku": "SKU-S3", "name": "Третій", "unit": "шт", "quantity": 3 }
            ]
            """);
        try
        {
            var store = new InMemoryCatalogStore();
            var service = new CatalogService(store);
            var progress = new ListProgress();

            var added = await service.ImportStreamAsync(new JsonCatalogImporter(), path, progress: progress);

            Assert.Equal(3, added);
            Assert.Equal(3, store.List().Count);
            Assert.Equal(new[] { 1, 2, 3 }, progress.Values);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // додаткове 2: зв'язані токени; спрацював саме таймаут, а не користувач
    public async Task ImportAsync_WhenTimeoutFires_IsCancelledByTimeoutToken_NotByUser()
    {
        using var user = new CancellationTokenSource();
        using var timeout = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(user.Token, timeout.Token);
        var service = new CatalogService(new InMemoryCatalogStore());

        await timeout.CancelAsync();   // імітуємо завершення таймауту

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new FakeImporter(TestData.Items()), "fake.json", linked.Token));

        Assert.True(timeout.IsCancellationRequested);
        Assert.False(user.IsCancellationRequested);
    }
}
