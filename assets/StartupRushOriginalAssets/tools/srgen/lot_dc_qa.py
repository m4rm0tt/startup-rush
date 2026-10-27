"""QA du lot datacenter / laboratoires (execute, deterministe).

Usage : python StartupRushOriginalAssets/tools/srgen/lot_dc_qa.py
  - previews/check_racks.png : chaque baie remplie de modules en se/sw, positionnes
    UNIQUEMENT via le manifeste (slots.positions[i].offset_px + ancre du module) ;
    verifie que chaque pixel opaque d'un module tombe dans le parallelogramme de
    ses slots (rapport imprime, code de sortie 1 si un pixel deborde).
  - previews/check_dc_{datacenter,lab,quantum,studio}.png : scenes composees avec
    le compositeur de reference (tools/srgen/scene.py) a partir de build_cache/.
Necessite un build prealable :
  python StartupRushOriginalAssets/tools/build_all.py --modules defs_servers,defs_laboratories
"""
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(ROOT, 'tools'))

import numpy as np  # noqa: E402
from srgen.imgio import over, save_png, upscale, shadow_layer_to_rgba  # noqa: E402
from srgen.iso import to_world, screen_of  # noqa: E402
from srgen.scene import Library, Scene, Node  # noqa: E402
from srgen.preview import draw_text  # noqa: E402

FILL = {
    'rack_compact': ['module_patch_panel_1u', 'module_network_switch_1u', 'module_server_1u', 'module_storage_2u',
                     'module_ups_2u', 'module_blank_1u'],
    'rack_standard': ['module_patch_panel_1u', 'module_network_switch_1u', 'module_cable_manager_1u', 'module_server_1u',
                      'module_server_1u', 'module_compute_2u', 'module_compute_node_2u', 'module_storage_2u',
                      'module_blank_1u', None, None, 'module_storage_jbod_4u', 'module_ups_2u'],
    'rack_high_density': ['module_network_switch_1u', 'module_patch_panel_1u', 'module_gpu_4u', 'module_gpu_4u',
                          'module_gpu_4u', 'module_compute_node_2u', 'module_compute_node_2u', 'module_blank_1u',
                          'module_cable_manager_1u'],
}
FILL_A = ['module_patch_panel_1u', 'module_network_switch_1u', 'module_cable_manager_1u', 'module_server_1u',
          'module_server_1u', 'module_compute_2u', 'module_compute_2u', 'module_storage_2u', 'module_blank_1u',
          'module_storage_jbod_4u', 'module_ups_2u']
FILL_B = ['module_network_switch_1u', 'module_patch_panel_1u', 'module_gpu_4u', 'module_gpu_4u',
          'module_compute_node_2u', 'module_compute_node_2u', 'module_gpu_4u', 'module_blank_1u']


def plan_modules(lib, rack_id, facing, fill):
    """Liste (index_de_slot, id_module, unites) en remplissant de haut en bas (None = slot vide)."""
    sl = lib.get(rack_id)['orientations'][facing]['slots']
    out, slot = [], 0
    for mid in fill:
        if mid is None:
            slot += 1
            continue
        n = lib.get(mid)['units']
        if slot + n > sl['count']:
            break
        out.append((slot, mid, n))
        slot += n
    return out


