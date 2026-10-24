using Application.Interfaces;
using Application.Users;
using Application.Users.HiddenLibraries;
using Domain.Primitives;
using Domain.Users;
using NSubstitute;

namespace Application.UnitTests.Users;

public class ShowHiddenLibrariesTests
{
    private static readonly TError s_saveError = new("DB500", "Save failed");

    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GetShowHiddenLibrariesQueryHandler _getHandler;
    private readonly SetShowHiddenLibrariesCommandHandler _setHandler;

    public ShowHiddenLibrariesTests()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _getHandler = new GetShowHiddenLibrariesQueryHandler(_userRepository);
        _setHandler = new SetShowHiddenLibrariesCommandHandler(_userRepository, _unitOfWork);
    }

    private User GivenUser(bool showHiddenLibraries)
    {
        var user = User.Create("user@example.com", "auth-id");
        user.UpdateShowHiddenLibraries(showHiddenLibraries);
        _userRepository.GetByIdAsync(user.Id).Returns(user);
        return user;
    }

    [Fact]
    public async Task ShowsHiddenLibrariesAsync_Should_FollowTheSettingOfTheUser_AndBeFalseForAnUnknownUser()
    {
        var user = GivenUser(showHiddenLibraries: true);

        (await _userRepository.ShowsHiddenLibrariesAsync(user.Id)).Should().BeTrue();
        (await _userRepository.ShowsHiddenLibrariesAsync(Guid.CreateVersion7())).Should().BeFalse();
    }

    [Fact]
    public async Task ShowsHiddenLibrariesAsync_Should_Throw_WhenTheRepositoryIsNull()
    {
        var act = () => ((IRepository<User, Guid>)null!).ShowsHiddenLibrariesAsync(Guid.CreateVersion7());

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetShowHiddenLibraries_Should_ReturnTheSettingOfTheUser()
    {
        var user = GivenUser(showHiddenLibraries: true);

        var result = await _getHandler.Handle(new GetShowHiddenLibrariesQuery(user.Id), TestContext.Current.CancellationToken);

        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task GetShowHiddenLibraries_Should_ReturnBadRequest_WhenTheUserIsEmpty()
    {
        var result = await _getHandler.Handle(new GetShowHiddenLibrariesQuery(Guid.Empty), TestContext.Current.CancellationToken);

        result.Error.Should().Be(UsersError.BadRequest);
        await _userRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task GetShowHiddenLibraries_Should_ReturnNotFound_WhenTheUserIsUnknown()
    {
        var result = await _getHandler.Handle(new GetShowHiddenLibrariesQuery(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Should().Be(UsersError.NotFound);
    }

    [Fact]
    public async Task SetShowHiddenLibraries_Should_SaveTheSettingOfTheUser()
    {
        var user = GivenUser(showHiddenLibraries: false);

        var result = await _setHandler.Handle(new SetShowHiddenLibrariesCommand(user.Id, true), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        user.ShowHiddenLibraries.Should().BeTrue();
        _userRepository.Received(1).Update(user);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetShowHiddenLibraries_Should_ReturnTheError_WhenTheSettingCannotBeSaved()
    {
        var user = GivenUser(showHiddenLibraries: false);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(s_saveError));

        var result = await _setHandler.Handle(new SetShowHiddenLibrariesCommand(user.Id, true), TestContext.Current.CancellationToken);

        result.Error.Should().Be(s_saveError);
    }

    [Fact]
    public async Task SetShowHiddenLibraries_Should_ReturnBadRequest_WhenTheRequestIsNullOrTheUserEmpty()
    {
        var nullRequest = await _setHandler.Handle(null!, TestContext.Current.CancellationToken);
        var emptyUser = await _setHandler.Handle(new SetShowHiddenLibrariesCommand(Guid.Empty, true), TestContext.Current.CancellationToken);

        nullRequest.Error.Should().Be(UsersError.BadRequest);
        emptyUser.Error.Should().Be(UsersError.BadRequest);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetShowHiddenLibraries_Should_ReturnNotFound_WhenTheUserIsUnknown()
    {
        var result = await _setHandler.Handle(new SetShowHiddenLibrariesCommand(Guid.CreateVersion7(), true), TestContext.Current.CancellationToken);

        result.Error.Should().Be(UsersError.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
