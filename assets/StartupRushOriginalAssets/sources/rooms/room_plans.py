"""Source des plans de pieces predefinies (rooms/*.json) et de leurs apercus.

Usage : python sources/rooms/room_plans.py [room_id,...]
Chaque plan est valide contre le manifeste (assets, orientations, sockets,
chevauchements, entrees) puis rendu avec le compositeur de reference.

Conventions : tile = [U, V] du coin nord de l'emprise (objets a emprise) ou de
la case contenant le centre (objets centres). Mur gauche = segments V=0..D-1,
mur droit = segments U=0..W-1 ; None = case couverte par une piece de 2 cases.
"""
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(ROOT, 'tools'))

from srgen.scene import Library  # noqa: E402
from srgen.rooms import validate, render_plan, expand_modules  # noqa: E402


def ws(desk, U, V, facing, base, hair, chair='chair_office', monitor='monitor_basic', tower='tower_office',
       extra=None, skin=None, hair_ramp='hair_brown', laptop=None, keyboard=True, sw=None):
    """Poste de travail : bureau + ecran/portable + tour + chaise + personnage assis."""
    ch = {'base': base, 'hair': f'hair_{hair}', 'hair_ramp': hair_ramp}
    if skin:
        ch['skin'] = skin
    kids = {'chair': {'asset': chair, 'character': ch}}
    if laptop:
        kids['laptop'] = laptop
    else:
        kids['monitor_main'] = monitor
        if keyboard:
            kids['keyboard'] = 'keyboard_mouse'
    if tower:
        kids['tower_floor'] = tower
    if extra:
        kids.update(extra)
    o = {'asset': desk, 'tile': [U, V], 'facing': facing, 'children': kids}
    if sw:
        o['swatches'] = sw
    return o


def ob(asset, U, V, facing='se', children=None, sw=None):
    o = {'asset': asset, 'tile': [U, V], 'facing': facing}
    if children:
        o['children'] = children
    if sw:
        o['swatches'] = sw
    return o


# ---------------------------------------------------------------------------
# 1. Chambre de depart (6 x 5)
# ---------------------------------------------------------------------------
BEDROOM = {
    'id': 'room_bedroom_start', 'name_fr': 'Chambre de départ', 'usage': 'current', 'tier': 1,
    'size_tiles': [6, 5],
    'story': "Petit logement d'un passionné : lit, rangement, bureau modeste. Pièce normale où l'on ajoute du mobilier.",
    'floor': [{'material': 'floor_wood_planks', 'rect': [0, 0, 6, 5], 'swatches': {'floor': 'oak'}}],
    'walls': {
        'left': {'segments': ['plain', 'plain', 'window', 'plain', 'door'], 'pattern': 'wallpaper',
                 'swatches': {'wall': 'sage', 'trim': 'plastic', 'wood': 'birch'}},
        'right': {'segments': ['plain', 'plain', 'window', 'plain', 'plain', 'plain'], 'pattern': 'paint',
                  'swatches': {'wall': 'cream', 'trim': 'plastic'}},
    },
    'wall_items': [
        {'asset': 'poster_project', 'wall': 'right', 'index': 4},
        {'asset': 'idea_sketch', 'wall': 'left', 'index': 3},
        {'asset': 'window_blinds', 'wall': 'left', 'index': 2},
        {'asset': 'window_blinds', 'wall': 'right', 'index': 2},
        {'asset': 'clock_wall', 'wall': 'right', 'index': 1},
        {'asset': 'sticky_notes', 'wall': 'left', 'index': 1},
    ],
    'objects': [
        ob('rug_small', 2, 2),
        ob('bookcase', 0, 0, 'se', {'shelf_1': 'rubber_duck_debug', 'shelf_2': 'trophy_project'}),
        ob('dresser', 1, 0, 'sw', {'top': 'plant_succulent'}),
        ob('bed_single', 3, 0, 'sw', sw={'fabric': 'sky'}),
        ob('nightstand', 5, 1, 'sw', {'top': 'lamp_desk'}),
        ws('desk_compact', 0, 2, 'se', 'player_m', 'short_crop', chair='chair_budget', tower='tower_used',
           extra={'desk_right': 'mug', 'desk_back_left': 'side_project'}),
        ob('lamp_floor', 5, 3),
        ob('moving_box', 5, 4),
        ob('plant_floor_medium', 1, 4),
        ob('trash_bin', 1, 3),
    ],
    'entrances': [{'wall': 'left', 'index': 4, 'kind': 'door', 'access_tile': [0, 4]}],
    'zones': [{'name': 'Coin nuit', 'rect': [3, 0, 3, 2]}, {'name': 'Poste de travail', 'rect': [0, 2, 2, 2]},
              {'name': 'Rangement', 'rect': [0, 0, 2, 2]}],
    'foreground': 'aucun mur avant (coupe) ; les objets hauts du premier rang peuvent etre estompes (occlusion)',
}

# ---------------------------------------------------------------------------
# Aides de composition
# ---------------------------------------------------------------------------
STAFF = [('employee_m_01', 'side_part', 'hair_brown'), ('employee_f_01', 'bob', 'hair_black'),
         ('employee_m_02', 'curly_top', 'hair_black'), ('employee_f_02', 'long_straight', 'hair_auburn'),
         ('employee_m_03', 'short_crop', 'hair_gray'), ('employee_f_03', 'bun', 'hair_black'),
         ('employee_m_04', 'side_part', 'hair_auburn'), ('employee_f_04', 'bob', 'hair_brown'),
         ('employee_m_05', 'short_crop', 'hair_black'), ('employee_f_05', 'long_straight', 'hair_brown'),
         ('employee_m_01', 'curly_top', 'hair_blond'), ('employee_f_02', 'bun', 'hair_blond')]


def staff(k):
    return STAFF[k % len(STAFF)]


def person(k):
    b, h, r = staff(k)
    return {'base': b, 'hair': f'hair_{h}', 'hair_ramp': r}


def pod_pair(U, V, k_front, k_back, desk='desk_simple', monitor='monitor_basic', chair='chair_office',
             tower='tower_office', empty=()):
    """Deux bureaux dos a dos : bureau 'nw' en (U,V) (personne vue de face, cote U-1) et bureau 'se'
    en (U+1,V) (personne vue de dos, cote U+2)."""
    out = []
    for (u, f, k) in ((U, 'nw', k_front), (U + 1, 'se', k_back)):
        kids = {'monitor_main': monitor, 'keyboard': 'keyboard_mouse'}
        if tower:
            kids['tower_floor'] = tower
        ch = {'asset': chair}
        if k is not None and k not in empty:
            ch['character'] = person(k)
        kids['chair'] = ch
        out.append({'asset': desk, 'tile': [u, V], 'facing': f, 'children': kids})
    return out


