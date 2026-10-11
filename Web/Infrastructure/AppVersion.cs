using System.Reflection;

namespace Web.Infrastructure;

// Version shown in Settings: the <Version> of Directory.Build.props, bumped by Versionize at each release.
internal static class AppVersion
{
    public static string Current { get; } = Format(
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        typeof(AppVersion).Assembly.GetName().Version);

    // The SDK appends "+<commit sha>" to the informational version: keep only the release number.
    internal static string Format(string? informationalVersion, Version? assemblyVersion)
    {
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var metadataStart = informationalVersion.IndexOf('+', StringComparison.Ordinal);
            return metadataStart < 0 ? informationalVersion : informationalVersion[..metadataStart];
        }

        return assemblyVersion?.ToString(3) ?? "unknown";
    }

    // Static files are served without a fingerprint: the version in the URL makes browsers (the iPhone home-screen
    // app above all) load the new file after a release instead of keeping the cached one.
    public static string Versioned(string path) => Versioned(path, Current);

    internal static string Versioned(string path, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return $"{path}?v={Uri.EscapeDataString(version)}";
    }
}
