using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;
using Domain.Series;

namespace Application.Series.List;

// The series of a library, by name (#1205).
public sealed class GetLibrarySeriesQueryHandler(
    ISeriesReadService seriesReadService,
    IRepository<Library, Guid> libraryRepository) : IQueryHandler<GetLibrarySeriesQuery, IReadOnlyList<SerieSummary>>
{
    public async Task<Result<IReadOnlyList<SerieSummary>>> Handle(GetLibrarySeriesQuery request, CancellationToken cancellationToken)
    {
        if (request is null || request.UserId == Guid.Empty || request.LibraryId == Guid.Empty)
        {
            return SeriesError.BadRequest;
        }

        var library = await libraryRepository.GetByIdAsync(request.LibraryId);
        if (library is null || library.UserId != request.UserId)
        {
            return LibrariesError.NotFound;
        }

        var books = await seriesReadService.ListBooksAsync(request.UserId, request.LibraryId, cancellationToken);
        var infos = await seriesReadService.ListInfosAsync(request.UserId, request.LibraryId, cancellationToken);

        return Result<IReadOnlyList<SerieSummary>>.Success(SerieGrouping.Summarize(books, infos));
    }
}
