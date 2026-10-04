using System.Collections.Frozen;
using System.Reflection;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Web.Infrastructure;

/// <summary>A logger as shown on the log levels page: its own level (null = inherited) and the level actually applied.</summary>
public sealed record LoggerLevel(string Name, LogEventLevel? ConfiguredLevel, LogEventLevel EffectiveLevel);

/// <summary>
/// Runtime-adjustable log levels, one Serilog <see cref="LoggingLevelSwitch"/> per declared logger (JHipster-like
/// logs dashboard). Serilog freezes its override table when the logger is built, so every logger that can be tuned
/// is declared at startup: the classes that take an <c>ILogger&lt;T&gt;</c>, their parent namespaces and a list of
/// third-party prefixes. Like Logback, a logger without a level of its own inherits it from its nearest declared
/// parent, then from <see cref="RootLoggerName"/> (Serilog:MinimumLevel:Default).
/// State is in memory only: a restart goes back to the levels of appsettings.
/// </summary>
public sealed class LogLevelSwitches
{
    public const string RootLoggerName = "ROOT";

    // "None": above Fatal, nothing gets through.
    public const LogEventLevel Off = LevelAlias.Off;

    // Embedded libraries tunable on their own; any other library is driven by its parent prefix (Microsoft, System...).
    private static readonly string[] s_libraryLoggers =
    [
        "Microsoft.AspNetCore.Components",
        "Microsoft.AspNetCore.Hosting.Diagnostics",
        "Microsoft.EntityFrameworkCore.Database.Command",
        "System.Net.Http.HttpClient",
        "Serilog.AspNetCore.RequestLoggingMiddleware",
        "Npgsql",
        "Hangfire",
    ];

    private readonly Lock _lock = new();
    private readonly FrozenDictionary<string, Node> _nodes;
    private readonly Node _root;

    public LogLevelSwitches(LogEventLevel defaultLevel, IReadOnlyDictionary<string, LogEventLevel> overrides, IEnumerable<string> loggerNames)
    {
        ArgumentNullException.ThrowIfNull(overrides);
        ArgumentNullException.ThrowIfNull(loggerNames);

        _root = new Node(RootLoggerName, defaultLevel);
        var names = loggerNames
            .Concat(overrides.Keys)
            .Where(name => !string.IsNullOrWhiteSpace(name) && name != RootLoggerName)
            .SelectMany(WithParentNamespaces)
            .Distinct(StringComparer.Ordinal);

        var nodes = new Dictionary<string, Node>(StringComparer.Ordinal) { [RootLoggerName] = _root };
        foreach (var name in names)
        {
            nodes[name] = new Node(name, overrides.TryGetValue(name, out var level) ? level : null);
        }

        foreach (var node in nodes.Values.Where(n => n != _root))
        {
            node.Parent = FindParent(node.Name, nodes);
        }

        _nodes = nodes.ToFrozenDictionary(StringComparer.Ordinal);
        Refresh();
    }

    public static LogLevelSwitches FromConfiguration(IConfiguration configuration, IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var minimumLevel = configuration.GetSection("Serilog:MinimumLevel");
        var defaultLevel = ParseLevel(minimumLevel.Value) ?? ParseLevel(minimumLevel["Default"]) ?? LogEventLevel.Information;
        var overrides = new Dictionary<string, LogEventLevel>(StringComparer.Ordinal);
        foreach (var entry in minimumLevel.GetSection("Override").GetChildren())
        {
            if (ParseLevel(entry.Value) is { } level)
            {
                overrides[entry.Key] = level;
            }
        }

        return new LogLevelSwitches(defaultLevel, overrides, DiscoverLoggerNames(assemblies).Concat(s_libraryLoggers));
    }

