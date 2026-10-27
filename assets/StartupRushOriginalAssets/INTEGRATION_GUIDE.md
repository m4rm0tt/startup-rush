# StartupRush — Guide d'intégration (C# / Avalonia)

Ce guide suffit pour intégrer le pack sans connaître sa génération. Point d'entrée unique : **`manifest.json`**.
Implémentation de référence exécutée (Python, à porter) : `tools/srgen/scene.py` (composition, tri, ombres),
`tools/srgen/compose.py` (recoloration, motifs), `tools/srgen/rooms.py` (plans de pièces).

> Testé : génération, décodage, composition de scènes complètes avec le compositeur de référence, contrôles
> automatiques (`QA_REPORT.md`). **Non testé : l'exécution dans Avalonia** (le jeu n'a pas été lancé ici).

## 1. Structure de `manifest.json`

| Clé | Contenu |
|---|---|
| `conventions` | unités, projection, orientations, ordre des couches, lumière, opacité d'ombre |
| `palette` | rampes (`ramps`, 6 couleurs hex par rampe) et couleurs fixes |
| `channels` | identifiants de canaux de masque, rampes proposées par canal (`swatches`), encodage du masque |
| `atlas`, `atlas_items` | paramètres d'atlas et table `fichier → {page, rect}` (rect = `[x, y, w, h]`) |
| `categories` | IDs par famille |
| `rooms`, `room_modules` | plans de pièces prédéfinies et modules insérables |
| `assets` | une entrée par asset (voir §3) |

Toutes les positions sont en **pixels natifs** (entiers) ou en **cases** (`*_tiles`) ou en **sous-unités**
(`*_su`, 32 par case), toujours nommées explicitement.

## 2. Repère et projection

```text
case (U, V) -> coin nord à l'écran : X = origineX + (U − V) × 32 ;  Y = origineY + (U + V) × 16
point (u, v, z) en su/px           : X = origineX + (u − v)      ;  Y = origineY + (u + v) / 2 − z
```

U va vers le bas-droite de l'écran, V vers le bas-gauche, z vers le haut. Caméra fixe : zoom par facteur entier,
déplacement libre, **pas de rotation**. Pour placer un sprite : `position_dessin = point_de_référence − anchor`.

## 3. Entrées d'assets

Champs communs : `id`, `category` (famille), `subcategory`, `name_fr`, `usage` (`current` | `future` | `shared`),
`kind`, `channels` (canal → rampe par défaut), `notes`.

### 3.1 Objets isométriques (`kind` = `floor`, `wall`, `ceiling`)

`orientations.<se|sw|nw|ne>` :

| Champ | Sens |
|---|---|
| `size`, `anchor` | taille du PNG et point d'ancrage (px) |
| `layers.base/glass/front/shadow` | fichiers ; `mask` + `mask_channels` pour les couches recolorables ; `atlas` / `mask_atlas` |
| `footprint_world_tiles` | emprise [ΔU, ΔV] dans cette orientation |
| `collision_tiles` | cases occupées, relatives à la case d'origine (coin nord) |
| `access` | cases d'usage à laisser libres + `user_facing` (orientation de l'utilisateur) |
| `selection_poly` | hexagone de sélection (px relatifs à l'ancre) |
| `sockets` | points d'accroche : `offset_px` (relatif à l'ancre), `accepts`, `child_facing`, `draw` |
| `occupant` | chaise : `seat_offset_px`, `character_orientation` |
| `screens` | zone d'affichage : `quad_px` (haut-gauche, haut-droit, bas-droit, bas-gauche), `visible` |
| `slots` | baies : `unit_px`, `count`, `positions[i].offset_px`, `visible` |
| `ports` | convoyeurs/machines : `tile`, `world_dir` (±U/±V), `kind` (`in`/`out`/`inout`) |
| `occlusion` | `fade_above_px` : objet haut qui peut être estompé s'il masque un personnage/curseur |

