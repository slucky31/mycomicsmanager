namespace Domain.Books;

// The reader settings are stored as numbers: keep the order of the values (0 = the default).

// How the reader lays the pages out.
public enum ReaderLayout
{
    // One page at the full width of the screen, scrolled vertically.
    FitWidth,

    // One page at the full height of the screen.
    SinglePage,

    // Two pages side by side at the full height of the screen.
    TwoPages
}

public enum ReaderScrolling
{
    // The pages follow each other in one vertical scroll.
    Continuous,

    // One page (or pair of pages) at a time, turned by taps, swipes or arrows.
    PageByPage
}

public enum ReadingDirection
{
    LeftToRight,

    // Mangas: the next page is on the left.
    RightToLeft
}

public sealed record ReaderSettings(ReaderLayout Layout, ReaderScrolling Scrolling, ReadingDirection Direction)
{
    public static ReaderSettings Default { get; } = new(ReaderLayout.FitWidth, ReaderScrolling.Continuous, ReadingDirection.LeftToRight);

    public bool IsValid() =>
        Enum.IsDefined(Layout) && Enum.IsDefined(Scrolling) && Enum.IsDefined(Direction);
}
