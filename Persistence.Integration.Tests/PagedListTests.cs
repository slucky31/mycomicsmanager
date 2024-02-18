using Base.Integration.Tests;
using Domain.Libraries;
using Persistence.Queries.Helpers;
using MockQueryable.NSubstitute;
using Ardalis.GuardClauses;

namespace Persistence.Integration.Tests;

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
        var pagedList = new PagedList<Library>((IQueryable<Library>)Context.Libraries);
        await pagedList.ExecuteQueryAsync(1, 5);

        // Assert
        pagedList.Page.Should().Be(1);        
        pagedList.TotalCount.Should().Be(count + nbItems);
    }

    // Arrange
    // Act
    // Assert

    // Mock IQueryable with NSubstitute
    // https://sinairv.github.io/blog/2015/10/04/mock-entity-framework-dbset-with-nsubstitute/

    private readonly List<string> list = ["1","2", "3", "4", "5", "6", "7", "8", "9", "10"];

    [Fact]
    public async Task PagedList_TotalCountAsync()
    {
        // Arrange : https://github.com/romantitov/MockQueryable
        var query = list.AsQueryable().BuildMock();

        // Act
        var pagedList = new PagedList<string>(query);
        await pagedList.ExecuteQueryAsync(1, 2);

        // Assert
        pagedList.TotalCount.Should().Be(10);
    }

    [Fact]
    public async Task PagedList_FirstPage_HasNextPageAsync_And_HasNoPreviousPage()
    {
        // Arrange
        var query = list.AsQueryable().BuildMock();

        // Act
        var pagedList = new PagedList<string>(query);
        await pagedList.ExecuteQueryAsync(1, 2);        

        // Assert
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeFalse();

    }

    [Fact]
    public async Task PagedList_SecondPage_HasNextPageAsync_And_HasPreviousPage()
    {
        // Arrange
        var query = list.AsQueryable().BuildMock();

        // Act
        var pagedList = new PagedList<string>(query);
        await pagedList.ExecuteQueryAsync(2, 2);        

        // Assert
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeTrue();
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Count.Should().Be(2);

    }

    [Fact]
    public async Task PagedList_FifthPage_HasNoNextPageAsync_And_HasPreviousPage()
    {
        // Arrange
        var query = list.AsQueryable().BuildMock();

        // Act
        var pagedList = new PagedList<string>(query);
        await pagedList.ExecuteQueryAsync(5, 2);        

        // Assert
        pagedList.HasNextPage.Should().BeFalse();
        pagedList.HasPreviousPage.Should().BeTrue();
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Count.Should().Be(2);
    }

    [Fact]
    public async Task PagedList_SixthPage_HasNoNextPageAsync_And_HasPreviousPage()
    {
        // Arrange
        var query = list.AsQueryable().BuildMock();

        // Act
        var pagedList = new PagedList<string>(query);
        await pagedList.ExecuteQueryAsync(6, 2);

        // Assert
        pagedList.HasNextPage.Should().BeFalse();
        pagedList.HasPreviousPage.Should().BeTrue();
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Count.Should().Be(0);
    }
}
