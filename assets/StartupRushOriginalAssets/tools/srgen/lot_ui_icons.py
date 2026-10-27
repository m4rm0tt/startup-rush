"""Chargement / validation / rendu des icones 16x16 du lot UI et planche de controle."""
import importlib
import numpy as np

from .lot_ui_core import canvas, fill, blit, tint, scale, fonts, tok

SIZE = 16


def load_icon_sets(names=('icons_a', 'icons_b', 'icons_c')):
    out = {}
    for n in names:
        try:
            mod = importlib.import_module('ui.' + n)
        except ModuleNotFoundError:
            continue
        for k, v in mod.__dict__.items():
            if k.startswith('ICONS_') and isinstance(v, dict):
                for iid, (label, rows) in v.items():
                    assert iid not in out, f'icone en double : {iid}'
                    out[iid] = (label, rows)
    return out


def validate(icons):
    errs = []
    for iid, (label, rows) in icons.items():
        if len(rows) != SIZE:
            errs.append(f'{iid}: {len(rows)} rangees')
        for j, r in enumerate(rows):
            if len(r) != SIZE:
                errs.append(f'{iid}: rangee {j} de {len(r)} caracteres')
            if set(r) - set('#.'):
                errs.append(f'{iid}: caracteres invalides rangee {j}')
    return errs


def icon_rgba(rows, color=(255, 255, 255, 255)):
    img = np.zeros((SIZE, SIZE, 4), np.uint8)
    for j, r in enumerate(rows):
        for i, c in enumerate(r[:SIZE]):
            if c == '#':
                img[j, i] = color
    return img


def sheet(icons, title, cols=10, k=3, tints=None):
    """Planche : chaque icone en encre sur papier + teintes d'etat."""
    F, _ = fonts()
    tints = tints or [('paper', 'ink'), ('navy', 'paper'), ('paper', 'teal'), ('paper', 'text_disabled')]
    cell_w, cell_h = 100, 50
    ids = list(icons.keys())
    rows = (len(ids) + cols - 1) // cols
    W = cols * cell_w + 8
    H = rows * cell_h + 24
    img = canvas(W, H, tok('paper_2'))
    F.draw(img, 6, 4, title, tok('ink'))
    for n, iid in enumerate(ids):
        label, rr = icons[iid]
        x = 4 + (n % cols) * cell_w
        y = 20 + (n // cols) * cell_h
        fill(img, x, y, x + cell_w - 3, y + cell_h - 3, tok('paper'))
        ic = icon_rgba(rr)
        blit(img, tint(scale(ic, 2), tok('ink')), x + 3, y + 3)
        for t, (bg, fg) in enumerate(tints):
            bx = x + 38 + (t % 3) * 19
            by = y + 2 + (t // 3) * 18
            fill(img, bx, by, bx + 17, by + 17, tok(bg))
            blit(img, tint(ic, tok(fg)), bx + 1, by + 1)
        F.draw(img, x + 3, y + 36, iid[:16], tok('ink'))
    return scale(img, k)