def bench_pair(U, V, ks_front, ks_back, monitor='monitor_basic', chair='chair_office'):
    """Deux bureaux longs (3 places) dos a dos le long de U."""
    out = []
    for (u, f, ks) in ((U, 'nw', ks_front), (U + 1, 'se', ks_back)):
        kids = {}
        for i, k in enumerate(ks):
            kids[f'monitor_{i}'] = monitor if (i + (k or 0)) % 3 else 'monitor_large'
            kids[f'keyboard_{i}'] = 'keyboard_mouse'
            ch = {'asset': chair}
            if k is not None:
                ch['character'] = person(k)
            kids[f'chair_{i}'] = ch
        out.append({'asset': 'desk_long', 'tile': [u, V], 'facing': f, 'children': kids})
    return out


def glass_room(U0, V0, U1, V1, sides=('front', 'left'), door=None, asset='partition_glass'):
    """Cloisons sur les aretes d'un rectangle [U0,U1) x [V0,V1).
    front : ligne v=V1 (segments sw en (U, V1)) ; back : ligne v=V0 ; left : ligne u=U0 (segments se en (U0, V)) ;
    right : ligne u=U1 (segments se en (U1, V)). door = (cote, index) remplace un segment par une porte vitree."""
    out = []
    for side in sides:
        if side in ('front', 'back'):
            v = V1 if side == 'front' else V0
            for i, u in enumerate(range(U0, U1)):
                a = 'partition_glass_door' if door == (side, i) and asset == 'partition_glass' else asset
                out.append({'asset': a, 'tile': [u, v], 'facing': 'sw'})
        else:
            u = U0 if side == 'left' else U1
            for i, v in enumerate(range(V0, V1)):
                a = 'partition_glass_door' if door == (side, i) and asset == 'partition_glass' else asset
                out.append({'asset': a, 'tile': [u, v], 'facing': 'se'})
    return out


def chair_at(U, V, facing, k=None, asset='chair_office'):
    o = {'asset': asset, 'tile': [U, V], 'facing': facing}
    if k is not None:
        o['character'] = person(k)
    return o


def rack(asset, U, V, facing, modules=None):
    """Baie + modules : modules = liste d'ids remplis du haut vers le bas (taille deduite du suffixe 1u/2u/4u)."""
    o = {'asset': asset, 'tile': [U, V], 'facing': facing}
    if modules:
        slots = []
        i = 0
        for mid in modules:
            size = 4 if mid.endswith('4u') else 2 if mid.endswith('2u') else 1
            if mid != 'gap':
                slots.append({'index': i, 'asset': mid})
            i += size
        o['children'] = {'slots': slots}
    return o


RACK_MIX = [
    ['module_patch_panel_1u', 'module_network_switch_1u', 'module_cable_manager_1u', 'module_server_1u',
     'module_server_1u', 'module_compute_2u', 'module_compute_2u', 'module_storage_2u', 'module_blank_1u',
     'module_storage_2u', 'module_server_1u', 'module_ups_2u'],
    ['module_patch_panel_1u', 'module_network_switch_1u', 'module_gpu_4u', 'module_gpu_4u', 'module_blank_1u',
     'module_compute_node_2u', 'module_compute_node_2u', 'module_cable_manager_1u', 'module_ups_2u'],
    ['module_network_switch_1u', 'module_network_switch_1u', 'module_storage_jbod_4u', 'module_storage_jbod_4u',
     'module_storage_2u', 'module_storage_2u', 'module_blank_1u', 'module_server_1u'],
]


# ---------------------------------------------------------------------------
# Modules de plan (inseres par 'include' avec un decalage)
# ---------------------------------------------------------------------------
MODULES = {
    'module_kitchenette': {
        'id': 'module_kitchenette', 'name_fr': 'Kitchenette (contre un mur gauche)', 'size_tiles': [4, 6],
        'floor': [{'material': 'floor_tile_ceramic', 'rect': [0, 0, 4, 6]}],
        'objects': [ob('kitchen_counter', 0, 0, 'se'), ob('kitchen_sink', 0, 1, 'se'),
                    ob('kitchen_counter', 0, 2, 'se', {'counter': 'coffee_machine'}), ob('fridge', 0, 3, 'se'),
                    ob('vending_drinks', 0, 4, 'se'), ob('vending_snacks', 0, 5, 'se'),
                    ob('table_meeting_small', 2, 1, 'se', {'center': 'mug'}), ob('water_cooler', 1, 5, 'se'),
                    ob('trash_bin', 1, 0), ob('recycling_station', 3, 0, 'sw')],
        'zones': [{'name': 'Kitchenette', 'rect': [0, 0, 4, 6]}],
    },
    'module_restroom': {
        'id': 'module_restroom', 'name_fr': 'Sanitaires (contre un mur gauche)', 'size_tiles': [4, 4],
        'floor': [{'material': 'floor_tile_ceramic', 'rect': [0, 0, 4, 4], 'swatches': {'floor': 'sky', 'floor2': 'plastic'}}],
        'objects': [ob('toilet_stall', 0, 0, 'se'), ob('toilet_stall', 0, 1, 'se'), ob('washbasin', 0, 2, 'se'),
                    ob('washbasin', 0, 3, 'se'), ob('trash_bin', 1, 3)]
                   + glass_room(0, 0, 3, 4, sides=('front',), asset='partition_acoustic')
                   + glass_room(0, 0, 3, 3, sides=('right',), asset='partition_acoustic'),
        'zones': [{'name': 'Sanitaires', 'rect': [0, 0, 3, 4]}],
    },
    'module_meeting_room': {
        'id': 'module_meeting_room', 'name_fr': 'Salle de réunion vitrée (contre un mur droit)', 'size_tiles': [6, 5],
        'objects': [ob('table_meeting_large', 1, 2, 'sw', {
                        'chair_0': {'asset': 'chair_meeting', 'character': person(2)},
                        'chair_2': {'asset': 'chair_meeting', 'character': person(5)},
                        'chair_4': {'asset': 'chair_meeting'},
                        'chair_1': {'asset': 'chair_meeting', 'character': person(8)},
                        'chair_3': {'asset': 'chair_meeting'},
                        'center': 'speakerphone'}),
                    ob('presentation_screen', 2, 0, 'sw', {'top': 'videoconf_bar'}),
                    ob('plant_floor_medium', 5, 0)]
                   + glass_room(0, 0, 6, 5, sides=('front', 'left'), door=('left', 3)),
        'zones': [{'name': 'Salle de reunion', 'rect': [0, 0, 6, 5]}],
    },
    'module_lounge': {
        'id': 'module_lounge', 'name_fr': 'Coin détente', 'size_tiles': [5, 4],
        'floor': [{'material': 'floor_parquet', 'rect': [0, 0, 5, 4]}],
        'objects': [ob('rug_large', 0, 1, 'sw'), ob('sofa', 0, 0, 'sw', sw={'fabric': 'teal'}),
                    ob('table_coffee', 1, 1, 'se', {'top': 'plant_succulent'}),
                    ob('armchair', 2, 1, 'sw', sw={'fabric': 'coral'}), ob('armchair', 0, 2, 'ne', sw={'fabric': 'sun'}),
                    ob('lamp_floor', 3, 0), ob('plant_floor_tall', 4, 0), ob('bookcase', 3, 2, 'se')],
        'zones': [{'name': 'Detente', 'rect': [0, 0, 5, 4]}],
    },
    'module_reception': {
        'id': 'module_reception', 'name_fr': "Accueil", 'size_tiles': [6, 4],
        'floor': [{'material': 'floor_parquet', 'rect': [0, 0, 6, 4], 'swatches': {'floor': 'oak'}}],
        'objects': [ob('reception_desk', 1, 0, 'sw', {'chair': {'asset': 'chair_office', 'character': person(9)},
                                                      'monitor_main': 'monitor_basic', 'counter_left': 'plant_succulent'}),
                    ob('rug_small', 1, 2), chair_at(0, 3, 'ne', asset='chair_meeting'),
                    chair_at(1, 3, 'ne', 6, asset='chair_meeting'), ob('plant_floor_tall', 5, 0),
                    ob('table_coffee', 3, 3, 'se')],
        'zones': [{'name': 'Accueil', 'rect': [0, 0, 6, 4]}],
    },
    'module_pod_4': {
        'id': 'module_pod_4', 'name_fr': 'Îlot de 4 postes', 'size_tiles': [4, 2],
        'objects': pod_pair(1, 0, 0, 1),
        'zones': [{'name': 'Ilot', 'rect': [0, 0, 4, 2]}],
    },
}


