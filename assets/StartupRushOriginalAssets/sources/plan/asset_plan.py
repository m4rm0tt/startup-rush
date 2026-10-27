"""Inventaire planifie (source de ASSET_PLAN.csv). Etabli AVANT la production en serie.

Colonnes : id, famille, sous-categorie, nom FR, emprise (cases L x P ou 'pose'),
orientations, usage, priorite (P1 jouable des le debut, P2 milieu, P3 tardif/futur),
lot de production, notes.
"""
A4, F2, O1, W2 = 'se/sw/nw/ne', 'se/sw', 'se', 'se/sw(murs)'
PLAN = [
    # --- architecture / sols / motifs ---
    ('wall_plain', 'architecture', 'wall', 'Mur plein', '1 case', W2, 'shared', 'P1', 'main', 'tranche de mur continu, plinthe'),
    ('wall_window', 'architecture', 'wall', 'Mur avec fenetre', '1 case', W2, 'shared', 'P1', 'main', 'vitre stylisee, appui'),
    ('wall_window_wide', 'architecture', 'wall', 'Mur avec grande fenetre', '2 cases', W2, 'shared', 'P2', 'main', ''),
    ('wall_door', 'architecture', 'wall', 'Mur avec porte', '1 case', W2, 'shared', 'P1', 'main', 'porte fermee statique'),
    ('wall_opening', 'architecture', 'wall', 'Mur avec passage', '1 case', W2, 'shared', 'P1', 'main', 'ouverture sans vantail'),
    ('wall_corner_inner', 'architecture', 'wall', 'Angle interieur', 'angle', 'all', 'shared', 'P1', 'main', ''),
    ('wall_corner_outer', 'architecture', 'wall', 'Angle saillant (retour)', 'angle', 'all', 'shared', 'P3', 'main', 'plans en L'),
    ('wall_end', 'architecture', 'wall', 'Terminaison de mur', 'chant', W2, 'shared', 'P1', 'main', ''),
    ('floor_wood_planks', 'floors', 'floor', 'Plancher bois', '1 case', 'variantes 2x1', 'shared', 'P1', 'main', ''),
    ('floor_parquet', 'floors', 'floor', 'Parquet', '1 case', '-', 'shared', 'P2', 'main', ''),
    ('floor_carpet', 'floors', 'floor', 'Moquette', '1 case', '-', 'shared', 'P1', 'main', ''),
    ('floor_carpet_tiles', 'floors', 'floor', 'Dalles de moquette', '1 case', 'variantes 2x2', 'shared', 'P2', 'main', ''),
    ('floor_tile_ceramic', 'floors', 'floor', 'Carrelage', '1 case', '-', 'shared', 'P1', 'main', 'cuisine, sanitaires'),
    ('floor_concrete', 'floors', 'floor', 'Beton cire', '1 case', 'variantes 2x2', 'shared', 'P2', 'main', ''),
    ('floor_vinyl', 'floors', 'floor', 'Vinyle', '1 case', '-', 'shared', 'P2', 'main', 'labos, salle propre'),
    ('floor_raised', 'floors', 'floor', 'Faux plancher technique', '1 case', '-', 'shared', 'P2', 'main', 'datacenter'),
    ('floor_raised_vent', 'floors', 'floor', 'Faux plancher perfore', '1 case', '-', 'shared', 'P2', 'main', ''),
    ('floor_edge', 'floors', 'floor_edge', 'Bord avant de dalle', '1 case', F2, 'shared', 'P1', 'main', ''),
    ('floor_edge_corner', 'floors', 'floor_edge', 'Coin avant de dalle', 'angle', 'all', 'shared', 'P1', 'main', ''),
    ('floor_threshold', 'floors', 'floor_overlay', 'Seuil de transition', 'arete', 'along_u/along_v', 'shared', 'P1', 'main', 'jonction de deux sols'),
    ('floor_marking', 'floors', 'floor_overlay', 'Marquage au sol', '1 case', '4 variantes', 'shared', 'P3', 'main', 'atelier, usine'),
]
for p, n in [('paint', 'Peinture unie'), ('two_tone', 'Deux tons'), ('wallpaper', 'Papier discret'), ('stripes', 'Bandes'),
             ('wood_panels', 'Lambris bois'), ('concrete', 'Beton'), ('brick', 'Brique interieure'), ('acoustic', 'Acoustique/technique')]:
    PLAN.append((f'pattern_{p}', 'wall_patterns', 'wall_pattern', n, 'calque', W2, 'shared',
                 'P1' if p in ('paint', 'two_tone') else 'P2', 'main', 'canal + delta, surface murale uniquement'))

