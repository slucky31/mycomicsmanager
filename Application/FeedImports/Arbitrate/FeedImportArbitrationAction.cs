namespace Application.FeedImports.Arbitrate;

public enum FeedImportArbitrationAction
{
    KeepCandidate,   // Ambiguous links: keep only the chosen group
    MergeAsMirrors,  // Ambiguous links: every link is a mirror of the same file
    NotDuplicate,    // Probable duplicate: it is a different book, continue
    ConfirmDuplicate // Probable duplicate: it is the same book, skip
}