# ---------------------------------------------------------------------------
# 2. Petit bureau / atelier de startup (8 x 6)
# ---------------------------------------------------------------------------
STARTUP = {
    'id': 'room_startup_small', 'name_fr': 'Petit bureau / atelier de startup', 'usage': 'current', 'tier': 2,
    'size_tiles': [8, 6],
    'story': "Premier local loue : esprit loft, trois postes, coin reunion improvise, cartons encore la.",
    'floor': [{'material': 'floor_concrete', 'rect': [0, 0, 8, 6]}],
    'walls': {
        'left': {'segments': ['plain', 'window', 'plain', 'plain', 'plain', 'door'], 'pattern': 'paint',
                 'swatches': {'wall': 'cream', 'trim': 'graphite', 'wood': 'oak'}},
        'right': {'segments': ['plain', 'window_wide', None, 'plain', 'window', 'plain', 'plain', 'plain'], 'pattern': 'brick',
                  'swatches': {'wall': 'brick', 'wall2': 'cream', 'trim': 'graphite'}},
    },
    'wall_items': [{'asset': 'planning_board', 'wall': 'left', 'index': 2}, {'asset': 'poster_project', 'wall': 'left', 'index': 4},
                   {'asset': 'clock_wall', 'wall': 'left', 'index': 0}, {'asset': 'access_point', 'wall': 'right', 'index': 3}],
    'objects': [
        ob('shelf_metal', 0, 0, 'se'),
        ws('desk_simple', 1, 0, 'sw', 'player_f', 'bob', monitor='monitor_large', tower='workstation_dev', hair_ramp='hair_black'),
        ob('printer_mfp', 3, 0, 'sw'),
        ws('desk_simple', 4, 0, 'sw', 'employee_m_04', 'side_part', hair_ramp='hair_auburn', extra={'desk_left': 'mug'}),
        ob('kitchen_counter', 6, 0, 'sw', {'counter': 'coffee_machine'}), ob('fridge', 7, 0, 'sw'),
        ws('desk_compact', 0, 3, 'se', 'employee_f_05', 'long_straight', chair='chair_budget', laptop='laptop_standard',
           tower=None),
        ob('network_cabinet', 0, 4, 'se'),
        ob('rug_small', 2, 3),
        ob('table_meeting_small', 2, 3, 'se', {'chair_0': {'asset': 'chair_budget', 'character': person(2)},
                                               'chair_2': {'asset': 'chair_budget'}, 'center': 'speakerphone'}),
        ob('whiteboard_mobile', 5, 3, 'se'),
        ob('armchair', 6, 2, 'sw', sw={'fabric': 'sun'}),
        ob('plant_floor_tall', 7, 2), ob('plant_floor_medium', 6, 5),
        ob('moving_box', 7, 5), ob('moving_box', 7, 4), ob('trash_bin', 1, 2),
    ],
    'entrances': [{'wall': 'left', 'index': 5, 'kind': 'door', 'access_tile': [0, 5]}],
    'zones': [{'name': 'Postes', 'rect': [1, 0, 5, 2]}, {'name': 'Reunion', 'rect': [2, 3, 2, 2]},
              {'name': 'Cafe', 'rect': [6, 0, 2, 2]}],
}

