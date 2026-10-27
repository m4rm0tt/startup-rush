# StartupRush — Bibliothèque originale d'assets pixel art (v1.0.0)

Bibliothèque **originale, modulaire et extensible** pour StartupRush (gestion tech, C#/Avalonia) : de la chambre
de départ au laboratoire quantique, en vue isométrique 2:1 fixe, avec personnages assis en couches, kit HUD
complet, police pixel originale, plans de pièces et outils de composition. Tout est **généré par des sources
éditables** (descriptions + générateur Python), vérifié automatiquement et visuellement.

> Ce README suffit pour intégrer le pack sans connaître sa génération. Documents de référence :
> `INTEGRATION_GUIDE.md` (placement, tri, couches, sockets, pièces), `COLOR_SYSTEM.md` (masques, recoloration,
> motifs), `STYLE_GUIDE.md` (contrat graphique), `QA_REPORT.md` (contrôles), `PROVENANCE.md`, `COVERAGE.csv`.

## 1. Démarrage rapide (intégrateur)

1. Lire `manifest.json` → `assets` (une entrée par asset), `rooms` (plans), `palette`, `channels`, `conventions`.
2. Placer une case de sol : `variants["u{U%pu}v{V%pv}"]`, dessiner à `(X0 + (U−V)·32 − 32, Y0 + (U+V)·16)`.
3. Placer un objet : `orientations[facing]` → dessiner `layers.base` à `case_écran(U,V) − anchor`, puis `glass`,
   puis `front` ; accumuler `shadow` dans la couche d'ombre commune (max, opacité 0,30).
4. Trier les objets par **tri topologique des emprises** (`INTEGRATION_GUIDE.md` §4).
5. Recolorer avec `mask` + rampes (`COLOR_SYSTEM.md` §4) ; personnages : `hair_back → body → head → hair_front`.
6. Charger une pièce prédéfinie : `rooms/<id>.json` (déjà développée, validée) ; aperçu dans `previews/rooms/`.

Zoom au plus proche voisin, facteurs entiers. Implémentations de référence exécutées : `tools/srgen/scene.py`
(Python) et **`tools/csharp/StartupRushReference/Program.cs` (C#, sans dépendance), dont les rendus des 10 pièces sont
identiques au pixel près à ceux de la référence Python** (voir `QA_REPORT.md`).

## 2. Contenu

| Famille | IDs | Orientations / variantes / états | PNG |
|---|---|---|---|
| Architecture (murs, angles, terminaisons) (`architecture`) | 8 | 14 | 14 |
| Sols, bords, seuils, marquages (`floors`) | 13 | 25 | 25 |
| Motifs muraux (calques) (`wall_patterns`) | 8 | 16 | 16 |
| Mobilier et vie des locaux (`furniture`) | 55 | 131 | 274 |
| Postes informatiques et accessoires (`computers`) | 32 | 115 | 226 |
| Datacenter (baies, modules, infrastructure) (`servers`) | 26 | 68 | 108 |
| Laboratoires et métiers tech (`laboratories`) | 27 | 83 | 171 |
| Industrie informatique (future) (`industry_future`) | 32 | 72 | 144 |
| Décoration et personnalité (`decoration`) | 25 | 66 | 118 |
| Personnages (bases + coiffures) (`characters`) | 18 | 72 | 124 |
| HUD : composants, icônes, thème (`hud`) | 224 | 339 | 347 |
| Polices originales (`fonts`) | 2 | 2 | 2 |
| Curseurs (`cursors`) | 12 | 12 | 24 |
| Effets (primitives statiques) (`vfx`) | 16 | 16 | 16 |
| Kit terminal (`terminal`) | 21 | 23 | 23 |
| Composants de projet (`project_components`) | 51 | 71 | 72 |
| **Total** | **570** | **1125** | **1704** (+ 714 masques) |

Ces nombres comptent des IDs distincts. Les variantes de couleur ne sont pas des fichiers : elles passent par les masques (voir COLOR_SYSTEM.md).

Pièces prédéfinies (`rooms/`, aperçus `previews/rooms/`, planche `previews/rooms_overview.png`) :

| Pièce | Taille (cases) | Usage | Postes occupés | Cases libres à aménager |
|---|---|---|---|---|
| Chambre de départ (`room_bedroom_start`) | 6 x 5 | current | 1 | 20 / 30 |
| Petit bureau / atelier de startup (`room_startup_small`) | 8 x 6 | current | 4 | 20 / 48 |
| Bureau moyen (`room_office_medium`) | 12 x 9 | current | 10 | 54 / 108 |
| Salle serveur compacte (`room_server_compact`) | 6 x 5 | current | 1 | 10 / 30 |
| Atelier de prototypage et de test matériel (`room_hw_workshop`) | 10 x 8 | current | 5 | 39 / 80 |
| Grand open space (`room_open_space`) | 18 x 14 | current | 25 | 148 / 252 |
| Plateau de siège (`room_hq_floor`) | 24 x 18 | current | 19 | 296 / 432 |
| Datacenter (`room_datacenter`) | 16 x 12 | current | 1 | 100 / 192 |
| Hall de fabrication et d'assemblage (étape future) (`room_factory_hall`) | 22 x 16 | future | 2 | 259 / 352 |
| Laboratoire avancé (zone de calcul quantique) (`room_advanced_lab`) | 14 x 10 | current | 3 | 81 / 140 |

Modules de plan réutilisables (`rooms/modules/`) : salle de réunion vitrée, kitchenette, sanitaires, coin détente,
accueil, îlot de 4 postes (avec distribution de personnages par instance).

## 3. Arborescence

```text
StartupRushOriginalAssets/
  assets/<famille>/        PNG de production (sprites, ombres, couches front/glass, calques de motifs)
  masks/<famille>/         masques de matériau (même taille et même ancre que le sprite)
  atlases/                 pages d'atlas par famille (+ pages de masques aux mêmes rectangles)
  rooms/                   10 plans de pièces + modules (JSON)
  previews/                planches de contrôle, scènes, écrans de référence (agrandis : pas des textures)
  sources/                 sources éditables (descriptions d'objets, personnages, plans, glyphes, icônes, inventaire)
  tools/                   moteur + outils (build, recoloration, atlas, manifeste, QA, couverture, archive)
  build_cache/             entrées de manifeste par famille, rapports QA, statuts des lots
  manifest.json  ASSET_PLAN.csv  COVERAGE.csv  INVENTORY.csv
  STYLE_GUIDE.md  COLOR_SYSTEM.md  INTEGRATION_GUIDE.md  PROVENANCE.md  QA_REPORT.md  README_FR.md
```

Familles : `architecture`, `floors`, `wall_patterns`, `furniture`, `computers`, `servers`, `laboratories`,
`industry_future` (usage `future` : ne casse pas le build présent), `decoration`, `characters`, `hud` (composants,
icônes, thème), `fonts`, `cursors`, `vfx`, `terminal`, `project_components`.

## 4. Points clés du système

- **Une géométrie, plusieurs couleurs** : sprite par défaut + masque (canal, indice de rampe). Une rampe par canal
  et par asset, garantie par le build. Couleur libre possible (`ramp_from_color`, OKLCh).
- **Orientations sans miroir** pour les objets (rendus depuis un modèle 3D local) ; personnages : miroir avec
  ombrage recalculé (lumière toujours à gauche).
- **Murs modulaires** rendus en tranches d'une scène continue : jonctions parfaites ; 8 motifs muraux en calques
  limités à la surface murale (jamais sur vitres, cadres, plinthes).
- **Améliorations visibles = pièces séparées** : 2ᵉ écran sur bras, webcam, modules de baie (slots de 4 px),
  produits sur convoyeurs, objets de bureau dans des sockets.
- **Personnages** : 12 bases chauves (2 avatars + 10 employés), 6 coiffures alignées sans décalage sur toutes les
  bases, 3 peaux et 5 teintes de cheveux par masque, portraits HUD synchronisés (recadrage de la vue `se`).
- **HUD « Papier »** : composants 9-slice avec marges, tailles minimales et états ; icônes 16 px teintables ;
  police originale (proportionnelle + mono, TTF + atlas bitmap) ; curseurs avec points chauds ; effets statiques
  (animation par le code) ; kit terminal ; cartes de composants de projet avec ports et liens.

## 5. Modifier ou étendre

| Besoin | Où |
|---|---|
| Ajouter/modifier un objet | `sources/defs_*.py` (décorateur `@iso_asset`, volumes + décors de faces) |
| Nouveau personnage / coiffure | `sources/characters.py` (crâne commun : la compatibilité est automatique) |
| Nouveau motif mural ou sol | `sources/defs_architecture.py` (fonctions de motif) |
| Nouveau plan de pièce | `sources/rooms/room_plans.py` (aides `pod_pair`, `bench_pair`, `glass_room`, modules) |
| HUD, icônes, police | `sources/ui/`, `sources/defs_ui.py` |
| Palette | `tools/srgen/palette.py` |

Puis : `python tools/rebuild_all.py` (nettoie et régénère tout, planches, plans, atlas, manifeste, QA, couverture,
rapport) et `python tools/make_archive.py` (inventaire SHA-256 + ZIP vérifié). Le rendu est déterministe.

Critère pour créer un nouveau sprite (§7G) : silhouette, emprise, usage visible, matériaux ou point d'interaction
qui changent. Sinon : recoloration, données ou pièce ajoutée dans un socket.

## 6. Limites connues

- **Intégration Avalonia non exécutée** (le jeu n'était pas disponible) : composition validée avec le compositeur de référence.
- Personnages en trois quarts stylisés : buste presque frontal, diagonales sw/ne obtenues par miroir (ombrage recalculé). Aucune animation (exigence de la commande).
- Le luminaire suspendu est livré mais absent des aperçus : en vue en coupe, son câble se lit comme un pied de lampadaire.
- Verre translucide : couche `glass` composée après le contenu ; deux vitres superposées dans un même objet ne montrent que la plus proche.
- Les 10 plans livrés sont rectangulaires ; les pièces en L sont possibles (angle saillant fourni) mais aucun plan en L n'est livré.
- Le parquet (vannerie fine) et la courbe de convoyeur sont lisibles mais simplifiés à l'échelle native.
- Aucun décor extérieur (exigence) : les fenêtres montrent un ciel stylisé.
- Polices vérifiées avec fontTools et Pillow ; le rendu Avalonia (anticrénelage du texte) reste à régler : l'atlas bitmap garantit un rendu au pixel.
- Les écrans de référence du HUD sont des compositions de test (complétude, lisibilité), pas une interface fonctionnelle.
- Couvrir toutes les inventions futures est impossible : le système d'extension (sources + générateur + critères §7G) est documenté.

## 7. Reprise par une autre session

Tout est reproductible depuis `sources/` + `tools/`. Pour continuer : lire `STYLE_GUIDE.md`, ajouter des
définitions dans `sources/defs_*.py`, lancer `tools/rebuild_all.py`, contrôler les planches `previews/sheet_*.png`
et `QA_REPORT.md`. Les statuts détaillés des lots parallèles sont dans `build_cache/STATUS_*.md`.
