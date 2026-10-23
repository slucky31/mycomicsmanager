using Application.Interfaces;
using Application.Series;
using Application.Series.SetTotal;
using Domain.Libraries;
using Domain.Primitives;
using Domain.Series;
using NSubstitute;

namespace Application.UnitTests.Series;

public class SetSerieTotalCommandHandlerTests
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    private readonly ISeriesInfoRepository _repository = Substitute.For<ISeriesInfoRepository>();
    private readonly IRepository<Library, Guid> _libraryRepository = Substitute.For<IRepository<Library, Guid>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SetSerieTotalCommandHandler _handler;
    private readonly Library _library = Library.Create("BD", "#594AE2", "Book", LibraryBookType.Physical, s_userId).Value!;

    public SetSerieTotalCommandHandlerTests()
    {
        _handler = new SetSerieTotalCommandHandler(_repository, _libraryRepository, _unitOfWork);
        _libraryRepository.GetByIdAsync(_library.Id).Returns(_library);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
    }

    private Task<Result> HandleAsync(string name, int? total, Guid? userId = null) =>
        _handler.Handle(new SetSerieTotalCommand(userId ?? s_userId, _library.Id, name, total, IsFinished: true), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenTheNameIsBlank()
    {
        var result = await HandleAsync(" ", 3);

        result.Error.Should().Be(SeriesError.BadRequest);
        await _libraryRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default);
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidTotalVolumes_WhenTheTotalIsBelowOne()
    {
        var result = await HandleAsync("Thorgal", 0);

        result.Error.Should().Be(SeriesError.InvalidTotalVolumes);
        await _libraryRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTheLibraryIsAnotherUsers()
    {
        var result = await HandleAsync("Thorgal", 3, Guid.CreateVersion7());

        result.Error.Should().Be(LibrariesError.NotFound);
        await _repository.DidNotReceiveWithAnyArgs().GetAsync(default, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_AddTheSeriesInfo_WhenTheSeriesHasNone()
    {
        var result = await HandleAsync(" Thorgal ", 41);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).GetAsync(_library.Id, "THORGAL", Arg.Any<CancellationToken>());
        _repository.Received(1).Add(Arg.Is<SeriesInfo>(s => s.Name == "Thorgal" && s.TotalVolumes == 41 && s.IsFinished));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_UpdateTheSeriesInfo_WhenTheSeriesHasOne()
    {
        var existing = SeriesInfo.Create(_library.Id, "Thorgal", 40, isFinished: false).Value!;
        _repository.GetAsync(_library.Id, "THORGAL", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await HandleAsync("thorgal", null);

        result.IsSuccess.Should().BeTrue();
        existing.TotalVolumes.Should().BeNull();
        existing.IsFinished.Should().BeTrue();
        _repository.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheSaveError_WhenSavingFails()
    {
        var error = new TError("DB500", "Database down");
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(error));

        var result = await HandleAsync("Thorgal", 3);

        result.Error.Should().Be(error);
    }
}
