using Application.Books.IsbnOwners;

namespace Web.Models;

// The other book that already holds an ISBN read on the pages (#1276): enough to tell whether it is the same one.
public sealed record IsbnOwnerViewModel(Guid BookId, string Title, string? SerieAndVolume, string ImageLink, string LibraryName)
{
    public string CoverAlt => $"Cover of {Title}";

    public static IsbnOwnerViewModel From(IsbnOwnerDto owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return new IsbnOwnerViewModel(
            owner.BookId,
            owner.Title,
            BookText.SerieAndVolume(owner.Serie, owner.Title, owner.VolumeNumber),
            owner.ImageLink,
            owner.LibraryName);
    }
}

// One ISBN read on the pages: it can be picked unless another book holds it already.
public sealed record IsbnCandidateViewModel(string Isbn, IsbnOwnerViewModel? Owner)
{
    public bool IsUsed => Owner is not null;
}

// The ISBNs read on the pages of a book without ISBN, each with the book that holds it, if any.
public sealed record IsbnCandidatesViewModel(IReadOnlyList<IsbnCandidateViewModel> Candidates)
{
    public static readonly IsbnCandidatesViewModel None = new([]);

    // None can be picked: the book is likely a duplicate of the one(s) shown.
    public bool AllUsed => Candidates.Count > 0 && Candidates.All(c => c.IsUsed);

    public string Message => (AllUsed, Candidates.Count) switch
    {
        (true, 1) => "The ISBN printed in this book is already used by another book. This one may be a duplicate: compare them.",
        (true, _) => "The ISBNs printed in this book are already used by other books. This one may be a duplicate: compare them.",
        (false, 1) => "An ISBN is printed in this book. Use it to fetch its information:",
        _ => "Several ISBNs are printed in this book. Pick its own to fetch its information:",
    };

    // Told once the pages were scanned.
    public string ScanMessage => (AllUsed, Candidates.Count) switch
    {
        (true, 1) => "The ISBN found in the pages is already used by another book",
        (true, _) => "The ISBNs found in the pages are already used by other books",
        _ => "Several ISBNs found in the pages: pick the book's own",
    };

    public static IsbnCandidatesViewModel Of(IReadOnlyList<string> isbns, IReadOnlyList<IsbnOwnerDto> owners)
    {
        ArgumentNullException.ThrowIfNull(isbns);
        ArgumentNullException.ThrowIfNull(owners);

        var ownerByIsbn = owners
            .GroupBy(o => o.Isbn, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => IsbnOwnerViewModel.From(g.First()), StringComparer.Ordinal);

        return new IsbnCandidatesViewModel([.. isbns.Select(isbn => new IsbnCandidateViewModel(isbn, ownerByIsbn.GetValueOrDefault(isbn)))]);
    }
}
