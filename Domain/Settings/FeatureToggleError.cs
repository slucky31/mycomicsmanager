using Domain.Primitives;

namespace Domain.Settings;

public static class FeatureToggleError
{
    public static readonly TError Unknown = new("TOGGLE404", "Unknown feature.");

    public static TError MissingConfiguration(string detail) => new("TOGGLE409", $"The feature cannot be turned on: {detail}");
}
