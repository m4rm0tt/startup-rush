"""Plans de pieces predefinies : construction, validation et rendu de reference.

Un plan (rooms/<id>.json) decrit : taille en cases (U x V), sols par zones,
murs gauche/droit (segments + motif + teintes), objets muraux, objets au sol
(case du coin nord, orientation, enfants dans les sockets, personnage assis),
modules inclus (salle de reunion, kitchenette...), entrees, zones nommees.
Le constructeur calcule cases occupees / libres / reservees (acces) et verifie
que chaque asset, orientation et socket existe dans le manifeste.
"""
import copy
import json
import os

import numpy as np

from .scene import Scene, Library, topo_sort
from .imgio import save_png, upscale, over, load_png
from .compose import recolor


class PlanError(Exception):
    pass


def _rot_cells(e, facing):
    o = e['orientations'].get(facing)
    if o is None:
        raise PlanError(f"{e['id']} : orientation {facing} absente")
    return o.get('collision_tiles', []), o.get('footprint_world_tiles', [1, 1]), o.get('access', [])


def expand_modules(plan, modules):
    """Insere les modules (objets, sols, zones) decales de leur offset."""
    p = copy.deepcopy(plan)
    for inc in plan.get('include', []):
        mod = modules[inc['module']]
        du, dv = inc['offset']
        for f in mod.get('floor', []):
            f2 = dict(f); r = f['rect']; f2['rect'] = [r[0] + du, r[1] + dv, r[2], r[3]]
            p.setdefault('floor', []).append(f2)
        cast = inc.get('cast')
        k = [0]

        def recast(spec):
            # remplace les personnages du module par la distribution propre a cette instance
            if not cast or not isinstance(spec, dict):
                return
            if 'character' in spec and spec['character']:
                spec['character'] = dict(cast[k[0] % len(cast)])
                k[0] += 1
            for v in (spec.get('children') or {}).values():
                recast(v)
        for ob in mod.get('objects', []):
            o2 = copy.deepcopy(ob); o2['tile'] = [ob['tile'][0] + du, ob['tile'][1] + dv]
            o2['from_module'] = inc['module']
            recast(o2)
            p.setdefault('objects', []).append(o2)
        for z in mod.get('zones', []):
            z2 = dict(z); r = z['rect']; z2['rect'] = [r[0] + du, r[1] + dv, r[2], r[3]]
            p.setdefault('zones', []).append(z2)
    return p


