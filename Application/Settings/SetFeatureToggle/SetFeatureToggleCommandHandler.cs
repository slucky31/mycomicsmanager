using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Primitives;
using Domain.Settings;

namespace Application.Settings.SetFeatureToggle;

// Stores the value chosen in the Settings page. Choosing the configured value removes the override:
// the feature follows the configuration again.
public sealed class SetFeatureToggleCommandHandler(
    IFeatureToggleOverrideRepository repository,
    IUnitOfWork unitOfWork,
    FeatureToggleDefaults defaults,
    FeatureToggles featureToggles) : ICommandHandler<SetFeatureToggleCommand>
{
    public async Task<Result> Handle(SetFeatureToggleCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);
        if (!Enum.IsDefined(command.Toggle))
        {
            return FeatureToggleError.Unknown;
        }

        if (command.Enabled && defaults.GetMissingConfiguration(command.Toggle) is { } missing)
        {
            return FeatureToggleError.MissingConfiguration(missing);
        }

        var followsConfiguration = command.Enabled == defaults.IsConfiguredOn(command.Toggle);
        var existing = await repository.GetAsync(command.Toggle, cancellationToken);
        if (followsConfiguration && existing is null)
        {
            featureToggles.Apply(command.Toggle, null);
            return Result.Success();
        }

        if (followsConfiguration)
        {
            repository.Remove(existing!);
        }
        else if (existing is null)
        {
            repository.Add(FeatureToggleOverride.Create(command.Toggle, command.Enabled));
        }
        else
        {
            existing.Set(command.Enabled);
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error!;
        }

        featureToggles.Apply(command.Toggle, followsConfiguration ? null : command.Enabled);
        return Result.Success();
    }
}
