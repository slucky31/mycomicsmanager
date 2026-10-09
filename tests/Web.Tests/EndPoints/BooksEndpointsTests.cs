using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Books.Read;
using Application.Interfaces;
using Application.Users;
using AwesomeAssertions;
using Domain.Books;
using Domain.Primitives;
using Domain.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Web.EndPoints;
using Xunit;

namespace Web.Tests.EndPoints;

public sealed class BooksEndpointsTests
{
    private readonly IUserReadService _userReadService = Substitute.For<IUserReadService>();
    private readonly IQueryHandler<GetBookPageQuery, ComicPage> _pageHandler = Substitute.For<IQueryHandler<GetBookPageQuery, ComicPage>>();
    private readonly User _user = User.Create("reader@example.com", "auth0|42");

    public BooksEndpointsTests()
    {
        _userReadService.GetUserByAuthId(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result<User>.Failure(UsersError.NotFound));
        _userReadService.GetUserByEmail(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result<User>.Failure(UsersError.NotFound));
    }

    private static ClaimsPrincipal Principal(string? sub, string? email)
    {
        var claims = new List<Claim>();
        if (sub is not null)
        {
            claims.Add(new Claim("sub", sub));
        }
        if (email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Name, email));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public async Task ResolveUserIdAsync_Should_ReturnUserFoundBySub()
    {
        _userReadService.GetUserByAuthId("auth0|42", Arg.Any<CancellationToken>()).Returns(_user);

        var userId = await BooksEndpoints.ResolveUserIdAsync(Principal("auth0|42", null), _userReadService, TestContext.Current.CancellationToken);

        userId.Should().Be(_user.Id);
    }

    [Fact]
    public async Task ResolveUserIdAsync_Should_FallBackToEmail_WhenSubIsUnknown()
    {
        _userReadService.GetUserByEmail("reader@example.com", Arg.Any<CancellationToken>()).Returns(_user);

        var userId = await BooksEndpoints.ResolveUserIdAsync(Principal("auth0|unknown", "reader@example.com"), _userReadService, TestContext.Current.CancellationToken);

        userId.Should().Be(_user.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown@example.com")]
    public async Task ResolveUserIdAsync_Should_ReturnNull_WhenUserIsUnknown(string? email)
    {
        var userId = await BooksEndpoints.ResolveUserIdAsync(Principal(null, email), _userReadService, TestContext.Current.CancellationToken);

        userId.Should().BeNull();
    }

    [Fact]
    public async Task GetBookPageAsync_Should_ReturnUnauthorized_WhenUserIsUnknown()
    {
        var result = await BooksEndpoints.GetBookPageAsync(Guid.CreateVersion7(), 0, Principal(null, null), new DefaultHttpContext(), _pageHandler, _userReadService, TestContext.Current.CancellationToken);

        result.Should().BeOfType<UnauthorizedHttpResult>();
        await _pageHandler.DidNotReceive().Handle(Arg.Any<GetBookPageQuery>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("BOK400", typeof(BadRequest))]
    [InlineData("BOK414", typeof(NotFound))]
    public async Task GetBookPageAsync_Should_MapQueryErrors(string errorCode, Type expectedResult)
    {
        _userReadService.GetUserByAuthId("auth0|42", Arg.Any<CancellationToken>()).Returns(_user);
        var error = errorCode == BooksError.BadRequest.Code ? BooksError.BadRequest : BooksError.PageNotFound;
        _pageHandler.Handle(Arg.Any<GetBookPageQuery>(), Arg.Any<CancellationToken>()).Returns(Result<ComicPage>.Failure(error));

        var result = await BooksEndpoints.GetBookPageAsync(Guid.CreateVersion7(), 3, Principal("auth0|42", null), new DefaultHttpContext(), _pageHandler, _userReadService, TestContext.Current.CancellationToken);

        result.Should().BeOfType(expectedResult);
    }

    [Fact]
    public async Task GetBookPageAsync_Should_ReturnCacheablePageForCurrentUser()
    {
        var bookId = Guid.CreateVersion7();
        var lastModified = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        _userReadService.GetUserByAuthId("auth0|42", Arg.Any<CancellationToken>()).Returns(_user);
        _pageHandler.Handle(new GetBookPageQuery(bookId, _user.Id, 3), Arg.Any<CancellationToken>())
            .Returns(new ComicPage(new byte[] { 1, 2, 3 }, "image/webp", lastModified));
        var httpContext = new DefaultHttpContext();

        var result = await BooksEndpoints.GetBookPageAsync(bookId, 3, Principal("auth0|42", null), httpContext, _pageHandler, _userReadService, TestContext.Current.CancellationToken);

        var file = result.Should().BeOfType<FileContentHttpResult>().Subject;
        file.ContentType.Should().Be("image/webp");
        file.FileContents.ToArray().Should().Equal(1, 2, 3);
        file.LastModified.Should().Be(new DateTimeOffset(lastModified));
        file.EntityTag!.Tag.Value.Should().Be($"\"{lastModified.Ticks:x}-3\"");
        httpContext.Response.Headers.CacheControl.ToString().Should().Be("private, max-age=86400");
    }
}
