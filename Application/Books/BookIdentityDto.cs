namespace Application.Books;

// Minimal book data used to detect duplicates before importing a new file.
public sealed record BookIdentityDto(Guid Id, string Serie, string Title, int VolumeNumber, string? Isbn, string LibraryName);
