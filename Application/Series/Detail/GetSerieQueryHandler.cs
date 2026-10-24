using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;
using Domain.Series;

namespace Application.Series.Detail;

// A series of a library, found by its name whatever its case (#1205).
public sealed class GetSerieQueryHandler(
    ISeriesReadService seriesReadService,
    IRepository<Library, Guid> libraryRepository) : IQueryHandler<GetSerieQuery, SerieDetail>
{
    public async Task<Result<SerieDetail>> Handle(GetSerieQuery request, CancellationToken cancellationToken)
    {
        if (request is null || request.UserId == Guid.Empty || request.LibraryId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name))
        {
            return SeriesError.BadRequest;
        }

        var library = await libraryRepository.GetByIdAsync(request.LibraryId);
        if (library is null || library.UserId != request.UserId)
        {
            return LibrariesError.NotFound;
        }

        var normalizedName = SerieNames.Normalize(request.Name);
        var allBooks = await seriesReadService.ListBooksAsync(request.UserId, request.LibraryId, cancellationToken);
        var books = SerieGrouping.OrderByVolume(allBooks.Where(b => SerieNames.Normalize(b.Serie) == normalizedName));
        if (books.Count == 0)
        {
            return SeriesError.NotFound;
        }

        var infos = await seriesReadService.ListInfosAsync(request.UserId, request.LibraryId, cancellationToken);
        var info = infos.FirstOrDefault(i => i.NormalizedName == normalizedName);

        return new SerieDetail(library.Name, SerieGrouping.Summarize(books, info), books, library.BookType);
    }
}
