"""Planches de demonstration de l'architecture (composees avec les fichiers livres uniquement) :
  previews/wall_patterns_demo.png : 8 motifs sur des pans reels (fenetre, porte, passage), 2 orientations
  previews/floors_demo.png        : chaque sol en carre 3x3 + seuils de transition + bords de dalle
"""
import os
import sys

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'tools'))

from srgen.scene import Library, Scene  # noqa: E402
from srgen.imgio import save_png, over, upscale  # noqa: E402
from srgen.preview import draw_text  # noqa: E402

SW = {
    'paint': {'wall': 'cream'}, 'two_tone': {'wall': 'cream', 'wall2': 'teal'}, 'wallpaper': {'wall': 'sage'},
    'stripes': {'wall': 'cream', 'wall2': 'sand'}, 'wood_panels': {'wood': 'oak', 'wall': 'cream'},
    'concrete': {'wall': 'concrete'}, 'brick': {'wall': 'brick', 'wall2': 'cream'},
    'acoustic': {'wall': 'cream', 'wall2': 'navy'},
}


def wall_demo(lib):
    cells = []
    for pat, sw in SW.items():
        sw = dict(sw, trim='plastic')
        sc = Scene(lib, origin=(4 * 32 + 8, 112 + 12))
        sc.floor_rect('floor_wood_planks', 0, 0, 4, 4)
        sc.slab_edges(4, 4)
        sc.wall('wall_corner_inner', 'all', 0, 0, swatches=sw)
        for V, k in enumerate(['plain', 'window', 'door', 'plain']):
            sc.wall(k, 'se', 0, V, pat, sw)
        for U, k in enumerate(['window', 'plain', 'opening', 'window']):
            sc.wall(k, 'sw', U, 0, pat, sw)
        sc.wall('wall_end', 'se', 0, 4, swatches=sw)
        sc.wall('wall_end', 'sw', 4, 0, swatches=sw)
        img = sc.render((8 * 32 + 16, 4 * 32 + 112 + 30), bg=(216, 207, 194))
        draw_text(img, 4, 3, f'PATTERN_{pat}')
        cells.append(img)
    return grid(cells, 4, 'MOTIFS MURAUX : SURFACE DU MUR UNIQUEMENT (VITRES, CADRES, PLINTHES ET PORTES INTACTS)')


def floor_demo(lib):
    floors = [a for a in lib.index.values() if a.get('kind') == 'floor_tile']
    cells = []
    for e in sorted(floors, key=lambda a: a['id']):
        sc = Scene(lib, origin=(3 * 32 + 8, 18))
        sc.floor_rect(e['id'], 0, 0, 3, 3)
        sc.slab_edges(3, 3)
        img = sc.render((6 * 32 + 16, 3 * 32 + 40), bg=(216, 207, 194))
        draw_text(img, 4, 3, e['id'])
        cells.append(img)
    # jonction de deux sols avec barres de seuil
    sc = Scene(lib, origin=(4 * 32 + 8, 18))
    sc.floor_rect('floor_wood_planks', 0, 0, 2, 4)
    sc.floor_rect('floor_tile_ceramic', 2, 0, 2, 4)
    for V in range(4):
        sc.floor_overlay('floor_threshold', 'along_v', 2, V)
    sc.slab_edges(4, 4)
    img = sc.render((8 * 32 + 16, 4 * 32 + 40), bg=(216, 207, 194))
    draw_text(img, 4, 3, 'JONCTION + FLOOR_THRESHOLD')
    cells.append(img)
    return grid(cells, 4, 'SOLS : PAVAGE 3X3, VARIANTES (U MOD PU, V MOD PV), BORDS DE DALLE ET SEUILS')


def grid(cells, cols, title):
    rows = [cells[i:i + cols] for i in range(0, len(cells), cols)]
    W = max(sum(c.shape[1] + 6 for c in r) for r in rows) + 6
    H = sum(max(c.shape[0] for c in r) + 6 for r in rows) + 16
    sh = np.zeros((H, W, 4), np.uint8)
    sh[..., :3] = (198, 189, 176); sh[..., 3] = 255
    draw_text(sh, 6, 4, title)
    y = 14
    for r in rows:
        x = 6
        for c in r:
            over(sh, c, x, y)
            x += c.shape[1] + 6
        y += max(c.shape[0] for c in r) + 6
    return sh


def main():
    lib = Library(ROOT)
    save_png(os.path.join(ROOT, 'previews', 'wall_patterns_demo.png'), upscale(wall_demo(lib), 2))
    save_png(os.path.join(ROOT, 'previews', 'floors_demo.png'), upscale(floor_demo(lib), 2))
    print('ok')


if __name__ == '__main__':
    main()
