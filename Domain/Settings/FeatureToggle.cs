namespace Domain.Settings;

// Features an administrator can turn on or off from the Settings page.
// ponytail: persisted by name, never rename a value (add new ones freely)
public enum FeatureToggle
{
    FeedImport,  // FeedImport:Enabled  - Miniflux sync and automatic downloads
    IsbnOcr,     // IsbnOcr:Enabled     - ISBN read on the pages of the imported books
    Bedetheque   // Bedetheque:Enabled  - Bedetheque search (via SerpApi)
}
