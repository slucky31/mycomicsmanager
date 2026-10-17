namespace Web.Services;

public interface IBookMoveWorkflow
{
    // Lets the user pick a library of the same type, then moves the book there. True only when the book was moved.
    Task<bool> ChooseAndMoveAsync(Guid bookId, CancellationToken cancellationToken = default);

    // The same for books of one library (#1150); returns how many were moved.
    Task<int> ChooseAndMoveAsync(IReadOnlyList<Guid> bookIds, CancellationToken cancellationToken = default);
}
