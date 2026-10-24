namespace Web.Services;

// Per-circuit signal: the libraries shown changed (e.g. "Show hidden libraries" in the settings), the navigation lists them again.
public sealed class LibrariesNotifier
{
    public event EventHandler? LibrariesChanged;

    public void NotifyChanged() => LibrariesChanged?.Invoke(this, EventArgs.Empty);
}
