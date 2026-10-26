# Utilisation des assets

Ce document dit ce que le jeu utilise vraiment dans le dossier `assets/` (les packs originaux, jamais modifiés), comment ces fichiers arrivent dans le jeu, et ce qui reste dessiné à la main.

- **Originaux** : `assets/` à la racine. Le dossier n'est pas versionné dans git (176 Mo). Les licences et `SOURCE.json` de chaque pack y restent.
- **Dérivés** : `UI/Assets/`. Ils sont produits par `python tools/build_assets.py`, qui donne toujours les mêmes fichiers. Le jeu ne lit que ce dossier, embarqué dans `UI.dll` comme ressource Avalonia : aucun chemin absolu, rien à copier à côté de l'exécutable.
- **Chargement** : chaque planche, chaque son et chaque musique est décodé une seule fois. Les sprites découpés sont gardés en cache (`UI/Pixel/Sprites.cs`, `UI/Audio/Sfx.cs`).

## Grilles et échelles

| Élément | Grille | Échelle à l'écran |
|---|---|---|
| Scène centrale | Dessin logique de 560 × 340, rendu en 280 × 170 pixels réels, puis recoloré en Endesga 32 avec un tramage de Bayer | Agrandie au plus proche voisin |
| Sprites Pixel Office dans la scène | 1 pixel de sprite = 1 pixel de la scène rendue (2 unités logiques), positions arrondies à la grille | Même densité que le reste de la pièce |
| Cadres 9-slice | Images de 48 × 48, bords de 6 px | 1 pixel du cadre = 2 unités d'interface (référence 1600 × 900) |
| Glyphes de touches | Tuiles de 16 px (Entrée : 32 × 32) | 32 unités par touche (Entrée : 32 unités, soit 1 unité par pixel) |

L'interface est conçue en 1600 × 900 et mise à l'échelle dans un `Viewbox`. Les images sont affichées sans lissage (`BitmapInterpolationMode.None`).

## Manifeste

### Sprites : 2dPig Pixel Office (CC0)

- **Source** : `assets/2dpig/pixel-office/PixelOfficeAssets.png`.
- **Dérivé** : `UI/Assets/Sprites/office.png`, avec trois modifications :
  - **Recoloration** du Sweetie 16 d'origine vers la couleur Endesga la plus proche.
  - **Teintes corrigées à la main** : peau, ombre de peau et deux verts, qui partaient vers le jaune.
  - **Ciel des nuages** rendu transparent, et **2e frame du chat** dessinée dans une zone vide de la planche.

Rectangles : x, y, largeur, hauteur en pixels de la planche.

| Identifiant | Rectangle | Rôle dans le jeu |
|---|---|---|
| `clouds` | 0, 0, 256, 36 | Nuages devant le ciel de la fenêtre (logement ≥ 1) |
| `table` | 85, 47, 26, 16 | Table de chaque employé |
| `plant` | 170, 65, 14, 19 | Plantes en pot (appartement et plus) |
| `sofa0` à `sofa3` | 119/120, 66–121, 33, 15–16 | Canapé du loft (gris), de la maison (bleu), de la villa (vert), du penthouse et de l'île (orange) |
| `whiteboard` | 84, 70, 26, 20 | Tableau blanc, locaux niveau 1 et plus |
| `calendar` | 234, 81, 17, 11 | Calendrier, dès le statut Fondateur |
| `coffeeMachine` | 159, 91, 7, 11 | Machine à expresso (café niveau 2 et plus) |
| `mug` | 169, 95, 8, 7 | Tasse (café niveau 1 et plus) |
| `clock` | 159, 108, 19, 6 | Horloge murale (logement ≥ 1) |
| `printer` | 233, 106, 15, 19 | Imprimante, locaux niveau 2 et plus |
| `vending` | 159, 123, 24, 34 | Distributeur, dès le statut Patron |
| `drinks` | 184, 126, 24, 31 | Frigo à boissons du « Barista personnel » |
| `cat0`, `cat1` | 65, 129, 16, 13 et 82, 129, 16, 13 | Atout « Chat de bureau » : 2 frames, queue relevée 0,5 s toutes les 2,4 s |
| `dog` | 59, 146, 24, 11 | Chien couché, maison avec jardin et plus |
| `cooler` | 147, 140, 9, 17 | Fontaine à eau, locaux niveau 3 et plus |
| `employee0` à `employee4` | personnages de la planche | Employés : un par employé recruté, jusqu'à 6 dessinés, puis « +N » |
| `bin`, `binderRed/Blue/Green` | | Déclarés, non placés pour l'instant |

