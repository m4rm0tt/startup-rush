# Utilisation des assets

**Depuis le lot 2 de la V1, tout ce qui se voit vient du pack original `assets/StartupRushOriginalAssets`** (v1.0.0) :
pièces, mobilier, informatique, personnages, HUD « Papier », icônes, terminal et polices. Le registre ID par ID est
[`docs/asset_usage.csv`](docs/asset_usage.csv) (état : validé, partiel, non implémenté) ; l'avancement est dans
[`docs/V1_ETAT.md`](docs/V1_ETAT.md).

- **Moteur** : `Core/Core.csproj` embarque `manifest.json` et `rooms/*.json` ; `Core/space/Pack.cs` en lit les emprises,
  accès, sockets et plans.
- **Interface** : `UI/UI.csproj` embarque `assets/**/*.png`, `masks/**/*.png`, le manifeste et les plans ; `UI/Iso/PackImages.cs`
  décode chaque PNG une fois, recolore par masque et garde le résultat en cache ; `UI/Iso/RoomRenderer.cs` assemble les pièces
  comme le compositeur de référence du pack (projection 2:1, tri topologique, ombres communes à 30 %, couches, sockets,
  personnages en couches). Les polices StartupRush Pixel et Mono sont des ressources Avalonia.
- **Aucun chemin de développement** dans le jeu livré : tout est dans l'exécutable.
- **Sons et musiques** : inchangés (Juhani Junkala, CC0), produits par `tools/build_assets.py`. La partie images et polices de
  ce script ne sert plus : ses sorties (anciens sprites, cadres Kenney, polices Press Start 2P, VT323, Pixelify Sans) ne sont
  plus livrées.

---

## Version précédente (tycoon 2.1), pour mémoire

Ce document dit ce que le jeu utilise vraiment dans le dossier `assets/` (les packs originaux, jamais modifiés), comment ces fichiers arrivent dans le jeu, et ce qui est dessiné à la main. Le placement de chaque meuble (emprise, ancrage, interactions) est dans `FURNITURE_MANIFEST.md`.

- **Originaux** : `assets/` à la racine, non versionné dans git (176 Mo). Les licences et `SOURCE.json` de chaque pack y restent.
- **Dérivés** : `UI/Assets/`, produits par `python tools/build_assets.py` (toujours les mêmes fichiers). Le jeu ne lit que ce dossier, embarqué dans l'exécutable : aucun chemin absolu, rien à copier à côté.
- **Chargement** : chaque image, son et musique est décodé une seule fois ; les sprites découpés sont gardés en cache (`UI/Pixel/Atlas.cs`, `UI/Audio/Sfx.cs`). Affichage au plus proche voisin, zoom entier, translation arrondie au pixel physique.

## Changement de projection

L'ancien jeu montrait une pièce de face. La refonte passe à une **vue 3/4 de dessus unique** (décision du cahier des charges, qui remplace la recommandation du catalogue précédent). Les packs vus de dessus mis de côté jusqu'ici deviennent la base du mobilier :

| Pack | Licence | Usage dans la refonte |
|---|---|---|
| Kenney Roguelike Indoors | CC0 | Bureaux, chaises (simple, ergonomique), plante, bibliothèque, tapis, lit, cuisine, frigo, table, kitchenette. Tuiles de 16 px découpées d'après `tilesheetInfo.txt` (marge 1 px). |
| 2dPig Pixel Office | CC0 | Les 5 personnages, écran d'ordinateur, unité centrale, canapés, tableau blanc, machine à café, fenêtres. Mêmes 16 px par case, vus de 3/4. |

Les deux packs sont recolorés vers la palette **Endesga 32** (couleur la plus proche, pondérée ; 4 teintes de Pixel Office fixées à la main : peau, ombre de peau, deux verts).

## Fichiers produits

