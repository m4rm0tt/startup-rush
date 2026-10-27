"""Reference de composition (a porter en C#) : recoloration par masque,
motifs muraux, assemblage de pieces. Algorithmes decrits dans COLOR_SYSTEM.md.
"""
import numpy as np

from .palette import RAMPS, CHANNELS, CHANNEL_NAMES, MASK_FLAG_PATTERN, ramp_from_color


def _ramp(r):
    if isinstance(r, str):
        return np.array(RAMPS[r], np.uint8)
    arr = np.array(r, np.uint8)
    if arr.shape[1] == 3:
        arr = np.concatenate([arr, np.full((arr.shape[0], 1), 255, np.uint8)], 1)
    return arr


def recolor(rgba, mask, swatches):
    """swatches : {nom_de_canal: nom_de_rampe | liste de 6 couleurs}.
    Les canaux absents gardent la couleur du sprite de base."""
    out = rgba.copy()
    sel = mask[..., 3] > 0
    for cname, ramp in swatches.items():
        c = CHANNELS[cname]
        m = sel & (mask[..., 0] == c)
        if not m.any():
            continue
        pal = _ramp(ramp)
        idx = np.clip(mask[..., 1][m], 0, 5)
        out[m, :3] = pal[idx, :3]
    return out


def apply_wall_pattern(rgba, mask, overlay, swatches, offsets=((0, 0),)):
    """Applique un calque de motif (meme ancre que la piece) puis recolore.
    offsets : decalages (dx, dy) du calque pour les pieces de plusieurs cases."""
    out = rgba.copy()
    h, w = mask.shape[:2]
    surf = (mask[..., 3] > 0) & ((mask[..., 2] & MASK_FLAG_PATTERN) > 0)
    ch = mask[..., 0].astype(int).copy()
    idx = mask[..., 1].astype(int).copy()
    done = np.zeros((h, w), bool)
    oh, ow = overlay.shape[:2]
    for (dx, dy) in offsets:
        ys, xs = np.nonzero(surf & ~done)
        oy = ys - dy; ox = xs - dx
        ok = (oy >= 0) & (oy < oh) & (ox >= 0) & (ox < ow)
        ys, xs, oy, ox = ys[ok], xs[ok], oy[ok], ox[ok]
        has = overlay[oy, ox, 3] > 0
        ys, xs, oy, ox = ys[has], xs[has], oy[has], ox[has]
        ch[ys, xs] = overlay[oy, ox, 0]
        idx[ys, xs] = np.clip(idx[ys, xs] + overlay[oy, ox, 1].astype(int) - 8, 0, 5)
        done[ys, xs] = True
    sel = mask[..., 3] > 0
    for c in np.unique(ch[sel]):
        cname = CHANNEL_NAMES[int(c)]
        if cname not in swatches:
            if c == CHANNELS['wall2'] and 'wall' in swatches:
                ramp = swatches['wall']
            else:
                continue
        else:
            ramp = swatches[cname]
        pal = _ramp(ramp)
        m = sel & (ch == c)
        out[m, :3] = pal[idx[m], :3]
    return out
