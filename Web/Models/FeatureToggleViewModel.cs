using Application.Settings;
using Domain.Settings;

namespace Web.Models;

public sealed record FeatureToggleViewModel(
    FeatureToggle Toggle,
    string Label,
    string Description,
    string ConfigurationKey,
    bool Enabled,
    bool ConfiguredOn,
    bool IsOverridden)
{
    public static FeatureToggleViewModel From(FeatureToggleState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var (label, description, configurationKey) = Describe(state.Toggle);
        return new FeatureToggleViewModel(state.Toggle, label, description, configurationKey, state.Enabled, state.ConfiguredOn, state.IsOverridden);
    }

    public string ConfigurationDisplay => $"Configured {(ConfiguredOn ? "on" : "off")} ({ConfigurationKey})";

    private static (string Label, string Description, string ConfigurationKey) Describe(FeatureToggle toggle) => toggle switch
    {
        FeatureToggle.FeedImport => ("Feed import", "Miniflux sync and automatic downloads through Debrid-Link.", "FeedImport:Enabled"),
        FeatureToggle.IsbnOcr => ("ISBN reading (OCR)", "Reads the ISBN printed on the pages of the imported digital books.", "IsbnOcr:Enabled"),
        FeatureToggle.Bedetheque => ("Bedetheque search", "Searches Bedetheque (through SerpApi) when looking up an ISBN.", "Bedetheque:Enabled"),
        FeatureToggle.Bnf => ("BnF catalogue search", "Searches the catalogue of the Bibliothèque nationale de France when looking up an ISBN.", "BnfCatalogue:Enabled"),
        FeatureToggle.GoogleBooks => ("Google Books search", "Searches Google Books when looking up an ISBN.", "GoogleBooks:Enabled"),
        FeatureToggle.OpenLibrary => ("OpenLibrary search", "Searches OpenLibrary when looking up an ISBN.", "OpenLibrary:Enabled"),
        FeatureToggle.Bubble => ("Bubble search", "Searches Bubble (bubblebd.com) when looking up an ISBN; needs Bubble:ApiKey.", "Bubble:Enabled"),
        _ => (toggle.ToString(), string.Empty, $"{toggle}:Enabled")
    };
}
