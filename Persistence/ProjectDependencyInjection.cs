using Application.ComicInfoSearch;
using Application.Interfaces;
using Application.Libraries;
using Application.Users;
using Domain.Books;
using Domain.Libraries;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.LocalStorage;
using Persistence.Queries;
using Persistence.Repositories;
using Persistence.Services;

namespace Persistence;

public static class ProjectDependencyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, string rootPath, IConfiguration configuration)
    {

        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly("Persistence")
                )
                .EnableDetailedErrors(true)
        );

        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>());

        services.AddScoped<IRepository<Library, Guid>, LibraryRepository>();
        services.AddScoped<IRepository<User, Guid>, UserRepository>();
        services.AddScoped<IRepository<Book, Guid>, BookRepository>();
        services.AddScoped<IBookRepository, BookRepository>();

        services.AddScoped<ILibraryReadService, LibraryReadService>();
        services.AddScoped<IUserReadService, UserReadService>();

        services.AddScoped<ILibraryLocalStorage>(provider => new LibraryLocalStorage(rootPath));

        // Config Cloudinary service for cover image storage
        var cloudinarySection = configuration.GetSection("Cloudinary");
        services.AddOptions<CloudinarySettings>()
            .Bind(cloudinarySection)
            .ValidateOnStart();
        services.AddScoped<ICloudinaryService, CloudinaryService>();

        return services;
    }
}
