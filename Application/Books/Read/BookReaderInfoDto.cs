using Domain.Books;

namespace Application.Books.Read;

public sealed record BookReaderInfoDto(
    Guid BookId,
    string Serie,
    string Title,
    int VolumeNumber,
    int PageCount,
    int LastReadPage,
    ReaderSettings Settings);
