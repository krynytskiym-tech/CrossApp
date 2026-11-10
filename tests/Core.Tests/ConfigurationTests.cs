using Core.Configuration;
using Xunit;

namespace Core.Tests;

public class ConfigurationTests
{
    [Fact]
    public void StorageOptions_Defaults_AreRelativeAndPortable()
    {
        var options = new StorageOptions();

        Assert.Equal("data", options.DataPath);
        Assert.Equal("items.json", options.FileName);
        Assert.False(Path.IsPathRooted(options.DataPath));
    }

    [Fact]
    public void ResolveFullPath_Relative_UsesBaseDirectory_AndCreatesDirectory()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}");
        try
        {
            var options = new StorageOptions { DataPath = "data", FileName = "items.json" };

            var full = options.ResolveFullPath(baseDir);

            Assert.Equal(Path.Combine(baseDir, "data", "items.json"), full);
            Assert.True(Directory.Exists(Path.Combine(baseDir, "data")));   // каталог створено заново
        }
        finally
        {
            TempDir.Delete(baseDir);
        }
    }

    [Fact]
    public void ResolveFullPath_RootedDataPath_IgnoresBaseDirectory()
    {
        var absolute = Path.Combine(Path.GetTempPath(), $"crossapp-abs-{Guid.NewGuid():N}");
        try
        {
            var options = new StorageOptions { DataPath = absolute, FileName = "x.json" };

            var full = options.ResolveFullPath(Path.Combine(Path.GetTempPath(), "ignored"));

            Assert.Equal(Path.Combine(absolute, "x.json"), full);
        }
        finally
        {
            TempDir.Delete(absolute);
        }
    }
}
