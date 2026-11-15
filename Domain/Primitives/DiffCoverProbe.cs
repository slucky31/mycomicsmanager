namespace Domain.Primitives;

/// <summary>Temporary class to check that diff-cover fails under 80 %.</summary>
public static class DiffCoverProbe
{
    /// <summary>Never called by any test.</summary>
    public static int Twice(int value)
    {
        var result = value * 2;
        return result;
    }
}
