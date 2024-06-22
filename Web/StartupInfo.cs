using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using Ardalis.GuardClauses;
using Serilog;
using static System.Console;

namespace Web;

public static class StartupInfo
{
    const double Mebi = 1024 * 1024;
    const double Gibi = Mebi * 1024;

    public static void Print()
    {
        Log.Information("MCM Starts ...");

        // OS and .NET information
        Log.Information($"{nameof(RuntimeInformation.OSArchitecture)}: {RuntimeInformation.OSArchitecture}");
        Log.Information($"{nameof(RuntimeInformation.OSDescription)}: {RuntimeInformation.OSDescription}");
        Log.Information($"{nameof(RuntimeInformation.FrameworkDescription)}: {RuntimeInformation.FrameworkDescription}");

        // Environment information
        Log.Information($"{nameof(Environment.UserName)}: {Environment.UserName}");
        Log.Information($"HostName : {Dns.GetHostName()}");        

        // Hardware information
        GCMemoryInfo gcInfo = GC.GetGCMemoryInfo();
        long totalMemoryBytes = gcInfo.TotalAvailableMemoryBytes;
        Log.Information($"{nameof(Environment.ProcessorCount)}: {Environment.ProcessorCount}");
        Log.Information($"{nameof(GCMemoryInfo.TotalAvailableMemoryBytes)}: {totalMemoryBytes} ({GetInBestUnit(totalMemoryBytes)})");

        var memoryLimitPaths = new string[]
        {
            "/sys/fs/cgroup/memory.max",
            "/sys/fs/cgroup/memory.high",
            "/sys/fs/cgroup/memory.low",
            "/sys/fs/cgroup/memory/memory.limit_in_bytes",
        };

        var currentMemoryPaths = new string[]
        {
            "/sys/fs/cgroup/memory.current",
            "/sys/fs/cgroup/memory/memory.usage_in_bytes",
        };

        // cgroup information
        if (OperatingSystem.IsLinux() &&
            GetBestValue(memoryLimitPaths, out long memoryLimit, out string? bestMemoryLimitPath) &&
            memoryLimit > 0)
        {
            // get memory cgroup information
            GetBestValue(currentMemoryPaths, out long currentMemory, out _);

            Log.Information($"cgroup memory constraint: {bestMemoryLimitPath}");
            Log.Information($"cgroup memory limit: {memoryLimit} ({GetInBestUnit(memoryLimit)})");
            Log.Information($"cgroup memory usage: {currentMemory} ({GetInBestUnit(currentMemory)})");
            Log.Information($"GC Hard limit %: {(double)totalMemoryBytes / memoryLimit * 100:N0}");
        }
    }

    public static string GetInBestUnit(long size)
    {
        if (size < Mebi)
        {
            return $"{size} bytes";
        }
        else if (size < Gibi)
        {
            double mebibytes = size / Mebi;
            return $"{mebibytes.ToString("F", CultureInfo.InvariantCulture)} MiB";
        }
        else
        {
            double gibibytes = size / Gibi;
            return $"{gibibytes.ToString("F", CultureInfo.InvariantCulture)} GiB";
        }
    }

    public static bool GetBestValue(string[] paths, out long limit, [NotNullWhen(true)] out string? bestPath)
    {
        Guard.Against.Null(paths);

        foreach (string path in paths)
        {
            if (Path.Exists(path) &&
                long.TryParse(File.ReadAllText(path), out limit))
            {
                bestPath = path;
                return true;
            }
        }

        bestPath = null;
        limit = 0;
        return false;
    }
}