def validate(plan, lib):
    """Calcule l'occupation et verifie les references. Retourne (plan enrichi, rapport)."""
    W, D = plan['size_tiles']
    occ = np.zeros((W, D), np.int32)       # 0 libre, >0 index objet + 1
    reserved = np.zeros((W, D), bool)
    issues = []
    for i, ob in enumerate(plan.get('objects', [])):
        aid = ob['asset']
        if aid not in lib.index:
            issues.append(f"asset inconnu : {aid}")
            continue
        e = lib.get(aid)
        f = ob.get('facing', 'se')
        if f not in e['orientations']:
            if e.get('symmetric') and 'se' in e['orientations']:
                f = 'se'
            else:
                issues.append(f"{aid} : orientation {f} indisponible")
                continue
        coll, fw, acc = _rot_cells(e, f)
        U0, V0 = ob['tile']
        for (cu, cv) in coll:
            U, V = U0 + cu, V0 + cv
            if not (0 <= U < W and 0 <= V < D):
                issues.append(f"{aid} hors de la piece en ({U},{V})")
                continue
            if occ[U, V]:
                issues.append(f"{aid} chevauche l'objet #{occ[U, V] - 1} en ({U},{V})")
            occ[U, V] = i + 1
        for a in acc:
            U, V = U0 + a['tile'][0], V0 + a['tile'][1]
            if 0 <= U < W and 0 <= V < D:
                reserved[U, V] = True
        o = e['orientations'][f]
        for sock, spec in (ob.get('children') or {}).items():
            if sock == 'slots':
                sl = o.get('slots')
                if not sl:
                    issues.append(f"{aid}/{f} : pas de slots pour des modules")
                    continue
                for it in spec:
                    if it['asset'] not in lib.index:
                        issues.append(f"module inconnu : {it['asset']}")
                    if it['index'] >= sl['count']:
                        issues.append(f"{aid} : slot {it['index']} hors capacite ({sl['count']})")
                continue
            if sock not in o.get('sockets', {}):
                issues.append(f"{aid}/{f} : socket {sock} absent")
                continue
            child = spec if isinstance(spec, str) else spec['asset']
            if child not in lib.index:
                issues.append(f"enfant inconnu : {child}")
            else:
                ce = lib.get(child)
                cf = o['sockets'][sock]['child_facing']
                if cf not in ce['orientations'] and not ce.get('symmetric'):
                    issues.append(f"{child} : orientation {cf} requise par {aid}.{sock} absente")
            if isinstance(spec, dict) and spec.get('character'):
                ch = spec['character']
                if ch['base'] not in lib.index:
                    issues.append(f"personnage inconnu : {ch['base']}")
                if ch.get('hair') and ch['hair'] not in lib.index:
                    issues.append(f"coiffure inconnue : {ch['hair']}")
    # une case d'acces ne doit pas etre occupee par un autre objet
    for i, ob in enumerate(plan.get('objects', [])):
        if ob['asset'] not in lib.index:
            continue
        e = lib.get(ob['asset'])
        f = ob.get('facing', 'se') if ob.get('facing', 'se') in e['orientations'] else 'se'
        for a in e['orientations'][f].get('access', []):
            U, V = ob['tile'][0] + a['tile'][0], ob['tile'][1] + a['tile'][1]
            if 0 <= U < W and 0 <= V < D and occ[U, V] and occ[U, V] != i + 1:
                other = plan['objects'][occ[U, V] - 1]['asset']
                issues.append(f"acces de {ob['asset']} en ({U},{V}) bloque par {other}")
    for ent in plan.get('entrances', []):
        U, V = ent['access_tile']
        if 0 <= U < W and 0 <= V < D:
            reserved[U, V] = True
            if occ[U, V]:
                issues.append(f"entree bloquee en ({U},{V})")
    free = [[u, v] for u in range(W) for v in range(D) if not occ[u, v] and not reserved[u, v]]
    # emplacements muraux libres (segments pleins sans objet mural) et occultants
    used = {(wi['wall'], wi['index']) for wi in plan.get('wall_items', [])}
    wall_slots = {side: [i for i, k in enumerate(plan['walls'][side]['segments'])
                         if k == 'plain' and (side, i) not in used] for side in ('left', 'right')}
    occluders = []
    for ob in plan.get('objects', []):
        e = lib.index.get(ob['asset'])
        if not e:
            continue
        f = ob.get('facing', 'se') if ob.get('facing', 'se') in e['orientations'] else 'se'
        if e['orientations'][f].get('occlusion') or (e.get('height_px') or 0) >= 64:
            occluders.append({'asset': ob['asset'], 'tile': ob['tile'], 'height_px': e.get('height_px')})
    plan = dict(plan)
    plan.setdefault('visible_walls', {'left': "mur gauche (orientation se) le long de V, U=0",
                                      'right': "mur droit (orientation sw) le long de U, V=0",
                                      'front': "aucun : coupe avant (bords de dalle floor_edge)"})
    plan.setdefault('foreground', "aucun mur avant ; les objets 'occluders' peuvent etre estompes s'ils masquent "
                                  "un personnage ou la selection")
    plan['computed'] = {
        'occupied_cells': [[u, v] for u in range(W) for v in range(D) if occ[u, v]],
        'reserved_access_cells': [[u, v] for u in range(W) for v in range(D) if reserved[u, v] and not occ[u, v]],
        'buildable_cells': free,
        'buildable_count': len(free),
        'cell_count': W * D,
        'free_wall_item_slots': wall_slots,
        'occluders': occluders,
        'workstations': sum(1 for ob in plan.get('objects', []) for c in (ob.get('children') or {}).values()
                            if isinstance(c, dict) and c.get('character')) + sum(1 for ob in plan.get('objects', [])
                                                                                 if ob.get('character')),
    }
    return plan, issues


