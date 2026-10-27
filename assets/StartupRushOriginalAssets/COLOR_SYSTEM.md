# StartupRush — Système de couleurs, masques et motifs (COLOR_SYSTEM)

Objectif : **une géométrie, plusieurs couleurs**. Aucun meuble n'est exporté quinze fois : chaque sprite recolorable
est livré une fois (couleurs par défaut) avec un **masque de matériau** de mêmes dimensions.

## 1. Rampes

Toutes les couleurs recolorables appartiennent à des **rampes de 6 teintes** (indice 0 = le plus sombre,
5 = le plus clair), définies dans `tools/srgen/palette.py` (`RAMPS`) et exportées dans `manifest.json` (`palette`).
Le relief n'est pas stocké comme une couleur mais comme un **indice de rampe** : changer de rampe conserve exactement
les faces claires/sombres, les arêtes éclairées et les lignes internes.

| Indice | Usage moteur |
|---|---|
| 0 | creux, lignes internes (partie située derrière une autre) |
| 1 | ombre interne, faces très sombres |
| 2 | face droite (+U), côté à l'ombre |
| 3 | face gauche (+V), surface de base des sols et motifs |
| 4 | dessus des volumes |
| 5 | arêtes éclairées, reflets |

## 2. Canaux de matériau (valeur exacte du canal R du masque)

| R | Canal | Exemples |
|---|---|---|
| 0 | `fixed` | non recolorable : contours, LED, livres, contenus d'écran, papier |
| 1 | `primary` | coque d'appareil, caissons, distributeurs |
| 2 | `secondary` | pantalon, plan de travail, éléments secondaires |
| 3 | `wood` | plateaux, portes, lambris |
| 4 | `fabric` | assises, canapés, rideaux, couettes |
| 5 | `metal` | piètements, poignées, structures |
| 6 | `glass` | vitres, vitrines |
| 7 | `screen` | fond d'écran (thème clair/sombre) |
| 8 | `skin` | peau (3 rampes : `skin_light`, `skin_medium`, `skin_deep`) |
| 9 | `hair` | cheveux, sourcils, barbes (5 rampes par défaut) |
| 10 | `accent` | liserés, coussins déco, couvercles |
| 11 | `wall` | surface murale principale (accepte un motif) |
| 12 | `floor` | sol principal |
| 13 | `trim` | plinthes, chambranles, chapeau de mur, encadrements |
| 14 | `wall2` | seconde teinte des motifs muraux |
| 15 | `floor2` | joints, mouchetis, perforations |
| 16 | `plant` | feuillages (réservé) |

Chaque entrée du manifeste liste ses canaux (`channels` : canal → rampe par défaut) et chaque couche ses
`mask_channels`. Tous les objets n'ont pas tous les canaux.

## 3. Format du masque

`masks/<famille>/<id>__<orientation>[__<couche>]__mask.png` — PNG RGBA 8 bits, **mêmes dimensions et même ancre**
que le sprite :

| Composante | Contenu |
|---|---|
| R | identifiant de canal (tableau ci-dessus) |
| G | indice de rampe 0..5 |
| B | drapeaux (bit 0 = surface murale recevant un motif) |
| A | 255 = pixel recolorable ; 0 = garder la couleur du sprite de base |

Valeurs exactes, sans anti-aliasing aux frontières. Les contours, pixels transparents et éléments fixes ont
`A = 0`. Contrôle automatique (`python tools/recolor.py --verify`) : aucun pixel de masque sur un pixel transparent
ou d'encre, chaque couleur de base = teinte `G` d'une rampe connue.

## 4. Algorithme de composition (référence : `tools/srgen/compose.py`)

```text
entrée : sprite RGBA (base), masque RGBA, swatches = { canal -> rampe[6] }
pour chaque pixel p :
    si masque[p].A == 0 : sortie[p] = base[p]                     # contour, transparence, fixe
    sinon :
        c = masque[p].R ; i = masque[p].G
        si c ∈ swatches : sortie[p].rgb = swatches[c][i].rgb ; sortie[p].a = base[p].a
        sinon           : sortie[p] = base[p]                     # canal non personnalisé
```

Propriété garantie : avec les rampes par défaut du manifeste, la sortie est identique au sprite de base.
Le verre semi-transparent garde son alpha (on ne remplace que RGB).

Exemple C# (Avalonia, `WriteableBitmap` en BGRA prémultiplié à convertir selon votre pipeline) :

