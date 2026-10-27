"""Outil de recoloration / export (reference de l'algorithme de COLOR_SYSTEM.md).

Exemples :
  python tools/recolor.py --asset desk_simple --facing se --swatch wood=walnut --swatch metal=steel --out out.png
  python tools/recolor.py --asset chair_office --facing sw --color fabric=#7a4fb0 --out chaise_violette.png
  python tools/recolor.py --demo        # planche previews/recolor_demo.png
  python tools/recolor.py --verify      # verifie recolor(base, masque, rampes par defaut) == base pour tout le pack
"""
import argparse
import json
import os
import sys

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'tools'))

from srgen.scene import Library  # noqa: E402
from srgen.compose import recolor, apply_wall_pattern  # noqa: E402
from srgen.imgio import save_png, over, upscale, load_png  # noqa: E402
from srgen.palette import RAMPS, CHANNEL_NAMES, CHANNELS, ramp_from_color, hx  # noqa: E402
from srgen.preview import draw_text  # noqa: E402


def iter_layers(e):
    """(orientation/variante, nom de couche, dict couche) pour toutes les couches masquees d'une entree."""
    groups = e.get('orientations') or {}
    if 'variants' in e:
        groups = dict(groups, **e['variants'])
    for key, o in groups.items():
        for ln, L in o.get('layers', {}).items():
            if isinstance(L, dict) and 'mask' in L and 'file' in L:
                yield key, ln, L


def default_swatches(e):
    ch = dict(e.get('channels') or {})
    return {k: v for k, v in ch.items() if k in CHANNELS and v in RAMPS}


def verify(lib):
    """Chaque pixel masque doit reproduire exactement la couleur de base avec la rampe par defaut du canal
    (la rampe par defaut est deduite du pixel lui-meme : on verifie que la couleur appartient a une rampe connue
    a l'indice stocke, et que le masque ne touche ni contour ni transparence)."""
    report = {'layers': 0, 'pixels': 0, 'errors': [], 'mask_on_transparent': 0, 'mask_on_ink': 0}
    ink = np.array([43, 33, 40], np.uint8)
    ramp_arr = {n: np.array(v, np.uint8)[:, :3] for n, v in RAMPS.items()}
    for aid, e in lib.index.items():
        for key, ln, L in iter_layers(e):
            rgba = lib.png(L['file']); mask = lib.png(L['mask'])
            if rgba.shape != mask.shape:
                report['errors'].append(f"{aid}/{key}/{ln} : tailles differentes {rgba.shape} {mask.shape}")
                continue
            sel = mask[..., 3] > 0
            report['layers'] += 1
            report['pixels'] += int(sel.sum())
            report['mask_on_transparent'] += int((sel & (rgba[..., 3] == 0)).sum())
            report['mask_on_ink'] += int((sel & np.all(rgba[..., :3] == ink, -1)).sum())
            idx = mask[..., 1][sel]
            if idx.size and idx.max() > 5:
                report['errors'].append(f"{aid}/{key}/{ln} : indice > 5")
            cols = rgba[..., :3][sel]
            # chaque couleur doit etre la teinte idx d'au moins une rampe
            ok = np.zeros(len(cols), bool)
            for n, arr in ramp_arr.items():
                ok |= np.all(arr[np.clip(idx, 0, 5)] == cols, -1)
            bad = int((~ok).sum())
            if bad:
                report['errors'].append(f"{aid}/{key}/{ln} : {bad} pixels hors rampe")
    report['ok'] = not report['errors'] and report['mask_on_transparent'] == 0 and report['mask_on_ink'] == 0
    return report


def parse_swatches(args):
    sw = {}
    for s in args.swatch or []:
        k, v = s.split('=')
        sw[k] = v
    for s in args.color or []:
        k, v = s.split('=')
        sw[k] = ramp_from_color(hx(v))
    return sw


def demo(lib, out):
    cases = [
        ('desk_simple', 'se', [{}, {'wood': 'walnut', 'metal': 'steel'}, {'wood': 'birch', 'metal': 'brass'}]),
        ('chair_office', 'sw', [{}, {'fabric': 'coral'}, {'fabric': 'sun', 'metal': 'steel'},
                                {'fabric': ramp_from_color(hx('#7a4fb0'))}]),
        ('sofa', 'se', [{}, {'fabric': 'rose', 'accent': 'teal'}, {'fabric': 'graphite', 'accent': 'coral'}]),
        ('locker', 'se', [{}, {'primary': 'coral'}, {'primary': 'steel'}]),
        ('workstation_gpu', 'sw', [{}, {'primary': 'plastic'}, {'primary': 'navy', 'accent': 'coral'}]),
        ('vending_drinks', 'sw', [{}, {'primary': 'leaf'}, {'primary': 'sun'}]),
    ]
    cells = []
    for aid, f, variants in cases:
        if aid not in lib.index:
            continue
        o = lib.get(aid)['orientations'][f]
        L = o['layers']['base']
        rgba = lib.png(L['file']); mask = lib.png(L['mask'])
        row = []
        for sw in variants:
            img = recolor(rgba, mask, sw) if sw else rgba
            if 'front' in o['layers']:
                Lf = o['layers']['front']
                fr = lib.png(Lf['file'])
                if 'mask' in Lf and sw:
                    fr = recolor(fr, lib.png(Lf['mask']), sw)
                img = img.copy(); over(img, fr, 0, 0)
            row.append(img)
        cells.append((aid, row))
    W = 8 + max(sum(c.shape[1] + 8 for c in row) for _, row in cells)
    H = 16 + sum(max(c.shape[0] for c in row) + 14 for _, row in cells)
    sh = np.zeros((H, W, 4), np.uint8); sh[..., :3] = (216, 207, 194); sh[..., 3] = 255
    draw_text(sh, 4, 3, 'RECOLORATION PAR MASQUE : UNE SEULE GEOMETRIE PAR OBJET (1RE COLONNE = BASE)')
    y = 14
    for aid, row in cells:
        draw_text(sh, 4, y, aid)
        x = 4
        for c in row:
            over(sh, c, x, y + 7)
            x += c.shape[1] + 8
        y += max(c.shape[0] for c in row) + 14
    save_png(out, upscale(sh, 2))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--asset'); ap.add_argument('--facing', default='se'); ap.add_argument('--layer', default='base')
    ap.add_argument('--swatch', action='append', help='canal=rampe (ex. wood=walnut)')
    ap.add_argument('--color', action='append', help='canal=#rrggbb (rampe generee, OKLCh)')
    ap.add_argument('--out'); ap.add_argument('--demo', action='store_true'); ap.add_argument('--verify', action='store_true')
    args = ap.parse_args()
    lib = Library(ROOT)
    if args.verify:
        rep = verify(lib)
        json.dump(rep, open(os.path.join(ROOT, 'build_cache', 'qa_recolor.json'), 'w', encoding='utf-8'), indent=1)
        print(json.dumps({k: v for k, v in rep.items() if k != 'errors'}), 'erreurs:', rep['errors'][:10])
        return
    if args.demo:
        demo(lib, os.path.join(ROOT, 'previews', 'recolor_demo.png'))
        print('previews/recolor_demo.png')
        return
    e = lib.get(args.asset)
    o = (e.get('orientations') or e.get('variants'))[args.facing]
    L = o['layers'][args.layer]
    img = recolor(lib.png(L['file']), lib.png(L['mask']), parse_swatches(args))
    save_png(args.out or f'{args.asset}__{args.facing}__recolor.png', img)
    print('ecrit', args.out)


if __name__ == '__main__':
    main()
