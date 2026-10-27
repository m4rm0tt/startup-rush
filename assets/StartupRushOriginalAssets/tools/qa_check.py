"""Controles qualite automatiques de la bibliotheque (resultats : build_cache/qa_report.json).

Couvre : decodage PNG, RGBA 8 bits, bloc sRGB, alpha, tailles declarees, masques (valeurs exactes,
pas de contamination), recoloration par defaut = base, IDs uniques et ASCII, references de fichiers,
rectangles d'atlas (pixels identiques a la source), pavage des sols, coherence des pieces murales,
calques de motifs, coiffures, slots de baies, ports de convoyeurs, plans de pieces, nettete
(aucune couleur hors palette dans les sprites opaques = aucun reechantillonnage).
Les controles visuels restent faits sur les planches (previews/) : un script ne juge pas l'esthetique.
"""
import json
import os
import re
import sys
import glob

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'tools'))

from srgen.palette import RAMPS, FIXED, CHANNELS  # noqa: E402

ID_RE = re.compile(r'^[a-z0-9]+(_[a-z0-9]+)*$')
FILE_RE = re.compile(r'^[a-z0-9_./\-]+$')
INK = np.array(FIXED['ink'][:3], np.uint8)


def palette_set():
    cols = set()
    for v in RAMPS.values():
        for c in v:
            cols.add(tuple(c[:3]))
    for v in FIXED.values():
        cols.add(tuple(v[:3]))
    return cols


class Report:
    def __init__(self):
        self.checks = {}

    def add(self, name, ok, detail='', count=None):
        c = self.checks.setdefault(name, {'ok': True, 'failures': [], 'count': 0})
        if count is not None:
            c['count'] += count
        if not ok:
            c['ok'] = False
            if len(c['failures']) < 40:
                c['failures'].append(detail)


def walk_layers(node, out, ctx=''):
    if isinstance(node, dict):
        if isinstance(node.get('file'), str):
            out.append((ctx, node))
        for k, v in node.items():
            if k in ('atlas', 'mask_atlas'):
                continue
            walk_layers(v, out, ctx + '/' + str(k))
    elif isinstance(node, list):
        for i, v in enumerate(node):
            walk_layers(v, out, ctx + f'[{i}]')