```csharp
// base, mask : byte[] RGBA de même taille ; ramps : Dictionary<int, uint[6]> (RGBA)
for (int p = 0; p < w * h * 4; p += 4) {
    if (mask[p + 3] == 0) continue;
    int ch = mask[p], idx = Math.Min(5, (int)mask[p + 1]);
    if (!ramps.TryGetValue(ch, out var ramp)) continue;
    uint c = ramp[idx];
    outPx[p] = (byte)(c >> 24); outPx[p + 1] = (byte)(c >> 16); outPx[p + 2] = (byte)(c >> 8);
    // alpha : celui de base[p + 3]
}
```

Mettre en cache le résultat par (sprite, jeu de swatches) : la recoloration se fait au chargement, pas à chaque image.

## 5. Motifs muraux

Les pièces murales marquent leur surface principale (face intérieure) avec le drapeau `B & 1`. Les vitres,
cadres, appuis, plinthes, chambranles, portes et le chapeau du mur n'ont **pas** ce drapeau : aucun motif ne
traverse la vitre ou la plinthe.

Un motif = un calque par orientation, `assets/wall_patterns/pattern_<nom>__<se|sw>.png`, aligné sur l'ancre de la
pièce (toutes les pièces 1 case d'une orientation partagent la même toile et la même ancre) :

| Composante | Contenu |
|---|---|
| R | canal de sortie (11 `wall`, 14 `wall2`, 3 `wood`) |
| G | delta d'indice + 8 (7 = −1, 8 = 0, 9 = +1) |
| A | 255 sur la face murale d'un segment |

```text
pour chaque pixel p de la pièce avec masque.A > 0 et (masque.B & 1) :
    q = p − ancre_pièce + ancre_calque (+ k × (−32, +16) mur gauche / k × (+32, +16) mur droit pour la k-ième case
        d'une pièce de plusieurs cases)
    si calque[q].A > 0 :
        canal = calque[q].R ; indice = clamp(masque.G + calque[q].G − 8, 0, 5)
puis recoloration (§4) avec les swatches de la pièce (wall, wall2, wood, trim, glass...).
```

Motifs livrés (période 32 su, se raccordent d'un segment à l'autre) : `paint` (uni), `two_tone` (soubassement +
cimaise), `wallpaper` (papier discret), `stripes` (bandes), `wood_panels` (lambris), `concrete` (béton banché, trous
de banche), `brick` (brique intérieure, joints en `wall2`), `acoustic` (panneaux perforés). Textures sources à plat :
`sources/wall_patterns/*_flat.png` ; fonctions génératrices : `sources/defs_architecture.py`.

## 6. Sols

Cases de 64 × 32, canal `floor` (+ `floor2` pour joints et mouchetis). Les motifs dont la période dépasse une case
sont livrés en variantes : `variante = (U mod pu, V mod pv)` (`period_tiles` dans le manifeste). Jonctions : barre de
seuil `floor_threshold` (le long de U ou de V), bords avant `floor_edge`, marquages d'atelier `floor_marking`.

## 7. Couleur libre (sélecteur de couleur)

`ramp_from_color(rgb)` (`tools/srgen/palette.py`) construit une rampe de 6 teintes où la couleur choisie devient
l'indice 3, en **OKLCh** :

| Indice | ΔL | × chroma | décalage de teinte |
|---|---|---|---|
| 0 | −0.30 | 0.80 | vers 265° (froid), max 22° |
| 1 | −0.19 | 0.92 | vers 265°, max 12° |
| 2 | −0.09 | 1.00 | vers 265°, max 5° |
| 3 | 0 | 1.00 | — |
| 4 | +0.09 | 0.92 | vers 70° (chaud), max 6° |
| 5 | +0.17 | 0.70 | vers 70°, max 12° |

(Pas de décalage de teinte si la chroma < 0.02 : gris neutres.) Démonstration : chaise violette dans
`previews/recolor_demo.png` (`--color fabric=#7a4fb0`).

## 8. Exemples livrés (validation, pas un produit cartésien)

- `previews/recolor_demo.png` : 6 objets × 3–4 teintes, géométrie unique.
- `previews/characters_recolor.png` et `characters_avatar_matrix.png` : peau × cheveux × tenue sans dupliquer de base.
- Scènes `previews/rooms/` : motifs et teintes différents par pièce à partir des mêmes segments.
