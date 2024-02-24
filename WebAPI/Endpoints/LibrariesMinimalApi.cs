using Application.Libraries.Create;
using Application.Libraries.Delete;
using Application.Libraries.List;
using Application.Libraries.GetById;
using Application.Libraries.Update;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Domain.Libraries;
using Domain.Primitives;

namespace WebAPI.Endpoints;

public class LibrariesMinimalApi : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("libraries", async (CreateLibraryCommand command, ISender sender) =>
        {
            await sender.Send(command);

            return Results.Ok();
        });

        app.MapGet("libraries", async (
            string? searchTerm,
            LibrariesColumn? sortColumn,
            SortOrder? sortOrder,
            int page,
            int pageSize,
            ISender sender) =>
        {
            var query = new GetLibrariesQuery(searchTerm, sortColumn, sortOrder, page, pageSize);

            var libraries = await sender.Send(query);

            return Results.Ok(libraries);
        });

        app.MapGet("libraries/{id}", async (string id, ISender sender) =>
        {
            try
            {
                return Results.Ok(await sender.Send(new GetLibraryQuery(new ObjectId(id))));
            }
            catch (ArgumentNullException ex) 
            {                
                return Results.NotFound(ex.Message);
            }

        });

        app.MapPut("libraries/{id}", async (string id, [FromBody] UpdateLibraryRequest request, ISender sender) =>
        {
            var command = new UpdateLibraryCommand(new ObjectId(id), request.Name);
            
            await sender.Send(command);

            return Results.NoContent();

        });

        app.MapDelete("libraries/{id}", async (string id, ISender sender) =>
        {            
            var result = await sender.Send(new DeleteLibraryCommand(new ObjectId(id)));

            if (result.IsSuccess)
            {
                return Results.NoContent();
            }
            else
            {
                return Results.NotFound();
            }                                        
        });
    }
}