def check_racks(lib):
    W, H = 760, 150
    img = np.zeros((H, W, 4), np.uint8)
    img[..., :3] = (216, 207, 194)
    img[..., 3] = 255
    report, total, bad = [], 0, 0
    x = 40
    for rid, fill in FILL.items():
        e = lib.get(rid)
        for f in ('se', 'sw'):
            o = e['orientations'][f]
            sl = o['slots']
            ax, ay = x + 60, 120
            if 'shadow' in o['layers']:
                over(img, shadow_layer_to_rgba(lib.png(o['layers']['shadow']['file'])[..., 3]),
                     ax - o['anchor'][0], ay - o['anchor'][1])
            over(img, lib.png(o['layers']['base']['file']), ax - o['anchor'][0], ay - o['anchor'][1])
            for (s_i, mid, n) in reversed(plan_modules(lib, rid, f, fill)):   # du bas vers le haut
                mo = lib.get(mid)['orientations'][f]
                ox, oy = sl['positions'][s_i]['offset_px']
                sp = lib.png(mo['layers']['base']['file'])
                px0, py0 = ax + ox - mo['anchor'][0], ay + oy - mo['anchor'][1]
                over(img, sp, px0, py0)
                corners = []
                for (lx, lz) in ((0, 0), (20, 0), (20, -4 * n), (0, -4 * n)):
                    u, v, _ = to_world(f, 0, 0, lx, 0, lz)
                    sx, sy = screen_of(u, v, lz)
                    corners.append((ax + ox + sx, ay + oy + sy))
                ys, xs = np.nonzero(sp[..., 3] > 0)
                cx = xs + px0 + 0.5
                cy = ys + py0 + 0.5
                crs = []
                for k in range(4):
                    (x1, y1), (x2, y2) = corners[k], corners[(k + 1) % 4]
                    crs.append((x2 - x1) * (cy - y1) - (y2 - y1) * (cx - x1))
                sgn = np.sign(sum(np.sum(c) for c in crs)) or 1
                inside = np.all([c * sgn >= -1e-9 for c in crs], axis=0)
                nb = int((~inside).sum())
                total += len(cx)
                bad += nb
                report.append(f"{rid:18s} {f} slot {s_i:2d} {mid:26s} {nb} px hors enveloppe / {len(cx)}")
            draw_text(img, x, 136, f"{rid[5:]} {f}")
            x += 120
    save_png(os.path.join(ROOT, 'previews', 'check_racks.png'), upscale(img, 2))
    return report, total, bad


def add_modules(lib, node, facing, fill):
    sl = lib.get(node.aid)['orientations'][facing]['slots']
    for (s_i, mid, n) in plan_modules(lib, node.aid, facing, fill):
        ox, oy = sl['positions'][s_i]['offset_px']
        node.after.append(Node(mid, facing, node.sx + ox, node.sy + oy, -s_i))


def scene_datacenter(lib):
    Wt, Dt = 11, 9
    sc = Scene(lib, origin=(330, 150))
    sc.room_shell(Wt, Dt, ['plain'] * Dt, ['plain', 'plain', 'door'] + ['plain'] * 8, 'floor_raised', 'acoustic', 'paint',
                  {'wall': 'cream', 'wall2': 'navy'}, {'wall': 'concrete'})
    e = lib.get('floor_raised_vent')
    var = e['variants']['u0v0']
    for V in range(1, 8):
        x, y = sc.tile_screen(4, V)
        sc.floor.append((lib.png(var['layers']['base']['file']), x - var['anchor'][0], y - var['anchor'][1]))
    for V in range(1, 8):
        aid = 'rack_cooler_inrow' if V == 4 else ('rack_high_density' if V in (6, 7) else 'rack_standard')
        n = sc.place(aid, 3, V, 'se')
        if aid != 'rack_cooler_inrow':
            add_modules(lib, n, 'se', FILL_B if aid == 'rack_high_density' else FILL_A)
        sc.place('rack_standard' if V != 4 else 'rack_cooler_inrow', 5, V, 'nw')
        sc.place('cable_tray_straight', 4, V, 'se')
    sc.place('cable_tray_corner', 4, 0, 'nw')
    for U in (5, 6):
        sc.place('cable_tray_straight', U, 0, 'sw')
    sc.place('crac_unit', 0, 1, 'se')
    sc.place('power_distribution_cabinet', 0, 4, 'se')
    sc.place('ups_floor', 0, 5, 'se')
    sc.place('ups_floor', 0, 6, 'se')
    sc.place('server_tower_classic', 8, 1, 'sw')
    rc = sc.place('rack_compact', 9, 1, 'sw')
    add_modules(lib, rc, 'sw', FILL['rack_compact'])
    for (V, aid) in ((3, 'security_cage_panel'), (4, 'security_cage_door'), (5, 'security_cage_panel')):
        sc.place(aid, 7, V, 'se')
    chair = {'chair': {'asset': 'chair_office'}} if 'chair_office' in lib.index else None
    sc.place('supervision_console', 8, 6, 'nw', chair)
    for U in (8, 9):
        sc.place('cable_conduit', U, 8, 'sw')
    return sc.render((900, 640))


