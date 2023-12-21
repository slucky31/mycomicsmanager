using Amazon.Util;
using Application;
using Application.Librairies.Create;
using Application.Librairies.Delete;
using Application.Librairies.Get;
using Application.Librairies.Update;
using Carter;
using Domain.Libraries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

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

        app.MapGet("libraries/{id}", async (string id, ISender sender) =>
        {
            try
            {
                return Results.Ok(await sender.Send(new GetLibraryQuery(new LibraryId(new ObjectId(id)))));
            }
            catch (ArgumentNullException ex) 
            {                
                return Results.NotFound(ex.Message);
            }

        });

        app.MapPut("libraries/{id}", async (string id, [FromBody] UpdateLibraryRequest request, ISender sender) =>
        {
            var command = new UpdateLibraryCommand(new LibraryId(new ObjectId(id)), request.Name);
            
            await sender.Send(command);

            return Results.NoContent();

        });

        app.MapDelete("libraries/{id}", async (string id, ISender sender) =>
        {
            try
            {
                await sender.Send(new DeleteLibraryCommand(new LibraryId(new ObjectId(id))));

                return Results.NoContent();
            }
            catch (LibraryNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
        });
    }
}
