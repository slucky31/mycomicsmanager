using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog.Events;
using Web.Infrastructure;

namespace Web.Components.Pages.Admin;

public partial class LogLevels
{
    private sealed record LevelOption(string Label, LogEventLevel Level, Color Color);

    private static readonly LevelOption[] s_levels =
    [
        new("Debug", LogEventLevel.Debug, Color.Success),
        new("Info", LogEventLevel.Information, Color.Info),
        new("Warning", LogEventLevel.Warning, Color.Warning),
        new("Error", LogEventLevel.Error, Color.Error),
        new("None", LogLevelSwitches.Off, Color.Dark),
    ];

    private static readonly int[] s_pageSizeOptions = [25, 50, 100];

    [Inject] private LogLevelSwitches LogLevelSwitches { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<LogLevels> Logger { get; set; } = default!;

    private IReadOnlyList<LoggerLevel> _loggers = [];
    private string? _filter;

    protected override void OnInitialized() => _loggers = LogLevelSwitches.GetLoggers();

    private bool MatchesFilter(LoggerLevel logger) =>
        string.IsNullOrWhiteSpace(_filter) || logger.Name.Contains(_filter.Trim(), StringComparison.OrdinalIgnoreCase);

    // The names come from LogLevelSwitches itself, so Set never meets an unknown logger here.
    private void SetLevel(string name, LogEventLevel? level)
    {
        LogLevelSwitches.Set(name, level);
        Logger.LogInformation("Log levels: {LoggerName} set to {Level}", name, level?.ToString() ?? "inherited");
        _loggers = LogLevelSwitches.GetLoggers();
    }

    private void ResetAll()
    {
        LogLevelSwitches.ResetAll();
        Logger.LogInformation("Log levels: all loggers reset to their configured levels");
        _loggers = LogLevelSwitches.GetLoggers();
        Snackbar.Add("Log levels reset to the configured values", Severity.Success);
    }
}
