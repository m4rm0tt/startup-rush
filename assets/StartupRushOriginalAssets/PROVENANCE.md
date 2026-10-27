# StartupRush — Provenance des ressources

## Auteur et contexte

Pack produit le 27 octobre 2026 par **Claude (modèle Opus 5.5, Anthropic)** dans Claude Desktop (onglet Code),
sur la machine de l'utilisateur (Windows 11), à la demande de l'utilisateur, pour son projet StartupRush.
La production a été répartie entre la session principale et trois agents parallèles de même modèle (kit HUD et
police ; datacenter et laboratoires ; industrie et décoration), tous soumis au même contrat graphique et au même
moteur. Aucun artiste humain n'a dessiné ces sprites : ils sont **générés par du code écrit pour ce projet**.
Selon la plateforme de diffusion du jeu, une mention « contenu créé avec l'aide d'une IA » peut être demandée.

## Outils réellement utilisés

| Outil | Version | Usage |
|---|---|---|
| Python | 3.13 | langage du générateur |
| NumPy | 2.4.6 | rendu vectorisé (lancer de rayons, masques) |
| Pillow | 13.0.0 | lecture/écriture PNG (RGBA 8 bits, bloc sRGB), vérifications |
| fontTools | 4.67.0 | construction et vérification des polices TTF (lot HUD) |
| .NET SDK | 10.0.401 (cible net8.0) | uniquement pour **vérifier** le portage C# du compositeur (aucun asset produit) |

Aucun éditeur d'images, aucun générateur d'images par IA, aucun service en ligne n'a été utilisé.
Aucun fichier n'a été téléchargé : **pas de pack Kenney, pas d'anciens sprites, pas d'assets des jeux de
référence, pas d'images de moteurs de recherche, pas de Google Fonts**.

## Méthodes de création

1. **Objets isométriques** (`sources/defs_*.py`) : chaque objet est décrit par des volumes simples (boîtes,
   polyèdres convexes, cylindres, ellipsoïdes) et des décors de faces (tiroirs, LED, aérations, écrans) dans son
   repère local. Le moteur `tools/srgen/iso.py` les rend par **lancer de rayons orthographique au pixel** : arêtes
   2:1 exactes, aucune interpolation. Les 4 orientations sont rendues depuis le même modèle (jamais de miroir).
2. **Architecture** (`sources/defs_architecture.py`) : murs rendus comme tranches d'une scène continue
   (`tools/srgen/slicer.py`), sols et motifs muraux définis par des fonctions mathématiques de motif.
3. **Personnages** (`sources/characters.py`) : peintre 2D par régions (`tools/srgen/raster2d.py`) : crâne commun,
   visages, carrures et tenues paramétrés ; ombrage directionnel calculé (recalculé après miroir).
4. **HUD, icônes, police, curseurs, effets, terminal, composants de projet** : grilles ASCII et code de
   composition propres au projet (voir `sources/ui/` et `sources/defs_ui.py`).
5. **Palette** : 38 rampes de 6 teintes choisies pour ce projet (`tools/srgen/palette.py`) ; ce n'est ni Endesga 32
   ni une palette d'un autre projet.
6. **Micro-police des planches de contrôle** (`tools/srgen/preview.py`, 3 × 5 px) : originale, utilisée uniquement
   pour étiqueter les aperçus.

## Apports externes

**Aucun.** Les références citées par la commande (Game Dev Tycoon, Startup Company) n'ont servi qu'à comprendre
l'intention (charme des personnages, variété d'équipements) ; aucun de leurs fichiers n'a été consulté ni copié.
Les formes représentées (bureaux, chaises, baies, convoyeurs, cryostat) sont des objets génériques du monde réel,
dessinés par code. Aucune marque, aucun logo, aucune personne réelle, aucune franchise. Les références cachées de
la chambre sont originales (canard de débogage, croquis d'idée, invite « >_ » générique, mini-projet).

## Statut des fichiers

| Dossier | Statut |
|---|---|
| `assets/`, `masks/`, `atlases/` | **générés** par `tools/build_all.py` et `tools/pack_atlas.py` ; ne pas retoucher à la main (modifier la source puis régénérer) |
| `sources/` | **sources éditables** : descriptions d'objets, personnages, plans de pièces, glyphes, icônes, inventaire |
| `tools/` | moteur et outils (construction, recoloration, atlas, manifeste, QA, archive) |
| `rooms/` | plans générés par `sources/rooms/room_plans.py` (validés contre le manifeste) |
| `previews/` | aperçus de contrôle (agrandis) : **pas des textures de production** |
| `build_cache/` | entrées de manifeste par famille, rapports QA, statuts des lots |

Régénération complète : `python tools/build_all.py`, puis `sources/rooms/room_plans.py`, `tools/pack_atlas.py`,
`tools/make_manifest.py`, `tools/qa_check.py`, `tools/make_coverage.py`. Le rendu est déterministe (graines
aléatoires fixes ; vérification de reproductibilité consignée dans `QA_REPORT.md`).

## Droits

Aucune déclaration CC0 ni licence de publication n'est imposée : le pack est livré à l'utilisateur pour son projet,
il n'a été envoyé sur aucun dépôt ni compte tiers. Aucune garantie juridique absolue d'originalité ne peut être
donnée ; la méthode (génération par code, sans source externe) est documentée ici pour permettre l'évaluation.
