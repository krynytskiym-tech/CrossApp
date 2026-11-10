namespace Core.Configuration;

// Налаштування застосунку. Біндяться з IConfiguration у Cli: Core про файли конфігурації не знає.
public sealed class AppOptions
{
    public StorageOptions Storage { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public WarehouseOptions Warehouse { get; init; } = new();
    public string EnvironmentName { get; init; } = "Development";
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; init; } = "Information";
}

public sealed class WarehouseOptions
{
    public string DefaultUnit { get; init; } = "шт";
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "items.json";

    // Профіль Production: дані в каталозі користувача (LocalApplicationData), а не поруч із програмою.
    public bool UseUserDataFolder { get; init; }

    public string ResolveFullPath(string baseDir)
    {
        string root;
        if (Path.IsPathRooted(DataPath))
        {
            root = DataPath;
        }
        else
        {
            string baseRoot = UseUserDataFolder
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrossApp")
                : baseDir;
            root = Path.Combine(baseRoot, DataPath);
        }

        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}
