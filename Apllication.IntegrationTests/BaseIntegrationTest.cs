using Application.Data;
using Application.Interfaces;
using Ardalis.GuardClauses;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application.IntegrationTests;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>
{    
    protected readonly ISender Sender;
    protected readonly IApplicationDbContext Context;
    protected readonly IUnitOfWork UnitOfWork;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Guard.Against.Null(factory);
        var _scope = factory.Services.CreateScope();

        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        Context = _scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        UnitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    }
}
