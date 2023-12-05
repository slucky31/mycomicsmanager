using Application;
using Application.Librairies.Create;
using Application.Librairies.Delete;
using Application.Librairies.Update;
using Carter;
using Domain.Libraries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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

        app.MapPut("libraries/{id:guid}", async (Guid id, [FromBody] UpdateLibraryRequest request, ISender sender) =>
        {
            var command = new UpdateLibraryCommand(new LibraryId(id), request.Name);

            await sender.Send(command);

            return Results.NoContent();

        });

        app.MapDelete("libraries/{id:guid}", async (Guid id, ISender sender) =>
        {
            try
            {
                await sender.Send(new DeleteLibraryCommand(new LibraryId(id)));

                return Results.NoContent();
            }
            catch (LibraryNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }                       
        });
    }
}
