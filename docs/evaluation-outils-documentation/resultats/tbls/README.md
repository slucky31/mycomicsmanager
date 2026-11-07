# mycomics

## Tables

| Name                                                                  | Columns | Comment                                                                                                                                                                                                | Type       |
| --------------------------------------------------------------------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------- |
| [public.Libraries](public.Libraries.md)                               | 9       | Bibliothèque d'un utilisateur, qui regroupe uniquement des BD physiques ou uniquement des BD numériques.                                                                                               | BASE TABLE |
| [public.Users](public.Users.md)                                       | 5       | Utilisateur de l'application.                                                                                                                                                                          | BASE TABLE |
| [public.Books](public.Books.md)                                       | 13      | Données communes à tous les livres (BD). Chaque ligne est spécialisée par une ligne de PhysicalBooks ou de DigitalBooks de même Id.                                                                    | BASE TABLE |
| [public.ReadingDates](public.ReadingDates.md)                         | 6       | Lecture d'un livre par l'utilisateur, avec la note donnée à cette occasion.                                                                                                                            | BASE TABLE |
| [public.DigitalBooks](public.DigitalBooks.md)                         | 9       | Spécialisation d'un livre numérique (fichier dans une bibliothèque numérique) ; même Id que la ligne de Books.                                                                                         | BASE TABLE |
| [public.PhysicalBooks](public.PhysicalBooks.md)                       | 1       | Spécialisation d'un livre physique (possédé sur papier) ; même Id que la ligne de Books, sans colonne propre.                                                                                          | BASE TABLE |
| [public.IsbnBedethequeUrls](public.IsbnBedethequeUrls.md)             | 5       | Cache de l'URL de la fiche Bedetheque associée à un ISBN.                                                                                                                                              | BASE TABLE |
| [public.ImportJobs](public.ImportJobs.md)                             | 15      | Traitement d'import d'un fichier de BD numérique déposé, suivi étape par étape jusqu'à la création du livre numérique.                                                                                 | BASE TABLE |
| [public.FeedImportDecisions](public.FeedImportDecisions.md)           | 24      | Décision d'import automatique issue d'un article d'un flux Miniflux (un article peut contenir plusieurs livres, donc plusieurs décisions) ; suit l'analyse, l'arbitrage éventuel et le téléchargement. | BASE TABLE |
| [public.FeedImportDecisionEvents](public.FeedImportDecisionEvents.md) | 9       | Historique des changements d'état d'une décision d'import de flux.                                                                                                                                     | BASE TABLE |
| [public.FeatureToggleOverrides](public.FeatureToggleOverrides.md)     | 5       | Valeurs d'activation de fonctionnalités choisies dans la page Paramètres ; elles remplacent la valeur de la configuration (appsettings / variables d'environnement) tant qu'elles existent.            | BASE TABLE |

## Relations

