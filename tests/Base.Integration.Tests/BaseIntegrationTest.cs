using Application.Interfaces;
using Application.Libraries;
using Application.Users;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Xunit;

namespace Base.Integration.Tests;



[Collection("Database collection")]
public abstract class BaseIntegrationTest : IDisposable
{
    protected IServiceScope _scope { get; }

    protected ApplicationDbContext Context { get; }

    protected IUnitOfWork UnitOfWork { get; }

    private bool disposed;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Guard.Against.Null(factory);

        _scope = factory.Services.CreateScope();

        Context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        UnitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

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

public class LibraryIntegrationTest : BaseIntegrationTest
{
    protected ILibraryReadService LibraryReadService { get; }

    protected IRepository<Library, Guid> LibraryRepository { get; }


    public LibraryIntegrationTest(IntegrationTestWebAppFactory factory) : base(factory)
    {
        LibraryRepository = _scope.ServiceProvider.GetRequiredService<IRepository<Library, Guid>>();

        LibraryReadService = _scope.ServiceProvider.GetRequiredService<ILibraryReadService>();

        // Clear change tracker to avoid concurrency issues
        Context.ChangeTracker.Clear();
        
        // Use ExecuteDelete for direct database cleanup without tracking
        Context.Database.ExecuteSqlRaw("DELETE FROM \"Libraries\"");
        Context.Database.ExecuteSqlRaw("DELETE FROM \"Users\"");
    }

}

public class UserIntegrationTest : BaseIntegrationTest
{
    protected IRepository<User, Guid> UserRepository { get; }

    protected IUserReadService UserReadService { get; }


    public UserIntegrationTest(IntegrationTestWebAppFactory factory) : base(factory)
    {
        UserRepository = _scope.ServiceProvider.GetRequiredService<IRepository<User, Guid>>();

        UserReadService = _scope.ServiceProvider.GetRequiredService<IUserReadService>();

        // Clear change tracker to avoid concurrency issues
        Context.ChangeTracker.Clear();
        
        // Use ExecuteDelete for direct database cleanup without tracking
        Context.Database.ExecuteSqlRaw("DELETE FROM \"Users\"");
        Context.Database.ExecuteSqlRaw("DELETE FROM \"Libraries\"");
    }

}

public class LibraryLocalStorageIntegrationTest : BaseIntegrationTest
{
    protected ILibraryLocalStorage LibraryLocalStorage { get; }


    public LibraryLocalStorageIntegrationTest(IntegrationTestWebAppFactory factory) : base(factory)
    {
        LibraryLocalStorage = _scope.ServiceProvider.GetRequiredService<ILibraryLocalStorage>();
    }

}
