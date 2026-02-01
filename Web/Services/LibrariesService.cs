using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Libraries.Create;
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
    ICommandHandler<UpdateLibraryCommand, Library> updateLibraryHandler,
    ICommandHandler<DeleteLibraryCommand> deleteLibraryHandler) : ILibrariesService
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

    public async Task<Result<Library>> Create(string? name)
    {
        var command = new CreateLibraryCommand(name ?? "");

        return await createLibraryHandler.Handle(command, CancellationToken.None);
    }

    public async Task<Result<Library>> Update(string? id, string? name)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return LibrariesError.ValidationError;
        }

        var command = new UpdateLibraryCommand(guidId, name ?? "");

        return await updateLibraryHandler.Handle(command, CancellationToken.None);
    }

    public async Task<Result<IPagedList<Library>>> FilterBy(string? searchTerm, LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize)
    {
        var query = new GetLibrariesQuery(searchTerm, sortColumn, sortOrder, page, pageSize);

        return await getLibrariesHandler.Handle(query, CancellationToken.None);
    }

    public async Task<Result> Delete(string? id)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return LibrariesError.ValidationError;
        }

        var command = new DeleteLibraryCommand(guidId);

        return await deleteLibraryHandler.Handle(command, CancellationToken.None);
    }
}
