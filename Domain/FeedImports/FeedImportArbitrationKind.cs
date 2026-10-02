namespace Domain.FeedImports;

// ponytail: persisted as int, append new values at the end and never reorder
public enum FeedImportArbitrationKind
{
    None,
    AmbiguousLinks,   // Links could not be grouped into mirrors / books with certainty
    ProbableDuplicate // A close but not certain match was found in a library
}
