namespace Domain.Books;

public sealed record BookMetadata(
    string Serie,
    string Title,
    string? ISBN,
    int VolumeNumber = 1,
    string ImageLink = "",
    string Authors = "",
    string Publishers = "",
    DateOnly? PublishDate = null,
    int? NumberOfPages = null);
