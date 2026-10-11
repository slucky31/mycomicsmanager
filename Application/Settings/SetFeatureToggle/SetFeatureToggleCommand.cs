using Application.Abstractions.Messaging;
using Domain.Settings;

namespace Application.Settings.SetFeatureToggle;

public record SetFeatureToggleCommand(FeatureToggle Toggle, bool Enabled) : ICommand;
