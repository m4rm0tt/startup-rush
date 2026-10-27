# StartupRush — Contrat graphique (STYLE_GUIDE)

Version du pack : **1.0.0**. Ce document est le contrat unique appliqué à toute la bibliothèque. Les valeurs
ci-dessous sont celles réellement implémentées dans le générateur (`tools/srgen/`) et vérifiées par le build.

## 1. Intention artistique

- Une startup qui commence petit : **chaleureuse, optimiste, crédible**, touches d'humour discrètes.
- Pixel art **net et lisible** : silhouettes travaillées, détails intentionnels (tiroirs, LED, aérations, câbles),
  matières simples, lumière cohérente. Inspiration d'intention seulement (charme des personnages « à la Game Dev
  Tycoon », variété d'équipements « à la Startup Company ») : **aucun dessin repris**.
- Interdits : flou, peinture numérique, photoréalisme, voxel 3D approximatif, rectangles indifférenciés, RGB
  partout, cyberpunk dystopique, cristaux/néons « magiques », logos ou marques, personnes réelles, franchises.
- La tech futuriste (quantique, fabrication) **prolonge la même DA** : mêmes contours, même palette, mêmes faces.

## 2. Projection (contrat de grille)

| Élément | Valeur |
|---|---|
| Vue | trois quarts axonométrique 2:1, caméra fixe (déplacement/zoom autorisés, **aucune rotation**) |
| Case de sol | losange **64 × 32 px natifs** |
| Unités | 1 case = **32 sous-unités (su)** sur U et V ; hauteur `z` en **px** |
| Projection (cases) | `screen_x = (U − V) × 32` ; `screen_y = (U + V) × 16 − z_px` |
| Projection (su) | `screen_x = u − v` ; `screen_y = (u + v) / 2 − z` |
| Axe U | vers le **bas-droite** de l'écran (face +U = face droite visible) |
| Axe V | vers le **bas-gauche** de l'écran (face +V = face gauche visible) |
| Axe z | vers le haut |
| Origine d'une pièce | coin nord de la case (U=0, V=0) au sol (z=0) ; l'intégrateur choisit sa position écran |
| Échantillonnage | centres de pixels ; intervalles semi-ouverts → les cases et modules se raboutent sans trou ni recouvrement |

Les dimensions d'un PNG ne sont **pas** la collision : l'emprise au sol est donnée en cases dans le manifeste.

### Ordre de rendu (peintre)

1. Sols (cases) → 2. bords de dalle, seuils, marquages, tapis (`floor_decor`) → 3. **couche d'ombre unique** →
4. murs du fond (angle, segments, terminaisons) puis objets muraux → 5. objets au sol triés par **tri topologique
des emprises** (A est derrière B si `A.u1 ≤ B.u0` ou `A.v1 ≤ B.v0`), puis 6. objets suspendus (plafonniers).

Dans un groupe : enfants `before_parent` (tour sous un bureau) → parent → enfants triés par profondeur monde
(`u + v` du socket) ; poste de travail : chaise (couche `base`) → personnage → chaise (couche `front`).
Implémentation de référence : `tools/srgen/scene.py` (`topo_sort`, `Scene.render`).

## 3. Échelle de référence (px natifs)