| Fichier | Contenu |
|---|---|
| `UI/Assets/Sprites/furniture.png` + `furniture.json` | Atlas de la scène : 19 meubles, 5 personnages, 1 fenêtre. Le JSON donne pour chacun le rectangle, la taille, l'emprise au sol et la source. |
| `UI/Assets/Sprites/office.png` | Planche Pixel Office recolorée (source de l'atlas, gardée pour référence). |
| `UI/Assets/Ui/skins/*.png` | Cadres 9-slice Kenney Pixel UI Pack, recolorés (panneaux, boutons, onglets). |
| `UI/Assets/Ui/input_prompts.png` | Kenney Input Prompts Pixel, copié tel quel. |
| `UI/Assets/Audio/Sfx/*.wav` | 16 bruitages de Juhani Junkala (512 Sound Effects, CC0). |
| `UI/Assets/Audio/Music/*.ogg` | 4 musiques de Juhani Junkala (4 Chiptunes Adventure, CC0). |
| `UI/Assets/Fonts/*.ttf` | Press Start 2P, Pixelify Sans, VT323 (OFL 1.1, licences livrées). |

## Sons : quand ils partent

| Son du jeu | Fichier | Moment |
|---|---|---|
| Click | work.wav | Sélection d'un objet ou d'une personne dans la scène |
| Select | select.wav | Bouton de menu |
| Notify | offer.wav | Notification ordinaire (nouvelle demande, etc.) |
| Coin | coin.wav | Contrat livré |
| Cash | cash.wav | Encaissement de 500 CHF ou plus dans la même image |
| Place | buy.wav | Meuble posé |
| Bill | bill.wav | Paiement de 500 CHF ou plus |
| Error | error.wav | Action refusée, placement impossible |
| Milestone | levelup.wav | Jalon atteint |
| Launch | victory.wav | Produit mis en ligne ou version publiée |
| Alarm | alarm.wav | Notification critique |
| Unpaid | unpaid.wav | Impayé |
| Event | event.wav | Avertissement (retard, panne, rumeur de concurrent) |
| PauseIn / PauseOut | pause_in.wav / pause_out.wav | Pause |
| Defeat | defeat.wav | Liquidation |

Musiques : `menu.ogg` dans les menus, `work1.ogg` et `work2.ogg` en alternance pendant la partie, `crisis.ogg` en impayés ou mise en demeure. Garde-fous : 10 sons au plus en même temps, un même son pas plus d'une fois par 45 ms, saturation douce.

Le mode capture (`--capture`) vérifie au lancement du build que les 16 sons et 4 musiques sont chargés depuis les ressources embarquées, et liste les sons déclenchés. **Les sons n'ont pas été écoutés** : le choix et les volumes restent à valider à l'oreille.

## Fait à la main

- Sols (parquet, carrelage, moquette), murs, porte, éclairage de nuit : dessinés par le code de la scène.
- L'ordinateur portable (aucun pack n'en a vu de 3/4).
- Les icônes 12 × 12 (`UI/Pixel/Icons.cs`) : barre d'outils, bulles d'activité au-dessus des personnes.
- Les aperçus de pages de produits (modules simulés : formulaires, grilles, offres...).

## Non utilisés (gardés dans `assets/`)

| Pack | Pourquoi |
|---|---|
| Kenney Roguelike Characters | Personnages fantasy ; ceux de Pixel Office collent au thème |
| Kenney Roguelike Modern City, Tiny City, Tiny Town | Extérieurs : la refonte ne simule pas la ville |
| Kenney Game Icons (+ Expansion), CC0 Document Icons | Vectoriels ou grands : ils jureraient avec les icônes pixel |
| Kenney Particle Pack | Pas du pixel art |
| Kenney Casino Audio | Les cartes du roguelike ont disparu |
| Kenney UI Audio, Interface Sounds, Digital Audio, UI Sounds | Une seule famille de bruitages (rétro 8 bits) |
| 5 Chiptunes Action | WAV lourds, plus énergiques que le ton d'un jeu de gestion |
| Input Prompts génériques et 1-Bit, Kenney Fonts, Silkscreen | Alternatives à ce qui est déjà utilisé |
| Pixel Office : nuages, drapeaux, distributeurs, chat, chien | Pas d'usage dans la nouvelle scène pour l'instant |
