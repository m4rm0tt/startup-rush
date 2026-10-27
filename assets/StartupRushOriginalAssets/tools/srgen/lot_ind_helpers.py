"""Aides de modelisation du lot industrie + decoration.

Ne modifie aucun module partage : ces fonctions composent uniquement des
primitives et des decors existants (tools/srgen/iso.py, shapes.py).
"""
import math
import numpy as np

from .iso import Fix, Use, Delta, rect, disc
from .palette import M, RAMPS

GRAPH = M('fixed', 'graphite')   # pieces sombres : non recolorables (un canal = une rampe)
STEEL = M('metal', 'steel')


# ---------------------------------------------------------------------------
# Tampon ASCII sur une face (affiches, ecrans, plaques)
# ---------------------------------------------------------------------------
def stamp(prim, face, rows, legend, s0=0.0, t_top=None):
    """Peint une image ASCII sur une face : la colonne i couvre s in [s0+i, s0+i+1),
    la ligne j (de haut en bas) couvre t in [t_top-j-1, t_top-j). Sur une face
    verticale, 1 case du tampon = exactement 1 pixel ecran (cisaille 2:1 iso).
    legend : caractere -> peinture (Use/Fix/Delta). '.' et ' ' = transparent."""
    H = len(rows)
    W = max(len(r) for r in rows)
    grid = np.array([list(r.ljust(W, '.')) for r in rows])
    if t_top is None:
        t_top = float(H)
    for ch, paint in legend.items():
        mg = grid == ch
        if not mg.any():
            continue

        def f(s, t, mg=mg):
            s = np.asarray(s); t = np.asarray(t)
            i = np.floor(s - s0).astype(int)
            j = np.floor(t_top - t).astype(int)
            ok = (i >= 0) & (i < W) & (j >= 0) & (j < H)
            out = np.zeros(s.shape, bool)
            out[ok] = mg[j[ok], i[ok]]
            return out
        prim.decal(face, ('fn', f), paint)


def hsh(a, b, seed=0):
    a = np.asarray(a, np.int64); b = np.asarray(b, np.int64)
    h = (a * 73856093) ^ (b * 19349663) ^ (seed * 83492791)
    h = (h ^ (h >> 13)) * 1274126177
    return (h ^ (h >> 16)) & 0xffff


def speckle(prim, face, seed, mod, paint, s_scale=1.0, t_scale=1.0):
    prim.decal(face, ('fn', lambda s, t: hsh(np.floor(s / s_scale), np.floor(t / t_scale), seed) % mod == 0), paint)


def mesh(prim, face, step=3, paint=None, s0=0, t0=0, s1=999, t1=999):
    paint = paint or Delta(+1)
    prim.decal(face, ('fn', lambda s, t: (((np.floor(s) % step) == 0) | ((np.floor(t) % step) == 0))
                      & (s >= s0) & (s < s1) & (t >= t0) & (t < t1)), paint)


def perforation(prim, face, s0, s1, t0, t1, step=2, paint=None):
    paint = paint or Delta(-1)
    prim.decal(face, ('fn', lambda s, t: ((np.floor(s) % step) == 0) & ((np.floor(t) % step) == 0)
                      & (s >= s0) & (s < s1) & (t >= t0) & (t < t1)), paint)


def hazard_stripes(prim, face, s0, s1, t0, t1, period=6):
    prim.decal(face, rect(s0, t0, s1, t1), Use(M('accent', 'hazard'), rel=0))
    prim.decal(face, ('fn', lambda s, t: ((np.floor(s + t) % period) < period / 2)
                      & (s >= s0) & (s < s1) & (t >= t0) & (t < t1)), Use(M('fixed', 'graphite'), idx=1))


def fan_disc(prim, face, cs, ct, r, ramp='graphite'):
    """Ventilateur vu de face (grille, pales, moyeu) dans le repere (s, t) d'une face."""
    G = M('fixed', ramp)
    prim.decal(face, ('fn', lambda s, t: (s - cs) ** 2 + (t - ct) ** 2 <= r * r), Use(G, idx=1))
    prim.decal(face, ('fn', lambda s, t: ((np.degrees(np.arctan2(t - ct, s - cs)) % 60) < 22)
                      & (np.hypot(s - cs, t - ct) < r - 0.8) & (np.hypot(s - cs, t - ct) > 1.0)), Use(G, idx=2))
    prim.decal(face, ('fn', lambda s, t: np.abs(np.hypot(s - cs, t - ct) - (r - 0.5)) < 0.5), Use(G, idx=3))
    prim.decal(face, disc(cs, ct, 1.2), Use(M('fixed', 'steel'), idx=4))


def stack_light(m, x, y, z, lit='leaf'):
    """Colonne lumineuse (vert / ambre / rouge) ; un segment allume (choix de design statique)."""
    m.cyl(x, y, 0.7, z, z + 3, GRAPH)
    zz = z + 3
    for ramp in ('leaf', 'sun', 'red'):
        p = m.cyl(x, y, 1.6, zz, zz + 2.5, M('fixed', ramp))
        if ramp == lit:
            p.decal('side', rect(-1, -1, 999, 999), Use(M('fixed', ramp), idx=5))
        zz += 2.5
    m.cyl(x, y, 1.6, zz, zz + 1, GRAPH)
    return zz + 1


def screen_panel(m, x0, y0, z0, x1, y1, z1, kind='ui', seed=0, face='front', mat=None):
    """Petit ecran (boitier sombre + contenu)."""
    from .shapes import ui_screen, code_screen
    p = m.box(x0, y0, z0, x1, y1, z1, mat or M('fixed', 'graphite'))
    w = (x1 - x0) if face in ('front', 'back') else (y1 - y0)
    h = z1 - z0
    if kind == 'code':
        code_screen(p, face, w, h, seed=seed)
    else:
        ui_screen(p, face, w, h, seed=seed)
    return p


def ring_segments(m, cx, cy, r, w, a0, a1, n, z0, z1, mat, **kw):
    """Anneau approxime par n segments tangents (rails de convoyeur courbe, garde-corps)."""
    from .shapes import rbox
    out = []
    for k in range(n):
        a = math.radians(a0 + (a1 - a0) * (k + 0.5) / n)
        L = 2 * math.pi * r * abs(a1 - a0) / 360.0 / n + 0.8
        out.append(rbox(m, cx + math.cos(a) * r, cy + math.sin(a) * r, L, w, math.degrees(a) + 90, z0, z1, mat, **kw))
    return out


def label(prim, face, s0, t0, w, h, lines=2):
    """Etiquette claire avec traits (texte suggere, jamais lisible)."""
    prim.decal(face, rect(s0, t0, s0 + w, t0 + h), Use(M('fixed', 'cream'), idx=5))
    for k in range(lines):
        tt = t0 + h - 2 - k * 2
        if tt < t0 + 1:
            break
        prim.decal(face, rect(s0 + 1, tt, s0 + w - 1 - (k % 2) * 2, tt + 1), Use(M('fixed', 'graphite'), idx=3))
