using Application;
using Carter;
using Persistence;
using Presentation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();

var connectionString = "mongodb+srv://api-rest-dev:cyKVB7Jc2oKsympb@dev.dvd91.azure.mongodb.net/";
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

app.Run();
