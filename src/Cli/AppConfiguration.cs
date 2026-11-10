using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cli;

public sealed record AppSettings(IConfiguration Config, AppOptions Options, StorageOptions Storage, string DataPath);

// Збирання конфігурації: єдине місце, що знає про appsettings.json, user-secrets і змінні середовища.
public static class AppConfiguration
{
    public static AppSettings Load(string[] args)
    {
        var envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";

        // Порядок джерел: пізніше джерело перекриває раніше.
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: "CROSSAPP_")
            .AddCommandLine(args.Where(a => a.StartsWith("--") && a.Contains('=')).ToArray())
            .Build();

        var options = config.Get<AppOptions>() ?? new AppOptions();

        // Проста змінна CROSSAPP_DATA_PATH перекриває розділ Storage.
        var legacy = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
        var storage = new StorageOptions
        {
            DataPath = string.IsNullOrWhiteSpace(legacy) ? options.Storage.DataPath : legacy,
            FileName = options.Storage.FileName,
            UseUserDataFolder = options.Storage.UseUserDataFolder
        };

        return new AppSettings(config, options, storage, storage.ResolveFullPath(AppContext.BaseDirectory));
    }
}
