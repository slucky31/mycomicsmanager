namespace Application.Books.Reading;

// A digital book opened in the reader and not finished yet (#1252).
public sealed record BookInProgressDto(
    Guid Id,
    Guid LibraryId,
    string Serie,
    string Title,
    int VolumeNumber,
    string ImageLink,
    int LastReadPage,
    int? NumberOfPages,
    DateTime? LastReadAt);
