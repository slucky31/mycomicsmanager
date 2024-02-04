using Application.Data;
using Application.Interfaces;
using Application.Libraries;
using Ardalis.GuardClauses;
using Domain.Libraries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Persistence;
using Persistence.Queries;
using Persistence.Repositories;
using Xunit;

namespace Base.Integration.Tests;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IDisposable
{
    private readonly IServiceScope _scope;
    protected ISender Sender { get; }
    protected ApplicationDbContext Context { get; }
    protected IRepository<Library, ObjectId> LibraryRepository { get; }

    protected ILibraryReadService LibraryReadService { get; }
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
        LibraryReadService = _scope.ServiceProvider.GetRequiredService<ILibraryReadService>();

        // Clear all MongoDb Collections before tests
        Context.Libraries.RemoveRange(Context.Libraries);
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
