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
}