# ---------------------------------------------------------------------------
# 3. Bureau moyen (12 x 9)
# ---------------------------------------------------------------------------
MEDIUM = {
    'id': 'room_office_medium', 'name_fr': 'Bureau moyen', 'usage': 'current', 'tier': 3,
    'size_tiles': [12, 9],
    'story': "L'equipe grandit : deux ilots de quatre postes, coin reunion, coin cafe et salon.",
    'floor': [{'material': 'floor_carpet_tiles', 'rect': [0, 0, 12, 9], 'swatches': {'floor': 'steel'}},
              {'material': 'floor_tile_ceramic', 'rect': [0, 5, 2, 4]}],
    'walls': {
        'left': {'segments': ['plain', 'plain', 'window', 'plain', 'window', 'plain', 'plain', 'plain', 'door'],
                 'pattern': 'two_tone', 'swatches': {'wall': 'cream', 'wall2': 'teal', 'trim': 'plastic', 'wood': 'walnut'}},
        'right': {'segments': ['plain', 'window', 'plain', 'window', 'plain', 'plain', 'window', 'plain', 'window', 'plain',
                               'plain', 'plain'], 'pattern': 'two_tone',
                  'swatches': {'wall': 'cream', 'wall2': 'teal', 'trim': 'plastic'}},
    },
    'wall_items': [{'asset': 'planning_board', 'wall': 'right', 'index': 4}, {'asset': 'poster_project', 'wall': 'left', 'index': 3},
                   {'asset': 'clock_wall', 'wall': 'left', 'index': 6}, {'asset': 'award_plaque', 'wall': 'right', 'index': 10},
                   {'asset': 'poster_schematic', 'wall': 'right', 'index': 9}, {'asset': 'access_point', 'wall': 'left', 'index': 1}],
    'objects': (
        pod_pair(3, 1, 0, 1) + pod_pair(3, 3, 2, 3, monitor='monitor_large') +
        pod_pair(8, 1, 4, 5, tower='workstation_creative') + pod_pair(8, 3, 6, 7, empty=(7,)) + [
            ob('plant_floor_tall', 0, 0), ob('bookcase', 0, 1, 'se'),
            ob('printer_mfp', 2, 0, 'sw'), ob('filing_cabinet', 6, 0, 'sw'), ob('filing_cabinet', 7, 0, 'sw'),
            ob('locker', 11, 0, 'sw'),
            ob('kitchen_counter', 0, 5, 'se', {'counter': 'coffee_machine'}), ob('kitchen_sink', 0, 6, 'se'),
            ob('fridge', 0, 7, 'se'), ob('water_cooler', 1, 8),
            ob('rug_small', 3, 6), ob('sofa', 3, 6, 'se', sw={'fabric': 'navy'}), ob('table_coffee', 4, 6, 'se'),
            chair_at(5, 7, 'nw', 9, asset='chair_meeting'),
            ob('table_meeting_small', 8, 6, 'se', {'chair_0': {'asset': 'chair_meeting', 'character': person(10)},
                                                   'chair_2': {'asset': 'chair_meeting', 'character': person(11)},
                                                   'chair_3': {'asset': 'chair_meeting'}, 'center': 'speakerphone'}),
            ob('whiteboard_mobile', 6, 7, 'sw'),
            ob('plant_floor_medium', 11, 8), ob('plant_floor_medium', 11, 5), ob('trash_bin', 6, 2),
        ]),
    'entrances': [{'wall': 'left', 'index': 8, 'kind': 'door', 'access_tile': [0, 8]}],
    'zones': [{'name': 'Ilot A', 'rect': [2, 1, 4, 4]}, {'name': 'Ilot B', 'rect': [7, 1, 4, 4]},
              {'name': 'Cafe', 'rect': [0, 5, 2, 4]}, {'name': 'Salon', 'rect': [3, 6, 3, 2]},
              {'name': 'Reunion', 'rect': [8, 6, 3, 3]}],
}

# ---------------------------------------------------------------------------
# 4. Grand open space (18 x 14)
# ---------------------------------------------------------------------------
OPEN = {
    'id': 'room_open_space', 'name_fr': 'Grand open space', 'usage': 'current', 'tier': 4,
    'size_tiles': [18, 14],
    'story': "Plateau ouvert : bancs de 3 postes, salle de reunion vitree, salon, kitchenette, cabines calmes.",
    'floor': [{'material': 'floor_carpet', 'rect': [0, 0, 18, 14], 'swatches': {'floor': 'navy'}},
              {'material': 'floor_tile_ceramic', 'rect': [0, 0, 2, 8]},
              {'material': 'floor_parquet', 'rect': [13, 8, 5, 6]},
              {'material': 'floor_wood_planks', 'rect': [13, 0, 5, 5], 'swatches': {'floor': 'birch'}}],
    'walls': {
        'left': {'segments': ['plain', 'window', 'plain', 'window', 'plain', 'window', 'plain', 'window', 'plain', 'window',
                              'plain', 'plain', 'door', 'plain'], 'pattern': 'acoustic',
                 'swatches': {'wall': 'cream', 'wall2': 'navy', 'trim': 'graphite', 'wood': 'walnut'}},
        'right': {'segments': ['plain', 'window_wide', None, 'plain', 'window_wide', None, 'plain', 'window_wide', None,
                               'plain', 'plain', 'window_wide', None, 'plain', 'window', 'plain', 'window', 'plain'],
                  'pattern': 'wallpaper', 'swatches': {'wall': 'sage', 'trim': 'graphite'}},
    },
    'wall_items': [{'asset': 'poster_project', 'wall': 'right', 'index': 9}, {'asset': 'clock_wall', 'wall': 'left', 'index': 10},
                   {'asset': 'photo_abstract', 'wall': 'right', 'index': 17}, {'asset': 'access_point', 'wall': 'right', 'index': 6}],
    'objects': (
        [ob('kitchen_counter', 0, 1, 'se'), ob('kitchen_sink', 0, 2, 'se'),
         ob('kitchen_counter', 0, 3, 'se', {'counter': 'coffee_machine'}), ob('fridge', 0, 4, 'se'),
         ob('vending_drinks', 0, 5, 'se'), ob('vending_snacks', 0, 6, 'se'), ob('water_cooler', 0, 7),
         ob('recycling_station', 0, 0, 'se')] +
        bench_pair(4, 2, [0, 1, 2], [3, 4, None]) + bench_pair(4, 6, [5, None, 6], [7, 8, 9]) +
        bench_pair(9, 2, [10, 11, 0], [1, None, 2]) + bench_pair(9, 6, [3, 4, 5], [None, 6, 7]) +
        glass_room(13, 0, 18, 5, sides=('front', 'left'), door=('left', 3)) +
        [ob('table_meeting_large', 14, 2, 'sw', {
            'chair_0': {'asset': 'chair_meeting', 'character': person(8)},
            'chair_2': {'asset': 'chair_meeting', 'character': person(9)},
            'chair_4': {'asset': 'chair_meeting', 'character': person(10)},
            'chair_1': {'asset': 'chair_meeting', 'character': person(11)}, 'chair_3': {'asset': 'chair_meeting'},
            'chair_5': {'asset': 'chair_meeting'}, 'center': 'speakerphone'}),
         ob('presentation_screen', 15, 0, 'sw', {'top': 'videoconf_bar'})] +
        [ob('partition_low', u, 10, 'sw') for u in range(3, 12)] +
        [ob('rug_large', 14, 10, 'sw'), ob('sofa', 14, 9, 'sw', sw={'fabric': 'teal'}),
         ob('table_coffee', 14, 10, 'se', {'top': 'plant_succulent'}), ob('armchair', 16, 10, 'sw', sw={'fabric': 'coral'}),
         ob('armchair', 15, 12, 'ne', sw={'fabric': 'sun'}), ob('lamp_floor', 17, 9), ob('plant_floor_tall', 13, 13),
         ob('bookcase', 16, 12, 'sw')] +
        [ob('partition_acoustic', 1, 11, 'sw'), ob('partition_acoustic', 2, 11, 'se'), ob('partition_acoustic', 2, 12, 'se'),
         chair_at(1, 12, 'se', 3, asset='chair_meeting')] +
        [ob('locker', 6, 0, 'sw'), ob('locker', 7, 0, 'sw'), ob('locker', 8, 0, 'sw'), ob('printer_mfp', 11, 0, 'sw'),
         ob('plant_floor_medium', 3, 0), ob('plant_floor_medium', 12, 0), ob('plant_floor_medium', 12, 9),
         ob('plant_floor_tall', 3, 13)]),
    'entrances': [{'wall': 'left', 'index': 12, 'kind': 'door', 'access_tile': [0, 12]}],
    'zones': [{'name': 'Bancs', 'rect': [3, 2, 9, 7]}, {'name': 'Reunion vitree', 'rect': [13, 0, 5, 5]},
              {'name': 'Salon', 'rect': [13, 8, 5, 6]}, {'name': 'Kitchenette', 'rect': [0, 0, 2, 8]},
              {'name': 'Cabine calme', 'rect': [1, 11, 2, 2]}],
}

