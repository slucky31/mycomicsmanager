using Application.Data;
using Application.Interfaces;
using Ardalis.GuardClauses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Xunit;

namespace Base.Integration.Tests;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IDisposable
{
    private readonly IServiceScope _scope;
    protected ISender Sender { get; }
    protected ApplicationDbContext Context { get; }
    protected IUnitOfWork UnitOfWork { get; }
    private bool disposed;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Guard.Against.Null(factory);
        
        _scope = factory.Services.CreateScope();
        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        Context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        UnitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Clear all MongoDb Collections berfore tests
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
