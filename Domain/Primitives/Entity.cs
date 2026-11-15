
using Ardalis.GuardClauses;
using Domain.Interfaces;

namespace Domain.Primitives;

public abstract class Entity<TEntityId> : IAuditable
{
    public TEntityId? Id { get; protected set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? ModifiedOnUtc { get; set; }

    public void CloneAuditable(IAuditable auditable)
    {
        Guard.Against.Null(auditable);
        CreatedOnUtc = auditable.CreatedOnUtc;
        ModifiedOnUtc = auditable.ModifiedOnUtc;
    }


}

internal static class TmpWarn { internal static int M() { var unused = 1; return 0; } }