# ---------------------------------------------------------------------------
# 5. Plateau de siege (24 x 18) compose de modules
# ---------------------------------------------------------------------------
HQ = {
    'id': 'room_hq_floor', 'name_fr': 'Plateau de siège', 'usage': 'current', 'tier': 5,
    'size_tiles': [24, 18],
    'story': "Siege : accueil, deux salles vitrees, kitchenette, sanitaires, ilots, salon et bureau de direction.",
    'floor': [{'material': 'floor_carpet_tiles', 'rect': [0, 0, 24, 18], 'swatches': {'floor': 'sage'}}],
    'include': [{'module': 'module_kitchenette', 'offset': [0, 0]}, {'module': 'module_restroom', 'offset': [0, 7]},
                {'module': 'module_meeting_room', 'offset': [12, 0], 'cast': [person(1), person(4), person(7)]},
                {'module': 'module_meeting_room', 'offset': [18, 0], 'cast': [person(10), person(3), person(6)]},
                {'module': 'module_pod_4', 'offset': [5, 1], 'cast': [person(2), person(3)]},
                {'module': 'module_pod_4', 'offset': [5, 4], 'cast': [person(4), person(5)]},
                {'module': 'module_pod_4', 'offset': [8, 1], 'cast': [person(6), person(7)]},
                {'module': 'module_pod_4', 'offset': [8, 4], 'cast': [person(8), person(9)]},
                {'module': 'module_pod_4', 'offset': [5, 7], 'cast': [person(10), person(11)]},
                {'module': 'module_lounge', 'offset': [9, 12]}, {'module': 'module_reception', 'offset': [1, 12],
                                                                  'cast': [person(0), person(5)]}],
    'walls': {
        'left': {'segments': ['plain', 'plain', 'plain', 'plain', 'plain', 'plain', 'plain', 'plain', 'plain', 'plain',
                              'window', 'plain', 'window', 'plain', 'window', 'plain', 'door', 'plain'],
                 'pattern': 'wood_panels', 'swatches': {'wood': 'oak', 'trim': 'plastic', 'wall': 'cream'}},
        'right': {'segments': ['plain', 'window_wide', None, 'plain', 'window_wide', None, 'plain', 'plain', 'window', 'plain',
                               'plain', 'plain', 'window_wide', None, 'window_wide', None, 'plain', 'plain', 'window_wide',
                               None, 'window_wide', None, 'plain', 'plain'], 'pattern': 'concrete',
                  'swatches': {'wall': 'concrete', 'trim': 'graphite'}},
    },
    'wall_items': [{'asset': 'poster_project', 'wall': 'right', 'index': 9}, {'asset': 'award_plaque', 'wall': 'left', 'index': 13},
                   {'asset': 'clock_wall', 'wall': 'left', 'index': 11}, {'asset': 'photo_abstract', 'wall': 'right', 'index': 16}],
    'objects': [
        ob('rack_compact', 12, 7, 'sw'), ob('network_cabinet', 13, 7, 'sw'),
        ws('desk_corner', 18, 12, 'se', 'employee_f_03', 'bun', chair='chair_ergonomic', monitor='monitor_large',
           tower='workstation_compute', keyboard=True),
        ob('bookcase', 17, 10, 'se'), ob('armchair', 21, 15, 'ne', sw={'fabric': 'navy'}),
        ob('armchair', 22, 15, 'ne', sw={'fabric': 'navy'}), ob('table_coffee', 21, 14, 'se'),
        ob('plant_floor_tall', 23, 17), ob('plant_floor_tall', 15, 17), ob('plant_floor_medium', 11, 8),
        ob('whiteboard_mobile', 9, 8, 'se'), ob('printer_mfp', 11, 0, 'sw'), ob('trash_bin', 4, 4),
    ],
    'entrances': [{'wall': 'left', 'index': 16, 'kind': 'door', 'access_tile': [0, 16]}],
    'zones': [{'name': 'Direction', 'rect': [17, 10, 7, 8]}, {'name': 'Ilots', 'rect': [5, 1, 7, 6]},
              {'name': 'Local technique', 'rect': [12, 7, 2, 1]}],
}

