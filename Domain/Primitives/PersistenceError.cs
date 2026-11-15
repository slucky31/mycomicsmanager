namespace Domain.Primitives;

public static class PersistenceError
{
    // The row was changed (or deleted) by another context since it was loaded: the tracked entities are discarded.
    public static readonly TError ConcurrencyConflict = new("DB_CONCURRENCY", "This item was changed in the meantime. The list was refreshed, try again.");
}
