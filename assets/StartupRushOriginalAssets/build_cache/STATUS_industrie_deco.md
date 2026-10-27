# STATUS — lot industrie future + decoration (lot_industrie_deco)

Date : 2026-10-27. Sources : `sources/defs_industry.py`, `sources/defs_decoration.py`, aides `tools/srgen/lot_ind_helpers.py` (nouveau module, aucun fichier partage modifie).


## Famille `industry_future` — 32 IDs livres

| id | orientations | couches | canaux (rampe par defaut) | ports / sockets |
|---|---|---|---|---|
| conveyor_straight | se/sw | base+shadow | metal:steel, secondary:rubber | ports in(in), out(out); sockets belt |
| conveyor_curve | se/sw/nw/ne | base+shadow | secondary:rubber, metal:steel | ports a(inout), b(inout); sockets belt |
| conveyor_junction_t | se/sw/nw/ne | base+shadow | metal:steel, secondary:rubber | ports in_main(in), in_side(in), out(out); sockets belt |
| conveyor_end | se/sw/nw/ne | base+shadow | metal:steel, secondary:rubber, accent:hazard | ports in(in); sockets belt |
| conveyor_transfer_table | se/sw | base+shadow | metal:steel, secondary:rubber, accent:hazard | ports back(inout), front(inout), left(inout), right(inout); sockets belt |
| assembly_station_manual | se/sw/nw/ne | base+shadow | metal:steel, secondary:teal, primary:plastic, screen:display | ports in(in), out(out); sockets chair, bench_left, bench_center, bench_right |
| assembly_cell_robotic | se/sw | base+shadow | accent:hazard, secondary:rubber, metal:steel, primary:steel, screen:display | ports in(in), out(out); sockets belt |
| pick_and_place_machine | se/sw | base+shadow | primary:plastic, accent:teal, glass:glass, secondary:rubber, metal:steel, screen:display | ports in(in), out(out) |
| reflow_oven | se/sw | base+shadow | primary:plastic, accent:teal, metal:steel, screen:display, secondary:rubber | ports in(in), out(out) |
| aoi_inspection | se/sw | base+shadow | primary:plastic, glass:glass, accent:teal, metal:steel, screen:display, secondary:rubber | ports in(in), out(out) |
| pcb_tester | se/sw | base+shadow | primary:plastic, metal:steel, glass:glass, secondary:rubber, screen:display | ports in(in), out(out) |
| chip_fab_tool | se/sw | base+shadow | primary:plastic, accent:teal, metal:steel, screen:display, secondary:steel | ports load_a(inout), load_b(inout) |
| precision_station | se/sw | base+shadow | metal:steel, glass:glass, screen:display | sockets stage |
| flash_station | se/sw/nw/ne | base+shadow | metal:steel, secondary:steel, primary:plastic, screen:display | ports in(in), out(out); sockets chair |
| stress_test_rack | se/sw | base+shadow | metal:steel, secondary:steel | - |
| packing_station | se/sw | base+shadow | metal:steel, wood:birch, primary:kraft, secondary:plastic | ports in(in), out(out); sockets table_center |
| storage_rack_industrial | se/sw | base+shadow | metal:navy, accent:coral, wood:birch | sockets level_0, level_1, level_2 |
| pallet | se/sw | base+shadow | wood:birch | sockets load |
| tote_bin | se/sw | base+shadow | primary:teal | - |
| dust_extraction | se/sw | base+shadow | primary:plastic, metal:steel, accent:teal | - |
| industrial_chiller | se/sw | base+shadow | primary:plastic, secondary:graphite, screen:display, accent:teal, metal:steel | - |
| control_cabinet | se/sw | base+shadow | primary:steel, screen:display, metal:steel | - |
| cleanroom_airlock | se/sw | base+shadow | primary:plastic, secondary:steel, glass:glass, metal:steel | - |
| cleanroom_partition | se/sw | base+shadow | secondary:steel, primary:plastic, glass:glass | - |
| product_pcb | se/sw | base+shadow | - | - |
| product_cpu | se | base+shadow | metal:steel | - |
| product_ram | se/sw | base+shadow | - | - |
| product_gpu | se/sw | base+shadow | primary:graphite, accent:teal, metal:steel | - |
| product_case | se/sw | base+shadow | primary:graphite | - |
| product_pc_assembled | se/sw | base+shadow | primary:graphite, glass:glass | - |
| product_box | se/sw | base+shadow | primary:kraft | - |
| product_wafer | se | base+shadow | - | - |