# ---------------------------------------------------------------------------
# 6. Salle serveur compacte (6 x 5)
# ---------------------------------------------------------------------------
SERVER = {
    'id': 'room_server_compact', 'name_fr': 'Salle serveur compacte', 'usage': 'current', 'tier': 3,
    'size_tiles': [6, 5],
    'story': "Premier hebergement maison : une rangee de baies, onduleur, climatisation, poste d'admin.",
    'floor': [{'material': 'floor_raised', 'rect': [0, 0, 6, 5]},
              {'material': 'floor_raised_vent', 'rect': [1, 1, 4, 1]}],
    'walls': {
        'left': {'segments': ['plain', 'plain', 'plain', 'plain', 'plain'], 'pattern': 'paint',
                 'swatches': {'wall': 'concrete', 'trim': 'graphite'}},
        'right': {'segments': ['plain', 'plain', 'plain', 'plain', 'plain', 'door'], 'pattern': 'acoustic',
                  'swatches': {'wall': 'concrete', 'wall2': 'steel', 'trim': 'graphite', 'wood': 'birch'}},
    },
    'wall_items': [{'asset': 'fiber_box', 'wall': 'left', 'index': 4}],
    'objects': [
        rack('rack_standard', 1, 0, 'sw', RACK_MIX[0]), rack('rack_standard', 2, 0, 'sw', RACK_MIX[1]),
        rack('rack_standard', 3, 0, 'sw', RACK_MIX[2]), rack('rack_compact', 4, 0, 'sw',
                                                             ['module_patch_panel_1u', 'module_network_switch_1u',
                                                              'module_server_1u', 'module_storage_2u', 'module_ups_2u']),
        ob('crac_unit', 0, 1, 'se'), ob('ups_floor', 0, 3, 'se'), ob('power_distribution_cabinet', 0, 4, 'se'),
        {'asset': 'desk_tech', 'tile': [2, 3], 'facing': 'sw', 'children': {
            'monitor_main': 'monitor_large', 'monitor_second': 'monitor_basic', 'keyboard': 'keyboard_mouse',
            'desk_right': 'nas_desktop', 'chair': {'asset': 'chair_office', 'character': person(8)}}},
        ob('cable_tray_straight', 1, 0, 'sw'), ob('cable_tray_straight', 2, 0, 'sw'), ob('cable_tray_straight', 3, 0, 'sw'),
        ob('cable_tray_straight', 4, 0, 'sw'),
    ],
    'entrances': [{'wall': 'right', 'index': 5, 'kind': 'door', 'access_tile': [5, 0]}],
    'zones': [{'name': 'Allee froide', 'rect': [1, 1, 4, 1]}, {'name': 'Admin', 'rect': [2, 3, 2, 2]}],
}

# ---------------------------------------------------------------------------
# 7. Datacenter (16 x 12)
# ---------------------------------------------------------------------------
def rack_row(V, facing, U0, U1, cooler_at=(), dense=False):
    out = []
    for k, u in enumerate(range(U0, U1)):
        if u in cooler_at:
            out.append(ob('rack_cooler_inrow', u, V, facing))
        else:
            asset = 'rack_high_density' if dense and k % 3 == 1 else 'rack_standard'
            out.append(rack(asset, u, V, facing, RACK_MIX[k % 3] if facing in ('se', 'sw') else None))
    return out


DATACENTER = {
    'id': 'room_datacenter', 'name_fr': 'Datacenter', 'usage': 'current', 'tier': 6,
    'size_tiles': [16, 12],
    'story': "Allees chaudes / froides, refroidissement de rangee, climatiseurs, distribution electrique, supervision.",
    'floor': [{'material': 'floor_raised', 'rect': [0, 0, 16, 12]},
              {'material': 'floor_raised_vent', 'rect': [2, 3, 10, 1]}, {'material': 'floor_raised_vent', 'rect': [2, 8, 10, 1]}],
    'walls': {
        'left': {'segments': ['plain'] * 11 + ['door'], 'pattern': 'concrete', 'swatches': {'wall': 'concrete', 'trim': 'graphite'}},
        'right': {'segments': ['plain'] * 16, 'pattern': 'acoustic',
                  'swatches': {'wall': 'concrete', 'wall2': 'graphite', 'trim': 'graphite'}},
    },
    'objects': (
        rack_row(2, 'sw', 2, 12, cooler_at=(6,)) + rack_row(4, 'ne', 2, 12, cooler_at=(6,)) +
        rack_row(7, 'sw', 2, 12, cooler_at=(7,), dense=True) + rack_row(9, 'ne', 2, 12, cooler_at=(7,)) +
        [ob('crac_unit', 0, 2, 'se'), ob('crac_unit', 0, 6, 'se'),
         ob('power_distribution_cabinet', 12, 0, 'sw'), ob('power_distribution_cabinet', 13, 0, 'sw'),
         ob('ups_floor', 14, 0, 'sw'), ob('ups_floor', 15, 0, 'sw')] +
        [ob('cable_tray_straight', u, v, 'sw') for v in (2, 4, 7, 9) for u in range(2, 12)] +
        [ob('security_cage_panel', u, 11, 'sw') for u in range(2, 12) if u != 6] + [ob('security_cage_door', 6, 11, 'sw')] +
        [{'asset': 'supervision_console', 'tile': [13, 7], 'facing': 'se', 'children': {
            'chair': {'asset': 'chair_ergonomic', 'character': person(4)}}}]),
    'entrances': [{'wall': 'left', 'index': 11, 'kind': 'door', 'access_tile': [0, 11]}],
    'zones': [{'name': 'Allee froide A', 'rect': [2, 3, 10, 1]}, {'name': 'Allee froide B', 'rect': [2, 8, 10, 1]},
              {'name': 'Allee chaude', 'rect': [2, 5, 10, 2]}, {'name': 'Supervision', 'rect': [13, 7, 3, 4]}],
}

