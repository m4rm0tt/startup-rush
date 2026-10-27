# Manifeste de placement du mobilier

Généré à partir de `UI/Assets/Sprites/furniture.json` (écrit par `tools/build_assets.py`) et du catalogue `Core/space/Furniture.cs`. Un test (`Tests/space/AssetManifestTest.cs`) vérifie que les deux concordent.

## Projection

Une seule projection pour toute la scène : **3/4 de dessus, style RPG**. Le sol est une grille de cases de 16 × 16 pixels vue de dessus ; chaque objet est dessiné de face avec son dessus visible. Les packs Kenney Roguelike Indoors et 2dPig Pixel Office sont tous deux dessinés ainsi, à la même densité de pixels.

- **Ancrage** : le bas du sprite est posé sur le bas de son emprise ; horizontalement, le sprite est centré sur son emprise. Un sprite plus haut que son emprise (frigo, personnages, écrans) dépasse vers le haut, sur les cases de derrière, sans les occuper.
- **Ordre de rendu** : tri par la ligne de base (bas de l'emprise, en pixels). À égalité, la chaise passe avant la personne assise, l'ordinateur juste après son bureau. Une personne assise est sur la case derrière son bureau : le bureau, dessiné après, cache le bas de son corps ; une personne qui passe devant le recouvre.
- **Rotation** : aucun sprite de ces packs n'existe en plusieurs orientations cohérentes (chaises de Kenney exceptées, mais pas les bureaux ni les personnages de face). Le jeu ne propose donc pas de rotation ; c'est indiqué dans les limites connues.
- **Collisions** : chaque case de l'emprise est bloquante, sauf pour les ordinateurs (posés sur un bureau, sans emprise au sol). Le pathfinding (largeur d'abord, 4 directions) ne traverse ni meubles ni murs ; on entre et on sort par la porte du mur du bas.
- **Points d'interaction** : un bureau a une **place de chaise** sur la case juste derrière lui (côté mur du fond) ; elle doit rester libre ou porter une chaise. Machine à café, canapés, tableau blanc et bibliothèque ont une **case de façade** juste devant eux (au sud), qui doit rester accessible.

## Objets