    /// <summary>Categories of the <c>ILogger&lt;T&gt;</c> taken by a constructor or an (injected) property.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011", Justification = "Only member signatures are read, never invoked nor accessed; NonPublic is needed for the private [Inject] properties of Razor components.")]
    public static IEnumerable<string> DiscoverLoggerNames(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        return assemblies
            .SelectMany(GetLoadableTypes)
            .SelectMany(type => type.GetConstructors(members).SelectMany(c => c.GetParameters()).Select(p => p.ParameterType)
                .Concat(type.GetProperties(members).Select(p => p.PropertyType)))
            .Select(GetLoggerCategory)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal);
    }

    public IReadOnlyList<LoggerLevel> GetLoggers()
    {
        lock (_lock)
        {
            return [.. _nodes.Values
                .OrderBy(n => n != _root)
                .ThenBy(n => n.Name, StringComparer.Ordinal)
                .Select(n => new LoggerLevel(n.Name, n.ConfiguredLevel, n.Switch.MinimumLevel))];
        }
    }

    /// <summary>Sets the level of a logger; null makes it inherit from its parent again (the root goes back to its initial level).</summary>
    /// <returns>false when the logger was not declared at startup.</returns>
    public bool Set(string name, LogEventLevel? level)
    {
        if (name is null || !_nodes.TryGetValue(name, out var node))
        {
            return false;
        }

        lock (_lock)
        {
            node.ConfiguredLevel = node == _root ? level ?? node.InitialLevel : level;
            Refresh();
        }

        return true;
    }

    public void ResetAll()
    {
        lock (_lock)
        {
            foreach (var node in _nodes.Values)
            {
                node.ConfiguredLevel = node.InitialLevel;
            }

            Refresh();
        }
    }

    /// <summary>Hands the switches to Serilog; call it after <c>ReadFrom.Configuration</c> so they replace its overrides.</summary>
    public LoggerConfiguration ApplyTo(LoggerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.MinimumLevel.ControlledBy(_root.Switch);
        foreach (var node in _nodes.Values.Where(n => n != _root))
        {
            configuration.MinimumLevel.Override(node.Name, node.Switch);
        }

        return configuration;
    }

    // Serilog applies the most specific override only, so the inheritance is resolved here: every switch holds its effective level.
    private void Refresh()
    {
        foreach (var node in _nodes.Values)
        {
            node.Switch.MinimumLevel = node.EffectiveLevel;
        }
    }

    private static Node? FindParent(string name, Dictionary<string, Node> nodes)
    {
        for (var dot = name.LastIndexOf('.'); dot > 0; dot = name.LastIndexOf('.', dot - 1))
        {
            if (nodes.TryGetValue(name[..dot], out var parent))
            {
                return parent;
            }
        }

        return nodes[RootLoggerName];
    }

    // "Web.Services.FeedImportSyncJob" -> itself, "Web.Services", "Web".
    private static IEnumerable<string> WithParentNamespaces(string name)
    {
        yield return name;
        for (var dot = name.LastIndexOf('.'); dot > 0; dot = name.LastIndexOf('.', dot - 1))
        {
            yield return name[..dot];
        }
    }

    private static string? GetLoggerCategory(Type type)
    {
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(ILogger<>))
        {
            return null;
        }

        var category = type.GetGenericArguments()[0];
        // Same name as Microsoft.Extensions.Logging gives the category: nested types use '.'.
        return category.ContainsGenericParameters || category.FullName is null ? null : category.FullName.Replace('+', '.');
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private static LogEventLevel? ParseLevel(string? value) =>
        Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var level) && Enum.IsDefined(level) ? level : null;

    private sealed class Node(string name, LogEventLevel? initialLevel)
    {
        public string Name { get; } = name;

        public LoggingLevelSwitch Switch { get; } = new();

        public LogEventLevel? InitialLevel { get; } = initialLevel;

        public LogEventLevel? ConfiguredLevel { get; set; } = initialLevel;

        public Node? Parent { get; set; }

        public LogEventLevel EffectiveLevel => ConfiguredLevel ?? Parent?.EffectiveLevel ?? LogEventLevel.Information;
    }
}