# ---------------------------------------------------------------------------
# 8. Atelier de prototypage / test materiel (10 x 8)
# ---------------------------------------------------------------------------
WORKSHOP = {
    'id': 'room_hw_workshop', 'name_fr': 'Atelier de prototypage et de test matériel', 'usage': 'current', 'tier': 4,
    'size_tiles': [10, 8],
    'story': "Etablis electroniques, impression 3D, decoupe laser, bancs de test PC et embarque, cellule cobot, studio.",
    'floor': [{'material': 'floor_vinyl', 'rect': [0, 0, 10, 8], 'swatches': {'floor': 'sage', 'floor2': 'cream'}}],
    'walls': {
        'left': {'segments': ['plain', 'window', 'plain', 'plain', 'window', 'plain', 'plain', 'door'], 'pattern': 'two_tone',
                 'swatches': {'wall': 'cream', 'wall2': 'sage', 'trim': 'plastic', 'wood': 'oak'}},
        'right': {'segments': ['plain', 'plain', 'window_wide', None, 'plain', 'plain', 'plain', 'window', 'plain', 'plain'],
                  'pattern': 'paint', 'swatches': {'wall': 'cream', 'trim': 'plastic'}},
    },
    'wall_items': [{'asset': 'poster_schematic', 'wall': 'right', 'index': 5}, {'asset': 'clock_wall', 'wall': 'left', 'index': 5},
                   {'asset': 'sticky_notes', 'wall': 'left', 'index': 3}],
    'objects': [
        ob('workbench_electronics', 1, 0, 'sw', {'bench_left': 'soldering_station', 'bench_center': 'microscope',
                                                 'bench_right': 'lab_power_supply', 'shelf_left': 'oscilloscope',
                                                 'chair': {'asset': 'chair_office', 'character': person(0)}}),
        ob('workbench_electronics', 4, 0, 'sw', {'bench_left': 'oscilloscope', 'bench_center': 'cnc_mill_desktop',
                                                 'bench_right': 'soldering_station', 'shelf_right': 'lab_power_supply',
                                                 'chair': {'asset': 'chair_office', 'character': person(3)}}),
        ob('printer_3d', 7, 0, 'sw'), ob('printer_3d', 8, 0, 'sw'), ob('instrument_rack', 9, 0, 'sw'),
        ob('shelf_metal', 0, 1, 'se'), ob('laser_cutter', 0, 3, 'se'),
        ob('test_bench_pc', 0, 5, 'se', {'chair': {'asset': 'chair_office', 'character': person(6)}}),
        ob('test_bench_embedded', 4, 3, 'nw', {'chair': {'asset': 'chair_office', 'character': person(5)}}),
        ob('calibration_bench', 5, 3, 'se'), chair_at(6, 4, 'nw', 7),
        ob('cobot_cell', 8, 4, 'se'),
        ob('greenscreen_backdrop', 4, 7, 'sw'), ob('softbox_light', 3, 6), ob('mic_stand', 5, 6), ob('camera_tripod', 6, 6),
        ob('plant_floor_medium', 9, 7), ob('plant_floor_medium', 1, 7), ob('trash_bin', 3, 1),
    ],
    'entrances': [{'wall': 'left', 'index': 7, 'kind': 'door', 'access_tile': [0, 7]}],
    'zones': [{'name': 'Electronique', 'rect': [1, 0, 6, 2]}, {'name': 'Fabrication rapide', 'rect': [0, 3, 2, 4]},
              {'name': 'Ilot de test', 'rect': [3, 3, 4, 2]}, {'name': 'Robotique', 'rect': [8, 4, 2, 2]},
              {'name': 'Studio capture', 'rect': [3, 6, 4, 2]}],
}

# ---------------------------------------------------------------------------
# 9. Hall de fabrication et d'assemblage (22 x 16) - futur
# ---------------------------------------------------------------------------
def conv_line_u(V, U0, U1, skip=()):
    return [ob('conveyor_straight', u, V, 'sw') for u in range(U0, U1) if u not in skip]


FACTORY = {
    'id': 'room_factory_hall', 'name_fr': "Hall de fabrication et d'assemblage (étape future)", 'usage': 'future', 'tier': 7,
    'size_tiles': [22, 16],
    'story': "Ligne CMS (placement, refusion, inspection, test), assemblage, flash, stress-test, emballage ; salle propre.",
    'floor': [{'material': 'floor_concrete', 'rect': [0, 0, 22, 16]},
              {'material': 'floor_vinyl', 'rect': [15, 0, 7, 4], 'swatches': {'floor': 'plastic', 'floor2': 'steel'}}],
    'floor_overlays': [{'asset': 'floor_marking', 'key': 'line_u', 'tile': [u, 5]} for u in range(0, 22)] +
                      [{'asset': 'floor_marking', 'key': 'line_u', 'tile': [u, 10]} for u in range(0, 22)] +
                      [{'asset': 'floor_marking', 'key': 'hatch', 'tile': [u, v]} for u in (19, 20) for v in (13, 14)],
    'walls': {
        'left': {'segments': ['plain'] * 14 + ['door', 'plain'], 'pattern': 'concrete',
                 'swatches': {'wall': 'concrete', 'trim': 'graphite'}},
        'right': {'segments': ['plain', 'window_wide', None, 'plain', 'window_wide', None, 'plain', 'plain', 'window_wide', None,
                               'plain', 'plain', 'plain', 'plain', 'plain', 'plain', 'plain', 'plain', 'plain', 'plain',
                               'plain', 'plain'], 'pattern': 'concrete', 'swatches': {'wall': 'concrete', 'trim': 'graphite'}},
    },
    'objects': (
        [ob('pick_and_place_machine', 1, 7, 'sw'), ob('conveyor_straight', 3, 7, 'se', {'belt': 'product_pcb'}),
         ob('reflow_oven', 4, 7, 'sw'), ob('conveyor_straight', 7, 7, 'se', {'belt': 'product_pcb'}),
         ob('aoi_inspection', 8, 7, 'sw'), ob('conveyor_straight', 9, 7, 'se'),
         ob('pcb_tester', 10, 7, 'sw'), ob('conveyor_straight', 12, 7, 'se', {'belt': 'product_gpu'}),
         ob('conveyor_straight', 13, 7, 'se'), ob('conveyor_transfer_table', 14, 7, 'se'),
         {'asset': 'assembly_station_manual', 'tile': [15, 7], 'facing': 'sw', 'children': {
             'chair': {'asset': 'chair_office', 'character': person(4)}, 'bench_center': 'product_case'}},
         ob('assembly_cell_robotic', 17, 6, 'sw'), ob('conveyor_straight', 19, 7, 'se', {'belt': 'product_pc_assembled'}),
         ob('conveyor_end', 20, 7, 'se')] +
        [{'asset': 'flash_station', 'tile': [1, 12], 'facing': 'sw', 'children': {
            'chair': {'asset': 'chair_office', 'character': person(9)}}},
         ob('conveyor_straight', 3, 12, 'se'), ob('stress_test_rack', 4, 12, 'sw'), ob('conveyor_straight', 6, 12, 'se'),
         ob('packing_station', 7, 12, 'sw', {'table_center': 'product_box'}), ob('conveyor_straight', 9, 12, 'se'),
         ob('storage_rack_industrial', 10, 12, 'sw', {'level_0': 'product_box', 'level_1': 'tote_bin'}),
         ob('storage_rack_industrial', 12, 12, 'sw', {'level_0': 'tote_bin', 'level_2': 'product_box'}),
         ob('pallet', 15, 12, 'sw', {'load': 'product_box'}), ob('pallet', 16, 12, 'sw'), ob('tote_bin', 14, 14)] +
        [ob('cleanroom_partition', 14, v, 'se') for v in (0, 2, 3)] + [ob('cleanroom_airlock', 14, 1, 'se')] +
        [ob('cleanroom_partition', u, 4, 'sw') for u in range(15, 22)] +
        [ob('chip_fab_tool', 17, 0, 'sw'), ob('precision_station', 20, 0, 'sw', {'stage': 'product_wafer'})] +
        [ob('dust_extraction', 0, 0, 'se'), ob('industrial_chiller', 2, 0, 'sw'), ob('control_cabinet', 4, 0, 'sw'),
         ob('control_cabinet', 5, 0, 'sw'), ob('storage_rack_industrial', 7, 0, 'sw', {'level_0': 'tote_bin'})]),
    'entrances': [{'wall': 'left', 'index': 14, 'kind': 'door', 'access_tile': [0, 14]}],
    'zones': [{'name': 'Ligne CMS', 'rect': [1, 7, 13, 2]}, {'name': 'Assemblage', 'rect': [14, 7, 6, 2]},
              {'name': 'Finition et expedition', 'rect': [1, 12, 16, 2]}, {'name': 'Salle propre', 'rect': [15, 0, 7, 4]},
              {'name': 'Utilites', 'rect': [0, 0, 8, 2]}],
}

