using Application;
using Carter;
using Microsoft.OpenApi.Models;
using Persistence;
using Presentation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Description = "Carter Sample API",
        Version = "v1",
        Title = "A Carter API to manage Actors/Films/Crew etc"
    });

});

builder.Services.AddCarter();

var connectionString = "mongodb+srv://api-rest-dev:xJjXHAYdkDzITEX3@dev.dvd91.azure.mongodb.net/";
var dataBaseName = "Dev";

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString, dataBaseName)
    .AddPresentation();

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.MapCarter();
app.Run();
