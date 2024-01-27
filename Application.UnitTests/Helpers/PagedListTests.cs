using Application.Helpers;
using FluentAssertions;
using MockQueryable.NSubstitute;

namespace Application.UnitTests.Helpers;
public class PagedListTests
{
    // Arrange
    // Act
    // Assert

    // Mock IQueryable with NSubstitute
    // https://sinairv.github.io/blog/2015/10/04/mock-entity-framework-dbset-with-nsubstitute/

    private readonly List<string> list = new()
    {
        "1","2", "3", "4", "5", "6", "7", "8", "9", "10"
    };

    [Fact]
    public async Task PagedList_TotalCountAsync()
    {
        // Arrange : https://github.com/romantitov/MockQueryable
        var query = list.AsQueryable().BuildMock();       

        // Act
        var pagedList = await PagedList<string>.CreateAsync(query, 1, 2);

        // Assert
        pagedList.TotalCount.Should().Be(10);
    }

    [Fact]
    public async Task PagedList_FirstPage_HasNextPageAsync_And_HasNoPreviousPage()
    {
        // Arrange
        var query = list.AsQueryable().BuildMock();

        // Act
        var pagedList = await PagedList<string>.CreateAsync(query, 1, 2);

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
        var pagedList = await PagedList<string>.CreateAsync(query, 2, 2);

        // Assert
        pagedList.HasNextPage.Should().BeTrue();
        pagedList.HasPreviousPage.Should().BeTrue();
        pagedList.Items.Count.Should().Be(2);

    }

    [Fact]
    public async Task PagedList_FifthPage_HasNoNextPageAsync_And_HasPreviousPage()
    {
        // Arrange
        var query = list.AsQueryable().BuildMock();

        // Act
        var pagedList = await PagedList<string>.CreateAsync(query, 5, 2);

        // Assert
        pagedList.HasNextPage.Should().BeFalse();
        pagedList.HasPreviousPage.Should().BeTrue();
        pagedList.Items.Count.Should().Be(2);
    }

    [Fact]
    public async Task PagedList_SixthPage_HasNoNextPageAsync_And_HasPreviousPage()
    {
        // Arrange
        var query = list.AsQueryable().BuildMock();

        // Act
        var pagedList = await PagedList<string>.CreateAsync(query, 6, 2);

        // Assert
        pagedList.HasNextPage.Should().BeFalse();
        pagedList.HasPreviousPage.Should().BeTrue();
        pagedList.Items.Count.Should().Be(0);
    }




}
