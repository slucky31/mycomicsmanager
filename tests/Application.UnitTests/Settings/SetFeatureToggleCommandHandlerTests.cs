using Application.Interfaces;
using Application.Settings;
using Application.Settings.SetFeatureToggle;
using Domain.Primitives;
using Domain.Settings;
using NSubstitute;

namespace Application.UnitTests.Settings;

public class SetFeatureToggleCommandHandlerTests
{
    private readonly FeatureToggleTestData _data = new();
    private readonly IFeatureToggleOverrideRepository _repository = Substitute.For<IFeatureToggleOverrideRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FeatureToggles _toggles;
    private readonly SetFeatureToggleCommandHandler _handler;

    public SetFeatureToggleCommandHandlerTests()
    {
        var defaults = _data.CreateDefaults();
        _toggles = new FeatureToggles(defaults);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Success(1));
        _handler = new SetFeatureToggleCommandHandler(_repository, _unitOfWork, defaults, _toggles);
    }

    private Task<Result> HandleAsync(FeatureToggle toggle, bool enabled) =>
        _handler.Handle(new SetFeatureToggleCommand(toggle, enabled), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_Should_ReturnUnknown_WhenToggleIsNotDefined()
    {
        var result = await HandleAsync((FeatureToggle)99, true);

        result.Error.Should().Be(FeatureToggleError.Unknown);
        await _repository.DidNotReceiveWithAnyArgs().GetAsync(default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Should_RefuseToTurnOn_WhenConfigurationIsMissing()
    {
        _data.Miniflux.ApiKey = string.Empty;

        var result = await HandleAsync(FeatureToggle.FeedImport, true);

        result.Error!.Code.Should().Be("TOGGLE409");
        result.Error.Description.Should().Contain("Miniflux:ApiKey");
        await _repository.DidNotReceiveWithAnyArgs().GetAsync(default, TestContext.Current.CancellationToken);
        _toggles.IsEnabled(FeatureToggle.FeedImport).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_AddOverride_WhenValueDiffersFromConfiguration()
    {
        var result = await HandleAsync(FeatureToggle.FeedImport, true);

        result.IsSuccess.Should().BeTrue();
        _repository.Received(1).Add(Arg.Is<FeatureToggleOverride>(o => o.Toggle == FeatureToggle.FeedImport && o.Enabled));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _toggles.IsEnabled(FeatureToggle.FeedImport).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_UpdateOverride_WhenOneAlreadyExists()
    {
        var existing = FeatureToggleOverride.Create(FeatureToggle.IsbnOcr, true);
        _repository.GetAsync(FeatureToggle.IsbnOcr, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await HandleAsync(FeatureToggle.IsbnOcr, false);

        result.IsSuccess.Should().BeTrue();
        existing.Enabled.Should().BeFalse();
        _repository.DidNotReceiveWithAnyArgs().Add(default!);
        _toggles.IsEnabled(FeatureToggle.IsbnOcr).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_RemoveOverride_WhenValueIsBackToConfiguration()
    {
        var existing = FeatureToggleOverride.Create(FeatureToggle.IsbnOcr, false);
        _repository.GetAsync(FeatureToggle.IsbnOcr, Arg.Any<CancellationToken>()).Returns(existing);
        _toggles.Apply(FeatureToggle.IsbnOcr, false);

        var result = await HandleAsync(FeatureToggle.IsbnOcr, true);

        result.IsSuccess.Should().BeTrue();
        _repository.Received(1).Remove(existing);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _toggles.GetStates().Single(s => s.Toggle == FeatureToggle.IsbnOcr).IsOverridden.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_NotSave_WhenValueIsTheConfiguredOneAndNoOverrideExists()
    {
        var result = await HandleAsync(FeatureToggle.Bedetheque, true);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
        _toggles.IsEnabled(FeatureToggle.Bedetheque).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_KeepPreviousValue_WhenSaveFails()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(FeatureToggleError.Unknown));

        var result = await HandleAsync(FeatureToggle.Bedetheque, false);

        result.Error.Should().Be(FeatureToggleError.Unknown);
        _toggles.IsEnabled(FeatureToggle.Bedetheque).Should().BeTrue();
    }
}
