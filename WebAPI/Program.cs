using Application;
using Ardalis.GuardClauses;
using Carter;
using Microsoft.OpenApi.Models;
using Persistence;
using Serilog;
using WebAPI;
using WebAPI.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(nameof(MongoDbOptions)));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Description = "MCM API",
        Version = "v1",
        Title = "An API to manage CBZ comic books"
    });

});

builder.Services.AddCarter();

var options = builder.Configuration.GetSection(nameof(MongoDbOptions)).Get<MongoDbOptions>();
Guard.Against.Null(options);

builder.Services
    .AddApplication()
    .AddInfrastructure(options.ConnectionString, options.DatabaseName);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));


builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.MapCarter();
app.Run();

public partial class Program { }