Hors orientation : `footprint_tiles` (local L × P) ou `size_su` (objet centré), `height_px`, `provides` (types de
sockets acceptés), `sockets_def` (sockets en coordonnées locales), `symmetric`, `walkable`, `fixed_parts`.

**Ancres**
- objet à emprise : ancre = coin nord de l'emprise → dessiner à `case_écran(U0, V0) − anchor` ;
- objet centré posé au sol : `case_écran(U + 0.5, V + 0.5)` arrondi ;
- objet mural (`wall_mount`) : comme la pièce murale de sa case : mur gauche `se` → case (0, j), mur droit `sw` → (i, 0) ;
- plafonnier (`ceiling`) : ancre au sol sous la lampe, l'image est au-dessus (l'ancre peut être hors de l'image).

### 3.2 Architecture

- `wall_piece` : `wall_plain`, `wall_window`, `wall_door`, `wall_opening` (1 case), `wall_window_wide` (2 cases),
  `wall_corner_inner`, `wall_corner_outer`, `wall_end`. Toutes les pièces 1 case d'une orientation partagent la
  **même toile et la même ancre**. Placement : mur gauche segment V=j → `case_écran(0, j) − anchor` ;
  mur droit segment U=i → `case_écran(i, 0) − anchor` ; angle → (0, 0) ; terminaisons → (0, D) et (W, 0).
  Pièce de 2 cases : ancre sur la première case, puis sauter la case suivante.
- `floor_tile` : `variants["u{U mod pu}v{V mod pv}"]`, 64 × 32, ancre (32, 0).
- `floor_edge` (`se` le long de U = W, `sw` le long de V = D) et `floor_edge_corner` à (W, D) (remplace les deux
  derniers modules) ; `floor_threshold` (`along_u`, `along_v`) ; `floor_marking` (calques de case).
- `wall_pattern_overlay` : calques de motifs muraux (voir `COLOR_SYSTEM.md` §5).

### 3.3 Personnages

- `character_base` (12) : couches `body` et `head` par orientation, toile 40 × 72, `seat_anchor` (20, 50),
  `skin_options`, `compatible_hair`, `portrait_crop` [x, y, w, h] sur la vue `se`.
- `character_hair` (6) : couches `hair_front` (+ `hair_back` pour cheveux longs/chignon), même toile, `offset_px`
  [0, 0] pour toutes les bases.
- Composition : `hair_back → body → head → hair_front`, toutes à `chaise + occupant.seat_offset_px − (20, 50)`.
- Recoloration : `skin` (peau du corps et de la tête), `hair` (cheveux + sourcils + barbe), `primary`/`secondary`/
  `accent` (tenue). Le jeu tire coiffure, rampe de cheveux et peau au hasard pour un employé.

## 4. Ordre de rendu

```text
1  sols (cases)
2  bords de dalle, seuils, marquages, tapis (render_layer = floor_decor)
3  couche d'ombre : pour chaque objet, max(alpha) de son PNG *_shadow dans un tampon commun ;
   puis le tampon est dessiné UNE fois en #2b2128 à 30 % d'opacité
4  murs : angle, segments, terminaisons ; puis objets muraux
5  objets au sol, triés (voir ci-dessous) ; pour chaque objet :
     enfants draw=before_parent → base → glass → front → enfants after_parent (triés par u+v du socket)
     postes (draw=workstation) : chaises dont le siège est derrière le centre du parent → avant lui ;
       devant → après lui ; chaise = base → personnage (4 couches) → front
6  plafonniers
```

**Tri des objets au sol** (emprises qui ne se chevauchent pas) : A est derrière B si `A.u1 ≤ B.u0` ou
`A.v1 ≤ B.v0` (bornes en cases, u1/v1 exclusifs). Faire un tri topologique sur cette relation (`topo_sort`
dans `scene.py`, ~30 lignes). Un simple tri par `U + V` échoue avec les objets longs.