| Objet | Nom | Type | Prix | Emprise (cases) | Sprite (px) | Rectangle dans l'atlas | Interaction | Source |
|---|---|---|---|---|---|---|---|---|
| `desk` | Bureau simple | Desk | 180 CHF | 2 × 1 | 32 × 16 | 1, 1, 32, 16 | place de chaise (x, y−1) | Kenney Roguelike Indoors, tuiles (4,3) (5,3) |
| `desk_pro` | Bureau large | Desk | 460 CHF | 2 × 1 | 32 × 16 | 1, 18, 32, 16 | place de chaise (x, y−1) | Kenney Roguelike Indoors, tuiles (5,5) (6,5) |
| `chair` | Chaise | Chair | 60 CHF | 1 × 1 | 16 × 16 | 1, 35, 16, 16 | — | Kenney Roguelike Indoors, tuile (0,2) |
| `chair_ergo` | Chaise ergonomique | Chair | 390 CHF | 1 × 1 | 16 × 16 | 1, 52, 16, 16 | — | Kenney Roguelike Indoors, tuile (0,8) |
| `laptop` | Vieux portable | Computer | 0 CHF | 1 × 1 | 14 × 9 | 1, 69, 14, 9 | posé sur un bureau | Dessin original (aucun portable dans les packs) |
| `pc` | PC fixe | Computer | 1 250 CHF | 1 × 1 | 19 × 23 | 1, 79, 19, 23 | posé sur un bureau | 2dPig Pixel Office, rectangle 231,104,19,23 |
| `workstation` | Station de travail | Computer | 3 100 CHF | 1 × 1 | 28 × 23 | 1, 103, 28, 23 | posé sur un bureau | 2dPig Pixel Office, 231,104,19,23 + unité centrale 233,135,9,17 |
| `plant` | Plante | Plant | 45 CHF | 1 × 1 | 16 × 16 | 1, 127, 16, 16 | — | Kenney Roguelike Indoors, tuile (16,0) |
| `coffee` | Machine à café | Coffee | 650 CHF | 1 × 1 | 16 × 23 | 1, 144, 16, 23 | case de façade | Kenney (1,12) + machine à café Pixel Office 159,91,7,11 |
| `sofa` | Canapé | Sofa | 900 CHF | 2 × 1 | 33 × 16 | 1, 168, 33, 16 | case de façade | 2dPig Pixel Office, rectangle 120,121,33,16 |
| `whiteboard` | Tableau blanc | Whiteboard | 220 CHF | 2 × 1 | 26 × 20 | 1, 185, 26, 20 | case de façade | 2dPig Pixel Office, rectangle 84,70,26,20 |
| `bookshelf` | Bibliothèque technique | Bookshelf | 320 CHF | 1 × 1 | 16 × 16 | 1, 206, 16, 16 | case de façade | Kenney Roguelike Indoors, tuile (20,17) |
| `rug` | Tapis | Decor | 120 CHF | 2 × 2 | 32 × 32 | 1, 223, 32, 32 | — | Kenney Roguelike Indoors, coins du tapis (16..18, 8..9) |
| `bed` | Lit | Fixture | 0 CHF | 2 × 1 | 32 × 16 | 1, 256, 32, 16 | — | Kenney Roguelike Indoors, tuiles (13,1) (12,1) |
| `kitchen` | Cuisine | Fixture | 0 CHF | 3 × 1 | 48 × 16 | 1, 273, 48, 16 | — | Kenney Roguelike Indoors, tuiles (0,12) (8,12) (1,12) |
| `fridge` | Frigo | Fixture | 0 CHF | 1 × 1 | 16 × 32 | 1, 290, 16, 32 | — | Kenney Roguelike Indoors, tuiles (11,15) (11,16) |
| `home_sofa` | Canapé du salon | Fixture | 0 CHF | 2 × 1 | 33 × 15 | 1, 323, 33, 15 | case de façade (pause) | 2dPig Pixel Office, rectangle 119,66,33,15 |
| `dining` | Table à manger | Fixture | 0 CHF | 2 × 2 | 32 × 32 | 1, 339, 32, 32 | — | Kenney Roguelike Indoors, tuiles (0..2, 0..1) |
| `kitchenette` | Kitchenette | Fixture | 0 CHF | 2 × 1 | 32 × 16 | 1, 372, 32, 16 | — | Kenney Roguelike Indoors, tuiles (8,12) (0,12) |
| `window` | Décor du mur du fond | — | — | — | 26 × 22 | 1, 389, 26, 22 | — | 2dPig Pixel Office, rectangle 59,96,26,22 |
| `person0` | Personnage (apparence) | — | — | — | 15 × 23 | 1, 412, 15, 23 | — | 2dPig Pixel Office, rectangle 2,105,15,23 |
| `person1` | Personnage (apparence) | — | — | — | 19 × 24 | 1, 436, 19, 24 | — | 2dPig Pixel Office, rectangle 19,104,19,24 |
| `person2` | Personnage (apparence) | — | — | — | 13 × 21 | 1, 461, 13, 21 | — | 2dPig Pixel Office, rectangle 40,107,13,21 |
| `person3` | Personnage (apparence) | — | — | — | 17 × 23 | 1, 483, 17, 23 | — | 2dPig Pixel Office, rectangle 3,132,17,23 |
| `person4` | Personnage (apparence) | — | — | — | 17 × 23 | 1, 507, 17, 23 | — | 2dPig Pixel Office, rectangle 22,132,17,23 |

Les rectangles des tuiles Kenney sont calculés à partir des métadonnées du pack (`tilesheetInfo.txt` : tuiles de 16 px, marge de 1 px). Les originaux de `assets/` ne sont jamais modifiés : l'atlas est un dérivé recoloré en Endesga 32.
