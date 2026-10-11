# ADR-0021: Refonte UI — mobile, tablette paysage et grand écran

**Statut** : Acceptée
**Date** : 2026-10-11
**Décideurs** : @nicolas-dufaut

## Contexte

[ADR-0007](0007-mudblazor-comme-librairie-de-composants.md) a retenu
MudBlazor avec son thème par défaut et laissé la direction graphique ouverte
(backlog « Refonte graphique de la liste de livres »). Une campagne de
captures de l'existant (91 captures, branche `docs/ui-redesign-captures`) sur
les appareils réellement utilisés a mis en évidence :

- **Appareils** : iPhone 14 (390 × 844, app installée sur l'écran d'accueil),
  tablette Samsung SM-T580 utilisée **uniquement en paysage** (viewport CSS
  ~1280 × 800), et grand écran 1920 × 1080.
- **Navigation** : à 1280 px, la tablette dépasse le seuil de 960 px et
  affiche la barre du haut « desktop », qui prend 64 px de hauteur sur un
  écran de 800 px.
- **Lecteur** : en tablette paysage, une page simple n'occupe que ~525 px de
  large (~60 % de noir). Les planches doubles ne représentent qu'environ
  5 % des pages. Les barres de contrôle recouvrent la planche.
- **Bibliothèque** : sur mobile, ~195 px de bandeaux avant le premier livre
  (~2,5 livres par écran) ; en tablette, 4 colonnes étroites tronquent titres
  et séries. Les vues Cards et List font double emploi.
- **Fiche livre** : sur mobile, le titre n'apparaît qu'à ~570 px et aucune
  action n'est visible hors du menu ⋮. Libellés mélangeant français et
  anglais.

Les maquettes ont été conçues et validées écran par écran dans un canevas
Claude Design (privé) : https://claude.ai/artifact/PM7HA5tDtfDgUTEtcPVtkX.
Le lecteur a été validé par deux prototypes interactifs testés sur les
appareils (`P1` tablette, `P2` iPhone).

## Options considérées

### Navigation en tablette paysage

- **Option A — Garder la barre du haut** : aucun changement ; mais coûte
  64 px de hauteur, la dimension la plus rare en paysage.
- **Option B — Rail latéral (retenue)** : 88 px de large (6 % de la
  largeur), toute la hauteur rendue au contenu, entrées sous le pouce quand
  on tient la tablette ; `MudDrawer` en `DrawerVariant.Mini`, coût faible.
  Inconvénient : une variante de mise en page de plus à maintenir.

### Mode de lecture par défaut

- **Option A — Double page** : ~1050 px utilisés sur 1280, mais pertinent
  pour ~5 % des pages seulement.
- **Option B — Page simple ajustée en hauteur** : comportement actuel, ~60 %
  de l'écran inutilisé.
- **Option C — Page ajustée à la largeur, défilement vertical continu
  (retenue)** : toute la largeur utilisée, lecture d'une main (tap = un
  écran), passage naturel d'une page à l'autre. Une page fait ~2,5 écrans en
  tablette, ~3 sur téléphone.

### Couleurs

Douze palettes ont été comparées sur la fiche livre (dont huit tirées du
logo : bleu, corail, violet, cyan, sombres). **La palette actuelle (C1) est
conservée.**

## Décision

### Formats et navigation

| Largeur | Navigation |
| ------- | ---------- |
| < 960 px | Barre du bas (5 entrées, libellés 12 px, entrée active sur fond coloré) |
| 960–1440 px | Rail latéral de 88 px (icône + libellé) |
| ≥ 1440 px | Barre latérale de 260 px avec la **liste des bibliothèques** |

La barre du haut actuelle disparaît. La navigation est masquée dans le
lecteur et pendant l'ajout d'un livre. Sur iPhone, l'app respecte les zones
système (`viewport-fit=cover`, `env(safe-area-inset-*)`).

### Palette (C1, inchangée)

| Rôle | Couleur |
| ---- | ------- |
| Action principale | `#594AE2` (texte blanc, 6,0:1) |
| Navigation | `#131B2C` |
| Fond | `#F4F4F6` |
| Texte / texte secondaire | `#1B1D24` / `#5C6070` |
| Bordures | `#E2E3EA` |