## 5. Sockets, slots, ports

- Enfant : `écran_enfant = écran_ancre_parent + socket.offset_px`, dessiné à `écran_enfant − enfant.anchor`,
  dans l'orientation `socket.child_facing`. `accepts` liste les types (`monitor`, `keyboard`, `laptop`,
  `desk_item`, `tower`, `chair`, `bench_item`, `webcam`...) ; un objet déclare les siens dans `provides`.
- Chaise d'un bureau : socket `chair` (`draw = workstation`). Postes multiples : `chair_0..n`, `seat_*`.
- Baie : modules dans `slots.positions[i]` (1U = 4 px ; un module 2U occupe les index i et i+1). Modules visibles
  en `se`/`sw` uniquement (en `nw`/`ne` on voit la porte arrière). Ils ne dépassent jamais de leur enveloppe.
- Convoyeurs : deux modules sont reliés si un port `out` et un port `in`/`inout` sont sur des cases voisines avec
  des directions opposées. Hauteur de bande : 12 px. Les produits se posent sur le socket `belt`.

## 6. Écrans dynamiques

`screens[].quad_px` donne les 4 coins de la dalle (relatifs à l'ancre) dans l'orientation courante. La dalle est un
parallélogramme : mapper une texture d'interface par transformation affine (3 coins suffisent). Ne rien dessiner si
`visible` est faux (dos de l'écran). Le sprite contient un contenu par défaut (code ou interface), canal `screen`.

## 7. Recoloration et motifs

Algorithme exact, encodage des masques et pseudo-code C# : **`COLOR_SYSTEM.md`**. Recolorer au chargement
(ou à la première demande) et mettre en cache par (sprite, swatches).

## 8. Plans de pièces (`rooms/*.json`)

| Clé | Contenu |
|---|---|
| `size_tiles` | [W, D] |
| `floor` | rectangles `[U, V, w, d]` + matériau + swatches (les suivants recouvrent les précédents) |
| `walls.left/right` | `segments` (types par case, `null` = couvert par une pièce de 2 cases), `pattern`, `swatches` |
| `wall_items` | objets muraux (`wall`, `index`) |
| `objects` | `asset`, `tile` [U, V], `facing`, `children` (sockets → asset ou {asset, character, children}), `swatches` |
| `include` | modules insérés (`module`, `offset`, `cast` optionnel = distribution des personnages) |
| `entrances` | portes et case d'accès |
| `zones` | zones nommées (rectangles) |
| `computed` | `occupied_cells`, `reserved_access_cells`, `buildable_cells` (cases où le joueur peut ajouter du mobilier) |
| `validation` | résultat du contrôle (assets, orientations, sockets, chevauchements, accès, entrées) |

Les plans sont **déjà développés** (modules insérés). Les modules restent disponibles dans `rooms/modules/` pour
composer de nouveaux plans. Aperçus : `previews/rooms/<id>.png` (natif) et `_x2.png`.

## 9. Atlas

Pages par famille (≤ 2048 px), padding 2 px, extrusion 1 px, masque dans une page jumelle aux **mêmes
rectangles**. Échantillonner exactement `rect`, au plus proche voisin. Les PNG découpés restent la référence
(l'atlas n'est qu'une forme d'empaquetage). Régénérer : `python tools/pack_atlas.py`.

## 10. Conseils Avalonia (non exécutés ici)

- Pixels nets : `RenderOptions.BitmapInterpolationMode="None"` sur les contrôles qui dessinent le monde et le HUD ;
  zoom entier (×2, ×3), positions arrondies à l'entier après zoom.
- Chargement : `new Bitmap(AssetLoader.Open(uri))` ; recoloration via `WriteableBitmap` (attention : Avalonia stocke
  en BGRA prémultiplié) ou en amont avec SkiaSharp ; mise en cache.
- Ombres : rendre les silhouettes dans un `RenderTargetBitmap` (max d'alpha), puis le dessiner avec `Opacity = 0.3`.
- Police pixel : voir la section HUD/police ci-dessous ; pour un rendu garanti au pixel, l'atlas bitmap de la police
  est fourni en plus des TTF.

## 11. Boîte à outils HUD, police, icônes, curseurs, effets, terminal

Entrées du manifeste de catégories `hud`, `fonts`, `cursors`, `vfx`, `terminal`, `project_components`
(IDs préfixés `hud_`, `icon_`, `font_`, `cursor_`, `vfx_`, `term_`, `comp_`, `port_`, `link_`). Atlas UI propre :
`assets/hud/ui_atlas_0.png` (+ `icons_atlas.png`), rectangles déjà attachés à chaque entrée (`atlas`).

- **Composants 9-slice** : `nine_slice` = [gauche, haut, droite, bas] en px, `min_size`, `content_padding` (zone du
  contenu dynamique), `stretch` (`stretch` ou `tile`), `resize` (`both`, `horizontal`, `vertical`, `none`),
  `states` (état → fichier : `normal`, `hover`, `pressed`, `disabled`, `focus`, `selected`...), `tokens` (couleur
  du texte par jeton du thème). Coins jamais étirés ; bords étirés ou répétés ; centre étiré. Texte et nombres
  rendus par le jeu (aucun texte cuit). Selon le composant : `pressed_content_offset`, `fill_rect`, `title_bar`,
  `slots` (emplacements de texte d'une carte), `scrim` (voile d'un modal).
- **Thème « Papier »** : jetons de couleur dans `hud_theme_papier` / `assets/hud/theme_tokens.json`.
- **Icônes** 16 × 16 blanches : teinter par multiplication avec un jeton (`ink`, `teal`, `success`, `warning`,
  `danger`, `info`, `text_disabled`...). Agrandir par facteur entier seulement.
- **Polices** : `assets/fonts/StartupRushPixel-Regular.ttf` et `StartupRushMono-Regular.ttf` (152 caractères,
  accents français, € et CHF ; grille 12 px : ascendante 10, descendante 2). Utiliser 12, 24 ou 36 px, positions
  entières, sans anticrénelage (`RenderOptions.TextRenderingMode="Alias"`) ; repli garanti au pixel : atlas bitmap
  + métriques JSON (`files.bitmap_atlas`, `files.bitmap_metrics`).
- **Curseurs** : `hotspot` en px ; exports ×2 étiquetés « technique » pour les curseurs système haute densité.
- **Effets** : sprites statiques avec `anchor` et `animation_hint` (l'animation est faite par le code : échelle,
  opacité, montée) ; losanges de case 64 × 32 ancrés comme les sols.
- **Terminal** : cadre 9-slice, surface en cellules 6 × 12 pour la police mono, curseurs, marqueurs de ligne,
  segments de progression. Le contenu (lignes de log) est du texte rendu par le jeu.
- **Composants de projet** : chaque `comp_*` a une icône et un aperçu abstrait 48 × 32 ; la carte `comp_card`
  (9-slice) fournit des emplacements pour nom, état, coût, qualité, exigences ; `port_*` (4 côtés × états) et
  `link_*` (droites, tirets, angles, T, pointes, teintables) dessinent le graphe des dépendances.
- Écrans de référence (tests de complétude, pas des assets) : `previews/screens/`, régénérés par
  `tools/ui_screens.py`.

## 12. Portage C# vérifié

`tools/csharp/StartupRushReference/Program.cs` (un fichier, sans dépendance) implémente en C# le chargement du
manifeste, le PNG, la projection, le tri topologique, les couches, l'ombre, les sockets, les personnages, les modules
de baie, la recoloration et les motifs. `tools/csharp_check.py` le compile et compare ses rendus des 10 pièces aux
rendus Python : résultat dans `QA_REPORT.md` (identiques au pixel près lors de la livraison). C'est une base sûre à
transposer dans la boucle de rendu Avalonia.
