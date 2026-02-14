namespace Web.Services;

public sealed record CreateBookRequest(
    string Series,
    string Title,
    string Isbn,
    int VolumeNumber = 1,
    string ImageLink = "",
    int Rating = 0,
    string Authors = "",
    string Publishers = "",
    DateOnly? PublishDate = null,
    int? NumberOfPages = null);
