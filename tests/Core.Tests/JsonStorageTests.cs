using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class JsonStorageTests
{
    [Fact]
    public void CatalogJson_RoundTrip_PreservesProducts()
    {
        var dir = TempDir.Create();
        try
        {
            var path = Path.Combine(dir, "items.json");

            CatalogJson.Write(path, TestData.Items());
            var back = CatalogJson.Read(path);

            Assert.Equal(TestData.Items().Select(p => p.ToDto()), back.Select(p => p.ToDto()));
        }
        finally
        {
            TempDir.Delete(dir);
        }
    }

    [Fact]
    public void CatalogJson_Write_UsesCamelCase_AndKeepsCyrillicReadable()
    {
        var dir = TempDir.Create();
        try
        {
            var path = Path.Combine(dir, "items.json");

            CatalogJson.Write(path, TestData.Items());
            var text = File.ReadAllText(path);

            Assert.Contains("\"sku\"", text);
            Assert.Contains("Великий запас", text);     // не \u0412...
            Assert.DoesNotContain("\\u", text);
        }
        finally
        {
            TempDir.Delete(dir);
        }
    }

    [Fact]
    public void CatalogJson_Write_IsAtomic_NoTmpFileLeft()
    {
        var dir = TempDir.Create();
        try
        {
            var path = Path.Combine(dir, "items.json");

            CatalogJson.Write(path, TestData.Items());

            Assert.True(File.Exists(path));
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            TempDir.Delete(dir);
        }
    }

    [Fact]
    public void CatalogJson_Write_CreatesMissingDirectory()
    {
        var dir = TempDir.Create();
        try
        {
            var path = Path.Combine(dir, "nested", "deeper", "items.json");

            CatalogJson.Write(path, TestData.Items());

            Assert.True(File.Exists(path));
        }
        finally
        {
            TempDir.Delete(dir);
        }
    }

    [Fact]
    public void CatalogJson_Read_MalformedJson_Throws_StorageException()
    {
        var path = TempFile.Write("[ { \"id\": ");
        try
        {
            Assert.Throws<StorageException>(() => CatalogJson.Read(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CatalogJson_Read_InvalidRecord_Throws_DomainException()
    {
        var path = TempFile.Write("""
            [ { "id": "X-1", "sku": "", "name": "Без SKU", "unit": "шт", "quantity": 1 } ]
            """);
        try
        {
            var ex = Assert.Throws<DomainException>(() => CatalogJson.Read(path));

            Assert.Contains("Запис 1", ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FileCatalogStore_PersistsBetweenInstances()
    {
        var dir = TempDir.Create();
        try
        {
            var path = Path.Combine(dir, "items.json");
            var first = new CatalogService(new FileCatalogStore(path));
            var added = first.Add("SKU-P", "Збережений", "шт", 7);

            var second = new CatalogService(new FileCatalogStore(path));

            var found = second.Find(added.Id);
            Assert.NotNull(found);
            Assert.Equal(7, found!.Quantity);
        }
        finally
        {
            TempDir.Delete(dir);
        }
    }

    [Fact]
    public void FileCatalogStore_RecreatesDeletedDirectory()
    {
        var dir = TempDir.Create();
        try
        {
            var path = Path.Combine(dir, "data", "items.json");
            var store = new FileCatalogStore(path);
            TempDir.Delete(Path.Combine(dir, "data"));      // каталогу даних немає

            new CatalogService(store).Add("SKU-D", "Після видалення", "шт", 1);

            Assert.True(File.Exists(path));
        }
        finally
        {
            TempDir.Delete(dir);
        }
    }
}
