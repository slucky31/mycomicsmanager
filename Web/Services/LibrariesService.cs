using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Libraries.Create;
using Application.Libraries.Delete;
using Application.Libraries.GetById;
using Application.Libraries.List;
using Application.Libraries.Summaries;
using Application.Libraries.Update;
using Application.Users.HiddenLibraries;
using Domain.Libraries;
using Domain.Primitives;

namespace Web.Services;

public class LibrariesService(
    IQueryHandler<GetLibraryQuery, Library> getLibraryHandler,
    IQueryHandler<GetLibrariesQuery, IPagedList<Library>> getLibrariesHandler,
    IQueryHandler<GetLibrarySummariesQuery, IReadOnlyList<LibrarySummary>> getLibrarySummariesHandler,
    ICommandHandler<CreateLibraryCommand, Library> createLibraryHandler,
    ICommandHandler<UpdateLibraryCommand, Library> updateLibraryHandler,
    ICommandHandler<DeleteLibraryCommand> deleteLibraryHandler,
    IQueryHandler<GetShowHiddenLibrariesQuery, bool> getShowHiddenLibrariesHandler,
    ICommandHandler<SetShowHiddenLibrariesCommand> setShowHiddenLibrariesHandler,
    ICurrentUserService currentUserService) : ILibrariesService
{
    public async Task<Result<Library>> GetById(string? id)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return LibrariesError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var query = new GetLibraryQuery(guidId, UserId: userIdResult.Value);

        return await getLibraryHandler.Handle(query, CancellationToken.None);
    }

    public async Task<Result<Library>> Create(CreateLibraryRequest request, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var command = new CreateLibraryCommand(
            request.Name,
            request.Color,
            request.Icon,
            request.BookType,
            userIdResult.Value);

        return await createLibraryHandler.Handle(command, cancellationToken);
    }

    public async Task<Result<Library>> Update(UpdateLibraryRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(request.Id, out var guidId))
        {
            return LibrariesError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var command = new UpdateLibraryCommand(
            guidId,
            request.Name,
            request.Color,
            request.Icon,
            userIdResult.Value,
            request.DefaultBookSortOrder,
            request.DefaultView,
            request.IsHidden);

        return await updateLibraryHandler.Handle(command, cancellationToken);
    }

    public async Task<Result<IPagedList<Library>>> FilterBy(string? searchTerm, LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var query = new GetLibrariesQuery(searchTerm, sortColumn, sortOrder, page, pageSize, userIdResult.Value);

        return await getLibrariesHandler.Handle(query, cancellationToken);
    }

    public Task<Result<IReadOnlyList<LibrarySummary>>> GetSummaries(CancellationToken cancellationToken = default) =>
        GetSummaries(includeHidden: false, cancellationToken);

    public async Task<Result<IReadOnlyList<LibrarySummary>>> GetSummaries(bool includeHidden, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await getLibrarySummariesHandler.Handle(new GetLibrarySummariesQuery(userIdResult.Value, includeHidden), cancellationToken);
    }

    public async Task<Result<bool>> GetShowHiddenLibraries(CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await getShowHiddenLibrariesHandler.Handle(new GetShowHiddenLibrariesQuery(userIdResult.Value), cancellationToken);
    }

    public async Task<Result> SetShowHiddenLibraries(bool show, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await setShowHiddenLibrariesHandler.Handle(new SetShowHiddenLibrariesCommand(userIdResult.Value, show), cancellationToken);
    }

    public async Task<Result> Delete(string? id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return LibrariesError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var command = new DeleteLibraryCommand(guidId, userIdResult.Value);

        return await deleteLibraryHandler.Handle(command, cancellationToken);
    }
}
