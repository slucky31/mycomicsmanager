using Application.Libraries.Create;
using Application.Libraries.GetById;
using Application.Libraries.Update;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using MongoDB.Bson;

namespace Web.Services;

public class LibrariesService(IMediator mediator) : ILibrariesService
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

}
