# Vérification — parcours front-office

## Synthèse du 2026-10-17

Périmètre : `05-user-journeys/front-office/` (matrice des rôles et 15 parcours UJ-FO-001 à 015) ; `back-office/` est vide. Trois vérificateurs indépendants, un par lot (sections ci-dessous).

| Lot | Contrôlés | Corrects | Corrigés ou complétés | Rétrogradés | Retirés |
|---|---|---|---|---|---|
| Rôles + UJ-FO-001 à 005 | 56 | 16 | 40 | 0 | 0 |
| UJ-FO-006 à 010 | 58 | 24 | 34 | 0 | 0 |
| UJ-FO-011 à 015 | 53 | 10 | 43 | 0 | 0 |
| **Total** | **167** | **50** | **117** | **0** | **0** |

Contrôle mécanique des citations : tous les fichiers cités existent et les plages de lignes sont valides ; les noms de fichiers nus ont été remplacés par les chemins complets.
Nouvelles questions : Q-UI-012 à 015, 020 à 023, 030 à 034 (`_questions.md`). Nouvelles anomalies : D-UI-007 à 012, 020 à 024, 030 à 035 (`_defects.md`).
Écarts relevés dans les règles BR-UI (non modifiées, hors périmètre) : voir Q-UI-034.
Suivi de couverture : aucune ligne ne correspond aux parcours ; elle n'a donc pas été passée à « Vérifié » (à créer par `/re-coverage`).

## 2026-10-17 — UJ-FO-006 à UJ-FO-010 (vérificateur indépendant)

Éléments contrôlés : 58 (étapes, lignes de validation et de permission) · corrects : 24 · corrigés ou complétés : 34 · rétrogradés : 0 · retirés : 0.

| Parcours | Contrôlés | Corrects | Corrigés |
|---|---|---|---|
| UJ-FO-006 | 17 | 8 | 9 |
| UJ-FO-007 | 10 | 4 | 6 |
| UJ-FO-008 | 10 | 5 | 5 |
| UJ-FO-009 | 11 | 4 | 7 |
| UJ-FO-010 | 10 | 3 | 7 |