| Référence | Valeur |
|---|---|
| Mur (hauteur totale) | **112 px**, épaisseur 8 su (à l'extérieur de la pièce), plinthe 6 px |
| Fenêtre | allège 34 px → linteau 90 px ; vitre stylisée (ciel + reflets, aucun décor extérieur) |
| Porte / passage | 84 px de haut, 24 su de large |
| Plateau de bureau | **30 px** ; plan de cuisine 31 px |
| Assise de chaise | **19 px** (point d'occupation de la chaise) |
| Personnage assis | toile 40 × 72, ~64 px des pieds au sommet du crâne, tête ~20 px (proportion tête/corps chibi ~1/3) |
| Écran standard | dalle 22 × 14 px |
| Baie standard | 1 case, ~100 px, 20 unités de 4 px (voir manifeste `slots`) |
| Armoire / distributeur | 76–80 px |

Taille de case ≈ 1 m de jeu (stylisé) ; un cube d'une case aurait ~39 px de haut.

## 4. Orientations

`facing` = direction vers laquelle regarde la **face avant** de l'objet :
`se` (avant → +U, visible), `sw` (avant → +V, visible), `nw` (avant → −U, dos visible), `ne` (avant → −V, dos visible).

- Objets adossés à un mur : **2 orientations** (`se` = mur gauche, `sw` = mur droit).
- Objets qui peuvent tourner le dos à la caméra (bureaux, chaises, canapés, baies en rangées) : **4 orientations**.
- Objets symétriques (tables rondes, poubelle, plantes) : **1 orientation** (`symmetric: true`).
- Les objets ne sont **jamais** obtenus par miroir : chaque orientation est rendue depuis le même modèle 3D
  local, avec la même lumière monde → textes, branchements et écrans ne sont jamais inversés.
- Personnages : vues `se` (face) et `nw` (dos) dessinées ; `sw` et `ne` sont des miroirs **dont l'ombrage est
  recalculé après miroir** (la lumière reste à gauche de l'écran).

## 5. Lumière, faces, ombres

- Lumière principale constante venant du **haut-gauche de l'écran** (vecteur monde vers la lumière
  `(−0.25, 0.45, 1)` normalisé).
- Indices de rampe (0 sombre → 5 clair) : dessus = 4, face gauche (+V) = 3, face droite (+U) = 2, creux = 0–1,
  arêtes éclairées = +1 (arête avant du dessus, arête verticale avant).
- **Ombres en couche séparée** : `<id>__<orientation>__shadow.png`, forme pleine (couleur `#2b2128`, alpha 255),
  même toile et même ancre que le sprite. Les ombres de tous les objets sont accumulées dans un tampon en **max**
  (pas d'addition), puis la couche est composée **une seule fois à 30 % d'opacité** au-dessus des sols et sous les
  murs/objets → aucun cumul aux recouvrements. Direction : vers la droite de l'écran, longueur stylisée courte.
- Objets muraux et suspendus : pas d'ombre au sol.

## 6. Contours

- Une seule encre : **`#2b2128`** (brun-prune chaud).
- Contour extérieur 1 px, 4-connexe, **à l'extérieur** de la forme (le sprite dépasse de 1 px de son volume).
- Lignes internes : quand une partie passe devant une autre, c'est la partie **arrière** qui reçoit un trait
  sombre (indice −2) : les détails fins (pieds, poignées) gardent leurs pixels.
- Les surfaces qui se raboutent (sols, murs) n'ont pas de contour aux jonctions (rendu en tranches d'une scène
  continue, voir `tools/srgen/slicer.py`).

## 7. Palette « Sunrise Desk »

Palette originale de 38 rampes × 6 teintes + couleurs fixes, définie dans `tools/srgen/palette.py` et documentée
dans `COLOR_SYSTEM.md`. Neutres chauds (`cream`, `sand`, `kraft`), bois (`oak`, `walnut`, `birch`), gris
informatiques (`steel`, `graphite`, `plastic`, `beige_pc`), accents mesurés (`teal`, `coral`, `sun`, `leaf`,
`sky`, `sage`, `rose`, `navy`, `red`), matières techniques (`screen`, `display`, `glass`, `copper`, `brass`,
`brick`, `concrete`, `rubber`, `hazard`), 3 peaux, 5 cheveux. Décalage de teinte : ombres vers le froid, lumières
vers le chaud. Aucune reprise d'Endesga 32 ni d'un thème noir/violet.

## 8. Ancres, emprise, collision, interactions

| Type | Ancre (point du PNG à placer sur la position écran indiquée) |
|---|---|
| Objet au sol à emprise | coin **nord** de l'emprise (U min, V min), z = 0 |
| Objet centré posé au sol | centre de la case |
| Objet posé dans un socket | son origine locale (centre de sa base) sur le point `offset_px` du socket parent |
| Pièce murale | coin intérieur bas de la case de mur : (U=0, V=j) mur gauche `se`, (U=i, V=0) mur droit `sw` |
| Objet mural | même ancre que la pièce murale de sa case |
| Case de sol | (32, 0) = coin nord |
| Personnage | point d'assise (20, 50) placé sur `occupant.seat_offset_px` de la chaise |

Le manifeste donne aussi, par orientation : `footprint_world_tiles`, `collision_tiles`, `access` (cases d'usage et
orientation de l'utilisateur), `sockets` (offset px, orientation de l'enfant, ordre de dessin), `occupant`,
`screens` (quadrilatère d'affichage), `selection_poly`, `slots`, `ports`, `occlusion`.

## 9. Fichiers

- PNG **RGBA 8 bits, alpha réel, sRGB** (bloc sRGB), aucun damier/halo/magenta, aucune interpolation.
- Zoom au plus proche voisin, positions entières. Les aperçus agrandis (`previews/`) ne sont pas des textures.
- Noms ASCII `snake_case` ; `<id>__<orientation>[__<couche>].png` ; masques dans `masks/` avec suffixe `__mask`.
- Couches possibles : `base`, `front` (occultant au premier plan), `shadow`, `body`/`head`/`hair_front`/`hair_back`
  (personnages), `overlay` (motifs muraux).

## 10. Personnages

- 2 avatars joueur + 10 employés (5 par présentation), tous **chauves** en base, mêmes proportions, même crâne
  par vue → les 6 coiffures s'alignent sans décalage (contrôle automatique dans le build).
- Différences lisibles : visage (yeux, sourcils, bouche, nez, lunettes, barbe, taches de rousseur...), carrure
  (fine/moyenne/large, variantes féminines), tenue (sweat, chemise-cravate, t-shirt, cardigan, chemise à
  carreaux, polo, chemisier, col roulé, blazer, veste en jean, veste ouverte).
- Couches : `hair_back → body → head → hair_front` ; la chaise appartient au mobilier.
- Statiques, assis : aucune animation de marche, frappe ou émotion.

## 11. Critère pour créer un nouveau modèle (§7G)

Nouveau sprite seulement si **silhouette, emprise, usage visible, matériaux ou point d'interaction** changent.
Couleur, motif, nom commercial fictif ou bonus de performance → recoloration ou données. Amélioration visible
(2ᵉ écran, module de baie, carte GPU) → pièce séparée posée dans un socket/slot, la base reste identique.

## 12. Liste de contrôle qualité (appliquée par lot)

1. Silhouette identifiable sans couleur ; 2. aucune arête floue, alignement pixel ; 3. lumière cohérente ;
4. contour unique, lignes internes côté arrière ; 5. masques sans contamination (contours, transparence,
éléments fixes à 0) ; 6. recoloration par défaut = sprite de base (vérifié) ; 7. ancre et emprise vérifiées sur
planche (losange + croix) ; 8. composition réelle dans une scène.
