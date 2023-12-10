namespace Domain.Primitives;

public sealed record TError(string Code, string? Description = null)
{
    public static readonly TError None = new(string.Empty);

    
}