Ancrage : les objets posés au sol ont leur bas sur la ligne du sol (y = 250). Les employés ont leur bas à y = 318, derrière leur table.

### Cadres d'interface : Kenney Pixel UI Pack (CC0)

- **Source** : `assets/kenney/pixel-ui-pack/9-Slice/Colored/{grey, grey_pressed, yellow, yellow_pressed, green, green_pressed, red, red_pressed}.png`.
- **Dérivés** : `UI/Assets/Ui/skins/*.png`. Le dessin est gardé (contour, biseau, ombre du bouton) et ses 4 teintes sont remplacées par la palette du jeu.
- **Une seule famille** de cadres, « Colored », sert pour tout le HUD.

| Cadre | Origine | Usage |
|---|---|---|
| `panel` | grey | Tous les panneaux (classe `panel`) |
| `inset` | grey_pressed | Zones en creux (classe `inset`) |
| `button`, `button_hover`, `button_pressed`, `button_disabled` | grey / grey_pressed | Boutons normaux et leurs états |
| `gold*` | yellow | Bouton principal, onglet choisi |
| `green*`, `red*` | green / red | Accepter / refuser, abandonner |

### Glyphes de touches : Kenney Input Prompts Pixel (CC0)

- **Fichier** : `assets/kenney/input-prompts-pixel/Tilemap/tilemap_packed.png`, copié tel quel dans `UI/Assets/Ui/input_prompts.png`.
- **Tuiles** de 16 px, 34 par rangée.

| Touche | Tuile | Où |
|---|---|---|
| Échap | 17 | Pause, retours, menu de fin |
| F11 | 28 | Légende de la pause |
| 1 à 4 | 51–54 | Cartes d'atout, choix d'événement |
| R | 88 | Relancer le tirage |
| Espace | 153 | Indice de la scène, légende |
| Entrée | 100 (2 × 2 tuiles) | Intro, événement, fin de run |
| Main (clic) | 578 | Indice de la scène |

### Bruitages

**Source principale** : *The Essential Retro Video Game Sound Effects Collection* de Juhani Junkala (CC0), dans `assets/opengameart/512-sound-effects-8-bit-style/…`. Tous ces fichiers sont en mono, 44,1 kHz, 16 bits.

**Cartes** : Kenney Casino Audio (CC0).

Je ne les ai pas écoutés. Je les ai choisis par catégorie et par nom, puis mesurés : durée, crête et niveau moyen. Le jeu ramène chaque son à un niveau moyen commun sans dépasser la crête, puis applique un volume relatif (voir `Sfx.cs`).

| Fichier du jeu | Original | Durée | Moment |
|---|---|---|---|
| `work.wav` | Menu Sounds/sfx_menu_move1 | 0,04 s | Chaque clic de travail (très discret) |
| `hover.wav` | Menu Sounds/sfx_menu_move2 | 0,06 s | Décompte des lingots |
| `select.wav` | Menu Sounds/sfx_menu_select1 | 0,35 s | Choix dans les menus |
| `offer.wav` | Simple Bleeps/sfx_sounds_Blip7 | 0,05 s | Nouvelle offre de contrat |
| `coin.wav` | Coins/sfx_coin_single1 | 0,42 s | Contrat livré |
| `cash.wav` | Coins/sfx_coin_cluster3 | 0,47 s | Gros gain, achat d'Héritage |
| `buy.wav` | Coins/sfx_coin_double4 | 0,26 s | Achat en boutique |
| `bill.wav` | Coins/sfx_coin_double6 | 0,27 s | Facture payée |
| `error.wav` | Negative Sounds/sfx_sounds_error3 | 0,21 s | Achat impossible, contrat raté |
| `crit.wav` | Positive Sounds/sfx_sounds_powerup15 | 0,19 s | Coup de génie |
| `levelup.wav` | Fanfares/sfx_sounds_fanfare1 | 0,52 s | Nouveau statut, début de run |
| `victory.wav` | Fanfares/sfx_sounds_fanfare3 | 0,29 s | Victoire |
| `alarm.wav` | Alarms/sfx_alarm_loop1 (joué une fois) | 0,79 s | Début de crise |
| `unpaid.wav` | Negative Sounds/sfx_sounds_negative2 | 0,76 s | Facture impayée |
| `defeat.wav` | Falling Sounds/sfx_sounds_falling2 | 0,90 s | Faillite |
| `event.wav` | Neutral Sounds/sfx_sound_neutral6 | 0,38 s | Événement à choix |
| `pause_in.wav`, `pause_out.wav` | Pause Sounds/sfx_sounds_pause1_in/out | | Entrée et sortie de pause |
| `card_fan.ogg` | Casino Audio/card-fan-1 | | Tirage d'atouts qui s'ouvre |
| `card_place.ogg` | Casino Audio/card-place-1 | | Atout choisi |
| `card_shuffle.ogg` | Casino Audio/card-shuffle | | Relance du tirage |

