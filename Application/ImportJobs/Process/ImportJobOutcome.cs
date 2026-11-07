using Domain.Books;

namespace Application.ImportJobs.Process;

// What a processed import produced: the book, or the archives found inside a split archive.
public sealed record ImportJobOutcome
{
    private ImportJobOutcome(DigitalBook? book, IReadOnlyList<string> splitFileNames)
    {
        Book = book;
        SplitFileNames = splitFileNames;
    }

    public DigitalBook? Book { get; }

    public IReadOnlyList<string> SplitFileNames { get; }

    public static ImportJobOutcome Imported(DigitalBook book) => new(book, []);

    public static ImportJobOutcome Split(IReadOnlyList<string> fileNames) => new(null, fileNames);
}
