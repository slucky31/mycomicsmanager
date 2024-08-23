using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Serilog;

namespace Web;

public static class StartupInfo
{
    private const double Mebi = 1024 * 1024;
    private const double Gibi = Mebi * 1024;
    private const string Mcm_Prodcut = "MCM";

    public static void Print()
    {
        Log.Information("\n\n" + """
            88,dPYba,,adPYba,   ,adPPYba, 88,dPYba,,adPYba,   
            88P'   "88"    "8a a8"     "" 88P'   "88"    "8a  
            88      88      88 8b         88      88      88  
            88      88      88 "8a,   ,aa 88      88      88  
            88      88      88  `"Ybbd8"' 88      88      88  
            """ + "\n");

        // OS and .NET information
        Log.Information("OSArchitecture: {@OSArchitecture}", RuntimeInformation.OSArchitecture);
        Log.Information("OSDescription: {@OSDescription}", RuntimeInformation.OSArchitecture);
        Log.Information("FrameworkDescription: {@FrameworkDescription}", RuntimeInformation.FrameworkDescription);

        // Environment information
        Log.Information("UserName: {@UserName}", Environment.UserName);
        Log.Information("HostName : {@HostName}", Dns.GetHostName());

        // Hardware information
        var gcInfo = GC.GetGCMemoryInfo();
        var totalMemoryBytes = gcInfo.TotalAvailableMemoryBytes;
        Log.Information("ProcessorCount: {@ProcessorCount}", Environment.ProcessorCount);
        Log.Information("TotalAvailableMemoryBytes: {@TotalMemoryBytes} ({@TotalMemoryBytesInBestUnit})", totalMemoryBytes, GetInBestUnit(totalMemoryBytes));

        // Version information
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var productAttribute = assembly.GetCustomAttribute<AssemblyProductAttribute>();
            if (productAttribute != null && productAttribute.Product == Mcm_Prodcut)
            {
                Log.Information("Assembly Versions: {@AssemblyName} - {@AssemblyVersion}", assembly.GetName().Name, assembly.GetName().Version!.ToString());
            }
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