**Garde-fous** :
- 10 sons au plus en même temps.
- Un même son ne se relance pas à moins de 45 ms d'intervalle.
- Saturation douce (tanh) au lieu d'un écrêtage sec.

### Musiques : Juhani Junkala, 4 Chiptunes Adventure (CC0)

Ce sont les versions OGG. Les WAV du même pack ont servi à mesurer chaque morceau : ce sont des boucles courtes qui commencent et finissent en pleine note, donc faites pour boucler.

| Fichier du jeu | Original | Durée | Moment |
|---|---|---|---|
| `menu.ogg` | 4. Stage Select | 21 s | Menus et écran de fin |
| `run1.ogg`, `run2.ogg` | 1. Stage 1, 2. Stage 2 | 41 s, 56 s | Pendant la run |
| `crisis.ogg` | 3. Boss Fight | 72 s | Pendant une crise |

**Lecture** :
- Les deux thèmes de run alternent toutes les deux boucles, pour qu'une run de 35 minutes ne répète pas un seul morceau de 41 s.
- Chaque changement se fait en fondu enchaîné de 1,5 s.
- Les niveaux sont alignés sur une même valeur moyenne sans dépasser la crête.

### Polices (SIL OFL 1.1)

| Police | Usage |
|---|---|
| Press Start 2P (`assets/fonts/pressstart2p`) | Logo, « PAUSE », « CHOISIS TON FONDATEUR » (classe `title`). Elle dessine ses majuscules accentuées en petit (« HéRITAGE »), donc elle ne sert pas aux titres qui en contiennent |
| Pixelify Sans | Textes de l'interface (déjà dans le jeu, même famille que la version fournie) |
| VT323 | Chiffres et journal |

Les accents français s'affichent bien dans le jeu : vérifié sur les captures. Les fichiers OFL sont livrés dans `Assets/Fonts/`.

## Fait à la main (aucun pack ne le fournit)

- **Le héros vu de dos et la pièce de face** : ses bras qui tapent, qui se lèvent quand un contrat est livré ou une facture payée, et la goutte de sueur en crise ou dans le rouge.
- **Le matériel** : ordinateurs, écrans et mur d'écrans, baies de serveurs, ordinateur quantique, chaises, fenêtres et vues de chaque logement (dont l'île privée), pluie, lumière d'alarme, couronne et trône du Roi du monde. Tout est dessiné en formes simples dans `SceneView.axaml.cs`, puis passé en pixel art par le rendu.
- **La box Internet** : plus de voyants et d'antennes à chaque niveau, et une parabole pour le satellite.
- **Les portraits** des quatre fondateurs et les 46 icônes en pixel art (`Portraits.cs`, `Icons.cs`).
- **Les particules** de pièces et les textes flottants.

## Non utilisés (gardés en réserve dans `assets/`)

| Pack | Pourquoi |
|---|---|
| Kenney Roguelike Indoors / Modern City / Tiny City / Tiny Town | Vus de dessus : ils changeraient la caméra frontale de la scène |
| Kenney Roguelike Characters, RPG portraits | Style fantasy ou vue de dessus ; les employés viennent de Pixel Office |
| Office objects, Office Stuff, Old Computer Hardware | Doublons de Pixel Office, ou objets avec de vraies marques |
| Kenney Game Icons (+ Expansion), CC0 Document Icons | Vectoriels ou de grande taille : ils jureraient avec les icônes pixel du jeu |
| Kenney Particle Pack | Pas du pixel art ; les particules restent des pixels dessinés |
| Kenney UI Audio, Interface Sounds, Digital Audio, UI Sounds (HaelDB) | Une seule famille de bruitages (rétro 8 bits) pour rester cohérent |
| 5 Chiptunes Action | En WAV uniquement (≈ 50 Mo) et plus énergiques ; le pack Adventure couvre menu, run et crise |
| Input Prompts (générique et 1-Bit), Kenney Fonts, Silkscreen | Alternatives à ce qui est déjà utilisé |
| Pixel Office : chaises de face, drapeaux, posters, fenêtres, vitre bleue | Pas cohérents avec la scène (chaise vue de dos, drapeaux nationaux) |

Les scènes d'exemple et les images d'aperçu des packs (`Preview.png`, `PixelOffice.png`, logos Kenney, fichiers `.url`) ne sont pas dans le jeu.