def scene_lab(lib):
    Wt, Dt = 10, 8
    sc = Scene(lib, origin=(300, 150))
    sc.room_shell(Wt, Dt, ['plain', 'window', 'plain', 'plain', 'window', 'plain', 'plain', 'plain'],
                  ['window', 'plain', 'window', 'plain', 'plain', 'window', 'plain', 'plain', 'plain', 'plain'],
                  'floor_vinyl', 'paint', 'two_tone', {'wall': 'cream'}, {'wall': 'cream', 'wall2': 'sage'})
    ch = {'asset': 'chair_office'} if 'chair_office' in lib.index else None

    def kids(d):
        if ch:
            d = dict(d, chair=ch)
        return d
    sc.place('workbench_electronics', 0, 1, 'se', kids({'bench_left': 'oscilloscope', 'bench_center': 'microscope',
                                                         'bench_right': 'soldering_station', 'shelf_left': 'lab_power_supply'}))
    sc.place('workbench_electronics', 0, 4, 'se', kids({'bench_left': 'cnc_mill_desktop', 'bench_right': 'studio_monitors'}))
    sc.place('test_bench_pc', 2, 0, 'sw', kids({}))
    sc.place('test_bench_embedded', 5, 0, 'sw', kids({}))
    sc.place('printer_3d', 8, 0, 'sw')
    sc.place('research_station', 9, 1, 'nw', kids({}))
    sc.place('instrument_rack', 0, 6, 'se')
    sc.place('laser_cutter', 3, 3, 'se')
    sc.place('cobot_cell', 6, 4, 'sw')
    sc.place('calibration_bench', 3, 6, 'se')
    sc.place('supervision_terminal', 9, 6, 'sw')
    return sc.render((860, 600))


def scene_quantum(lib):
    Wt, Dt = 9, 7
    sc = Scene(lib, origin=(290, 150))
    sc.room_shell(Wt, Dt, ['plain'] * Dt, ['plain', 'window_wide', None] + ['plain'] * 6, 'floor_vinyl', 'acoustic', 'paint',
                  {'wall': 'cream', 'wall2': 'sky'}, {'wall': 'plastic'}, floor_sw={'floor': 'plastic', 'floor2': 'steel'})
    sc.place('quantum_control_rack', 0, 2, 'se')
    sc.place('quantum_control_rack', 0, 3, 'se')
    sc.place('quantum_cryostat_open', 2, 2, 'se')
    sc.place('quantum_cryostat_closed', 5, 4, 'se')
    sc.place('cryo_support_unit', 6, 0, 'sw')
    sc.place('compute_cabinet', 0, 5, 'se')
    sc.place('supervision_terminal', 3, 5, 'nw')
    return sc.render((760, 560))


def scene_studio(lib):
    Wt, Dt = 7, 6
    sc = Scene(lib, origin=(240, 150))
    sc.room_shell(Wt, Dt, ['plain'] * Dt, ['plain'] * Wt, 'floor_carpet', 'acoustic', 'acoustic',
                  {'wall': 'cream', 'wall2': 'graphite'}, {'wall': 'cream', 'wall2': 'graphite'})
    sc.place('greenscreen_backdrop', 2, 0, 'sw')
    sc.place('camera_tripod', 3, 3, 'nw')
    sc.place('softbox_light', 1, 2, 'se')
    sc.place('softbox_light', 5, 2, 'sw')
    sc.place('mic_stand', 4, 2, 'nw')
    sc.place('xr_play_mat', 4, 3, 'se')
    sc.place('xr_base_station', 6, 1, 'se')
    sc.place('xr_base_station', 4, 5, 'se')
    return sc.render((600, 470))


def main():
    lib = Library(ROOT)
    report, total, bad = check_racks(lib)
    print('\n'.join(report))
    print(f"TOTAL {total} px de modules, {bad} hors enveloppe")
    out = os.path.join(ROOT, 'previews')
    for name, fn in (('datacenter', scene_datacenter), ('lab', scene_lab), ('quantum', scene_quantum),
                     ('studio', scene_studio)):
        save_png(os.path.join(out, f'check_dc_{name}.png'), upscale(fn(lib), 2))
        print(f"previews/check_dc_{name}.png")
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main())