FURN = [
    ('desk_simple', 'desk', 'Bureau simple', '2x1', A4, 'P1'), ('desk_compact', 'desk', 'Bureau compact', '1x1', A4, 'P1'),
    ('desk_long', 'desk', 'Bureau long', '3x1', A4, 'P2'), ('desk_corner', 'desk', "Bureau d'angle", '2x2', A4, 'P2'),
    ('desk_tech', 'desk', 'Bureau technique renforce', '2x1', A4, 'P2'), ('table_collab', 'table', 'Table collective', '3x2', F2, 'P2'),
    ('chair_budget', 'chair', 'Chaise economique', 'pose', A4, 'P1'), ('chair_office', 'chair', 'Chaise de bureau', 'pose', A4, 'P1'),
    ('chair_ergonomic', 'chair', 'Chaise ergonomique', 'pose', A4, 'P2'), ('chair_meeting', 'chair', 'Chaise de reunion', 'pose', A4, 'P2'),
    ('table_meeting_small', 'table', 'Petite table de reunion (ronde)', '2x2', O1, 'P2'),
    ('table_meeting_large', 'table', 'Grande table de reunion', '4x2', F2, 'P2'),
    ('reception_desk', 'desk', "Banque d'accueil", '3x2', F2, 'P2'), ('wardrobe_office', 'storage', 'Armoire', '1x1', F2, 'P2'),
    ('locker', 'storage', 'Casiers', '1x1', F2, 'P2'), ('bookcase', 'storage', 'Bibliotheque', '2x1', F2, 'P1'),
    ('shelf_metal', 'storage', 'Etagere metallique', '2x1', F2, 'P2'), ('pedestal_drawer', 'storage', 'Caisson a roulettes', 'pose', A4, 'P2'),
    ('filing_cabinet', 'storage', 'Classeur a tiroirs', '1x1', F2, 'P2'), ('sideboard_low', 'storage', 'Meuble bas', '2x1', F2, 'P2'),
    ('display_case', 'storage', 'Presentoir vitre', '1x1', F2, 'P3'), ('whiteboard_mobile', 'board', 'Tableau blanc mobile', '2x1', F2, 'P1'),
    ('planning_board', 'board', 'Tableau de planning mural', 'mur 2 cases', W2, 'P2'),
    ('presentation_screen', 'av', 'Ecran de presentation sur pied', '2x1', F2, 'P2'),
    ('partition_low', 'partition', 'Cloison basse', '1 case', F2, 'P2'), ('partition_acoustic', 'partition', 'Separation acoustique', '1 case', F2, 'P2'),
    ('partition_glass', 'partition', 'Vitrage interieur', '1 case', F2, 'P2'),
    ('partition_glass_door', 'partition', 'Vitrage interieur avec porte', '1 case', F2, 'P3'),
    ('trash_bin', 'misc', 'Poubelle', 'pose', O1, 'P1'), ('recycling_station', 'misc', 'Bacs de tri', '1x1', F2, 'P2'),
    ('coat_rack', 'misc', 'Porte-manteau', 'pose', O1, 'P2'), ('lamp_ceiling', 'lighting', 'Luminaire suspendu', 'pose', O1, 'P2'),
    ('lamp_desk', 'lighting', 'Lampe de bureau', 'pose', A4, 'P1'), ('lamp_floor', 'lighting', 'Lampadaire', 'pose', O1, 'P1'),
    ('sofa', 'lounge', 'Canape', '2x1', A4, 'P2'), ('armchair', 'lounge', 'Fauteuil', '1x1', A4, 'P2'),
    ('table_coffee', 'lounge', 'Table basse', '1x1', O1, 'P2'),
    ('kitchen_counter', 'kitchen', 'Element de cuisine', '1x1', F2, 'P1'), ('kitchen_sink', 'kitchen', 'Evier', '1x1', F2, 'P1'),
    ('fridge', 'kitchen', 'Refrigerateur', '1x1', F2, 'P1'), ('microwave', 'kitchen', 'Micro-ondes', 'pose', F2, 'P1'),
    ('coffee_machine', 'kitchen', 'Machine a cafe', 'pose', F2, 'P1'), ('water_cooler', 'kitchen', 'Fontaine a eau', '1x1', F2, 'P2'),
    ('vending_drinks', 'kitchen', 'Distributeur de boissons', '1x1', F2, 'P2'),
    ('vending_snacks', 'kitchen', 'Distributeur de snacks', '1x1', F2, 'P2'),
    ('toilet', 'sanitary', 'WC', '1x1', A4, 'P2'), ('washbasin', 'sanitary', 'Lavabo', '1x1', F2, 'P2'),
    ('toilet_stall', 'sanitary', 'Cabine sanitaire', '1x1', F2, 'P3'),
    ('bed_single', 'bedroom', 'Lit simple', '2x1', F2, 'P1'), ('dresser', 'bedroom', 'Commode', '1x1', F2, 'P1'),
    ('nightstand', 'bedroom', 'Table de chevet', 'pose', F2, 'P1'),
    ('rug_small', 'floor_decor', 'Tapis', '2x2', O1, 'P1'), ('rug_large', 'floor_decor', 'Grand tapis', '3x2', F2, 'P2'),
    ('window_blinds', 'window_dressing', 'Stores de fenetre', 'mur 1 case', W2, 'P2'),
]
for (i, sub, n, fp, o, pr) in FURN:
    PLAN.append((i, 'furniture', sub, n, fp, o, 'current', pr, 'main', ''))

