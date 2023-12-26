using Application.Helpers;
using Application.IntegrationTests;
using Domain.Dto;
using Domain.Libraries;
using FluentAssertions;

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
