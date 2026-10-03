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
    public static readonly TError DebridLinkUnavailable = new("FEED502D", "Debrid-Link n'a pas pu être joint ou a renvoyé une erreur.");
    public static readonly TError DebridLinkUnauthorized = new("FEED401D", "Clé d'API Debrid-Link absente ou invalide (DebridLink:ApiKey).");
    public static readonly TError DebridLinkQuotaReached = new("FEED429D", "Quota Debrid-Link atteint.");
    public static readonly TError DebridLinkFileUnavailable = new("FEED410D", "Fichier indisponible chez l'hébergeur (lien mort ou supprimé).");
    public static readonly TError DebridLinkHostQuotaReached = new("FEED429H", "Quota Debrid-Link atteint pour cet hébergeur.");
    public static readonly TError DebridLinkHostNotSupported = new("FEED422D", "Hébergeur non pris en charge par Debrid-Link.");
    public static readonly TError DownloadHostNotAllowed = new("FEED403H", "Domaine de téléchargement non autorisé.");
    public static readonly TError DownloadFailed = new("FEED502F", "Le téléchargement du fichier a échoué.");
    public static readonly TError FileTooLarge = new("FEED413", "Fichier trop volumineux (Import:MaxFileSizeMb).");
    public static readonly TError UnsupportedFileType = new("FEED415", "Type de fichier non pris en charge (Import:SupportedExtensions).");
    public static readonly TError TargetLibraryInvalid = new("FEED409L", "La bibliothèque cible existe mais n'est pas une bibliothèque numérique.");
    public static readonly TError MinifluxUnavailable = new("FEED502", "Miniflux could not be reached or returned an error.");
}
