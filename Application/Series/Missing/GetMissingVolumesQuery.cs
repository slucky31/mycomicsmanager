using Application.Abstractions.Messaging;

namespace Application.Series.Missing;

public sealed record GetMissingVolumesQuery(Guid UserId) : IQuery<IReadOnlyList<LibraryMissingVolumes>>;
