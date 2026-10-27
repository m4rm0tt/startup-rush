"""Planches de controle du lot UI : composants HUD + etats, test 9-slice multi-tailles."""
import numpy as np

from .lot_ui_core import canvas, fill, blit, scale, fonts, tok, nine, rect


def _states(entry):
    img, sp = entry
    st = sp.get('states')
    if st:
        return list(st.items())
    return [('base', img)]


def hud_sheet(items, title, width=900, k=2):
    F, _ = fonts()
    blocks = []
    for iid, entry in items.items():
        sts = _states(entry)
        if not sts or sts[0][1] is None:
            continue
        cells = []
        for sname, im in sts:
            h, w = im.shape[:2]
            label_w = F.measure(sname)
            cw = max(w, label_w) + 6
            cell = canvas(cw, h + 16, (0, 0, 0, 0))
            blit(cell, im, 3, 2)
            F.draw(cell, 3, h + 3, sname, tok('text_muted'))
            cells.append(cell)
        tw = max(F.measure(iid) + 6, sum(c.shape[1] for c in cells))
        th = max(c.shape[0] for c in cells) + 14
        blk = canvas(tw + 6, th, tok('paper'))
        F.draw(blk, 3, 1, iid, tok('ink'))
        x = 0
        for c in cells:
            blit(blk, c, x, 13)
            x += c.shape[1]
        blocks.append(blk)
    rows, cur, cw = [], [], 0
    for b in blocks:
        if cur and cw + b.shape[1] + 4 > width:
            rows.append(cur); cur, cw = [], 0
        cur.append(b); cw += b.shape[1] + 4
    if cur:
        rows.append(cur)
    H = 22 + sum(max(b.shape[0] for b in r) + 4 for r in rows)
    sheet = canvas(width + 8, H, tok('paper_2'))
    F.draw(sheet, 6, 4, title, tok('ink'))
    y = 20
    for r in rows:
        x = 4
        for b in r:
            blit(sheet, b, x, y)
            x += b.shape[1] + 4
        y += max(b.shape[0] for b in r) + 4
    return scale(sheet, k)


def nine_test(items, ids, sizes=((40, 22), (72, 30), (130, 48)), k=2):
    """Chaque composant 9-slice rendu a plusieurs tailles : coins intacts."""
    F, _ = fonts()
    rows = []
    for iid in ids:
        img, sp = items[iid]
        base = img if img is not None else next(iter(sp['states'].values()))
        ms = sp['min_size']
        row_imgs = []
        rz = sp.get('resize', 'both')
        if rz == 'none':
            continue
        for (w, h) in sizes:
            w2 = max(w, ms[0]); h2 = max(h, ms[1])
            if rz == 'horizontal':
                h2 = base.shape[0]
            if rz == 'vertical':
                w2 = base.shape[1]
            if sp.get('nine_slice') == [0, 0, 0, 0] and sp.get('stretch') != 'tile':
                continue
            row_imgs.append(nine(base, w2, h2, sp['nine_slice'], sp.get('stretch', 'stretch')))
        if row_imgs:
            rows.append((iid, row_imgs))
    W = 20 + sum(s[0] for s in sizes) + 200
    H = 24 + sum(max(im.shape[0] for im in r[1]) + 18 for r in rows)
    sheet = canvas(W, H, tok('paper_2'))
    F.draw(sheet, 6, 4, "TEST 9-SLICE : coins intacts, centre et bords etires selon l'axe autorise", tok('ink'))
    y = 22
    for iid, ims in rows:
        F.draw(sheet, 6, y, iid, tok('ink'))
        x = 6
        for im in ims:
            blit(sheet, im, x, y + 13)
            x += im.shape[1] + 10
        y += max(im.shape[0] for im in ims) + 18
    return scale(sheet, k)
