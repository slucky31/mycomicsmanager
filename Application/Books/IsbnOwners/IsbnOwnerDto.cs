namespace Application.Books.IsbnOwners;

// A book that holds one of the ISBNs read on the pages of another book (#1276): what is needed to compare them.
public sealed record IsbnOwnerDto(
    string Isbn,
    Guid BookId,
    string Serie,
    string Title,
    int VolumeNumber,
    string ImageLink,
    string LibraryName);
