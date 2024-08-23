using Application.Interfaces;
using Application.Libraries;
using Application.Users;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Users;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Persistence;
using Xunit;

namespace Base.Integration.Tests;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IDisposable
{
    private readonly IServiceScope _scope;
    protected ISender Sender { get; }
    protected ApplicationDbContext Context { get; }

    protected IRepository<Library, ObjectId> LibraryRepository { get; }
    protected IRepository<User, ObjectId> UserRepository { get; }

    protected ILibraryReadService LibraryReadService { get; }
    protected IUserReadService UserReadService { get; }

    protected ILibraryLocalStorage LibraryLocalStorage { get; }

    protected IUnitOfWork UnitOfWork { get; }

    private bool disposed;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Guard.Against.Null(factory);

        _scope = factory.Services.CreateScope();
        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        Context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        UnitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        LibraryRepository = _scope.ServiceProvider.GetRequiredService<IRepository<Library, ObjectId>>();
        UserRepository = _scope.ServiceProvider.GetRequiredService<IRepository<User, ObjectId>>();

        LibraryReadService = _scope.ServiceProvider.GetRequiredService<ILibraryReadService>();
        UserReadService = _scope.ServiceProvider.GetRequiredService<IUserReadService>();

        LibraryLocalStorage = _scope.ServiceProvider.GetRequiredService<ILibraryLocalStorage>();

        // Clear all MongoDb Collections before tests
        Context.Libraries.RemoveRange(Context.Libraries);
        Context.Users.RemoveRange(Context.Users);

        Context.SaveChanges();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                _scope?.Dispose();
            }
            disposed = true;
        }
    }
}
