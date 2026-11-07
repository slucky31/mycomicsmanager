using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.ImportJobs.DownloadFile;
using Application.Users;
using AwesomeAssertions;
using Domain.ImportJobs;
using Domain.Primitives;
using Domain.Users;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using Web.EndPoints;
using Xunit;

namespace Web.Tests.EndPoints;

public sealed class ImportJobsEndpointsTests
{
    private readonly IUserReadService _userReadService = Substitute.For<IUserReadService>();
    private readonly IQueryHandler<GetFailedImportFileQuery, FailedImportFile> _handler = Substitute.For<IQueryHandler<GetFailedImportFileQuery, FailedImportFile>>();
    private readonly User _user = User.Create("reader@example.com", "auth0|42");
    private readonly ClaimsPrincipal _principal = new(new ClaimsIdentity([new Claim("sub", "auth0|42")], "test"));

    public ImportJobsEndpointsTests()
    {
        _userReadService.GetUserByAuthId("auth0|42", Arg.Any<CancellationToken>()).Returns(_user);
    }

    [Fact]
    public async Task DownloadFailedFileAsync_Should_ReturnUnauthorized_WhenUserIsUnknown()
    {
        _userReadService.GetUserByAuthId(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result<User>.Failure(UsersError.NotFound));

        var result = await ImportJobsEndpoints.DownloadFailedFileAsync(Guid.CreateVersion7(), new ClaimsPrincipal(new ClaimsIdentity()), _handler, _userReadService, TestContext.Current.CancellationToken);

        result.Should().BeOfType<UnauthorizedHttpResult>();
        await _handler.DidNotReceive().Handle(Arg.Any<GetFailedImportFileQuery>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("IMP400", typeof(BadRequest<string>))]
    [InlineData("IMP413", typeof(BadRequest<string>))]
    [InlineData("IMP404", typeof(NotFound<string>))]
    [InlineData("IMP414", typeof(NotFound<string>))]
    public async Task DownloadFailedFileAsync_Should_MapQueryErrors(string errorCode, Type expectedResult)
    {
        TError[] errors = [ImportJobError.BadRequest, ImportJobError.NotFailed, ImportJobError.NotFound, ImportJobError.FailedFileNotFound];
        _handler.Handle(Arg.Any<GetFailedImportFileQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<FailedImportFile>.Failure(errors.Single(e => e.Code == errorCode)));

        var result = await ImportJobsEndpoints.DownloadFailedFileAsync(Guid.CreateVersion7(), _principal, _handler, _userReadService, TestContext.Current.CancellationToken);

        result.Should().BeOfType(expectedResult);
    }

    [Fact]
    public async Task DownloadFailedFileAsync_Should_ServeTheFileUnderItsOriginalName()
    {
        var jobId = Guid.CreateVersion7();
        _handler.Handle(new GetFailedImportFileQuery(jobId, _user.Id), Arg.Any<CancellationToken>())
            .Returns(new FailedImportFile("/import/lib/errors/comic_20261107.cbz", "comic.cbz"));

        var result = await ImportJobsEndpoints.DownloadFailedFileAsync(jobId, _principal, _handler, _userReadService, TestContext.Current.CancellationToken);

        var file = result.Should().BeOfType<PhysicalFileHttpResult>().Subject;
        file.FileName.Should().Be("/import/lib/errors/comic_20261107.cbz");
        file.FileDownloadName.Should().Be("comic.cbz");
        file.ContentType.Should().Be(ImportJobsEndpoints.FailedFileContentType);
        file.EnableRangeProcessing.Should().BeTrue();
    }
}
