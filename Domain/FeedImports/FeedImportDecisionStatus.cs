namespace Domain.FeedImports;

// ponytail: persisted as int, append new values at the end and never reorder
public enum FeedImportDecisionStatus
{
    Pending,             // Article enregistré depuis Miniflux, en attente d'analyse
    LinksExtracted,      // Liens de téléchargement extraits et regroupés
    AwaitingArbitration, // Cas ambigu (regroupement, doublon probable) à arbitrer
    SkippedDuplicate,    // BD déjà présente dans une bibliothèque
    Downloading,         // Téléchargement en cours via Debrid-Link
    Downloaded,          // Fichier déposé dans la bibliothèque « À trier »
    Imported,            // Import terminé (ImportJob Completed)
    Ignored,             // Ignoré par l'utilisateur
    Failed               // Échec (voir ErrorStep / ErrorMessage)
}