```mermaid
erDiagram

"public.Books" }o--|| "public.Libraries" : "FOREIGN KEY (#quot;LibraryId#quot;) REFERENCES #quot;Libraries#quot;(#quot;Id#quot;) ON DELETE CASCADE"
"public.ReadingDates" }o--|| "public.Books" : "FOREIGN KEY (#quot;BookId#quot;) REFERENCES #quot;Books#quot;(#quot;Id#quot;) ON DELETE CASCADE"
"public.DigitalBooks" |o--|| "public.Books" : "FOREIGN KEY (#quot;Id#quot;) REFERENCES #quot;Books#quot;(#quot;Id#quot;) ON DELETE CASCADE"
"public.PhysicalBooks" |o--|| "public.Books" : "FOREIGN KEY (#quot;Id#quot;) REFERENCES #quot;Books#quot;(#quot;Id#quot;) ON DELETE CASCADE"
"public.FeedImportDecisionEvents" }o--|| "public.FeedImportDecisions" : "FOREIGN KEY (#quot;FeedImportDecisionId#quot;) REFERENCES #quot;FeedImportDecisions#quot;(#quot;Id#quot;) ON DELETE CASCADE"

"public.Libraries" {
  uuid Id
  varchar_200_ Name
  timestamp_with_time_zone CreatedOnUtc
  timestamp_with_time_zone ModifiedOnUtc
  integer BookType
  varchar_7_ Color
  varchar_100_ Icon
  uuid UserId
  integer DefaultBookSortOrder
}
"public.Users" {
  uuid Id
  text Email
  text AuthId
  timestamp_with_time_zone CreatedOnUtc
  timestamp_with_time_zone ModifiedOnUtc
}
"public.Books" {
  uuid Id
  varchar_200_ Serie
  varchar_200_ Title
  varchar_20_ ISBN
  integer VolumeNumber
  varchar_500_ ImageLink
  timestamp_with_time_zone CreatedOnUtc
  timestamp_with_time_zone ModifiedOnUtc
  varchar_200_ Authors
  integer NumberOfPages
  date PublishDate
  varchar_200_ Publishers
  uuid LibraryId FK
}
"public.ReadingDates" {
  uuid Id
  timestamp_with_time_zone Date
  uuid BookId FK
  timestamp_with_time_zone CreatedOnUtc
  timestamp_with_time_zone ModifiedOnUtc
  integer Rating
}
"public.DigitalBooks" {
  uuid Id FK
  varchar_500_ FilePath
  bigint FileSize
  integer LastReadPage
  text__ IsbnCandidates
  timestamp_with_time_zone IsbnScannedAt
  integer ReaderLayout
  integer ReaderScrolling
  integer ReadingDirection
}
"public.PhysicalBooks" {
  uuid Id FK
}
"public.IsbnBedethequeUrls" {
  uuid Id
  varchar_20_ ISBN
  varchar_500_ Url
  timestamp_with_time_zone CreatedOnUtc
  timestamp_with_time_zone ModifiedOnUtc
}
"public.ImportJobs" {
  uuid Id
  varchar_260_ OriginalFileName
  varchar_500_ OriginalFilePath
  bigint OriginalFileSize
  uuid LibraryId
  uuid DigitalBookId
  integer Status
  timestamp_with_time_zone CreatedAt
  timestamp_with_time_zone CompletedAt
  varchar_2000_ ErrorMessage
  varchar_100_ ErrorStep
  timestamp_with_time_zone CreatedOnUtc
  timestamp_with_time_zone ModifiedOnUtc
  integer ConvertedImagesCount
  integer TotalImagesToConvert
}
"public.FeedImportDecisions" {
  uuid Id
  uuid UserId
  bigint MinifluxEntryId
  varchar_500_ EntryTitle
  varchar_2000_ EntryUrl
  timestamp_with_time_zone PublishedAt
  varchar_200_ ParsedSerie
  varchar_300_ ParsedTitle
  integer ParsedVolume
  jsonb Links
  varchar_2000_ ChosenMirror
  uuid MatchedBookId
  uuid ImportJobId
  integer Status
  varchar_1000_ Reason
  integer DecidedBy
  varchar_2000_ ErrorMessage
  varchar_100_ ErrorStep
  timestamp_with_time_zone CreatedAt
  timestamp_with_time_zone UpdatedAt
  timestamp_with_time_zone CreatedOnUtc
  timestamp_with_time_zone ModifiedOnUtc
  integer ArbitrationKind
  integer ItemIndex
}
"public.FeedImportDecisionEvents" {
  uuid Id
  uuid FeedImportDecisionId FK
  timestamp_with_time_zone OccurredAt
  integer PreviousStatus
  integer Status
  integer DecidedBy
  varchar_1000_ Description
  timestamp_with_time_zone CreatedOnUtc
  timestamp_with_time_zone ModifiedOnUtc
}
"public.FeatureToggleOverrides" {
  uuid Id
  varchar_64_ Toggle
  boolean Enabled
  timestamp_with_time_zone CreatedOnUtc
  timestamp_with_time_zone ModifiedOnUtc
}
```

---

> Generated by [tbls](https://github.com/k1LoW/tbls)