def char_layers_from_lib(lib):
    """Fabrique la fonction de rendu de personnage a partir des PNG livres (pas du code source)."""
    cache = {}

    def render(spec, orient):
        key = (json.dumps(spec, sort_keys=True), orient)
        if key in cache:
            return cache[key]
        be = lib.get(spec['base'])
        bo = be['orientations'][orient]
        sw = {'skin': spec.get('skin', be['channels']['skin'])}
        hair_sw = {'hair': spec.get('hair_ramp', 'hair_brown')}
        sw.update(spec.get('swatches', {}))
        out = []
        layers = {}
        if spec.get('hair'):
            he = lib.get(spec['hair'])
            for ln, L in he['orientations'][orient]['layers'].items():
                layers[ln] = recolor(lib.png(L['file']), lib.png(L['mask']), hair_sw)
        for ln in ('body', 'head'):
            L = bo['layers'][ln]
            layers[ln] = recolor(lib.png(L['file']), lib.png(L['mask']), dict(sw, **hair_sw))
        for ln in ('hair_back', 'body', 'head', 'hair_front'):
            if ln in layers:
                out.append((layers[ln], tuple(be['seat_anchor'])))
        cache[key] = out
        return out
    return render


def render_plan(plan, lib, out_png=None, scale=2, margin=40, bg=(46, 40, 50)):
    W, D = plan['size_tiles']
    # taille ecran : losange + hauteur des murs
    sw_ = (W + D) * 32 + 2 * margin
    sh_ = (W + D) * 16 + 112 + 2 * margin + 20
    origin = (margin + D * 32, margin + 112 + 10)
    sc = Scene(lib, origin=origin)
    walls = plan['walls']
    floors = plan.get('floor', [])
    for f in floors:
        u, v, w, d = f['rect']
        sc.floor_rect(f['material'], u, v, w, d, f.get('swatches'))
    for f in plan.get('floor_overlays', []):
        sc.floor_overlay(f['asset'], f['key'], f['tile'][0], f['tile'][1], f.get('swatches'))
    sc.slab_edges(W, D)
    L = walls['left']; R = walls['right']
    sc.wall('wall_corner_inner', 'all', 0, 0, swatches=L.get('swatches'))
    for V, k in enumerate(L['segments']):
        if k:
            sc.wall(k, 'se', 0, V, L.get('pattern'), L.get('swatches'))
    for U, k in enumerate(R['segments']):
        if k:
            sc.wall(k, 'sw', U, 0, R.get('pattern'), R.get('swatches'))
    sc.wall('wall_end', 'se', 0, D, swatches=L.get('swatches'))
    sc.wall('wall_end', 'sw', W, 0, swatches=R.get('swatches'))
    def known(spec):
        if isinstance(spec, list):      # modules de baie
            return True
        if isinstance(spec, str):
            return spec in lib.index
        ok = spec['asset'] in lib.index
        for c in (spec.get('children') or {}).values():
            ok = ok and known(c)
        return ok
    for wi in plan.get('wall_items', []):
        if wi['asset'] in lib.index:
            sc.wall_item(wi['asset'], wi['wall'], wi['index'], wi.get('swatches'))
    decor_first = []
    for ob in plan.get('objects', []):
        if ob['asset'] not in lib.index:
            continue
        ob = dict(ob)
        ob['children'] = {k: ([it for it in v if it['asset'] in lib.index] if k == 'slots' else v)
                          for k, v in (ob.get('children') or {}).items() if known(v)}
        e = lib.get(ob['asset'])
        f = ob.get('facing', 'se')
        if f not in e['orientations']:
            f = 'se'
        if e.get('render_layer') == 'floor_decor':
            decor_first.append(ob)
            continue
        sc.place(ob['asset'], ob['tile'][0], ob['tile'][1], f, ob.get('children'), ob.get('swatches'),
                 ob.get('character'))
    # tapis : couche floor_decor (apres les sols, avant ombres et objets)
    for ob in decor_first:
        e = lib.get(ob['asset'])
        f = ob.get('facing', 'se') if ob.get('facing', 'se') in e['orientations'] else 'se'
        o = e['orientations'][f]
        x, y = sc.tile_screen(*ob['tile'])
        img = sc._layer_img(o['layers']['base'], ob.get('swatches'))
        sc.edges.append((img, x - o['anchor'][0], y - o['anchor'][1]))
    img = sc.render((sw_, sh_), bg=bg, char_render=char_layers_from_lib(lib))
    # objets suspendus (plafonniers) : par-dessus tout
    for ob in plan.get('ceiling', []):
        e = lib.get(ob['asset'])
        o = e['orientations']['se']
        x, y = sc.tile_screen(*ob['tile'])
        over(img, lib.png(o['layers']['base']['file']), x - o['anchor'][0], y - o['anchor'][1])
    if out_png:
        save_png(out_png, img)
        if scale > 1:
            save_png(out_png.replace('.png', f'_x{scale}.png'), upscale(img, scale))
    return img
