# Statut — lot datacenter / laboratoires

Sources : `sources/defs_servers.py`, `sources/defs_laboratories.py`, aides `tools/srgen/lot_dc_shapes.py`,
QA exécutable `tools/srgen/lot_dc_qa.py`. Entrées de manifeste : `build_cache/iso_servers.json`,
`build_cache/custom_servers_modules.json`, `build_cache/iso_laboratories.json`.

## Livré : 53 IDs (100 % du plan pour ce lot)

### servers : 15 assets iso + 11 modules de baie
| ID | Orientations | Notes |
|---|---|---|
| rack_compact | se sw nw ne | 8 slots, roulettes |
| rack_standard | se sw nw ne | 20 slots |
| rack_high_density | se sw nw ne | 20 slots, porte arrière refroidie (3 ventilateurs), panier à câbles sur le toit |
| module_server_1u, module_network_switch_1u, module_patch_panel_1u, module_blank_1u, module_cable_manager_1u | se sw | 1U = 4 px |
| module_compute_2u, module_storage_2u, module_compute_node_2u, module_ups_2u | se sw | 2U = 8 px |
| module_storage_jbod_4u, module_gpu_4u | se sw | 4U = 16 px (GPU : 3 grilles de ventilateurs + liseré corail) |
| server_tower_classic | se sw nw ne | 8 baies, écran d'état, face arrière (alim, ports) |
| nas_desktop | se sw nw ne | posé (desk_item / shelf_item) |
| ups_floor | se sw | |
| power_distribution_cabinet | se sw | pictogramme sans texte, conduits jusqu'à z=112 |
| rack_cooler_inrow | se sw nw ne | même hauteur que rack_standard |
| crac_unit | se sw | 2x1 |
| cable_tray_straight | se sw | se = le long de V, sw = le long de U ; z 98-104 ; aucune collision |
| cable_tray_corner | se sw nw ne | angle en L, câbles emboîtés qui tournent |
| cable_conduit | se sw | passe-câble franchissable |
| supervision_console | se sw nw ne | 3x1, 3 zones d'écran (quads), sockets chair/keyboard/desk_* |
| security_cage_panel, security_cage_door | se sw | grillage soudé ajouré (pixels transparents) |

### laboratories : 27 assets
Établis/postes (sockets bench_*, shelf_*, chair) : workbench_electronics (A4), test_bench_pc (A4),
test_bench_embedded (A4), research_station (A4, 2 quads écran), calibration_bench (se/sw).
Objets d'établi posés (bench_item + desk_item, A4) : soldering_station, microscope, oscilloscope (quad écran),
lab_power_supply, cnc_mill_desktop, studio_monitors.
Au sol : instrument_rack, printer_3d, laser_cutter, cobot_cell (2x2), greenscreen_backdrop, compute_cabinet,
quantum_cryostat_open, quantum_cryostat_closed, cryo_support_unit, supervision_terminal (se/sw) ;
camera_tripod, softbox_light, mic_stand, xr_base_station, quantum_control_rack (A4) ; xr_play_mat (se, symétrique, au sol, franchissable, sans ombre).

## Système de slots (baies)
- `unit_px` = 4 (1U), slots : compact 8, standard 20, haute densité 20. Point de slot local : x0 = 6 su,
  y_face = 4 su, z_top = 41 / 88 / 89 ; slot i : z = z_top − 4·i. Façade d'un module : 20 su.
- x0 + y_face pair : les `slots.positions[i].offset_px` sont des pixels entiers exacts (aucun arrondi baie ↔ module).
- Placement : `module_top_left = ancre_baie + slots.positions[i].offset_px − ancre_module` (même orientation).
  Un module de n unités occupe i..i+n−1. Dessiner après la baie, du bas vers le haut.
- Modules visibles en se/sw seulement (`slots.visible`) ; en nw/ne la baie montre sa porte arrière.
- Les modules sont des plaques de façade (constructeur spécialisé `servers_modules`) : leurs pixels sont exactement
  le parallélogramme du slot (pas de contour extérieur, pas de faces latérales).
- QA : `previews/check_racks.png` → 7 360 px de modules posés via le manifeste uniquement, **0 hors enveloppe**.

## Contrôles effectués
- Build déterministe des deux familles, 424 fichiers référencés (dont 151 masques) : tous présents, aucun ID en double.
- Planches visuelles : `previews/sheet_servers.png`, `previews/sheet_laboratories.png` (toutes orientations, ancre, emprise).
- Scènes composées avec le compositeur de référence (`tools/srgen/scene.py`) depuis build_cache :
  `previews/check_dc_datacenter.png` (allées chaude/froide, modules en baie, chemins de câbles),
  `check_dc_lab.png` (objets d'établi dans leurs sockets, chaises de poste), `check_dc_quantum.png`, `check_dc_studio.png`.
- Visibilité des quads d'écran vérifiée (`visible: true` en se/sw, `false` en nw/ne pour les écrans tournés vers l'avant).

## Manques / limites
- Aucun ID planifié manquant.
- Les modules ne sont pas dessinés sur les baies vues de dos (nw/ne) : choix documenté, pas un oubli.
- Vitres translucides : le moteur ne compose pas les pixels situés derrière une vitre *du même sprite*
  (z-buffer unique). L'imprimante 3D utilise donc des reflets opaques fins au lieu d'une vitre avant ; la cellule cobot
  n'a de panneau translucide qu'au fond (toujours derrière le robot).

## Points pour l'intégration (lot principal)
1. Chemins de câbles : `render_layer` = objects (tag `overhead`). Ils se trient comme les objets au sol (tri
   topologique des emprises) et, sur une case partagée avec un objet posé dessous, se dessinent APRÈS lui.
   Ne pas les dessiner en dernier : une rangée de baies plus proche de la caméra doit les masquer.
2. `tools/srgen/scene.py` ne gère pas les slots : `lot_dc_qa.add_modules()` montre comment ajouter les modules en
   enfants `after` d'une baie (à intégrer au compositeur de référence si souhaité).
3. `Fix('nom')` renvoie silencieusement `None` si le nom n'existe pas dans `FIXED` (erreur plus tard au rendu).

## Commandes de reconstruction
```
python StartupRushOriginalAssets/tools/build_all.py --modules defs_servers,defs_laboratories
python StartupRushOriginalAssets/tools/make_previews.py servers,laboratories
python StartupRushOriginalAssets/tools/srgen/lot_dc_qa.py
```
