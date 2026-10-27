"""Empaquetage en atlas (forme d'integration optionnelle ; les PNG decoupes restent la source).

- Une serie de pages par famille, 2048 x 2048 max, padding 2 px, extrusion 1 px des bords.
- Chaque couche couleur et son masque ont le MEME rectangle : page couleur `<famille>_<n>.png`,
  page masque `<famille>_<n>__mask.png`.
- Les rectangles excluent l'extrusion : echantillonner exactement [x, y, w, h], au plus proche voisin.
Ecrit atlases/*.png et atlases/atlas_index.json (fichier source -> page + rectangle).
"""
import json
import os
import sys

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'tools'))

from srgen.imgio import load_png, save_png  # noqa: E402

PAGE = 2048
UI_FAMILIES = {'hud', 'fonts', 'cursors', 'vfx', 'terminal', 'project_components'}
PAD = 2
EXT = 1


def collect_items(entries):
    """(famille, fichier, masque ou None) pour toutes les couches referencees."""
    items = []
    seen = set()

    def add(fam, L):
        f = L.get('file')
        if not f or f in seen or not f.endswith('.png'):
            return
        seen.add(f)
        items.append((fam, f, L.get('mask')))

    def walk(fam, node):
        if isinstance(node, str):
            if node.startswith('assets/') and node.endswith('.png') and '__mask' not in node:
                add(fam, {'file': node})
            return
        if isinstance(node, dict):
            if 'file' in node and isinstance(node['file'], str):
                add(fam, node)
            for k, v in node.items():
                if k in ('file', 'mask'):
                    continue
                walk(fam, v)
        elif isinstance(node, list):
            for v in node:
                walk(fam, v)
    for e in entries:
        fam = e.get('category', 'misc')
        if fam in UI_FAMILIES:
            continue   # le kit HUD livre son propre atlas (references 'atlas' deja dans ses entrees)
        walk(fam, e.get('orientations', {}))
        walk(fam, e.get('variants', {}))
        walk(fam, e.get('files', {}))
        walk(fam, e.get('states', {}))
        walk(fam, e.get('file'))
        walk(fam, e.get('icons', {}))
        walk(fam, e.get('parts', {}))
    return items


def shelf_pack(sizes):
    """Rangement en etageres ; retourne [(page, x, y)] dans l'ordre des tailles fournies."""
    order = sorted(range(len(sizes)), key=lambda i: (-sizes[i][1], -sizes[i][0]))
    pos = [None] * len(sizes)
    page = 0; x = PAD; y = PAD; shelf_h = 0
    for i in order:
        w, h = sizes[i]
        w2, h2 = w + 2 * EXT, h + 2 * EXT
        if w2 + PAD > PAGE or h2 + PAD > PAGE:
            raise ValueError(f'sprite trop grand pour une page : {w}x{h}')
        if x + w2 + PAD > PAGE:
            x = PAD; y += shelf_h + PAD; shelf_h = 0
        if y + h2 + PAD > PAGE:
            page += 1; x = PAD; y = PAD; shelf_h = 0
        pos[i] = (page, x + EXT, y + EXT)
        x += w2 + PAD
        shelf_h = max(shelf_h, h2)
    return pos


def blit_extruded(page, img, x, y):
    h, w = img.shape[:2]
    page[y:y + h, x:x + w] = img
    page[y - 1, x:x + w] = img[0]
    page[y + h, x:x + w] = img[-1]
    page[y - 1:y + h + 1, x - 1] = page[y - 1:y + h + 1, x]
    page[y - 1:y + h + 1, x + w] = page[y - 1:y + h + 1, x + w - 1]


def main():
    from srgen.scene import Library
    lib = Library(ROOT)
    entries = list(lib.index.values())
    items = collect_items(entries)
    by_fam = {}
    for fam, f, m in items:
        by_fam.setdefault(fam, []).append((f, m))
    out_dir = os.path.join(ROOT, 'atlases')
    os.makedirs(out_dir, exist_ok=True)
    for old in os.listdir(out_dir):
        if old.endswith('.png'):
            os.remove(os.path.join(out_dir, old))
    index = {'page_size': PAGE, 'padding': PAD, 'extrude': EXT, 'sampling': 'nearest', 'pages': [], 'items': {}}
    for fam, lst in sorted(by_fam.items()):
        imgs = [load_png(os.path.join(ROOT, f)) for f, _ in lst]
        sizes = [(im.shape[1], im.shape[0]) for im in imgs]
        pos = shelf_pack(sizes)
        npages = max(p[0] for p in pos) + 1
        for pg in range(npages):
            used = [i for i in range(len(lst)) if pos[i][0] == pg]
            W = max(pos[i][1] + sizes[i][0] + EXT + PAD for i in used)
            H = max(pos[i][2] + sizes[i][1] + EXT + PAD for i in used)
            W = 1 << (W - 1).bit_length(); H = 1 << (H - 1).bit_length()
            color = np.zeros((H, W, 4), np.uint8)
            mask = np.zeros((H, W, 4), np.uint8)
            has_mask = False
            for i in used:
                f, m = lst[i]
                _, x, y = pos[i]
                blit_extruded(color, imgs[i], x, y)
                if m:
                    mi = load_png(os.path.join(ROOT, m))
                    blit_extruded(mask, mi, x, y)
                    has_mask = True
                rect = [x, y, sizes[i][0], sizes[i][1]]
                index['items'][f] = {'page': f'atlases/{fam}_{pg}.png', 'rect': rect}
                if m:
                    index['items'][m] = {'page': f'atlases/{fam}_{pg}__mask.png', 'rect': rect}
            save_png(os.path.join(out_dir, f'{fam}_{pg}.png'), color)
            pinfo = {'file': f'atlases/{fam}_{pg}.png', 'size': [W, H], 'family': fam, 'sprites': len(used)}
            if has_mask:
                save_png(os.path.join(out_dir, f'{fam}_{pg}__mask.png'), mask)
                pinfo['mask_file'] = f'atlases/{fam}_{pg}__mask.png'
            index['pages'].append(pinfo)
            print(f'{fam}_{pg}: {W}x{H}, {len(used)} sprites')
    json.dump(index, open(os.path.join(out_dir, 'atlas_index.json'), 'w', encoding='utf-8'), indent=1)
    print(len(index['items']), 'entrees indexees,', len(index['pages']), 'pages')


if __name__ == '__main__':
    main()
