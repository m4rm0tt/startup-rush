# StartupRush — Rapport qualité (QA_REPORT)

Rapport généré par `tools/make_qa_report.py` à partir des résultats réels des contrôles (`tools/qa_check.py`, contrôles des personnages, de recoloration et de reproductibilité).

**Bilan automatique : tous les contrôles passent.**  
Manifeste : 570 entrées, 10 pièces, 6 modules ; 1704 PNG d'assets, 714 masques.

## 1. Contrôles automatiques

| Contrôle | Résultat | Éléments vérifiés | Échecs |
|---|---|---|---|
| IDs uniques | OK | 570 | 0 |
| IDs ASCII snake_case | OK | 570 | 0 |
| Fichiers references presents | OK | 2415 | 0 |
| Noms de fichiers ASCII | OK | 2415 | 0 |
| PNG RGBA 8 bits | OK | 2437 | 0 |
| Bloc sRGB present | OK | 2437 | 0 |
| Decodage PNG | OK | 2437 | 0 |
| Tailles = manifeste (iso) | OK | 1220 | 0 |
| Alpha binaire des sprites (0/255) | OK | 710 | 0 |
| Nettete : aucune couleur hors palette (pas de reechantillonnage) | OK | 710 | 0 |
| Masque meme taille que le sprite | OK | 714 | 0 |
| Masque : alpha 0/255 | OK | 714 | 0 |
| Masque jamais sur la transparence | OK | 714 | 0 |
| Masque jamais sur le contour | OK | 714 | 0 |
| Masque : canaux connus | OK | 714 | 0 |
| Masque : indices 0..5 | OK | 714 | 0 |
| Recoloration par defaut = sprite de base | OK | 714 | 0 |
| Ombres : forme pleine | OK | 478 | 0 |
| Calques de motifs : alpha 0/255 | OK | 16 | 0 |
| HUD : tailles = manifeste | OK | 480 | 0 |
| Curseurs : point chaud dans l'image | OK | 12 | 0 |
| HUD : marges 9-slice coherentes | OK | 107 | 0 |
| Atlas : rectangles dans la page | OK | 2402 | 0 |
| Atlas : pixels identiques aux PNG sources | OK | 2402 | 0 |
| Atlas : sans chevauchement | OK | 21 | 0 |
| Sols : pavage 4x4 sans trou ni recouvrement | OK | 9 | 0 |
| Sols : 1024 px par case | OK | 16 | 0 |
| Murs : toile et ancre communes | OK | 2 | 0 |
| Motifs = surface murale marquee (pas de vitre/plinthe) | OK | 16 | 0 |
| Coiffures alignees sur les 12 bases (crane commun, scalp, meches) | OK | 24 | 0 |
| 10 employes + 2 avatars presents | OK | 12 | 0 |
| Modules de baie dans leur slot (px) | OK | 7360 | 0 |
| Convoyeurs : ports relies dans l'usine | OK | 41 | 0 |
| Plans de pieces valides | OK | 10 | 0 |

## 2. Contrôles spécifiques

- Personnages : 12 bases × 4 orientations, 6 coiffures ; 24 contrôles coiffure × orientation (scalp non couvert, mèches détachées) : 0 échec ; couches de tête différentes du crâne commun : 0.
- Recoloration (tout le pack) : 714 couches masquées, 994351 pixels ; masque sur transparence : 0, sur contour : 0 ; couleurs hors rampe : 0 couche(s) en erreur.
- Reproductibilité (deux constructions completes successives, comparaison SHA-256) : 2441 / 2441 fichiers identiques (0 différent).
- **Portage C# du compositeur** (`tools/csharp/`, .NET 10.0.401, aucune dépendance) : compilation réussie ; rendu des 10 pièces depuis le seul manifeste : **10 / 10 identiques au pixel près** au rendu Python de référence.
- Lot HUD (voir `build_cache/STATUS_ui.md`) : polices rechargées avec fontTools, rendu FreeType 12/24 px identique au pixel à la référence (0 pixel gris), 9-slice testé à 3 tailles sur 19 composants (`previews/ui_nine_slice_test.png`).
- Une rampe par canal et par asset : garantie par le build (`normalize_channels`), les parties secondaires sont signalées dans `fixed_parts` du manifeste.
- Plans de pièces : chaque plan est validé (assets, orientations, sockets, chevauchements, cases d'accès, entrées) avant rendu ; résultat dans `rooms/<id>.json` → `validation`.

## 3. Couverture (COVERAGE.csv)

| Statut | Nombre |
|---|---|
| complete | 268 |
| partial | 0 |
| missing | 0 |
| extra | 1 |

## 4. Contrôles visuels effectués

Les scripts ne jugent pas l'esthétique : chaque planche ci-dessous a été inspectée à l'œil pendant la production (agrandissements au plus proche voisin), et les défauts trouvés ont été corrigés à la source.

| Planche / scène | Fichier(s) |
|---|---|
| Lot temoin (mur, fenetre, motifs par masque, sol, bureau, chaise, PC, employe chauve + coiffure) | `_work (essais), previews/sheet_furniture.png, previews/characters_*.png` |
| Chambre temoin | `previews/rooms/room_bedroom_start_x2.png` |
| Planches de familles | `previews/sheet_<famille>.png (architecture, sols, mobilier, informatique, serveurs, labos, industrie, decoration)` |
| Murs + 8 motifs sur pans reels | `previews/wall_patterns_demo.png` |
| Sols, seuils, bords de dalle | `previews/floors_demo.png` |
| Personnages : bases, alignement des coiffures (face/dos), avatar 2x3x3, recoloration, portraits | `previews/characters_*.png` |
| Recoloration multi-teintes sans duplication | `previews/recolor_demo.png` |
| Baies remplies (slots) | `previews/check_racks.png` |
| Convoyeurs (boucle + ligne) | `previews/check_conveyors.png` |
| Scenes : chambre, petit bureau, bureau moyen, open space, siege, salle serveur, datacenter, atelier, usine, labo | `previews/rooms/*.png, previews/rooms_overview.png` |
| HUD : composants et etats, 9-slice multi-tailles, icones, police, curseurs/VFX, terminal, composants de projet | `previews/ui_*.png` |
| Ecrans de reference du HUD | `previews/screens/*.png` |

Corrections faites suite aux contrôles visuels (extraits) : jambes des personnages vus de face orientées sous le bureau ; fenêtre masquée par une bibliothèque déplacée ; plafonniers retirés des aperçus (lecture ambiguë en coupe) ; occultations corrigées dans l'atelier ; distribution des personnages variée par instance de module ; cloisons posées sur les arêtes pour fermer les angles des salles vitrées ; couche `glass` ajoutée au moteur (contenu visible derrière les vitres) ; moquette en dalles adoucie.

## 5. Ce qui n'a PAS été testé

- **Aucune intégration Avalonia n'a été exécutée** : le jeu n'était pas disponible dans cette session. La composition a été validée avec le compositeur de référence Python (`tools/srgen/scene.py`), qui n'utilise que le manifeste et les fichiers livrés.
- Le chargement des polices TTF a été vérifié avec fontTools et Pillow (voir le lot HUD) ; pas dans Avalonia.
- Le portage C# valide l'algorithme d'assemblage en .NET, pas le rendu par Avalonia/Skia (BGRA prémultiplié, interpolation, mise à l'échelle), qui reste à intégrer et vérifier dans le jeu.
- Les écrans de référence du HUD sont des compositions de test, pas une interface fonctionnelle.