## Famille `decoration` — 25 IDs livres

| id | orientations | couches | canaux (rampe par defaut) | ports / sockets |
|---|---|---|---|---|
| plant_floor_medium | se | base+shadow | primary:brick, plant:leaf | - |
| plant_floor_tall | se | base+shadow | primary:plastic, wood:walnut, plant:leaf | - |
| plant_succulent | se | base+shadow | primary:sky, plant:sage | - |
| plant_trailing | se | base+shadow | primary:plastic, plant:leaf | - |
| poster_project | se/sw | base | wood:oak | - |
| poster_schematic | se/sw | base | metal:graphite | - |
| photo_abstract | se/sw | base | primary:plastic | - |
| clock_wall | se/sw | base | primary:coral | - |
| award_plaque | se/sw | base | wood:walnut, metal:brass | - |
| sticky_notes | se/sw | base | - | - |
| idea_sketch | se/sw | base | secondary:cream | - |
| figurine_robot | se/sw/nw/ne | base+shadow | primary:sky, screen:display, secondary:plastic, accent:coral | - |
| books_stack | se/sw/nw/ne | base+shadow | primary:coral, secondary:teal, accent:sun | - |
| storage_boxes | se/sw/nw/ne | base+shadow | primary:kraft, secondary:sky | - |
| mug | se/sw/nw/ne | base+shadow | primary:coral | - |
| bottle | se | base+shadow | glass:glass, accent:teal, primary:sky | - |
| snack_bowl | se | base+shadow | primary:sun | - |
| notebook | se/sw/nw/ne | base+shadow | primary:navy, accent:coral, secondary:sky | - |
| paper_stack | se/sw/nw/ne | base+shadow | secondary:plastic | - |
| cable_organizer | se/sw/nw/ne | base+shadow | primary:graphite | - |
| trophy_project | se/sw/nw/ne | base+shadow | wood:walnut, metal:brass | - |
| rubber_duck_debug | se/sw/nw/ne | base+shadow | accent:sun | - |
| retro_message | se/sw/nw/ne | base+shadow | primary:beige_pc, screen:display | - |
| side_project | se/sw/nw/ne | base+shadow | primary:plastic, metal:steel | - |
| moving_box | se/sw | base+shadow | primary:kraft | - |

## Couverture du plan

- IDs planifies pour ce lot : 57 ; livres : 57 ; manquants : 0 
- Aucun ID hors plan ajoute (les variantes sont des orientations, pas des copies).

## Conventions

