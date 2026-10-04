using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Web;

internal static class StartupInfo
{
    private const double Mebi = 1024 * 1024;
    private const double Gibi = Mebi * 1024;
    private const string Mcm_Product = "MCM";

    internal static void Print(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.LogInformation("\n\n" + """
            88,dPYba,,adPYba,   ,adPPYba, 88,dPYba,,adPYba,   
            88P'   "88"    "8a a8"     "" 88P'   "88"    "8a  
            88      88      88 8b         88      88      88  
            88      88      88 "8a,   ,aa 88      88      88  
            88      88      88  `"Ybbd8"' 88      88      88  
            """ + "\n");

        // OS and .NET information
        logger.LogInformation("OSArchitecture: {@OSArchitecture}", RuntimeInformation.OSArchitecture);
        logger.LogInformation("OSDescription: {@OSDescription}", RuntimeInformation.OSArchitecture);
        logger.LogInformation("FrameworkDescription: {@FrameworkDescription}", RuntimeInformation.FrameworkDescription);

        // Environment information
        logger.LogInformation("UserName: {@UserName}", Environment.UserName);
        logger.LogInformation("HostName : {@HostName}", Dns.GetHostName());

        // Hardware information
        var gcInfo = GC.GetGCMemoryInfo();
        var totalMemoryBytes = gcInfo.TotalAvailableMemoryBytes;
        logger.LogInformation("ProcessorCount: {@ProcessorCount}", Environment.ProcessorCount);
        logger.LogInformation("TotalAvailableMemoryBytes: {@TotalMemoryBytes} ({@TotalMemoryBytesInBestUnit})", totalMemoryBytes, GetInBestUnit(totalMemoryBytes));

        // Version information
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var productAttribute = assembly.GetCustomAttribute<AssemblyProductAttribute>();
            if (productAttribute != null && productAttribute.Product == Mcm_Product)
            {
                logger.LogInformation("Assembly Versions: {@AssemblyName} - {@AssemblyVersion}", assembly.GetName().Name, assembly.GetName().Version!.ToString());
            }
        }
    }

    internal static string GetInBestUnit(long size)
    {
        if (size < Mebi)
        {
            return $"{size} bytes";
        }
        else if (size < Gibi)
        {
            var mebibytes = size / Mebi;
            return $"{mebibytes.ToString("F", CultureInfo.InvariantCulture)} MiB";
        }
        else
        {
            var gibibytes = size / Gibi;
            return $"{gibibytes.ToString("F", CultureInfo.InvariantCulture)} GiB";
        }
    }

}
