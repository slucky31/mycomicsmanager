using Application.Abstractions.Messaging;

namespace Application.ImportJobs.MigrateLegacy;

// The number of files still in the old library, shown on the Settings page (#1292).
public record CountLegacyFilesQuery : IQuery<int>;
