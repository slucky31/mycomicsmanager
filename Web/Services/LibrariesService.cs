﻿using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Libraries.Create;
using Application.Libraries.Delete;
using Application.Libraries.GetById;
using Application.Libraries.List;
using Application.Libraries.Update;
using Domain.Libraries;
using Domain.Primitives;

namespace Web.Services;

public class LibrariesService : ILibrariesService
{
    private readonly ICommandHandler<CreateLibraryCommand, Library> handler_CreateLibraryCommand;
    private readonly ICommandHandler<UpdateLibraryCommand, Library> handler_UpdateLibraryCommand;
    private readonly ICommandHandler<DeleteLibraryCommand> handler_DeleteLibraryCommand;

    private readonly IQueryHandler<GetLibraryQuery, Library> handler_GetLibraryQuery;
    private readonly IQueryHandler<GetLibrariesQuery, IPagedList<Library>> handler_GetLibrariesQuery;

    public LibrariesService(IQueryHandler<GetLibraryQuery, Library> handler_GetLibraryQuery,
                            IQueryHandler<GetLibrariesQuery, IPagedList<Library>> handler_GetLibrariesQuery,
                            ICommandHandler<CreateLibraryCommand, Library> handler_CreateLibraryCommand,
                            ICommandHandler<UpdateLibraryCommand, Library> handler_UpdateLibraryCommand,
                            ICommandHandler<DeleteLibraryCommand> handler_DeleteLibraryCommand)
    {
        this.handler_GetLibraryQuery = handler_GetLibraryQuery;
        this.handler_GetLibrariesQuery = handler_GetLibrariesQuery;
        this.handler_CreateLibraryCommand = handler_CreateLibraryCommand;
        this.handler_UpdateLibraryCommand = handler_UpdateLibraryCommand;
        this.handler_DeleteLibraryCommand = handler_DeleteLibraryCommand;
    }

    public async Task<Result<Library>> GetById(string? id)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return LibrariesError.ValidationError;
        }

        var query = new GetLibraryQuery(guidId);

        return await handler_GetLibraryQuery.Handle(query, CancellationToken.None);
    }

    public async Task<Result<Library>> Create(string? name)
    {
        var command = new CreateLibraryCommand(name ?? "");

        return await handler_CreateLibraryCommand.Handle(command, CancellationToken.None);
    }

    public async Task<Result<Library>> Update(string? id, string? name)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return LibrariesError.ValidationError;
        }

        var command = new UpdateLibraryCommand(guidId, name ?? "");

        return await handler_UpdateLibraryCommand.Handle(command, CancellationToken.None);
    }

    public async Task<Result<IPagedList<Library>>> FilterBy(string? searchTerm, LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize)
    {
        var query = new GetLibrariesQuery(searchTerm, sortColumn, sortOrder, page, pageSize);

        return await handler_GetLibrariesQuery.Handle(query, CancellationToken.None);

    }

    public async Task<Result> Delete(string? id)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return LibrariesError.ValidationError;
        }

        var command = new DeleteLibraryCommand(guidId);

        return await handler_DeleteLibraryCommand.Handle(command, CancellationToken.None);
    }

}