COMP = [
    ('laptop_old', 'laptop', 'Portable ancien'), ('laptop_standard', 'laptop', 'Portable standard'),
    ('laptop_pro', 'laptop', 'Portable professionnel'), ('laptop_gaming', 'laptop', 'Portable puissant'),
    ('tower_used', 'tower', "Tour d'occasion"), ('tower_office', 'tower', 'Tour bureautique'),
    ('workstation_dev', 'tower', 'Station de developpement'), ('workstation_creative', 'tower', 'Station creative'),
    ('workstation_gpu', 'tower', 'Station GPU/IA'), ('workstation_compute', 'tower', 'Poste de calcul avance'),
    ('mini_pc', 'tower', 'Mini-PC'), ('monitor_basic', 'monitor', 'Ecran simple'), ('monitor_large', 'monitor', 'Grand ecran'),
    ('monitor_ultrawide', 'monitor', 'Ecran ultralarge'), ('monitor_arm_second', 'monitor', 'Module 2e ecran'),
    ('keyboard_mouse', 'peripheral', 'Clavier et souris'), ('graphics_tablet', 'peripheral', 'Tablette graphique'),
    ('headset_stand', 'peripheral', 'Casque sur support'), ('webcam', 'peripheral', 'Webcam (sur ecran)'),
    ('docking_station', 'peripheral', "Station d'accueil"), ('printer_mfp', 'office', 'Imprimante/scanner'),
    ('videoconf_bar', 'av', 'Barre de visioconference'), ('speakerphone', 'av', 'Pieuvre de conference'),
    ('device_test_rack', 'test', 'Poste de test mobile'), ('console_test_kit', 'test', 'Kit de test console generique'),
    ('xr_headset_stand', 'test', 'Casque XR sur support'), ('capture_device', 'test', 'Boitier de capture'),
    ('network_cabinet', 'network', 'Baie de rangement technique'), ('router', 'network', 'Routeur'),
    ('network_switch_desk', 'network', 'Switch de bureau'), ('access_point', 'network', "Point d'acces (mural)"),
    ('fiber_box', 'network', 'Boitier fibre (mural)'),
]
WALL_SIDE = {'printer_mfp', 'videoconf_bar', 'network_cabinet', 'access_point', 'fiber_box'}
for (i, sub, n) in COMP:
    PLAN.append((i, 'computers', sub, n, 'pose', F2 if i in WALL_SIDE else A4, 'current',
                 'P1' if i in ('laptop_old', 'tower_used', 'monitor_basic', 'keyboard_mouse', 'router') else 'P2', 'main',
                 'sans marque' + (' ; revise en cours de production : adosse a un mur ou pose sur un ecran, 2 orientations '
                                  'suffisent' if i in WALL_SIDE else '')))

