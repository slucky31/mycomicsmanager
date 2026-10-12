using Domain.Books;

namespace Web.Models;

// Sent to bookReader.js: how the taps, swipes and scrolling behave.
// The key is also rendered on the scrolled element: until both match, the pages on screen were rendered
// for other settings than the script knows, and their scrolling is not the reader moving.
public sealed record ReaderScriptOptions(bool Paged, bool RightToLeft, bool FitWidth, string Key)
{
    public static ReaderScriptOptions From(ReaderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new ReaderScriptOptions(
            settings.Scrolling == ReaderScrolling.PageByPage,
            settings.Direction == ReadingDirection.RightToLeft,
            settings.Layout == ReaderLayout.FitWidth,
            $"{settings.Layout}-{settings.Scrolling}-{settings.Direction}");
    }
}

// Returned by bookReader.js when it attaches to the reader. PortraitAccepted: the reader chose to read in portrait
// on a phone earlier in this session.
public sealed record ReaderClientState(bool CanUseFullscreen, bool GestureHelpSeen, bool PortraitAccepted);
