"""Aides de modelisation reutilisables (formes composees, decors d'ecrans, details)."""
import math

from .iso import Poly, Box, Cyl, Ell, HCyl, Fix, Use, Delta, rect, hline, vline, frame, disc, FRONT_IF_BACK
from .palette import M, FIXED

INK = Fix('ink')


def rbox(m, cx, cy, length, width, angle_deg, z0, z1, mat, **kw):
    """Boite pivotee autour de z (barre de pietement etoile, bras robotise...)."""
    a = math.radians(angle_deg)
    ca, sa = math.cos(a), math.sin(a)
    hl, hw = length / 2.0, width / 2.0
    # axes locaux : d = (ca, sa) le long, n = (-sa, ca) en travers
    planes = [(ca, sa, 0, ca * cx + sa * cy + hl, 'front'),
              (-ca, -sa, 0, -(ca * cx + sa * cy) + hl, 'back'),
              (-sa, ca, 0, -sa * cx + ca * cy + hw, 'right'),
              (sa, -ca, 0, sa * cx - ca * cy + hw, 'left'),
              (0, 0, 1, z1, 'top'), (0, 0, -1, -z0, 'bottom')]
    r = hl + hw
    return m.add(Poly(planes, (cx - r, cy - r, z0, cx + r, cy + r, z1), mat, **kw))


def chamfer_box(m, x0, y0, z0, x1, y1, z1, c, mat, **kw):
    """Boite aux aretes verticales chanfreinees (meubles arrondis, machines)."""
    planes = [(1, 0, 0, x1, 'right'), (-1, 0, 0, -x0, 'left'),
              (0, 1, 0, y1, 'back'), (0, -1, 0, -y0, 'front'),
              (0, 0, 1, z1, 'top'), (0, 0, -1, -z0, 'bottom')]
    k = 1 / math.sqrt(2)
    for (sx, sy, cx_, cy_, tag) in ((1, -1, x1, y0, 'right'), (-1, -1, x0, y0, 'left'),
                                     (1, 1, x1, y1, 'right'), (-1, 1, x0, y1, 'left')):
        # plan passant a distance c du coin
        nx, ny = sx * k, sy * k
        d = nx * cx_ + ny * cy_ - c * k
        planes.append((nx, ny, 0, d, tag))
    return m.add(Poly(planes, (x0, y0, z0, x1, y1, z1), mat, **kw))


def star_base(m, z=0, r=9, mat=None, casters=True, legs=5, rot=18):
    mat = mat or M('metal', 'graphite')
    for i in range(legs):
        ang = rot + i * 360.0 / legs
        a = math.radians(ang)
        cx, cy = math.cos(a) * r / 2.0, math.sin(a) * r / 2.0
        rbox(m, cx, cy, r, 2.0, ang, z + 1, z + 3, mat)
        if casters:
            m.cyl(math.cos(a) * r, math.sin(a) * r, 1.3, z, z + 1.6, M('metal', 'graphite'))
    m.cyl(0, 0, 2.2, z + 1, z + 4, mat)


CODE_COLORS = ['sun', 'teal', 'coral', 'sky', 'plastic', 'leaf']


def code_screen(prim, face, w, h, seed=0, bg=None, margin=1, light=False):
    """Contenu par defaut d'un ecran : editeur de code stylise (lignes colorees)."""
    from .palette import RAMPS
    bg = bg or M('screen', 'display')
    prim.decal(face, rect(margin, margin, w - margin, h - margin), Use(bg, idx=1 if not light else 4))
    prim.decal(face, rect(margin, h - margin - 2, w - margin, h - margin), Use(bg, idx=2 if not light else 3))
    rows = list(range(int(h - margin - 4), int(margin), -2))
    import random
    rnd = random.Random(seed)
    indent = 0
    for i, t in enumerate(rows):
        if rnd.random() < 0.18:
            continue
        indent = max(0, min(3, indent + rnd.choice([-1, 0, 0, 1])))
        s0 = margin + 1 + indent * 2
        ln = rnd.randint(3, max(4, int(w - s0 - margin - 2)))
        col = RAMPS[CODE_COLORS[(i + seed) % len(CODE_COLORS)]][4]
        prim.decal(face, rect(s0, t, min(w - margin - 1, s0 + ln), t + 1), Fix(col))
        if rnd.random() < 0.4 and s0 + ln + 3 < w - margin - 1:
            col2 = RAMPS[CODE_COLORS[(i + seed + 2) % len(CODE_COLORS)]][3]
            prim.decal(face, rect(s0 + ln + 1, t, min(w - margin - 1, s0 + ln + 1 + rnd.randint(1, 4)), t + 1), Fix(col2))


def ui_screen(prim, face, w, h, seed=0, bg=None, margin=1):
    """Contenu par defaut d'un ecran d'interface (fenetres, graphiques)."""
    from .palette import RAMPS
    bg = bg or M('screen', 'display')
    prim.decal(face, rect(margin, margin, w - margin, h - margin), Use(bg, idx=1))
    prim.decal(face, rect(margin, h - margin - 2, w - margin, h - margin), Fix(RAMPS['teal'][3]))
    cw = max(4, (w - 2 * margin - 3) // 2)
    prim.decal(face, rect(margin + 1, margin + 1, margin + 1 + cw, h - margin - 3), Fix(RAMPS['navy'][3]))
    for k in range(3):
        bh = 2 + (seed + k * 3) % max(2, int(h - 2 * margin - 6))
        prim.decal(face, rect(margin + 2 + k * 3, margin + 2, margin + 4 + k * 3, margin + 2 + bh), Fix(RAMPS['sun'][4]))
    prim.decal(face, rect(margin + 2 + cw, margin + 1, w - margin - 1, h - margin - 3), Fix(RAMPS['navy'][2]))
    for k, t in enumerate(range(int(h - margin - 5), int(margin + 1), -2)):
        prim.decal(face, rect(margin + 3 + cw, t, w - margin - 2 - (k % 3), t + 1), Fix(RAMPS['plastic'][3]))


def led(prim, face, s, t, color='led_green', w=1, h=1):
    prim.decal(face, rect(s, t, s + w, t + h), Fix(color))


def vents(prim, face, s0, s1, t0, t1, step=2, delta=-1, vertical=False):
    if vertical:
        s = s0
        while s < s1:
            prim.decal(face, rect(s, t0, s + 1, t1), Delta(delta))
            s += step
    else:
        t = t0
        while t < t1:
            prim.decal(face, rect(s0, t, s1, t + 1), Delta(delta))
            t += step


def drawers(prim, face, s0, s1, t0, t1, n, handle_mat=None, gap=1):
    """n tiroirs empiles sur une face, joints sombres et poignees."""
    hm = handle_mat or M('metal', 'steel')
    hgt = (t1 - t0) / n
    for i in range(n):
        a = t0 + i * hgt
        prim.decal(face, rect(s0, a, s1, a + gap), Delta(-1))
        mid = (s0 + s1) / 2.0
        prim.decal(face, rect(mid - 2, a + hgt - 3, mid + 2, a + hgt - 2), Use(hm, idx=4))
