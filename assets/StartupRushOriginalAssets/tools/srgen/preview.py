"""Planches de controle des familles (previews/), avec emprise et ancre dessinees.

Les etiquettes utilisent une micro-police 3x5 originale (TAG35) propre aux
planches de controle ; la police d'interface livree est dans assets/fonts/.
"""
import os
import numpy as np

from .imgio import load_png, over, save_png, upscale, shadow_layer_to_rgba

# Micro-police 3x5 (originale) : 15 bits par glyphe, lignes de haut en bas
_G = {
    'A': '010101111101101', 'B': '110101110101110', 'C': '011100100100011', 'D': '110101101101110',
    'E': '111100110100111', 'F': '111100110100100', 'G': '011100101101011', 'H': '101101111101101',
    'I': '111010010010111', 'J': '001001001101010', 'K': '101101110101101', 'L': '100100100100111',
    'M': '101111111101101', 'N': '110101101101101', 'O': '010101101101010', 'P': '110101110100100',
    'Q': '010101101110011', 'R': '110101110101101', 'S': '011100010001110', 'T': '111010010010010',
    'U': '101101101101111', 'V': '101101101101010', 'W': '101101111111101', 'X': '101101010101101',
    'Y': '101101010010010', 'Z': '111001010100111', '0': '111101101101111', '1': '010110010010111',
    '2': '110001010100111', '3': '110001010001110', '4': '101101111001001', '5': '111100110001110',
    '6': '011100111101111', '7': '111001010010010', '8': '111101111101111', '9': '111101111001110',
    '_': '000000000000111', '-': '000000111000000', '.': '000000000000010', ':': '000010000010000',
    '/': '001001010100100', '(': '010100100100010', ')': '010001001001010', ' ': '000000000000000',
    '+': '000010111010000', 'x': '000101010101000', '=': '000111000111000', ',': '000000000010100',
    "'": '010010000000000', '!': '010010010000010', '?': '110001010000010', '>': '100010001010100', '<': '001010100010001', '#': '101111101111101', '%': '101001010100101',
}


def text_mask(s, scale=1):
    s = s.upper()
    w = max(1, len(s) * 4 - 1)
    out = np.zeros((5, w), bool)
    for i, ch in enumerate(s):
        g = _G.get(ch, _G[' '] if ch not in 'ÉÈÊ' else _G['E'])
        for r in range(5):
            for c in range(3):
                if g[r * 3 + c] == '1':
                    out[r, i * 4 + c] = True
    if scale > 1:
        out = np.repeat(np.repeat(out, scale, 0), scale, 1)
    return out


def draw_text(img, x, y, s, color=(43, 33, 40, 255), scale=1):
    m = text_mask(s, scale)
    h, w = m.shape
    H, W = img.shape[:2]
    x1, y1 = min(W, x + w), min(H, y + h)
    if x1 <= x or y1 <= y:
        return w
    sub = m[:y1 - y, :x1 - x]
    img[y:y1, x:x1][sub] = color
    return w


def diamond_outline(img, x, y, su, sv, color=(160, 150, 138, 255)):
    """Contour de l'emprise (su x sv cases) dont le coin nord est en (x, y)."""
    pts = []
    def seg(u0, v0, u1, v1):
        n = int(max(abs(u1 - u0), abs(v1 - v0)) * 32)
        for k in range(n + 1):
            u = u0 + (u1 - u0) * k / max(1, n); v = v0 + (v1 - v0) * k / max(1, n)
            pts.append((int(round(x + (u - v) * 32)), int(round(y + (u + v) * 16))))
    seg(0, 0, su, 0); seg(su, 0, su, sv); seg(su, sv, 0, sv); seg(0, sv, 0, 0)
    H, W = img.shape[:2]
    for (px, py) in pts:
        if 0 <= px < W and 0 <= py < H:
            img[py, px] = color


def family_sheet(lib, ids, out_path, title='', scale=2, cols_px=1400, bg=(216, 207, 194)):
    """Planche : chaque asset dans toutes ses orientations, ombre + couche front composees."""
    cells = []
    for aid in ids:
        e = lib.get(aid)
        orients = e.get('orientations') or {}
        if not orients and 'variants' in e:
            orients = e['variants']
        imgs = []
        for key, o in orients.items():
            L = o.get('layers', {})
            base = L.get('base') or L.get('overlay')
            if not base:
                continue
            w0, h0 = o['size']
            ax0, ay0 = o['anchor']
            # etendre la cellule si l'ancre sort de l'image (objets suspendus)
            ex0, ey0 = min(0, ax0), min(0, ay0)
            ex1, ey1 = max(w0, ax0 + 1), max(h0, ay0 + 1)
            w, h = ex1 - ex0, ey1 - ey0
            cell = np.zeros((h + 12, w + 8, 4), np.uint8)
            ax, ay = ax0 - ex0, ay0 - ey0
            ox_, oy_ = 4 - ex0, 2 - ey0
            fw = o.get('footprint_world_tiles')
            if fw:
                diamond_outline(cell, 4 + ax, 2 + ay, fw[0], fw[1])
            if 'shadow' in L:
                over(cell, shadow_layer_to_rgba(lib.png(L['shadow']['file'])[..., 3]), ox_, oy_)
            over(cell, lib.png(base['file']), ox_, oy_)
            if 'glass' in L:
                over(cell, lib.png(L['glass']['file']), ox_, oy_)
            if 'front' in L:
                over(cell, lib.png(L['front']['file']), ox_, oy_)
            # croix d'ancre
            cx, cy = 4 + ax, 2 + ay
            for d in (-2, -1, 1, 2):
                for (px, py) in ((cx + d, cy), (cx, cy + d)):
                    if 0 <= px < cell.shape[1] and 0 <= py < cell.shape[0]:
                        cell[py, px] = (200, 70, 60, 255)
            draw_text(cell, 2, h + 5, key, (110, 98, 90, 255))
            imgs.append(cell)
        if not imgs:
            continue
        cw = sum(c.shape[1] for c in imgs) + 4 * (len(imgs) - 1)
        chh = max(c.shape[0] for c in imgs)
        lab = aid
        lw = len(lab) * 4 + 4
        block = np.zeros((chh + 10, max(cw, lw) + 6, 4), np.uint8)
        draw_text(block, 2, 1, lab)
        x = 2
        for c in imgs:
            block[8:8 + c.shape[0], x:x + c.shape[1]] = np.where(c[..., 3:4] > 0, c, block[8:8 + c.shape[0], x:x + c.shape[1]])
            x += c.shape[1] + 4
        cells.append(block)
    # mise en page en rangees
    W = cols_px // scale
    rows = []; cur = []; cw = 0
    for c in cells:
        if cur and cw + c.shape[1] + 6 > W:
            rows.append(cur); cur = []; cw = 0
        cur.append(c); cw += c.shape[1] + 6
    if cur:
        rows.append(cur)
    Ht = 14 + sum(max(c.shape[0] for c in r) + 6 for r in rows)
    Wt = max([sum(c.shape[1] + 6 for c in r) for r in rows] + [W]) + 8
    sheet = np.zeros((Ht, Wt, 4), np.uint8)
    sheet[..., :3] = bg; sheet[..., 3] = 255
    draw_text(sheet, 4, 3, title or os.path.basename(out_path), (43, 33, 40, 255))
    y = 14
    for r in rows:
        x = 4
        for c in r:
            over(sheet, c, x, y)
            x += c.shape[1] + 6
        y += max(c.shape[0] for c in r) + 6
    save_png(out_path, upscale(sheet, scale))
    return sheet.shape
