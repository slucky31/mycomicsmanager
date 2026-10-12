namespace Web.Models;

// Sent to bookReader.js: how the taps, swipes and scrolling behave.
public sealed record ReaderScriptOptions(bool Paged, bool RightToLeft, bool FitWidth);

// Returned by bookReader.js when it attaches to the reader.
public sealed record ReaderClientState(bool CanUseFullscreen, bool GestureHelpSeen);
