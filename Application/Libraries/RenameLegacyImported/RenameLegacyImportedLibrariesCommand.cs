using Application.Abstractions.Messaging;

namespace Application.Libraries.RenameLegacyImported;

// Run at startup: the library of the feed imports was called "À trier" before it became "Imported" (#1109).
public record RenameLegacyImportedLibrariesCommand : ICommand<int>;
