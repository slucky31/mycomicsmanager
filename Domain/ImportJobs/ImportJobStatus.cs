namespace Domain.ImportJobs;

public enum ImportJobStatus
{
    Pending,           // En attente de traitement
    Extracting,        // Extraction des images de l'archive/PDF
    Converting,        // Conversion des images en WebP
    SearchingMetadata, // Recherche de metadonnees via Bedetheque + mise a jour ComicInfo.xml
    UploadingCover,    // Upload de la couverture sur Cloudinary
    BuildingArchive,   // Construction de l'archive CBZ finale (avec ComicInfo.xml mis a jour)
    Completed,         // Import termine avec succes
    Failed             // Import echoue
}
