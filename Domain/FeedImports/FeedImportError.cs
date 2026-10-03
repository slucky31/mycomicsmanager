using Domain.Primitives;

namespace Domain.FeedImports;

public static class FeedImportError
{
    public static readonly TError BadRequest = new("FEED400", "Verify the feed import request parameters.");
    public static readonly TError Disabled = new("FEED403", "Feed import is disabled (FeedImport:Enabled).");
    public static readonly TError NotFound = new("FEED404", "Feed import decision not found.");
    public static readonly TError CategoryNotFound = new("FEED404C", "Miniflux category not found.");
    public static readonly TError InvalidStatusTransition = new("FEED405", "Invalid feed import decision status transition.");
    public static readonly TError SourceHostNotAllowed = new("FEED403S", "Domaine source non autorisé.");
    public static readonly TError PageUnavailable = new("FEED502P", "La page de l'article n'a pas pu être récupérée.");
    public static readonly TError Duplicate = new("FEED409", "A decision already exists for this Miniflux entry.");
    public static readonly TError MinifluxUnavailable = new("FEED502", "Miniflux could not be reached or returned an error.");
}