# ---------------------------------------------------------------------------
# 10. Laboratoire avance + zone quantique (14 x 10)
# ---------------------------------------------------------------------------
LAB = {
    'id': 'room_advanced_lab', 'name_fr': 'Laboratoire avancé (zone de calcul quantique)', 'usage': 'current', 'tier': 8,
    'size_tiles': [14, 10],
    'story': "Recherche avancee : calculateurs, espace XR, et zone vitree du cryostat quantique avec son controle.",
    'floor': [{'material': 'floor_vinyl', 'rect': [0, 0, 14, 10], 'swatches': {'floor': 'plastic', 'floor2': 'steel'}},
              {'material': 'floor_raised', 'rect': [7, 0, 7, 5]}],
    'walls': {
        'left': {'segments': ['plain', 'window', 'plain', 'plain', 'window', 'plain', 'plain', 'plain', 'door', 'plain'],
                 'pattern': 'paint', 'swatches': {'wall': 'plastic', 'trim': 'graphite', 'wood': 'birch'}},
        'right': {'segments': ['plain', 'plain', 'window_wide', None, 'plain', 'plain', 'plain', 'plain', 'plain', 'plain',
                               'plain', 'plain', 'plain', 'plain'], 'pattern': 'acoustic',
                  'swatches': {'wall': 'plastic', 'wall2': 'navy', 'trim': 'graphite'}},
    },
    'wall_items': [{'asset': 'poster_schematic', 'wall': 'left', 'index': 6}, {'asset': 'planning_board', 'wall': 'right', 'index': 5}],
    'objects': (
        [ob('research_station', 0, 2, 'se', {'chair': {'asset': 'chair_ergonomic', 'character': person(11)}}),
         ob('research_station', 0, 5, 'se', {'chair': {'asset': 'chair_ergonomic', 'character': person(2)}}),
         ob('compute_cabinet', 2, 0, 'sw'), ob('compute_cabinet', 4, 0, 'sw')] +
        glass_room(7, 0, 14, 5, sides=('front', 'left'), door=('left', 2)) +
        [ob('quantum_control_rack', 8, 0, 'sw'), ob('quantum_control_rack', 9, 0, 'sw'),
         ob('quantum_cryostat_open', 10, 0, 'sw'), ob('quantum_cryostat_closed', 12, 0, 'sw'),
         ob('cryo_support_unit', 8, 3, 'sw'), ob('supervision_terminal', 11, 3, 'sw')] +
        [ob('xr_play_mat', 2, 6), ob('xr_base_station', 1, 6), ob('xr_base_station', 4, 8),
         ob('desk_tech', 6, 6, 'se', {'monitor_main': 'monitor_ultrawide', 'keyboard': 'keyboard_mouse',
                                      'desk_left': 'xr_headset_stand',
                                      'chair': {'asset': 'chair_ergonomic', 'character': person(1)}})] +
        [ob('whiteboard_mobile', 5, 2, 'se'), ob('plant_floor_tall', 13, 9), ob('plant_floor_medium', 6, 0)]),
    'entrances': [{'wall': 'left', 'index': 8, 'kind': 'door', 'access_tile': [0, 8]}],
    'zones': [{'name': 'Zone quantique (vitree)', 'rect': [7, 0, 7, 5]}, {'name': 'Calcul', 'rect': [2, 0, 4, 2]},
              {'name': 'Espace XR', 'rect': [1, 6, 4, 3]}, {'name': 'Recherche', 'rect': [0, 2, 2, 5]}],
}

PLANS = [BEDROOM, STARTUP, MEDIUM, OPEN, HQ, SERVER, DATACENTER, WORKSHOP, FACTORY, LAB]


def main(only=None):
    lib = Library(ROOT)
    os.makedirs(os.path.join(ROOT, 'rooms'), exist_ok=True)
    os.makedirs(os.path.join(ROOT, 'rooms', 'modules'), exist_ok=True)
    for mid, mod in MODULES.items():
        json.dump(mod, open(os.path.join(ROOT, 'rooms', 'modules', f'{mid}.json'), 'w', encoding='utf-8'),
                  ensure_ascii=False, indent=1)
    report = {}
    for plan in PLANS:
        if only and plan['id'] not in only:
            continue
        full = expand_modules(plan, MODULES)
        full, issues = validate(full, lib)
        full['validation'] = {'issues': issues, 'ok': not issues}
        json.dump(full, open(os.path.join(ROOT, 'rooms', f"{plan['id']}.json"), 'w', encoding='utf-8'),
                  ensure_ascii=False, indent=1)
        render_plan(full, lib, os.path.join(ROOT, 'previews', 'rooms', f"{plan['id']}.png"))
        report[plan['id']] = issues
        print(plan['id'], 'OK' if not issues else f'{len(issues)} probleme(s)', issues[:6])
    return report


if __name__ == '__main__':
    main(set(sys.argv[1].split(',')) if len(sys.argv) > 1 else None)