- **Hauteur de bande commune : 12 px** (dessus des bandes, rebords a rouleaux des postes, bouts de bande des machines en ligne). Largeur de bande 20 su, centree dans la case (y local 6..26 / x local 6..26).
- **Ports** (champ `ports` par orientation) : `tile` = case monde (relative a la case d'ancrage) qui porte le port ; `world_dir` = direction de l'arete d'echange (+U, -U, +V, -V) ; `kind` = in / out / inout. Deux modules sont raccordes si un port `out` et un port `in`/`inout` sont sur des cases voisines avec des directions opposees (verifie par `previews/check_conveyors.png` : 38 ports raccordes, 4 ouverts = vraies extremites de ligne).
- Convoyeur droit : orientations se/sw seulement (symetrique, `flow_reversible: true`). Courbe : relie les aretes locales `back` et `right` ; les 4 orientations couvrent les 4 virages. Jonction en T : arrivee laterale par l'arete locale `left`.
- Machines en ligne CMS (placement, four, AOI, testeur) : flux le long de x local (port `in` = arete gauche, `out` = arete droite). Pour un flux le long de U, utiliser l'orientation sw.
- Produits : objets poses (`provides: ['product']`), se posent sur le socket `belt` (16, 16, 12) des convoyeurs, `load` de la palette, `stage`/`bench_*` des postes.
- **Objets muraux** (decoration) : emprise (1,1), se = mur gauche, sw = mur droit, ancre = coin de la case de mur (comme les segments), `kind: wall`, `wall_mount: true`, `collision: []`, sans couche d'ombre. Couche de rendu `wall_items` (apres les murs).
- **Objets de bureau** : centres sur le point de pose, `provides: ['desk_item']`, 4 orientations quand ils sont asymetriques (suivent l'orientation du bureau), 1 orientation `symmetric: true` sinon. `plant_trailing` depasse SOUS son ancre (tiges qui retombent d'une etagere).
- **Recoloration : un canal = une seule rampe par asset** (audit `_work/ind/audit_channels.py` : 0 conflit sur les 57 assets du lot). Les pieces sombres structurelles sont en canal `fixed`.

## Controles effectues

- `_work/ind/qa_lot.py` : 57 entrees, 138 orientations, 393 PNG decodes ; mode RGBA ; tailles base/masque/ombre identiques ; alpha strictement 0/255 ; masques dans la silhouette, canaux valides, indices 0..5 ; recoloration avec les rampes par defaut = sprite de base (pixel exact) ; ombres monochromes ; ports valides ; aucun ID en double dans tout build_cache. **0 erreur.**
- Controle visuel : `previews/sheet_industry_future.png`, `previews/sheet_decoration.png`, `previews/check_conveyors.png` (boucle + ligne CMS + produits, assemblee uniquement via le manifeste), `previews/check_deco_room.png` (objets muraux sur les deux murs, objets de bureau poses via les sockets des bureaux du lot principal).

## Limites / remarques

- Les sprites de petits objets de bureau sont volontairement minuscules (echelle du bureau) : la succulente fait ~10 px.
- La courbe de convoyeur utilise `Model.clip` (decoupe d'un cylindre a la case) ; la bande courbe ne projette pas d'ombre (l'ombre d'un cylindre complet serait fausse) — seuls pieds et rails en projettent.
- Le robot (cellule) est une pose statique ; aucun etat (marche/arret) n'est dessine. Les temoins lumineux allumes sont un choix de design statique.

## Problemes constates dans le code partage (non modifie)

1. `registry.build_iso_asset` ne retient qu'une rampe par canal (`channels`) : si un asset utilise un meme canal avec deux rampes, la recoloration par defaut fusionne les pieces. Audit sur les familles du lot principal (au moment du controle) : desk_compact, desk_tech, chair_office, chair_ergonomic, monitor_arm_second, workstation_compute en conflit. Correctif suggere : pieces secondaires en canal `fixed`, ou un audit bloquant dans le QA global.
2. `iso.Model.slab_tilt` calcule sa bbox en supposant l'epaisseur vers +y : pour une plaque inclinee vers l'avant, la toile peut etre trop petite d'1-2 px (contourne par `bounds_hint` sur le bras robotise).
3. `scene.Scene._draw_node` ne dessine ni les couches `front` des objets non-chaises ni les ombres des enfants (pas genant pour ce lot : aucune couche `front` utilisee).

## Reconstruire

```
python StartupRushOriginalAssets/tools/build_all.py --modules defs_industry,defs_decoration
python StartupRushOriginalAssets/tools/make_previews.py industry_future,decoration
python _work/ind/check_conveyors.py      # previews/check_conveyors.png
python _work/ind/check_deco.py           # previews/check_deco_room.png
python _work/ind/qa_lot.py               # QA du lot
python _work/ind/audit_channels.py defs_industry,defs_decoration
```
(scripts de controle dans le dossier `_work/ind/` a la racine de la session ; a deplacer dans `tools/` si on veut les livrer.)
