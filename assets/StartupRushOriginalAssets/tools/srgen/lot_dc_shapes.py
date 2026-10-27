"""Aides de modelisation du lot datacenter / laboratoires.

Formes : barres orientees en 3D (trepieds, bras robotises), grilles, decors de
facade (ventilateurs, baies de disques, ports, perforations, courbes d'ecran).
Toutes les couleurs viennent de la palette (srgen.palette.RAMPS / FIXED).
"""
import math
import numpy as np

from .iso import Poly, Fix, Use, Delta, rect, disc
from .palette import RAMPS, FIXED


def ramp_col(name, idx, a=255):
    c = RAMPS[name][idx]
    return (c[0], c[1], c[2], a)


def strut(m, p0, p1, w, mat, h=None, **kw):
    """Barre droite de section w x h entre deux points locaux (x, y en su ; z en px)."""
    p0 = np.array(p0, float); p1 = np.array(p1, float)
    d = p1 - p0
    L = float(np.linalg.norm(d))
    d = d / L
    up = np.array([0.0, 0.0, 1.0])
    e1 = np.cross(d, up)
    if np.linalg.norm(e1) < 1e-6:
        e1 = np.array([1.0, 0.0, 0.0])
    e1 = e1 / np.linalg.norm(e1)
    e2 = np.cross(e1, d)
    e2 = e2 / np.linalg.norm(e2)
    hw = w / 2.0
    hh = (h if h is not None else w) / 2.0
    c = (p0 + p1) / 2.0
    planes = []
    for (n, half, tag) in ((e1, hw, 'right'), (-e1, hw, 'left'), (e2, hh, 'top'), (-e2, hh, 'bottom'),
                           (d, L / 2.0, 'front'), (-d, L / 2.0, 'back')):
        planes.append((float(n[0]), float(n[1]), float(n[2]), float(n @ c + half), tag))
    r = max(hw, hh) + 1
    lo = np.minimum(p0, p1) - r
    hi = np.maximum(p0, p1) + r
    return m.add(Poly(planes, (lo[0], lo[1], lo[2], hi[0], hi[1], hi[2]), mat, **kw))


def tripod(m, cx, cy, z_top, spread, mat, leg_w=1.2, col_w=1.4, col_top=None):
    """Trepied : trois jambes inclinees et une colonne centrale."""
    for k in range(3):
        a = math.radians(90 + k * 120)
        fx, fy = cx + math.cos(a) * spread, cy + math.sin(a) * spread
        strut(m, (fx, fy, 0), (cx, cy, z_top), leg_w, mat)
    if col_top:
        m.box(cx - col_w / 2, cy - col_w / 2, z_top - 2, cx + col_w / 2, cy + col_w / 2, col_top, mat)


# ---------------------------------------------------------------------------
# Decors de facade (coordonnees de face : s vers la droite, t vers le haut)
# ---------------------------------------------------------------------------

def fan_grill(p, face, cs, ct, r, spokes=4, dark=-2):
    """Grille de ventilateur ronde : cuve sombre, rayons, moyeu."""
    p.decal(face, disc(cs, ct, r, r), Delta(dark))
    p.decal(face, ('fn', lambda s, t: np.abs(np.hypot(s - cs, t - ct) - (r - 0.6)) < 0.55), Delta(dark + 1))

    def spk(s, t):
        ang = np.arctan2(t - ct, s - cs)
        rr = np.hypot(s - cs, t - ct)
        k = (ang * spokes / (2 * math.pi)) % 1.0
        return (rr < r - 1.0) & (rr > 0.8) & ((k < 0.18) | (k > 0.92))
    p.decal(face, ('fn', spk), Delta(dark + 1))
    p.decal(face, disc(cs, ct, max(0.8, r * 0.28), max(0.8, r * 0.28)), Delta(0))


