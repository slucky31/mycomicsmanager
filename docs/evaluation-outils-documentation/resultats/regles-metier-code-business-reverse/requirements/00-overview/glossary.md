# Glossaire

Termes métier utilisés dans tous les énoncés d'exigences. Un énoncé doit employer la colonne **Terme**, jamais les noms du code.
Les définitions marquées **À confirmer** attendent la validation du produit.

| Terme | Définition | Noms code / base (alias) | Source | Confirmé par le produit |
|---|---|---|---|---|
| Livre | Album de bande dessinée du catalogue, physique ou numérique | `Book` | `Domain/Books/Book.cs` | Non |
| Livre physique | Exemplaire papier possédé par l'utilisateur. À confirmer | `PhysicalBook` | `Domain/Books/PhysicalBook.cs` | Non |
| Livre numérique | Livre disposant d'un fichier d'archive lisible en ligne. À confirmer | `DigitalBook` | `Domain/Books/DigitalBook.cs` | Non |
| Série | Ensemble de livres partageant un même titre de série. À confirmer | `Serie` | `Application/Books/Create/` (commande de création) | Non |
| ISBN | Identifiant international d'un livre, utilisé pour la recherche et le scan. À confirmer | `ISBN`, `Isbn` | `Domain/Books/Book.cs` | Non |
| Métadonnées du livre | Informations descriptives du livre (titre, auteurs, éditeur…). À confirmer | `BookMetadata` | `Domain/Books/BookMetadata.cs` | Non |
| Date de lecture | Date à laquelle l'utilisateur a lu un livre ; un livre peut en avoir plusieurs | `ReadingDate` | `Domain/Books/ReadingDate.cs` | Non |
| Progression de lecture | Position atteinte dans un livre numérique. À confirmer | `ReadingProgress` | `Application/Books/ReadingProgress/` | Non |
| Réglages du lecteur | Préférences d'affichage de la lecture en ligne. À confirmer | `ReaderSettings` | `Domain/Books/ReaderSettings.cs` | Non |
| Lecteur | Écran de lecture en ligne d'un livre numérique | `BookReader` | `Web/Components/Pages/Books/BookReader.razor` | Non |
| Bibliothèque | Collection nommée de livres, avec type, tri et icône | `Library` | `Domain/Libraries/Library.cs` | Non |
| Type de bibliothèque | Nature des livres d'une bibliothèque (physique ou numérique). À confirmer | `LibraryBookType` | `Domain/Libraries/LibraryBookType.cs` | Non |
| Ordre de tri des livres | Critère de tri des livres d'une bibliothèque. À confirmer | `BookSortOrder` | `Domain/Libraries/BookSortOrder.cs` | Non |
| Déplacement de livre | Changement de bibliothèque d'un livre (avec son fichier) | `MoveBook` | `Application/Books/Move/` | Non |
| Utilisateur | Personne authentifiée propriétaire de ses livres et bibliothèques. À confirmer | `User` | `Domain/Users/User.cs` | Non |
| Administrateur | Utilisateur ayant le rôle `Admin`, autorisé à superviser les tâches de fond | `Admin` | `Web/Infrastructure/HangfireAuthorizationFilter.cs:10-11` | Non |
| Tâche d'import | Traitement asynchrone d'un fichier déposé, avec un statut | `ImportJob` | `Domain/ImportJobs/ImportJob.cs` | Non |
| Statut de tâche d'import | État d'avancement d'une tâche d'import. À confirmer | `ImportJobStatus` | `Domain/ImportJobs/ImportJobStatus.cs` | Non |
| Dossier d'import | Dossier surveillé où l'on dépose des archives à importer | `ImportDirectory` | `Web/appsettings.json` clé `Import` | Non |
| Archive de bande dessinée | Fichier cbz, cbr, zip, rar ou pdf contenant les pages d'un album | `.cbz`, `.cbr`, `SupportedExtensions` | `Web/appsettings.json` clé `Import` | Non |
| Analyse des pages (OCR) | Lecture du texte de pages pour retrouver l'ISBN d'un livre importé | `IsbnOcr`, `Tesseract` | `Persistence/Services/TesseractPageTextRecognizer.cs` | Non |
| Scan ISBN | Recherche automatique de l'ISBN sur les pages d'un livre ou d'une bibliothèque | `IsbnScan` | `Application/Books/IsbnScan/` | Non |
| Fonctionnalité activable | Fonction que l'administrateur active ou désactive sans redéploiement | `FeatureToggle` | `Domain/Settings/FeatureToggle.cs:5-14` | Non |
| Import par flux | Récupération automatique des sorties repérées dans un lecteur de flux | `FeedImport` | `Application/FeedImports/` | Non |
| Décision d'import par flux | Enregistrement du sort réservé à une entrée du flux (retenue, écartée, téléchargée…). À confirmer | `FeedImportDecision` | `Domain/FeedImports/FeedImportDecision.cs` | Non |
| Arbitrage | Choix à faire quand plusieurs candidats ou doublons sont possibles. À confirmer | `Arbitration` | `Domain/FeedImports/FeedImportArbitrationKind.cs` | Non |
| Miroir de téléchargement | Lien alternatif d'hébergeur pour un même fichier | `DownloadMirror` | `Domain/FeedImports/DownloadMirror.cs` | Non |
| Bibliothèque « À trier » | Bibliothèque cible par défaut des fichiers téléchargés | `TargetLibraryName` | `Web/appsettings.json` clé `FeedImport` | Non |
| Catalogue externe | Source tierce de métadonnées (OpenLibrary, BnF, Google Books, Bédéthèque, Bubble) | `ComicSearchService` | `Application/ComicInfoSearch/ComicSearchService.cs` | Non |
| Couverture | Image de couverture d'un livre, hébergée chez un service d'images | `Cover`, `Cloudinary` | `Persistence/Services/CloudinaryService.cs` | Non |
| Statistiques | Indicateurs chiffrés sur la collection et la lecture. À confirmer | `Statistics` | `Application/Statistics/Get/` | Non |
| Garde SSRF | Contrôle qui limite les appels sortants aux hôtes autorisés | `SsrfGuardHandler` | `Web/Infrastructure/SsrfGuardHandler.cs` | Non |
| Surcharge de fonctionnalité | Valeur choisie par l'administrateur pour une fonctionnalité activable, qui remplace celle de la configuration | `FeatureToggleOverride`, table `FeatureToggleOverrides` | `Domain/Settings/FeatureToggleOverride.cs:11` | Non |
| Note de lecture | Appréciation de 1 à 5 donnée à une lecture ; 0 à la création. À confirmer | `ReadingDate.Rating`, `Rating` | `Domain/Books/ReadingDate.cs:13-23` | Non |
| Rang dans l'article | Position d'un livre parmi ceux d'un même article de flux ; 0 pour la décision créée depuis l'entrée | `ItemIndex`, `MinifluxEntryId` | `Domain/FeedImports/FeedImportDecision.cs:16-17` | Non |
| Statut de décision d'import | État d'avancement d'une décision (En attente, Liens extraits, À arbitrer, Doublon ignoré, Téléchargement, Téléchargé, Importé (inutilisé), Ignoré, Échec) | `FeedImportDecisionStatus`, `Status` | `Domain/FeedImports/FeedImportDecisionStatus.cs:4-15` | Non |
| Nature d'arbitrage | Raison pour laquelle l'utilisateur doit trancher : liens ambigus ou doublon probable | `FeedImportArbitrationKind`, `ArbitrationKind` | `Domain/FeedImports/FeedImportArbitrationKind.cs:4-9` | Non |
| Fiche Bédéthèque mémorisée | Adresse de la fiche Bédéthèque d'un ISBN, gardée pour éviter de la rechercher à nouveau | `IsbnBedethequeUrl`, table `IsbnBedethequeUrls` | `Persistence/ApplicationDbContext.cs:101-104` | Non |
| Mise en page / sens de lecture | Préférences du lecteur : largeur de page, page seule, deux pages ; gauche à droite ou droite à gauche (mangas) | `ReaderSettings`, `ReaderLayout` | `Domain/Books/ReaderSettings.cs:6-33` | Non |
| Clés de protection des sessions | Clés qui protègent les cookies de connexion, conservées en base pour survivre à un redéploiement | `DataProtectionKey`, table `DataProtectionKeys` | `Persistence/ApplicationDbContext.cs:37-38` | Non |
| Modifications concurrentes | Refus d'enregistrer une tâche ou une décision modifiée entre-temps par un autre traitement | `xmin`, jeton de concurrence | `Persistence/ApplicationDbContext.cs:91-95` | Non |
| Bibliothèque physique | Bibliothèque de livres papier, où l'on ajoute un livre par scan, saisie d'ISBN ou saisie manuelle. À confirmer | `LibraryBookType.Physical`, « Physical books » | `Web/Components/Pages/Libraries/LibraryForm.razor:33` | Non |
| Bibliothèque numérique | Bibliothèque de fichiers (CBZ, CBR, PDF…) lisibles dans l'application, alimentée par l'import. À confirmer | `LibraryBookType.Digital`, « Digital books » | `Web/Components/Pages/Libraries/LibraryForm.razor:34` | Non |
| Lecture (historique) | Passage d'un livre de l'état non lu à lu, daté du jour et noté de 1 à 5 ; un livre peut avoir plusieurs lectures | « Add a reading », `ReadingDate` | `Web/Components/Pages/Books/BookDetail.razor:200-224` | Non |
| Import d'informations depuis le web | Écran qui compare les valeurs actuelles d'un livre à celles des catalogues en ligne et applique celles que l'utilisateur choisit, champ par champ | « Import info from the web », `FindBookDetails` | `Web/Components/Pages/Books/FindBookDetails.razor.cs:225-235` | Non |
| Mode recherche d'ISBN | Ouverture du lecteur pour retrouver l'ISBN imprimé dans les pages d'un livre, sans enregistrer la progression de lecture | `IsbnSearchMode`, `?mode=isbn` | `Web/Components/Pages/Books/BookReader.razor.cs:15` | Non |
| Doublon probable | Entrée du flux dont la série et le tome correspondent à un livre déjà possédé ; l'utilisateur doit confirmer ou infirmer | `CanResolveDuplicate`, « Not a duplicate » / « Confirm duplicate » | `Web/Components/Pages/FeedImportsPage.razor:38-42,194-210` | Non |
| Liens ambigus | Entrée du flux qui offre plusieurs fichiers possibles ; l'utilisateur garde le bon ou déclare qu'ils sont identiques | `CanChooseCandidate`, `MergeAsMirrors` | `Web/Components/Pages/FeedImportsPage.razor:43-58` | Non |
| Import bloqué | Import ni terminé ni en attente que l'utilisateur peut déclarer en échec. À confirmer | `IsStuck`, `ForceFail` | `Web/Components/SharedComponents/ImportJobCard.razor.cs:18` | Non |