SERV = [
    ('rack_compact', 'rack', 'Baie compacte', '1x1', A4, 'P2'), ('rack_standard', 'rack', 'Baie standard', '1x1', A4, 'P2'),
    ('rack_high_density', 'rack', 'Baie haute densite', '1x1', A4, 'P3'),
    ('module_server_1u', 'rack_module', 'Serveur 1U (module)', 'slot', F2, 'P2'),
    ('module_compute_2u', 'rack_module', 'Serveur de calcul 2U', 'slot', F2, 'P2'),
    ('module_storage_2u', 'rack_module', 'Stockage/NAS 2U', 'slot', F2, 'P2'),
    ('module_storage_jbod_4u', 'rack_module', 'Tiroir de disques 4U', 'slot', F2, 'P3'),
    ('module_gpu_4u', 'rack_module', 'Serveur GPU 4U', 'slot', F2, 'P3'),
    ('module_network_switch_1u', 'rack_module', 'Switch 1U', 'slot', F2, 'P2'),
    ('module_patch_panel_1u', 'rack_module', 'Panneau de brassage 1U', 'slot', F2, 'P2'),
    ('module_compute_node_2u', 'rack_module', 'Noeud de calcul 2U', 'slot', F2, 'P3'),
    ('module_ups_2u', 'rack_module', 'Onduleur rack 2U', 'slot', F2, 'P2'),
    ('module_blank_1u', 'rack_module', 'Obturateur 1U', 'slot', F2, 'P2'),
    ('module_cable_manager_1u', 'rack_module', 'Passe-cables 1U', 'slot', F2, 'P2'),
    ('server_tower_classic', 'server', 'Serveur tour', '1x1', A4, 'P2'), ('nas_desktop', 'server', 'NAS de bureau', 'pose', A4, 'P2'),
    ('ups_floor', 'power', 'Onduleur au sol', '1x1', F2, 'P2'),
    ('power_distribution_cabinet', 'power', 'Armoire de distribution', '1x1', F2, 'P2'),
    ('rack_cooler_inrow', 'cooling', 'Refroidissement de rangee', '1x1', A4, 'P3'),
    ('crac_unit', 'cooling', 'Climatiseur de salle', '2x1', F2, 'P2'),
    ('cable_tray_straight', 'cabling', 'Chemin de cables', '1 case', 'along_u/along_v', 'P2'),
    ('cable_tray_corner', 'cabling', 'Chemin de cables (angle)', '1 case', A4, 'P3'),
    ('cable_conduit', 'cabling', 'Conduit au sol', '1 case', F2, 'P3'),
    ('supervision_console', 'console', 'Console de supervision', '3x1', A4, 'P2'),
    ('security_cage_panel', 'security', 'Grille de securite', '1 case', F2, 'P3'),
    ('security_cage_door', 'security', 'Porte de cage', '1 case', F2, 'P3'),
]
for (i, sub, n, fp, o, pr) in SERV:
    PLAN.append((i, 'servers', sub, n, fp, o, 'current', pr, 'lot_datacenter_labs', ''))

LABS = ['workbench_electronics', 'soldering_station', 'microscope', 'oscilloscope', 'lab_power_supply', 'instrument_rack',
        'printer_3d', 'cnc_mill_desktop', 'laser_cutter', 'test_bench_pc', 'test_bench_embedded', 'cobot_cell',
        'calibration_bench', 'camera_tripod', 'softbox_light', 'mic_stand', 'greenscreen_backdrop', 'studio_monitors',
        'xr_base_station', 'xr_play_mat', 'research_station', 'compute_cabinet', 'quantum_cryostat_open',
        'quantum_cryostat_closed', 'quantum_control_rack', 'cryo_support_unit', 'supervision_terminal']
for i in LABS:
    PLAN.append((i, 'laboratories', 'lab', '(voir manifeste)', 'variable', F2, 'current',
                 'P3' if ('quantum' in i or 'compute' in i) else 'P2', 'lot_datacenter_labs', ''))