Couleurs à sens fixe, identiques partout : étoiles (ambre), « Read » (vert),
Delete (rouge, toujours séparé des autres actions), pastilles ISBN
(`IsbnScanStateIcon`). Police : Roboto (MudBlazor) ; Bangers pour le logo.

### Lecteur

- Par défaut : **page ajustée à la largeur, défilement vertical continu**,
  pages séparées par 8 px de noir, sans repère « Page N » ni indicateur de
  position.
- Tap 30 % droite / gauche = un écran plus bas / plus haut (recouvrement
  ~80 px tablette, ~40 px téléphone), tap centre = contrôles.
- Contrôles compacts (56 px en haut, barre du bas avec curseur et compteur) ;
  masqués : filet de progression de 3 px. Outils ISBN dans ⋯.
- Panneau « Display » : Fit width (défaut) / Single page / Two pages,
  Continuous / Page by page, Left to right / Right to left (manga) ;
  mémorisé par livre. Aide aux gestes à la première ouverture.
- Téléphone : lecture en paysage ; iOS ne permettant pas de verrouiller
  l'orientation, une invite « Rotate your phone » s'affiche en portrait, avec
  « Read in portrait anyway ».
- Grand écran : page limitée à ~1100 px ; raccourcis Espace, ← →, F, Échap.

### Écrans

- **Bibliothèque** : une barre unique ; deux vues seulement, **Covers**
  (défaut) et **List** (fusion des anciennes vues List et Cards) ; tri et vue
  dans une feuille du bas (mobile) ou un menu (tablette, grand écran) ;
  actions d'un livre dans une feuille / un menu.
- **Fiche livre** : couverture + infos en tête ; action principale
  « Continue reading · Page X of Y » (numérique) ou « Add a reading »
  (papier) ; actions secondaires avec libellé (dont CBZ) ; détails en grille ;
  libellés en anglais.
- **Liste des bibliothèques** : cartes avec 3 couvertures, icône et couleur,
  nombre de livres. Pas de carte « Continue reading ».
- **Ajout d'un livre** : feuille de choix (scan en principal), scanner revu,
  nouvelle étape **« Book found »** pour ajouter en un geste, formulaire
  regroupé avec erreurs claires.
- **Écrans secondaires** : flux filtrés par pastilles (liste + détail sur
  tablette), import par fichiers avec statut par étape, statistiques avec un
  chiffre clé, paramètres résumés, import web limité aux champs qui diffèrent.

### Règles transverses

- Langue de l'interface : **anglais**.
- Cibles tactiles ≥ 44 px ; `aria-label` sur tout bouton icône (règle
  existante de `CLAUDE.md`).
- Sur mobile, menus contextuels en feuilles du bas ; sur tablette et grand
  écran, menus ancrés au bouton.

## Conséquences

### Positives

- Les trois formats ont une mise en page pensée pour eux, au lieu d'un seul
  seuil à 960 px.
- Le lecteur exploite toute la largeur de la tablette, validé par prototype
  sur l'appareil.
- Moins de code UI à maintenir côté bibliothèque (suppression de la vue
  Cards).

### Négatives

- Trois variantes de navigation à maintenir et à tester.
- Le défilement continu du lecteur demande de charger les pages
  progressivement et de suivre la page visible pour la progression.
- Les préférences de vue « Cards » existantes doivent basculer sur « List ».
- Le canevas de maquettes est privé : les issues y renvoient, mais seul le
  mainteneur peut l'ouvrir.

### Couches impactées

- [x] Web (navigation, lecteur, pages, thème)
- [x] Application (réglages de lecture par livre, données des cartes de bibliothèque)
- [ ] Domain
- [x] Persistence (réglages de lecture par livre)

## Liens

- ADR liées : [ADR-0007](0007-mudblazor-comme-librairie-de-composants.md), [ADR-0011](0011-code-behind-et-css-isolation.md)
- Captures de l'existant : branche `docs/ui-redesign-captures`, dossier `design/captures/`
- Maquettes : canevas Claude Design https://claude.ai/artifact/PM7HA5tDtfDgUTEtcPVtkX
- Issues d'implémentation : #1110 (navigation), #1111 (lecteur), #1112 (lecteur : affichage et manga), #1113 (lecteur téléphone), #1114 (bibliothèque), #1115 (fiche livre), #1116 (liste des bibliothèques), #1117 (ajout d'un livre), #1118 (écrans secondaires)
