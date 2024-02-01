using Application.Helpers;
using Base.Integration.Tests;
using Domain.Libraries;


namespace Application.UnitTests.Helpers;

public class PagedListTests : BaseIntegrationTest
{
    
    public PagedListTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateAsync_Should_ReturnPagedList()
    {        
        // Arrange
        var nbItems = 50;
        int count = Context.Libraries.Count();
        for (int i = 0; i < nbItems; i++)
        {
            var lib = Library.Create("lib-"+i);
            Context.Libraries.Add(lib);            
        }
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
        
        // Act
        PagedList<Library> pagedList = await PagedList<Library>.CreateAsync((IQueryable<Library>)Context.Libraries, 1, 5);

        // Assert
        pagedList.Page.Should().Be(1);        
        pagedList.TotalCount.Should().Be(count + nbItems);
    }
}
