namespace Application.Interfaces;

#pragma warning disable CA1054 // URL parameters stored as strings for DB compatibility
public interface IIsbnBedethequeCacheRepository
{
    Task<string?> GetUrlByIsbnAsync(string isbn, CancellationToken ct = default);
    Task SaveAsync(string isbn, string url, CancellationToken ct = default);
}
#pragma warning restore CA1054
