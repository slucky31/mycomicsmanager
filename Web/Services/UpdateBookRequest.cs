namespace Web.Services;

public sealed record UpdateBookRequest(
    string? Id,
    string Series,
    string Title,
    string Isbn,
    int VolumeNumber,
    string ImageLink,
    int Rating,
    string Authors = "",
    string Publishers = "",
    DateOnly? PublishDate = null,
    int? NumberOfPages = null);
