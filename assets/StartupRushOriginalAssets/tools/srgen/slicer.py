"""Decoupe d'une scene continue en modules (murs, sols, bords).

On rend une scene complete (ex. deux murs en L avec leurs ouvertures), puis on
attribue chaque pixel a un module selon la position monde du point touche et la
primitive gagnante. Les pixels de contour exterieur heritent du module voisin
(priorite : dessous, droite, gauche, dessus). Les modules se raboutent donc
pixel pour pixel, sans couture ni double contour.
"""
import numpy as np

from .iso import render


def render_owned(model, owner_fn):
    """owner_fn(prim_name, u, v, z) -> tableau d'entiers (codes de module).
    Retourne (res, owner) ou owner a la taille de l'image (0 = aucun)."""
    res = render(model, 'world')
    lay = res.layers['base']
    pid = lay.pid
    t = res.depth['base']
    a, b = res.a, res.b
    h, w = pid.shape
    owner = np.zeros((h, w), np.int32)
    for i, wp in enumerate(res.wprims):
        m = pid == i
        if not m.any():
            continue
        u = a[m] + t[m]; v = b[m] + t[m]; z = t[m]
        owner[m] = owner_fn(wp.src.name or '', u, v, z)
    ring = pid == -3
    for (dy, dx) in ((1, 0), (0, 1), (0, -1), (-1, 0)):
        nb = np.zeros_like(owner)
        ys0, ys1 = max(0, -dy), h - max(0, dy)
        xs0, xs1 = max(0, -dx), w - max(0, dx)
        nb[ys0:ys1, xs0:xs1] = owner[ys0 + dy:ys1 + dy, xs0 + dx:xs1 + dx]
        fill = ring & (owner == 0) & (nb > 0)
        owner[fill] = nb[fill]
    return res, owner


def anchor_px(res, u, v, z=0.0):
    X0, Y0 = res.origin
    sx = u - v; sy = (u + v) / 2.0 - z
    return int(round(sx - X0)), int(round(sy - Y0))


def extract(res, owner, codes, anchor, rect=None):
    """Extrait les pixels des codes donnes. rect = (dx0, dy0, dx1, dy1) relatif a l'ancre."""
    lay = res.layers['base']
    m = np.isin(owner, list(codes))
    rgba = np.where(m[..., None], lay.rgba, 0).astype(np.uint8)
    mask = np.where(m[..., None], lay.mask, 0).astype(np.uint8)
    ax, ay = anchor
    if rect is None:
        ys, xs = np.nonzero(m)
        rect = (xs.min() - ax, ys.min() - ay, xs.max() + 1 - ax, ys.max() + 1 - ay)
    dx0, dy0, dx1, dy1 = rect
    out = np.zeros((dy1 - dy0, dx1 - dx0, 4), np.uint8)
    outm = np.zeros_like(out)
    H, W = m.shape
    sx0, sy0 = ax + dx0, ay + dy0
    for arr, dst in ((rgba, out), (mask, outm)):
        x0 = max(0, sx0); y0 = max(0, sy0)
        x1 = min(W, sx0 + out.shape[1]); y1 = min(H, sy0 + out.shape[0])
        dst[y0 - sy0:y1 - sy0, x0 - sx0:x1 - sx0] = arr[y0:y1, x0:x1]
    return out, outm, (-dx0, -dy0), rect


def bbox_rel(owner, codes, anchor):
    m = np.isin(owner, list(codes))
    ys, xs = np.nonzero(m)
    ax, ay = anchor
    return (xs.min() - ax, ys.min() - ay, xs.max() + 1 - ax, ys.max() + 1 - ay)


def union_rect(rects):
    return (min(r[0] for r in rects), min(r[1] for r in rects),
            max(r[2] for r in rects), max(r[3] for r in rects))
