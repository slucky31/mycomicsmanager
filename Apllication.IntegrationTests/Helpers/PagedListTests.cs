using Application.Helpers;
using Base.Integration.Tests;
using Domain.Dto;
using Domain.Libraries;


namespace Application.UnitTests.Helpers;

public class PagedListTests : BaseIntegrationTest
{
    
    public PagedListTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        Context.Libraries.RemoveRange(Context.Libraries);
    }

    [Fact]
    public async Task CreateAsync_Should_ReturnPagedList()
    {        
        // Arrange
        var nbItems = 50;        
        for (int i = 0; i < nbItems; i++)
        {
            var lib = LibraryDto.Create(Library.Create("lib-"+i));
            Context.Libraries.Add(lib);            
        }
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
        
        // Act
        PagedList<LibraryDto> pagedList = await PagedList<LibraryDto>.CreateAsync((IQueryable<LibraryDto>)Context.Libraries, 1, 5);

        // Assert
        pagedList.Page.Should().Be(1);
        pagedList.TotalCount.Should().Be(50);
    }
}
