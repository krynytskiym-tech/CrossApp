using System.Runtime.InteropServices;

namespace Core;

public sealed record EnvironmentReport(
    string OsDescription,
    string OsVersion,
    string ProcessArchitecture,
    string DotNetVersion,
    string Runtime,
    string BaseDirectory,
    string CurrentDirectory,
    string DetectedRid,
    string ReportedRid,
    string BuildNote);

public static class EnvironmentInfo
{
    public static EnvironmentReport Collect() => new(
        RuntimeInformation.OSDescription,
        Environment.OSVersion.ToString(),
        RuntimeInformation.ProcessArchitecture.ToString(),
        Environment.Version.ToString(),
        RuntimeInformation.FrameworkDescription,
        AppContext.BaseDirectory,
        Environment.CurrentDirectory,
        DetectRid(),
        RuntimeInformation.RuntimeIdentifier,
        GetBuildNote());

    private static string GetBuildNote() =>
#if NET10_0_OR_GREATER
        "збірка під .NET 10.0";
#else
        "збірка під .NET 8.0";
#endif

    private static string DetectRid()
    {
        string os =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "unknown";

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "unknown"
        };

        return $"{os}-{arch}";
    }
}