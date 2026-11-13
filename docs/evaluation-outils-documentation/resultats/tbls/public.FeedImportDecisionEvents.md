# public.FeedImportDecisionEvents

## Description

Historique des changements d'état d'une décision d'import de flux.

## Columns

| Name                 | Type                     | Default | Nullable | Children | Parents                                                     | Comment                                                                                                                                                                                                                                                   |
| -------------------- | ------------------------ | ------- | -------- | -------- | ----------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Id                   | uuid                     |         | false    |          |                                                             |                                                                                                                                                                                                                                                           |
| FeedImportDecisionId | uuid                     |         | false    |          | [public.FeedImportDecisions](public.FeedImportDecisions.md) | Décision concernée.                                                                                                                                                                                                                                       |
| OccurredAt           | timestamp with time zone |         | false    |          |                                                             | Date de l'événement.                                                                                                                                                                                                                                      |
| PreviousStatus       | integer                  |         | true     |          |                                                             | État précédent (mêmes valeurs que FeedImportDecisions.Status) ; vide pour l'événement de création.                                                                                                                                                        |
| Status               | integer                  |         | false    |          |                                                             | Nouvel état (mêmes valeurs que FeedImportDecisions.Status : 0 = En attente d'analyse, 1 = Liens extraits, 2 = En attente d'arbitrage, 3 = Doublon ignoré, 4 = Téléchargement en cours, 5 = Téléchargé, 6 = Importé (non utilisé), 7 = Ignoré, 8 = Échec). |
| DecidedBy            | integer                  |         | false    |          |                                                             | Auteur de l'événement : 0 = Automatique, 1 = Utilisateur.                                                                                                                                                                                                 |
| Description          | varchar(1000)            |         | false    |          |                                                             | Raison ou description de l'événement.                                                                                                                                                                                                                     |
| CreatedOnUtc         | timestamp with time zone |         | false    |          |                                                             |                                                                                                                                                                                                                                                           |
| ModifiedOnUtc        | timestamp with time zone |         | true     |          |                                                             |                                                                                                                                                                                                                                                           |

## Constraints

| Name                                                            | Type        | Definition                                                                                    |
| --------------------------------------------------------------- | ----------- | --------------------------------------------------------------------------------------------- |
| FK_FeedImportDecisionEvents_FeedImportDecisions_FeedImportDeci~ | FOREIGN KEY | FOREIGN KEY ("FeedImportDecisionId") REFERENCES "FeedImportDecisions"("Id") ON DELETE CASCADE |
| PK_FeedImportDecisionEvents                                     | PRIMARY KEY | PRIMARY KEY ("Id")                                                                            |

## Indexes

| Name                                             | Definition                                                                                                                                |
| ------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------- |
| PK_FeedImportDecisionEvents                      | CREATE UNIQUE INDEX "PK_FeedImportDecisionEvents" ON public."FeedImportDecisionEvents" USING btree ("Id")                                 |
| IX_FeedImportDecisionEvents_FeedImportDecisionId | CREATE INDEX "IX_FeedImportDecisionEvents_FeedImportDecisionId" ON public."FeedImportDecisionEvents" USING btree ("FeedImportDecisionId") |

## Relations

```mermaid
erDiagram

"public.FeedImportDecisionEvents" }o--|| "public.FeedImportDecisions" : "FOREIGN KEY (#quot;FeedImportDecisionId#quot;) REFERENCES #quot;FeedImportDecisions#quot;(#quot;Id#quot;) ON DELETE CASCADE"

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
```

---

> Generated by [tbls](https://github.com/k1LoW/tbls)
