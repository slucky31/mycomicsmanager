using Application.Interfaces;
using Application.Libraries.Create;
using Application.Libraries.Delete;
using Application.Libraries.GetById;
using Application.Libraries.List;
using Application.Libraries.Update;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using MongoDB.Bson;

namespace Web.Services;

internal class LibrariesService(IMediator mediator) : ILibrariesService
{
    public async Task<Result<Library>> GetById(string? id)
    {
        return await mediator.Send(new GetLibraryQuery(new ObjectId(id)));
    }

    public async Task<Result<Library>> Create(string? name)
    {
        return await mediator.Send(new CreateLibraryCommand(name ?? ""));
    }

    public async Task<Result<Library>> Update(string? id, string? name)
    {
        return await mediator.Send(new UpdateLibraryCommand(new ObjectId(id), name ?? ""));
    }

    public async Task<IPagedList<Library>> FilterBy(string? searchTerm, LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize)
    {
        return await mediator.Send(new GetLibrariesQuery(searchTerm, sortColumn, sortOrder, page, pageSize));
    }

    public async Task<Result> Delete(string? id)
    {
        return await mediator.Send(new DeleteLibraryCommand(new ObjectId(id)));
    }

}