def main():
    rep = Report()
    man = json.load(open(os.path.join(ROOT, 'manifest.json'), encoding='utf-8'))
    assets = man['assets']
    pal = palette_set()
    # --- IDs
    ids = [a['id'] for a in assets]
    rep.add('ids_uniques', len(ids) == len(set(ids)), 'doublons', count=len(ids))
    for i in ids:
        rep.add('ids_ascii_snake_case', bool(ID_RE.match(i)), i, count=1)
    # --- fichiers references
    refs = []
    for a in assets:
        walk_layers(a, refs, a['id'])
    files = set()
    for ctx, L in refs:
        for key in ('file', 'mask'):
            f = L.get(key)
            if isinstance(f, str):
                files.add(f)
    for f in sorted(files):
        p = os.path.join(ROOT, f)
        rep.add('fichiers_references_presents', os.path.exists(p), f, count=1)
        rep.add('noms_fichiers_ascii', bool(FILE_RE.match(f)), f, count=1)
    # --- PNG : decodage, mode, sRGB
    pngs = sorted(glob.glob(os.path.join(ROOT, 'assets', '**', '*.png'), recursive=True) +
                  glob.glob(os.path.join(ROOT, 'masks', '**', '*.png'), recursive=True) +
                  glob.glob(os.path.join(ROOT, 'atlases', '*.png')))
    for p in pngs:
        rel = os.path.relpath(p, ROOT).replace('\\', '/')
        try:
            im = Image.open(p); im.load()
            ok = im.mode == 'RGBA'
            rep.add('png_rgba_8bits', ok, f'{rel} {im.mode}', count=1)
            rep.add('png_bloc_srgb', 'srgb' in im.info, rel, count=1)
        except Exception as ex:
            rep.add('png_decodage', False, f'{rel} {ex}', count=1)
            continue
        rep.add('png_decodage', True, count=1)
    # --- couches : taille declaree, alpha, masques, nettete
    declared = {}
    for a in assets:
        for key, groups in (('orientations', a.get('orientations') or {}), ('variants', a.get('variants') or {})):
            for o_name, o in groups.items():
                if isinstance(o, dict) and 'size' in o and 'layers' in o:
                    for ln, L in o['layers'].items():
                        if isinstance(L, dict) and 'file' in L:
                            declared[L['file']] = (tuple(o['size']), ln, L.get('mask'), a['id'], a.get('category'))
    for f, (size, ln, mask, aid, cat) in sorted(declared.items()):
        p = os.path.join(ROOT, f)
        if not os.path.exists(p):
            continue
        arr = np.array(Image.open(p).convert('RGBA'))
        h, w = arr.shape[:2]
        rep.add('tailles_declarees', (w, h) == size, f'{f} {w}x{h} != {size}', count=1)
        alpha = arr[..., 3]
        if ln in ('base', 'front', 'body', 'head', 'hair_front', 'hair_back'):
            ok = bool(np.isin(alpha, [0, 255]).all())
            rep.add('alpha_binaire_sprites', ok, f, count=1)
            opaque = alpha == 255
            cols = arr[..., :3][opaque]
            if cols.size:
                uniq = np.unique(cols.reshape(-1, 3), axis=0)
                off = [tuple(c) for c in uniq if tuple(c) not in pal]
                rep.add('nettete_couleurs_palette', not off, f'{f} : {len(off)} couleurs hors palette', count=1)
        if ln == 'shadow':
            rep.add('ombre_forme_pleine', bool(np.isin(alpha, [0, 255]).all()), f, count=1)
        if ln == 'overlay':
            rep.add('calque_motif_alpha', bool(np.isin(alpha, [0, 255]).all()), f, count=1)
        if mask:
            mp = os.path.join(ROOT, mask)
            if not os.path.exists(mp):
                rep.add('masques_presents', False, mask, count=1)
                continue
            m = np.array(Image.open(mp).convert('RGBA'))
            rep.add('masque_meme_taille', m.shape == arr.shape, mask, count=1)
            if m.shape != arr.shape:
                continue
            sel = m[..., 3] > 0
            rep.add('masque_alpha_binaire', bool(np.isin(m[..., 3], [0, 255]).all()), mask, count=1)
            rep.add('masque_hors_transparence', not (sel & (alpha == 0)).any(), mask, count=1)
            rep.add('masque_hors_contour', not (sel & np.all(arr[..., :3] == INK, -1)).any(), mask, count=1)
            chans = set(np.unique(m[..., 0][sel]).tolist()) if sel.any() else set()
            rep.add('masque_canaux_connus', chans <= set(CHANNELS.values()), f'{mask} {chans}', count=1)
            idx = m[..., 1][sel]
            rep.add('masque_indices_0_5', bool((idx <= 5).all()) if idx.size else True, mask, count=1)
            # recoloration par defaut = base : chaque pixel = teinte idx de la rampe par defaut du canal
            entry_channels = None
            for a in assets:
                if a['id'] == aid:
                    entry_channels = a.get('channels') or {}
                    break
            bad = 0
            if sel.any() and entry_channels:
                for cname, ramp in entry_channels.items():
                    if cname not in CHANNELS or ramp not in RAMPS:
                        continue
                    cm = sel & (m[..., 0] == CHANNELS[cname])
                    if not cm.any():
                        continue
                    pal_r = np.array(RAMPS[ramp], np.uint8)[:, :3]
                    exp = pal_r[np.clip(m[..., 1][cm], 0, 5)]
                    bad += int((~np.all(exp == arr[..., :3][cm], -1)).sum())
            rep.add('recolor_defaut_egal_base', bad == 0, f'{f} : {bad} px', count=1)
    # --- kit HUD : tailles declarees, marges 9-slice coherentes, points chauds dans l'image
    def ui_walk(node, aid):
        if isinstance(node, dict):
            f = node.get('file'); sz = node.get('size')
            if isinstance(f, str) and f.endswith('.png') and isinstance(sz, list) and len(sz) == 2:
                p = os.path.join(ROOT, f)
                if os.path.exists(p):
                    w, h = Image.open(p).size
                    rep.add('ui_tailles_declarees', (w, h) == tuple(sz), f'{f} {w}x{h} != {sz}', count=1)
            for k, v in node.items():
                if k != 'atlas':
                    ui_walk(v, aid)
        elif isinstance(node, list):
            for v in node:
                ui_walk(v, aid)
    for a in assets:
        if a.get('category') in ('hud', 'fonts', 'cursors', 'vfx', 'terminal', 'project_components'):
            ui_walk(a, a['id'])
            ns = a.get('nine_slice'); sz = a.get('size')
            if ns and sz:
                l, t, r, b = ns
                ok = l + r < sz[0] and t + b < sz[1] and min(ns) >= 0
                ms = a.get('min_size')
                if ms:
                    ok = ok and ms[0] >= l + r and ms[1] >= t + b
                rep.add('ui_9slice_marges_coherentes', ok, f"{a['id']} {ns} {sz} min {ms}", count=1)
            hs = a.get('hotspot')
            if hs and a.get('size'):
                rep.add('curseurs_point_chaud_dans_image', 0 <= hs[0] < a['size'][0] and 0 <= hs[1] < a['size'][1],
                        a['id'], count=1)
    # --- atlas : toute reference {file, atlas{page, rect}} du manifeste (atlas du pack ou du kit HUD)
    items = dict(man.get('atlas_items') or {})
    def collect_atlas(node):
        if isinstance(node, dict):
            f = node.get('file'); at = node.get('atlas')
            if isinstance(f, str) and isinstance(at, dict) and 'page' in at and 'rect' in at:
                items.setdefault(f, at)
            for v in node.values():
                collect_atlas(v)
        elif isinstance(node, list):
            for v in node:
                collect_atlas(v)
    collect_atlas(assets)
    pages = {}
    for f, it in items.items():
        pg = it['page']
        if pg not in pages:
            pages[pg] = np.array(Image.open(os.path.join(ROOT, pg)).convert('RGBA'))
        x, y, w, h = it['rect']
        P = pages[pg]
        ok_bounds = x >= 0 and y >= 0 and x + w <= P.shape[1] and y + h <= P.shape[0]
        rep.add('atlas_rect_dans_page', ok_bounds, f, count=1)
        if ok_bounds and os.path.exists(os.path.join(ROOT, f)):
            src = np.array(Image.open(os.path.join(ROOT, f)).convert('RGBA'))
            rep.add('atlas_pixels_identiques', src.shape == (h, w, 4) and np.array_equal(P[y:y + h, x:x + w], src), f, count=1)
    # chevauchements dans une page
    by_page = {}
    for f, it in items.items():
        by_page.setdefault(it['page'], []).append(it['rect'])
    for pg, rects in by_page.items():
        cov = None
        for (x, y, w, h) in rects:
            if cov is None:
                P = pages.get(pg)
                cov = np.zeros(P.shape[:2], np.uint8) if P is not None else None
            if cov is not None:
                cov[y:y + h, x:x + w] += 1
        if cov is not None:
            rep.add('atlas_sans_chevauchement', int(cov.max()) <= 1, pg, count=1)
    # --- sols : pavage exact (chaque pixel d'un sol 4x4 couvert une seule fois)
    for a in assets:
        if a.get('kind') == 'floor_tile':
            cov = np.zeros((32 * 8 + 64, 64 * 8 + 64), np.int32)
            pu, pv = a['period_tiles']
            for U in range(4):
                for V in range(4):
                    var = a['variants'][f'u{U % pu}v{V % pv}']
                    t = np.array(Image.open(os.path.join(ROOT, var['layers']['base']['file'])).convert('RGBA'))[..., 3] > 0
                    x = 32 * 4 + (U - V) * 32 - 32; y = (U + V) * 16
                    cov[y:y + 32, x:x + 64] += t
            inner = cov[48:80, 96:160]
            rep.add('sols_pavage_sans_trou_ni_recouvrement', cov.max() == 1 and inner.min() == 1, a['id'], count=1)
            for var in a['variants'].values():
                t = np.array(Image.open(os.path.join(ROOT, var['layers']['base']['file'])).convert('RGBA'))[..., 3] > 0
                rep.add('sols_1024_px_par_case', int(t.sum()) == 1024, var['layers']['base']['file'], count=1)
    # --- murs : toile et ancre communes par orientation (pieces 1 case)
    walls = [a for a in assets if a.get('kind') == 'wall_piece' and a['id'] in
             ('wall_plain', 'wall_window', 'wall_door', 'wall_opening')]
    for orient in ('se', 'sw'):
        sigs = set((tuple(a['orientations'][orient]['size']), tuple(a['orientations'][orient]['anchor'])) for a in walls)
        rep.add('murs_toile_ancre_communes', len(sigs) == 1, f'{orient} {sigs}', count=1)
    # calques de motif = pixels marques de la piece pleine
    plain = next((a for a in assets if a['id'] == 'wall_plain'), None)
    if plain:
        for orient in ('se', 'sw'):
            m = np.array(Image.open(os.path.join(ROOT, plain['orientations'][orient]['layers']['base']['mask'])).convert('RGBA'))
            flagged = (m[..., 3] > 0) & ((m[..., 2] & 1) > 0)
            for a in assets:
                if a.get('kind') == 'wall_pattern_overlay':
                    o = a['orientations'][orient]
                    ov = np.array(Image.open(os.path.join(ROOT, o['layers']['overlay']['file'])).convert('RGBA'))
                    same = ov.shape[:2] == m.shape[:2] and np.array_equal(ov[..., 3] > 0, flagged)
                    rep.add('motifs_limites_a_la_surface_murale', same, f"{a['id']} {orient}", count=1)
    # --- personnages
    qc = os.path.join(ROOT, 'build_cache', 'qa_characters.json')
    if os.path.exists(qc):
        q = json.load(open(qc, encoding='utf-8'))
        s = q['summary']
        rep.add('coiffures_alignees_toutes_bases', s['hair_failures'] == 0 and s['skull_mismatch_layers'] == 0,
                json.dumps(s), count=s['hair_checks'])
        bases = [a for a in assets if a.get('kind') == 'character_base']
        rep.add('dix_employes_deux_avatars', len([b for b in bases if b['id'].startswith('employee')]) == 10 and
                len([b for b in bases if b['id'].startswith('player')]) == 2, '', count=len(bases))
    # --- baies : modules dans l'enveloppe (controle du lot datacenter)
    try:
        import importlib
        from srgen.scene import Library
        dcqa = importlib.import_module('srgen.lot_dc_qa')
        _, total, bad = dcqa.check_racks(Library(ROOT))
        rep.add('modules_baie_dans_slots', bad == 0, f'{bad} px hors enveloppe sur {total}', count=total)
    except Exception as ex:
        rep.add('modules_baie_dans_slots', False, f'controle non execute : {ex}', count=0)
    # --- convoyeurs : ports de la ligne de l'usine relies deux a deux
    try:
        fac = json.load(open(os.path.join(ROOT, 'rooms', 'room_factory_hall.json'), encoding='utf-8'))
        idx = {a['id']: a for a in assets}
        ports = []
        for ob in fac['objects']:
            e = idx.get(ob['asset'])
            if not e:
                continue
            o = e['orientations'].get(ob.get('facing', 'se'))
            for pt in (o or {}).get('ports', []):
                U = ob['tile'][0] + pt['tile'][0]; V = ob['tile'][1] + pt['tile'][1]
                ports.append((U, V, pt['world_dir'], pt['kind'], ob['asset']))
        step = {'+U': (1, 0), '-U': (-1, 0), '+V': (0, 1), '-V': (0, -1)}
        opp = {'+U': '-U', '-U': '+U', '+V': '-V', '-V': '+V'}
        matched = 0
        port_tiles = {}
        for ob in fac['objects']:
            e = idx.get(ob['asset'])
            o = (e or {}).get('orientations', {}).get(ob.get('facing', 'se'), {})
            if o.get('ports'):
                for (cu, cv) in o.get('collision_tiles', [[0, 0]]):
                    port_tiles[(ob['tile'][0] + cu, ob['tile'][1] + cv)] = ob['asset']
        broken = []
        for (U, V, d, k, aid) in ports:
            du, dv = step[d]
            if any((U2, V2) == (U + du, V + dv) and d2 == opp[d] for (U2, V2, d2, k2, a2) in ports):
                matched += 1
            elif (U + du, V + dv) in port_tiles:
                broken.append(f'{aid} ({U},{V}) {d} -> {port_tiles[(U + du, V + dv)]}')
        rep.add('convoyeurs_ports_relies', not broken,
                f'{matched}/{len(ports)} relies ; mal relies : {broken}', count=len(ports))
    except Exception as ex:
        rep.add('convoyeurs_ports_relies', False, f'controle non execute : {ex}', count=0)
    # --- plans de pieces
    for r in man.get('rooms', []):
        plan = json.load(open(os.path.join(ROOT, r['file']), encoding='utf-8'))
        issues = plan.get('validation', {}).get('issues', [])
        rep.add('plans_valides', not issues, f"{r['id']} {issues[:3]}", count=1)
    # --- resume
    summary = {k: {'ok': v['ok'], 'count': v['count'], 'failures': len(v['failures'])} for k, v in rep.checks.items()}
    out = {'summary': summary, 'details': rep.checks, 'all_ok': all(v['ok'] for v in rep.checks.values())}
    json.dump(out, open(os.path.join(ROOT, 'build_cache', 'qa_report.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
    for k, v in summary.items():
        print(('OK ' if v['ok'] else 'KO ') + f"{k:45s} n={v['count']:6d} echecs={v['failures']}")
    print('TOUT OK' if out['all_ok'] else 'DES CONTROLES ONT ECHOUE')


if __name__ == '__main__':
    main()