Changements principaux :
- UJ-FO-006 étape 2a : le script de la caméra filtre déjà les ISBN invalides ; « Invalid ISBN scanned » n'est pas atteint en pratique et la caméra n'est pas relancée ensuite (BR-UI-018 inexact). Étape 5 / validations : le contrôle de doublon porte sur tous les livres, pas sur ceux de l'utilisateur ; contrôle serveur des longueurs titre/série/ISBN = base seulement.
- UJ-FO-007 : chemins complets, citation erronée remplacée (liste des lectures), contrôles de propriétaire de la lecture et de la suppression de lecture ajoutés, messages d'échec ajoutés.
- UJ-FO-008 : ISBN lu dans les pages, absence d'ISBN, progression des sources, copie de couverture (exceptions) précisées ; chemin nu remplacé.
- UJ-FO-009 : libellé « Continue reading », écran d'échec, « Mark as read » non grisé à la note 0, contrôles serveur ajoutés ; chemin nu remplacé.
- UJ-FO-010 : étape 2 corrigée (le serveur enregistre l'ISBN unique inconnu, sinon propositions, y compris pour un seul ISBN déjà utilisé) ; étape 6 corrigée (aucune progression enregistrée en mode ISBN, BR-UI-047 inexact) ; critère de sélection du scan de bibliothèque précisé.

Questions et anomalies proposées : transmises au demandeur (non écrites dans `_questions.md` / `_defects.md`, selon consigne). Statut du suivi de couverture : aucune ligne de suivi propre aux parcours trouvée ; non modifié.

## 2026-10-17 — roles-and-permissions et UJ-FO-001 à UJ-FO-005 (vérificateur indépendant)

Éléments contrôlés : 56 (plus 3 lignes de la matrice qui renvoient seulement à des parcours vérifiés ailleurs : UJ-FO-006, -007, -011, -013, -014) · corrects : 16 · corrigés ou complétés : 40 · rétrogradés : 0 · retirés : 0.

| Fichier | Contrôlés | Corrects | Corrigés |
|---|---|---|---|
| roles-and-permissions | 12 | 4 | 8 |
| UJ-FO-001 | 7 | 1 | 6 |
| UJ-FO-002 | 6 | 1 | 5 |
| UJ-FO-003 | 11 | 3 | 8 |
| UJ-FO-004 | 10 | 5 | 5 |
| UJ-FO-005 | 10 | 2 | 8 |

Changements principaux :
- roles-and-permissions : l'état de santé `/health` est public et détaillé (ligne ajoutée, D-UI-008 proposée) ; l'accès refusé à la page des niveaux de journalisation redirige en fait vers la connexion ; citations imprécises remplacées (dossier, simple lecture de l'utilisateur, ligne de journalisation décalée).
- UJ-FO-001 : adresse demandée non conservée après connexion, session de 7 jours glissants, conditions exactes de création du compte (e-mail requis), retour de déconnexion ; chemin nu `Web/Components/Pages/Error.razor` remplacé.
- UJ-FO-002 : citation d'étape 3 erronée (feuille d'actions) remplacée, ordre par nom, 3 couvertures au plus, échec du premier chargement qui montre l'état vide (D-UI-007 proposée).
- UJ-FO-003 : trois mentions « Non retrouvé » côté serveur fausses (le type, la présence et la longueur de la couleur et de l'icône sont contrôlés) ; comparaison du nom (casse et espaces ignorés) ; collision de dossier entre utilisateurs pour une bibliothèque numérique (D-UI-009 proposée).
- UJ-FO-004 : bibliothèque introuvable = page d'état avec bouton, et non retour automatique ; messages de refus, déplacement de dossiers au renommage et fonctionnement de la boîte de confirmation précisés.
- UJ-FO-005 : adresse invalide = retour silencieux (pas de « Library not found ») ; citation de permission erronée (création de livre au lieu de la liste) remplacée ; recherche insensible à la casse ; échec du premier chargement qui montre « No books in this library yet » (D-UI-011 proposée).

Questions et anomalies proposées : transmises au demandeur (non écrites dans `_questions.md` / `_defects.md`, selon consigne). Le statut du suivi de couverture n'est pas modifié : aucune ligne de suivi propre aux parcours trouvée, et les parcours UJ-FO-006 à UJ-FO-015 ne font pas partie de cette passe.

## 2026-10-17 — UJ-FO-011 à UJ-FO-015 (vérificateur indépendant)

Éléments contrôlés : 53 (étapes, lignes de validation et de permission) · corrects : 10 · corrigés ou complétés : 43 · rétrogradés : 0 · retirés : 0.

| Parcours | Contrôlés | Corrects | Corrigés |
|---|---|---|---|
| UJ-FO-011 | 11 | 3 | 8 |
| UJ-FO-012 | 16 | 2 | 14 |
| UJ-FO-013 | 6 | 2 | 4 |
| UJ-FO-014 | 9 | 1 | 8 |
| UJ-FO-015 | 11 | 2 | 9 |

Changements principaux :
- UJ-FO-011 : validation « taille » (le dépassement lève une exception non interceptée, pas un « motif du refus ») ; validation « même fichier » (inopérante pour un envoi depuis l'écran : chemin stocké unique) ; 10 fichiers non contrôlé côté serveur ; message de succès conditionné à l'absence de toute erreur ; action « forcer l'échec » limitée aux imports ni terminés ni « Pending » (BR-UI-053 écrit « Waiting » à tort) ; permissions : sources corrigées.
- UJ-FO-012 : conditions des actions manuelles relevées dans le domaine ; « Delete downloaded » supprime aussi les décisions dont la tâche d'import a été retirée ; « en téléchargement » = moins d'une heure, import en cours contrôlé côté serveur seulement ; limites de la correction bien contrôlées côté serveur ; arbitrage sans clé Debrid-Link = rien lancé, rien signalé.
- UJ-FO-013 : étape 3 (indicateurs et graphique restent affichés pour 0 livre) ; fenêtre de 12 mois UTC ; règles des indicateurs ; chiffres périmés conservés après un échec.
- UJ-FO-014 : critère de « Suggested » ; refus du téléchargement non signalés par l'application ; message de confirmation de la fiche différent de celui de la bibliothèque ; suppression du fichier après enregistrement, sans contrôle du résultat ; message d'échec de déplacement = motif du serveur.
- UJ-FO-015 : carte des 7 fonctionnalités et refus de configuration incomplète limité à 3 ; niveaux de journalisation exacts ; rôle `Admin` contrôlé en trois endroits mais jamais attribué dans le dépôt (Q-UI-030) ; source de l'étape 2 corrigée.

Questions et anomalies proposées (Q-UI-030 à Q-UI-03x, D-UI-030 à D-UI-035) : transmises au demandeur, non écrites dans `_questions.md` / `_defects.md`, selon consigne. Statut du suivi de couverture non modifié (passe partielle sur le dossier).
