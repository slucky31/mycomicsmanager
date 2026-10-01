using Application.Interfaces;
using Application.Statistics.Get;
using Domain.Books;
using Domain.Libraries;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Queries;

public class StatisticsReadService(ApplicationDbContext context) : IStatisticsReadService
{
    public async Task<StatisticsDto> GetStatisticsAsync(
        Guid userId,
        Guid? libraryId,
        DateTime readingsFromUtc,
        CancellationToken cancellationToken = default)
    {
        var books = FilterBooks(context.Set<Book>().AsNoTracking(), userId, libraryId);
        var digitalBooks = FilterBooks(context.Set<DigitalBook>().AsNoTracking(), userId, libraryId);
        var readings = books.SelectMany(b => b.ReadingDates);

        var totalBooks = await books.CountAsync(cancellationToken);
        var totalSeries = await books.Select(b => b.Serie).Distinct().CountAsync(cancellationToken);
        var readBooks = await books.CountAsync(b => b.ReadingDates.Any(), cancellationToken);
        var totalPages = await books.SumAsync(b => (long?)b.NumberOfPages, cancellationToken) ?? 0;
        var digitalStorageBytes = await digitalBooks.SumAsync(b => (long?)b.FileSize, cancellationToken) ?? 0;

        var totalLibraries = await context.Set<Library>()
            .AsNoTracking()
            .CountAsync(l => l.UserId == userId && (libraryId == null || l.Id == libraryId), cancellationToken);

        var totalReadings = await readings.CountAsync(cancellationToken);
        var averageRating = await readings.AverageAsync(rd => (double?)rd.Rating, cancellationToken);

        var readingsPerMonth = await readings
            .Where(rd => rd.Date >= readingsFromUtc)
            .GroupBy(rd => new { rd.Date.Year, rd.Date.Month })
            .Select(g => new MonthlyReadingCountDto(g.Key.Year, g.Key.Month, g.Count()))
            .ToListAsync(cancellationToken);

        return new StatisticsDto
        {
            TotalBooks = totalBooks,
            TotalSeries = totalSeries,
            TotalLibraries = totalLibraries,
            ReadBooks = readBooks,
            TotalReadings = totalReadings,
            AverageRating = averageRating,
            TotalPages = totalPages,
            DigitalStorageBytes = digitalStorageBytes,
            ReadingsPerMonth = readingsPerMonth
        };
    }

    private static IQueryable<TBook> FilterBooks<TBook>(IQueryable<TBook> books, Guid userId, Guid? libraryId)
        where TBook : Book
        => books.Where(b => b.Library!.UserId == userId && (libraryId == null || b.LibraryId == libraryId));
}
