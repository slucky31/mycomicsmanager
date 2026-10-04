namespace Web.Services;

// Per-circuit signal: a decision changed on the Feeds page, the navigation badge counts again.
public sealed class FeedImportNotifier
{
    public event EventHandler? DecisionsChanged;

    public void NotifyChanged() => DecisionsChanged?.Invoke(this, EventArgs.Empty);
}