IND = ['conveyor_straight', 'conveyor_curve', 'conveyor_junction_t', 'conveyor_end', 'conveyor_transfer_table',
       'assembly_station_manual', 'assembly_cell_robotic', 'pick_and_place_machine', 'reflow_oven', 'aoi_inspection',
       'pcb_tester', 'chip_fab_tool', 'precision_station', 'flash_station', 'stress_test_rack', 'packing_station',
       'storage_rack_industrial', 'pallet', 'tote_bin', 'dust_extraction', 'industrial_chiller', 'control_cabinet',
       'cleanroom_airlock', 'cleanroom_partition', 'product_pcb', 'product_cpu', 'product_ram', 'product_gpu',
       'product_case', 'product_pc_assembled', 'product_box', 'product_wafer']
for i in IND:
    PLAN.append((i, 'industry_future', 'industry', '(voir manifeste)', 'variable', F2, 'future', 'P3',
                 'lot_industrie_deco', "ports d'entree/sortie dans le manifeste"))

DECO = ['plant_succulent', 'plant_floor_medium', 'plant_floor_tall', 'plant_trailing', 'poster_project', 'poster_schematic',
        'photo_abstract', 'figurine_robot', 'books_stack', 'storage_boxes', 'mug', 'bottle', 'snack_bowl', 'notebook',
        'moving_box', 'paper_stack', 'sticky_notes', 'cable_organizer', 'clock_wall', 'award_plaque', 'trophy_project',
        'rubber_duck_debug', 'idea_sketch', 'retro_message', 'side_project']
for i in DECO:
    PLAN.append((i, 'decoration', 'decor', '(voir manifeste)', 'variable', F2, 'current', 'P1', 'lot_industrie_deco', ''))

CHAR = [('player_m', 'avatar'), ('player_f', 'avatar')] + \
       [(f'employee_m_0{k}', 'employee') for k in range(1, 6)] + [(f'employee_f_0{k}', 'employee') for k in range(1, 6)]
for (i, sub) in CHAR:
    PLAN.append((i, 'characters', sub, 'Base chauve assise', 'pose (chaise)', A4, 'current', 'P1', 'main', 'couches body/head + masques'))
for h in ['short_crop', 'side_part', 'curly_top', 'long_straight', 'bun', 'bob']:
    PLAN.append((f'hair_{h}', 'characters', 'hair', 'Coiffure', 'overlay', A4, 'current', 'P1', 'main', 'hair_front/hair_back + masque'))

UI = [('font_startuprush_pixel', 'fonts'), ('font_startuprush_mono', 'fonts'), ('icons_set', 'hud'), ('hud_panels', 'hud'),
      ('hud_buttons', 'hud'), ('hud_inputs', 'hud'), ('hud_navigation', 'hud'), ('hud_feedback', 'hud'), ('hud_charts', 'hud'),
      ('cursors_set', 'cursors'), ('vfx_set', 'vfx'), ('terminal_kit', 'terminal'),
      ('project_component_cards', 'project_components'), ('project_component_previews', 'project_components'),
      ('reference_screens', 'hud')]
for (i, fam) in UI:
    PLAN.append((i, fam, 'ui', '(boite a outils HUD)', '-', '-', 'shared', 'P1', 'lot_ui', ''))

ROOMS = [('room_bedroom_start', 'Chambre de depart', '6x5'), ('room_startup_small', 'Petit bureau/atelier', '8x6'),
         ('room_office_medium', 'Bureau moyen', '12x9'), ('room_open_space', 'Grand open space', '18x14'),
         ('room_hq_floor', 'Plateau de siege', '24x18'), ('room_server_compact', 'Salle serveur compacte', '6x5'),
         ('room_datacenter', 'Datacenter', '16x12'), ('room_hw_workshop', 'Atelier prototypage materiel', '10x8'),
         ('room_factory_hall', 'Hall de fabrication (futur)', '22x16'),
         ('room_advanced_lab', 'Laboratoire avance + zone quantique', '14x10')]
for (i, n, fp) in ROOMS:
    PLAN.append((i, 'rooms', 'room_plan', n, fp, '-', 'future' if 'factory' in i else 'current',
                 'P1' if ('bedroom' in i or 'small' in i) else 'P2', 'main', 'plan JSON + apercu'))
