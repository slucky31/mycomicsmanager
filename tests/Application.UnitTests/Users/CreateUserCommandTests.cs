using Application.Users.Create;
using Application.Users;
using Domain.Users;
using NSubstitute;
using Application.Interfaces;
using MongoDB.Bson;
using Application.Libraries.Create;
using Ardalis.GuardClauses;
using Domain.Libraries;
using MockQueryable.NSubstitute;
using Persistence.Queries.Helpers;
using Domain.Primitives;

namespace Application.UnitTests.Users;

public class CreateUserCommandHandlerTests
{
    private static readonly CreateUserCommand Command = new("test@test.com", "1234");

    private readonly IRepository<User, ObjectId> _userRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IUserReadService _userReadServiceMock;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _userRepositoryMock = Substitute.For<IRepository<User, ObjectId>>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _userReadServiceMock = Substitute.For<IUserReadService>();

        _handler = new CreateUserCommandHandler(_userRepositoryMock, _unitOfWorkMock, _userReadServiceMock);
    }

    [Fact]
    public async Task Handle_Should_ExecuteSaveChangeAsyncOnce()
    {
        // Arrange
        _userRepositoryMock.Add(Arg.Any<User>());

        // Act
        await _handler.Handle(Command, default);

        // Assert
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnBadREquest_WhenCommandEmailIsEmpty()
    {
        // Arrange
        CreateUserCommand commandWithEmptyEmail = new("", "1234");

        // Act
        var result = await _handler.Handle(commandWithEmptyEmail, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.BadRequest);
    }

    [Fact]
    public async Task Handle_ShouldReturnBadREquest_WhenCommandAuthIdIsEmpty()
    {
        // Arrange
        CreateUserCommand commandWithEmptyAuthId = new("test@test.com", "");

        // Act
        var result = await _handler.Handle(commandWithEmptyAuthId, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.BadRequest);
    }

    [Fact]
    public async Task Handle_ShouldReturnDuplicate_WhenAUserWithSameEmailOrAuthIdAlreadyExist()
    {
        // Arrange
        User user = User.Create(Command.email, Command.authId);        
        _userReadServiceMock.GetUserByAuthIdAndEmail(Command.email, Command.authId).Returns(user);            

        // Act
        var result = await _handler.Handle(Command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.Duplicate);
    }

}

