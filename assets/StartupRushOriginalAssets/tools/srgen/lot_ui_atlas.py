"""Atlas d'interface (lot UI) : rangement en etageres, marge 2 px, extrusion 1 px.

L'extrusion recopie la bordure de chaque image dans la marge pour eviter tout
debordement d'echantillonnage (zoom au plus proche voisin, 9-slice). Le rectangle
publie est celui de l'image d'origine (sans extrusion).
"""
import numpy as np


def pack(images, max_w=1024, pad=2):
    """images : liste (cle, rgba). Retourne (pages[list rgba], rects{cle: (page, x, y, w, h)})."""
    order = sorted(images, key=lambda kv: (-kv[1].shape[0], -kv[1].shape[1], kv[0]))
    pages = []
    rects = {}
    cur = None

    def new_page():
        return {'shelves': [], 'h': 0, 'items': []}
    cur = new_page()
    pages_meta = [cur]
    for key, img in order:
        h, w = img.shape[:2]
        W, H = w + 2 * pad, h + 2 * pad
        placed = False
        for sh in cur['shelves']:
            if sh['h'] >= H and sh['x'] + W <= max_w:
                rects[key] = (len(pages_meta) - 1, sh['x'] + pad, sh['y'] + pad, w, h)
                sh['x'] += W
                placed = True
                break
        if not placed:
            if cur['h'] + H > max_w:
                cur = new_page()
                pages_meta.append(cur)
            sh = {'y': cur['h'], 'h': H, 'x': W}
            cur['shelves'].append(sh)
            cur['h'] += H
            rects[key] = (len(pages_meta) - 1, pad, sh['y'] + pad, w, h)
        cur['items'].append(key)
    imgs = dict(images)
    for pi, pm in enumerate(pages_meta):
        Hp = 1
        while Hp < pm['h']:
            Hp *= 2
        Wp = max_w
        page = np.zeros((Hp, Wp, 4), np.uint8)
        for key in pm['items']:
            _, x, y, w, h = rects[key]
            im = imgs[key]
            page[y:y + h, x:x + w] = im
            # extrusion 1 px
            page[y - 1, x:x + w] = im[0]
            page[y + h, x:x + w] = im[-1]
            page[y:y + h, x - 1] = im[:, 0]
            page[y:y + h, x + w] = im[:, -1]
            page[y - 1, x - 1] = im[0, 0]; page[y - 1, x + w] = im[0, -1]
            page[y + h, x - 1] = im[-1, 0]; page[y + h, x + w] = im[-1, -1]
        pages.append(page)
    return pages, rects
