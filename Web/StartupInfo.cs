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
        Log.Information("MCM Startup Information:");

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

    public static (bool, long, string?) GetBestValue(string[] paths)
    {
        long limit = 0;
        string? bestPath = null;
        
        Guard.Against.Null(paths);
        foreach (string path in paths)
        {
            if (Path.Exists(path) && long.TryParse(File.ReadAllText(path), out limit))
            {
                return (true, limit, path);
            }
        }
        return (false, limit, bestPath);
    }
}
