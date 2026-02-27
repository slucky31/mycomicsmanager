using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Libraries.Create;
using Application.Libraries.CreateDefault;
using Application.Libraries.Delete;
using Application.Libraries.GetById;
using Application.Libraries.List;
using Application.Libraries.Update;
using Domain.Libraries;
using Domain.Primitives;

namespace Web.Services;

public class LibrariesService(
    IQueryHandler<GetLibraryQuery, Library> getLibraryHandler,
    IQueryHandler<GetLibrariesQuery, IPagedList<Library>> getLibrariesHandler,
    ICommandHandler<CreateLibraryCommand, Library> createLibraryHandler,
    ICommandHandler<CreateDefaultLibraryCommand, Library> createDefaultLibraryHandler,
    ICommandHandler<UpdateLibraryCommand, Library> updateLibraryHandler,
    ICommandHandler<DeleteLibraryCommand> deleteLibraryHandler,
    ICurrentUserService currentUserService) : ILibrariesService
{
    public async Task<Result<Library>> GetById(string? id)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return LibrariesError.ValidationError;
        }

        var query = new GetLibraryQuery(guidId);

        return await getLibraryHandler.Handle(query, CancellationToken.None);
    }

    public async Task<Result<Library>> Create(CreateLibraryRequest request, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var command = new CreateLibraryCommand(
            request.Name,
            request.Color,
            request.Icon,
            request.BookType,
            userIdResult.Value);

        return await createLibraryHandler.Handle(command, cancellationToken);
    }

    public async Task<Result<Library>> CreateDefault(CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var command = new CreateDefaultLibraryCommand(userIdResult.Value);

        return await createDefaultLibraryHandler.Handle(command, cancellationToken);
    }

    public async Task<Result<Library>> Update(UpdateLibraryRequest request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(request.Id, out var guidId))
        {
            return LibrariesError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var command = new UpdateLibraryCommand(
            guidId,
            request.Name,
            request.Color,
            request.Icon,
            userIdResult.Value);

        return await updateLibraryHandler.Handle(command, cancellationToken);
    }

    public async Task<Result<IPagedList<Library>>> FilterBy(string? searchTerm, LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var query = new GetLibrariesQuery(searchTerm, sortColumn, sortOrder, page, pageSize, userIdResult.Value);

        return await getLibrariesHandler.Handle(query, cancellationToken);
    }

    public async Task<Result> Delete(string? id, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return LibrariesError.ValidationError;
        }

        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
            return userIdResult.Error;

        var command = new DeleteLibraryCommand(guidId, userIdResult.Value);

        return await deleteLibraryHandler.Handle(command, cancellationToken);
    }
}
