using Application.Abstractions.Messaging;

namespace Application.Series.SetTotal;

// The number of volumes published of a series and whether it is finished (#1205); no number: unknown.
public sealed record SetSerieTotalCommand(Guid UserId, Guid LibraryId, string Name, int? TotalVolumes, bool IsFinished) : ICommand;