def drive_bays(p, face, s0, t0, cols, rows, w, h, gs=1, gt=1, led_col='led_green', led_every=1):
    """Baies de disques : tiroirs sombres avec une LED par tiroir."""
    for r in range(rows):
        for c in range(cols):
            s = s0 + c * (w + gs)
            t = t0 + r * (h + gt)
            p.decal(face, rect(s, t, s + w, t + h), Delta(-1))
            p.decal(face, rect(s, t + h - 1, s + w, t + h), Delta(+1))
            if led_col and (r * cols + c) % led_every == 0:
                p.decal(face, rect(s + w - 1, t, s + w, t + 1), Fix(led_col))


def port_row(p, face, s0, s1, t, step=2, col=None, led=None, led_step=None):
    """Rangee de ports RJ (carres sombres) ; LED optionnelle au-dessus."""
    s = s0
    k = 0
    while s < s1:
        p.decal(face, rect(s, t, s + 1, t + 1), Fix(col) if col else Delta(-2))
        if led and (led_step is None or k % led_step == 0):
            p.decal(face, rect(s, t + 1, s + 1, t + 2), Fix(led))
        s += step
        k += 1


def perforation(p, face, s0, t0, s1, t1, step=2, delta=-1, phase=0):
    def f(s, t):
        si = np.floor(s).astype(int); ti = np.floor(t).astype(int)
        return (s >= s0) & (s < s1) & (t >= t0) & (t < t1) & (((si + phase) % step) == 0) & ((ti % step) == 0)
    p.decal(face, ('fn', f), Delta(delta))


def mesh(p, face, s0, t0, s1, t1, ss=3, st=4, paint=None):
    def f(s, t):
        si = np.floor(s).astype(int); ti = np.floor(t).astype(int)
        inside = (s >= s0) & (s < s1) & (t >= t0) & (t < t1)
        return inside & (((si - int(s0)) % ss == 0) | ((ti - int(t0)) % st == 0))
    p.decal(face, ('fn', f), paint if paint is not None else Delta(-1))


def waveform(p, face, s0, t0, w, h, color, periods=2.0, kind='sine'):
    """Trace d'oscilloscope (une ligne de 1 px)."""
    def f(s, t):
        x = (s - s0) / max(1e-6, w)
        if kind == 'sine':
            y = t0 + h / 2.0 + (h / 2.0 - 0.6) * np.sin(x * periods * 2 * math.pi)
        else:  # carre
            y = t0 + np.where(((x * periods * 2) % 2) < 1, h - 1.0, 1.0)
        return (s >= s0) & (s < s0 + w) & (np.abs(t - y) < 0.6)
    p.decal(face, ('fn', f), Fix(color))


def digits(p, face, s0, t0, n, color, w=2, gap=1):
    """Afficheur 7 segments stylise : blocs de chiffres (aucun texte lisible)."""
    for k in range(n):
        s = s0 + k * (w + gap)
        p.decal(face, rect(s, t0, s + w, t0 + 1), Fix(color))
        p.decal(face, rect(s, t0 + 2, s + w, t0 + 3), Fix(color))
        p.decal(face, rect(s + (k % 2) * (w - 1), t0, s + (k % 2) * (w - 1) + 1, t0 + 3), Fix(color))


def knobs(p, face, pts, delta=+2, r=0.9):
    for (s, t) in pts:
        p.decal(face, disc(s, t, r, r), Delta(delta))


def hazard_triangle(p, face, s, t, size=4):
    """Pictogramme triangle d'avertissement (jaune, bord fonce), sans texte."""
    yel = ramp_col('hazard', 4)
    dark = ramp_col('graphite', 1)

    def tri(ss, tt, shrink=0.0):
        rel_t = (tt - t) / size
        half = (1.0 - rel_t) * size / 2.0 - shrink
        return (tt >= t + shrink * 0.8) & (tt < t + size - shrink) & (np.abs(ss - (s + size / 2.0)) <= half)
    p.decal(face, ('fn', lambda ss, tt: tri(ss, tt)), Fix(dark))
    p.decal(face, ('fn', lambda ss, tt: tri(ss, tt, 0.9)), Fix(yel))
    p.decal(face, rect(s + size / 2.0 - 0.5, t + 1.6, s + size / 2.0 + 0.5, t + size - 1.4), Fix(dark))
