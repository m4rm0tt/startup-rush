"""Architecture modulaire : murs, angles, terminaisons, sols, bords, seuils, motifs.

Murs : rendus comme tranches d'une scene continue (voir tools/srgen/slicer.py).
  - mur gauche  = le long de V, face interieure tournee vers +U -> orientation 'se'
  - mur droit   = le long de U, face interieure tournee vers +V -> orientation 'sw'
  Epaisseur 8 su (exterieure a la piece), hauteur 112 px, plinthe 6 px.
  Toutes les pieces d'une orientation partagent la meme toile et la meme ancre :
  ancre = coin interieur bas de la case de mur (U=0,V=j) ou (U=i,V=0).
Sols : losanges 64x32, ancre (32, 0) = coin nord de la case.
Motifs muraux : calques (canal, delta) appliques sur les pixels marques
'surface murale' (bit 0 du canal B du masque).
"""
import os
import numpy as np

from srgen.iso import Model, Box, Fix, Use, Delta, rect, hline, vline, frame, WALL_H, TILE
from srgen.palette import M, RAMPS, CHANNELS, MASK_FLAG_PATTERN, FIXED
from srgen.registry import custom_builder, PACK_VERSION
from srgen.slicer import render_owned, anchor_px, extract, bbox_rel, union_rect
from srgen.imgio import save_png

T = 8            # epaisseur du mur (su)
H = WALL_H       # 112 px
BB_H, BB_D = 6, 2
N = 4            # longueur des pans de la scene de rendu (cases)

WALL = M('wall', 'cream')
TRIM = M('trim', 'plastic')
CAP = M('trim', 'plastic')
GLASS = M('glass', 'glass')
DOOR = M('wood', 'birch')

WIN = dict(s0=5, s1=27, z0=34, z1=90)
DOORW = dict(s0=4, s1=28, z1=84)


# ---------------------------------------------------------------------------
# Construction d'un pan de mur continu (strates + ouvertures)
# ---------------------------------------------------------------------------
def _box_run(m, side, a0, a1, z0, z1, name, mat, flags=0, flag_faces=None, depth=(-T, 0), **kw):
    """side 'L' : le long de v (u in depth) ; side 'R' : le long de u (v in depth)."""
    d0, d1 = depth
    if side == 'L':
        return m.add(Box(d0, a0, z0, d1, a1, z1, mat, name=name, flags=flags, flag_faces=flag_faces, **kw))
    return m.add(Box(a0, d0, z0, a1, d1, z1, mat, name=name, flags=flags, flag_faces=flag_faces, **kw))


def inner_tag(side):
    return 'right' if side == 'L' else 'back'


def build_run(m, side, start, end, openings, tag):
    """Pan de mur de `start` a `end` (su le long du mur), ouvertures (a0, a1, z0, z1)."""
    zs = sorted(set([0, H] + [o[2] for o in openings] + [o[3] for o in openings]))
    for za, zb in zip(zs[:-1], zs[1:]):
        cuts = [(o[0], o[1]) for o in openings if o[2] <= za and o[3] >= zb]
        segs = []
        cur = start
        for (c0, c1) in sorted(cuts):
            if c0 > cur:
                segs.append((cur, c0))
            cur = max(cur, c1)
        if cur < end:
            segs.append((cur, end))
        for (s0, s1) in segs:
            mat = {'top': CAP, 'default': WALL}
            _box_run(m, side, s0, s1, za, zb, tag, mat, flags=MASK_FLAG_PATTERN, flag_faces=[inner_tag(side)])
    # plinthe (interrompue aux portes)
    door_cuts = sorted([(o[0] - 2, o[1] + 2) for o in openings if o[2] == 0])
    cur = max(start, 0)
    for (c0, c1) in door_cuts + [(end, end)]:
        if c0 > cur:
            _box_run(m, side, cur, c0, 0, BB_H, tag + 'b', TRIM, depth=(0, BB_D))
        cur = max(cur, c1)


def feat_box(m, side, a_off, s0, s1, z0, z1, d0, d1, mat, name, **kw):
    return _box_run(m, side, a_off + s0, a_off + s1, z0, z1, name, mat, depth=(d0, d1), **kw)


def add_window(m, side, j, name):
    a = j * TILE
    s0, s1, z0, z1 = WIN['s0'], WIN['s1'], WIN['z0'], WIN['z1']
    # vitre (en retrait)
    g = feat_box(m, side, a, s0 + 2, s1 - 2, z0 + 2, z1 - 2, -6, -4, GLASS, name, outline=False)
    _glass_decals(g, side, (s1 - s0 - 4))
    # cadre
    for (a0, a1, b0, b1) in ((s0, s0 + 2, z0, z1), (s1 - 2, s1, z0, z1), (s0, s1, z0, z0 + 2),
                             (s0, s1, z1 - 2, z1), (15, 17, z0, z1), (s0, s1, 70, 72)):
        feat_box(m, side, a, a0, a1, b0, b1, -6, -3, TRIM, name)
    # appui interieur
    feat_box(m, side, a, s0 - 1, s1 + 1, z0 - 3, z0, 0, 3, TRIM, name)


def add_window_wide(m, side, j, name):
    a = j * TILE
    s0, s1, z0, z1 = 5, 59, WIN['z0'], WIN['z1']
    g = feat_box(m, side, a, s0 + 2, s1 - 2, z0 + 2, z1 - 2, -6, -4, GLASS, name, outline=False)
    _glass_decals(g, side, (s1 - s0 - 4))
    for (a0, a1, b0, b1) in ((s0, s0 + 2, z0, z1), (s1 - 2, s1, z0, z1), (s0, s1, z0, z0 + 2),
                             (s0, s1, z1 - 2, z1), (31, 33, z0, z1), (18, 19, z0, z1), (45, 46, z0, z1),
                             (s0, s1, 70, 72)):
        feat_box(m, side, a, a0, a1, b0, b1, -6, -3, TRIM, name)
    feat_box(m, side, a, s0 - 1, s1 + 1, z0 - 3, z0, 0, 3, TRIM, name)


def _glass_decals(g, side, width):
    face = inner_tag(side)
    # ciel stylise : reflets diagonaux et bas plus sombre (aucun decor exterieur)
    g.decal(face, rect(0, 0, 999, 999), Use(GLASS, idx=3))
    g.decal(face, rect(0, 0, 999, 10), Use(GLASS, idx=2))
    g.decal(face, ('fn', lambda s, t: ((s + t * 0.9) % 26 >= 4) & ((s + t * 0.9) % 26 < 6)), Use(GLASS, idx=5))
    g.decal(face, ('fn', lambda s, t: ((s + t * 0.9) % 26 >= 8) & ((s + t * 0.9) % 26 < 9)), Use(GLASS, idx=4))
    g.decal(face, rect(0, 40, 999, 999), Use(GLASS, rel=0, idx=4))
    g.decal(face, ('fn', lambda s, t: ((s + t * 0.9) % 26 >= 4) & ((s + t * 0.9) % 26 < 6) & (t >= 40)), Use(GLASS, idx=5))


def add_door(m, side, j, name, leaf=True):
    a = j * TILE
    s0, s1, z1 = DOORW['s0'], DOORW['s1'], DOORW['z1']
    # chambranle (en saillie dans la piece)
    feat_box(m, side, a, s0 - 2, s0, 0, z1 + 3, 0, 2, TRIM, name)
    feat_box(m, side, a, s1, s1 + 2, 0, z1 + 3, 0, 2, TRIM, name)
    feat_box(m, side, a, s0 - 2, s1 + 2, z1, z1 + 3, 0, 2, TRIM, name)
    face = inner_tag(side)
    if leaf:
        d = feat_box(m, side, a, s0, s1, 0, z1, -6, -4, DOOR, name)
        d.decal(face, frame(3, 46, 21, 78), Delta(-1))
        d.decal(face, frame(3, 8, 21, 40), Delta(-1))
        d.decal(face, rect(4, 47, 20, 48), Delta(+1))
        d.decal(face, rect(4, 9, 20, 10), Delta(+1))
        hx0 = 18 if side == 'L' else 3
        feat_box(m, side, a, s0 + hx0, s0 + hx0 + 3, 40, 42, -4, -2, M('metal', 'brass'), name)
        feat_box(m, side, a, s0 + hx0, s0 + hx0 + 1, 38, 44, -4, -3.5, M('metal', 'brass'), name)
    else:
        v = feat_box(m, side, a, s0, s1, 0, z1, -8, -7, Fix(FIXED['void']), name, outline=False)
        v.decal(face, rect(0, 0, 999, 5), Fix('void_floor'))
        v.decal(face, rect(0, 5, 999, 7), Fix('void_edge'))


def make_scene(left_pieces, right_pieces, n=N, end=True):
    """left_pieces / right_pieces : dict index -> type ('window','door','opening','window_wide')."""
    m = Model(0, 0, outline='full', shadow=False, edge_hl=True)
    m.W = m.D = 0
    for side, pieces in (('L', left_pieces), ('R', right_pieces)):
        openings = []
        for j, kind in pieces.items():
            a = j * TILE
            if kind == 'window':
                openings.append((a + WIN['s0'], a + WIN['s1'], WIN['z0'], WIN['z1']))
            elif kind == 'window_wide':
                openings.append((a + 5, a + 59, WIN['z0'], WIN['z1']))
            elif kind in ('door', 'opening'):
                openings.append((a + DOORW['s0'], a + DOORW['s1'], 0, DOORW['z1']))
        build_run(m, side, -T, n * TILE, openings, side)
        for j, kind in pieces.items():
            nm = f"{side}f{j}"
            if kind == 'window':
                add_window(m, side, j, nm)
            elif kind == 'window_wide':
                add_window_wide(m, side, j, nm)
            elif kind == 'door':
                add_door(m, side, j, nm, leaf=True)
            elif kind == 'opening':
                add_door(m, side, j, nm, leaf=False)
    return m


def owner_fn_factory(n=N, wide=()):
    end = n * TILE

    def fn(name, u, v, z):
        out = np.zeros(u.shape, np.int32)
        if name.startswith('Lf'):
            j = int(name[2:]); out[:] = 10 + j; return out
        if name.startswith('Rf'):
            j = int(name[2:]); out[:] = 30 + j; return out
        if name.startswith('L'):
            out[:] = 10 + np.floor(v / TILE).astype(np.int32)
            out[v < 0] = 1
            out[v >= end - 1e-6] = 9
            return out
        if name.startswith('R'):
            out[:] = 30 + np.floor(u / TILE).astype(np.int32)
            out[u < 0] = 1
            out[u >= end - 1e-6] = 29
            return out
        return out
    return fn


# ---------------------------------------------------------------------------
# Motifs muraux : fn(s, t) -> (canal, delta) ; s le long de la case [0,32), t = z
# ---------------------------------------------------------------------------
def _hash(a, b, seed=0):
    a = np.asarray(a, np.int64); b = np.asarray(b, np.int64)
    h = (a * 73856093) ^ (b * 19349663) ^ (seed * 83492791)
    h = (h ^ (h >> 13)) * 1274126177
    return (h ^ (h >> 16)) & 0xffff


C_WALL, C_WALL2, C_WOOD = CHANNELS['wall'], CHANNELS['wall2'], CHANNELS['wood']


def pat_paint(s, t):
    return np.full(s.shape, C_WALL), np.zeros(s.shape, int)


def pat_two_tone(s, t):
    ch = np.where(t < 44, C_WALL2, C_WALL)
    d = np.zeros(s.shape, int)
    d[(t >= 44) & (t < 45)] = 1
    ch = np.where((t >= 42) & (t < 45), C_WALL2, ch)
    d[(t >= 42) & (t < 43)] = -1
    d[(t >= 43) & (t < 45)] = 1
    return ch, d


def pat_wallpaper(s, t):
    ch = np.full(s.shape, C_WALL)
    d = np.zeros(s.shape, int)
    si = np.floor(s).astype(int); ti = np.floor(t).astype(int)
    row = ti // 12
    ss = (si + (row % 2) * 4) % 8
    tt = ti % 12
    motif = ((ss == 4) & (tt >= 5) & (tt <= 7)) | ((tt == 6) & (ss >= 3) & (ss <= 5))
    d[motif] = -1
    dot = (ss == 0) & (tt == 0)
    d[dot] = -1
    return ch, d


def pat_stripes(s, t):
    si = np.floor(s).astype(int)
    ch = np.where((si % 8) < 4, C_WALL, C_WALL2)
    d = np.zeros(s.shape, int)
    return ch, d


def pat_wood_panels(s, t):
    si = np.floor(s).astype(int); ti = np.floor(t).astype(int)
    ch = np.full(s.shape, C_WOOD)
    d = np.zeros(s.shape, int)
    d[si % 8 == 0] = -1
    d[si % 8 == 1] = 1
    board = si // 8
    g = _hash(board, ti // 9, 3)
    grain = ((g % 7) == 0) & ((si % 8) >= 3) & ((si % 8) <= 5)
    d[grain] = -1
    return ch, d


def pat_concrete(s, t):
    si = np.floor(s).astype(int); ti = np.floor(t).astype(int)
    ch = np.full(s.shape, C_WALL)
    d = np.zeros(s.shape, int)
    d[si % 32 == 0] = -1
    d[(ti % 56 == 55)] = -1
    hole = (((si % 16) == 8) | ((si % 16) == 9)) & (((ti % 28) == 14) | ((ti % 28) == 15))
    d[hole] = -2
    sp = _hash(si, ti, 5) % 23 == 0
    d[sp & (d == 0)] = -1
    return ch, d


def pat_brick(s, t):
    si = np.floor(s).astype(int); ti = np.floor(t).astype(int)
    row = ti // 7
    off = (row % 2) * 8
    bx = (si + off) // 16
    mortar = ((ti % 7) == 6) | (((si + off) % 16) == 15)
    ch = np.where(mortar, C_WALL2, C_WALL)
    d = np.zeros(s.shape, int)
    g = _hash(bx, row, 7) % 10
    d[(~mortar) & (g < 2)] = -1
    d[(~mortar) & (g == 9)] = 1
    d[(~mortar) & ((ti % 7) == 0) & (g >= 2)] = 1
    return ch, d


def pat_acoustic(s, t):
    si = np.floor(s).astype(int); ti = np.floor(t).astype(int)
    ch = np.full(s.shape, C_WALL2)
    d = np.zeros(s.shape, int)
    seam = ((si % 16) == 0) | ((ti % 16) == 15)
    d[seam] = -2
    perf = ((si % 3) == 2) & ((ti % 3) == 1) & ~seam & ((si % 16) >= 2) & ((si % 16) <= 14) & ((ti % 16) >= 1) & ((ti % 16) <= 13)
    d[perf] = -1
    hl = ((ti % 16) == 0) & ~seam
    d[hl] = 1
    base = ti < 8
    ch = np.where(base, C_WALL, ch)
    d[base] = 0
    return ch, d


PATTERNS = {
    'paint':       (pat_paint,       'Peinture unie',            {'wall': 'cream'}),
    'two_tone':    (pat_two_tone,    'Deux tons + cimaise',       {'wall': 'cream', 'wall2': 'teal'}),
    'wallpaper':   (pat_wallpaper,   'Papier peint discret',      {'wall': 'sage'}),
    'stripes':     (pat_stripes,     'Bandes verticales',         {'wall': 'cream', 'wall2': 'sand'}),
    'wood_panels': (pat_wood_panels, 'Lambris bois',              {'wood': 'oak'}),
    'concrete':    (pat_concrete,    'Beton banche',              {'wall': 'concrete'}),
    'brick':       (pat_brick,       'Brique interieure',         {'wall': 'brick', 'wall2': 'cream'}),
    'acoustic':    (pat_acoustic,    'Panneaux acoustiques',      {'wall': 'cream', 'wall2': 'navy'}),
}


# ---------------------------------------------------------------------------
# Sols : fn(u, v) -> (canal, delta), u et v en su (monde), periode en cases
# ---------------------------------------------------------------------------
C_FLOOR, C_FLOOR2 = CHANNELS['floor'], CHANNELS['floor2']


def fl_planks(u, v):
    ui = np.floor(u).astype(int); vi = np.floor(v).astype(int)
    row = vi // 8
    off = (row % 4) * 16
    d = np.zeros(u.shape, int)
    d[(vi % 8) == 7] = -1
    d[((ui + off) % 64) == 0] = -1
    g = _hash((ui + off) // 64, row, 11)
    gr = _hash(ui // 6, row, 13) % 9 == 0
    d[gr & ((vi % 8) == 3) & (d == 0)] = -1
    d[((g % 5) == 0) & ((vi % 8) == 1) & (d == 0)] = 1
    return np.full(u.shape, C_FLOOR), d


def fl_parquet(u, v):
    ui = np.floor(u).astype(int); vi = np.floor(v).astype(int)
    cell = ((ui // 16) + (vi // 16)) % 2
    lu = ui % 16; lv = vi % 16
    d = np.zeros(u.shape, int)
    strip = np.where(cell == 0, lu % 4, lv % 4)
    d[strip == 3] = -1
    d[(lu == 15) | (lv == 15)] = -1
    d[(strip == 0) & (d == 0) & ((np.where(cell == 0, lv, lu) % 8) == 2)] = 1
    return np.full(u.shape, C_FLOOR), d


def fl_carpet(u, v):
    ui = np.floor(u).astype(int) % 32; vi = np.floor(v).astype(int) % 32
    h = _hash(ui, vi, 17) % 100
    d = np.zeros(u.shape, int)
    d[h < 10] = -1
    d[h > 95] = 1
    return np.full(u.shape, C_FLOOR), d


def fl_carpet_tiles(u, v):
    ch, d = fl_carpet(u, v)
    ui = np.floor(u).astype(int); vi = np.floor(v).astype(int)
    d[(ui % 32 == 0) | (vi % 32 == 0)] = -1
    # dalles alternees : sens de pose suggere par un mouchetis oriente, discret
    alt = ((ui // 32 + vi // 32) % 2) == 1
    h = _hash(ui % 32, vi % 32, 31) % 100
    d[alt & (h < 8) & (d == 0)] = -1
    d[(~alt) & (h > 96) & (d == 0)] = 1
    return ch, d


def fl_ceramic(u, v):
    ui = np.floor(u).astype(int); vi = np.floor(v).astype(int)
    grout = ((ui % 16) == 15) | ((vi % 16) == 15)
    ch = np.where(grout, C_FLOOR2, C_FLOOR)
    d = np.zeros(u.shape, int)
    d[(~grout) & (((ui % 16) == 0) | ((vi % 16) == 0))] = 1
    return ch, d


def fl_concrete(u, v):
    ui = np.floor(u).astype(int); vi = np.floor(v).astype(int)
    d = np.zeros(u.shape, int)
    d[(ui % 64 == 0) | (vi % 64 == 0)] = -1
    h = _hash(ui % 64, vi % 64, 19) % 100
    d[(h < 4) & (d == 0)] = -1
    d[(h > 97) & (d == 0)] = 1
    cloud = _hash(ui // 12, vi // 12, 23) % 5 == 0
    d[cloud & (h < 30) & (d == 0)] = -1
    return np.full(u.shape, C_FLOOR), d


def fl_vinyl(u, v):
    ui = np.floor(u).astype(int) % 32; vi = np.floor(v).astype(int) % 32
    h = _hash(ui, vi, 29) % 100
    ch = np.where(h < 7, C_FLOOR2, C_FLOOR)
    d = np.zeros(u.shape, int)
    d[(h >= 7) & (h < 10)] = -1
    return ch, d


def fl_raised(u, v):
    ui = np.floor(u).astype(int); vi = np.floor(v).astype(int)
    lu = ui % 16; lv = vi % 16
    d = np.zeros(u.shape, int)
    d[(lu == 15) | (lv == 15)] = -2
    d[((lu == 0) | (lv == 0)) & (d == 0)] = 1
    ch = np.full(u.shape, C_FLOOR)
    return ch, d


def fl_raised_vent(u, v):
    ch, d = fl_raised(u, v)
    ui = np.floor(u).astype(int); vi = np.floor(v).astype(int)
    lu = ui % 16; lv = vi % 16
    perf = (lu >= 3) & (lu <= 12) & (lv >= 3) & (lv <= 12) & ((lu % 2) == 1) & ((lv % 2) == 1)
    ch = np.where(perf, C_FLOOR2, ch)
    d[perf] = -1
    return ch, d


FLOORS = {
    # id: (fonction, nom, periode (pu, pv), rampes par defaut)
    'floor_wood_planks':   (fl_planks, 'Plancher bois (lames)', (2, 1), {'floor': 'oak'}),
    'floor_parquet':       (fl_parquet, 'Parquet a batons (vannerie)', (1, 1), {'floor': 'walnut'}),
    'floor_carpet':        (fl_carpet, 'Moquette', (1, 1), {'floor': 'navy'}),
    'floor_carpet_tiles':  (fl_carpet_tiles, 'Dalles de moquette', (2, 2), {'floor': 'steel'}),
    'floor_tile_ceramic':  (fl_ceramic, 'Carrelage', (1, 1), {'floor': 'plastic', 'floor2': 'steel'}),
    'floor_concrete':      (fl_concrete, 'Beton cire', (2, 2), {'floor': 'concrete'}),
    'floor_vinyl':         (fl_vinyl, 'Vinyle mouchete', (1, 1), {'floor': 'sage', 'floor2': 'cream'}),
    'floor_raised':        (fl_raised, 'Faux plancher technique', (1, 1), {'floor': 'steel'}),
    'floor_raised_vent':   (fl_raised_vent, 'Faux plancher perfore (soufflage)', (1, 1), {'floor': 'steel', 'floor2': 'graphite'}),
}


def render_floor_tile(fn, iu, iv, defaults, base_idx=3):
    """Rend la case (iu, iv) d'un motif de sol : losange 64x32, ancre (32, 0)."""
    w, h = 64, 32
    xs = np.arange(w) + 0.5 - 32
    ys = np.arange(h) + 0.5
    SX, SY = np.meshgrid(xs, ys)
    u = SX / 2 + SY; v = SY - SX / 2
    inside = (u >= 0) & (u < 32) & (v >= 0) & (v < 32)
    U = u + iu * 32; V = v + iv * 32
    ch, d = fn(U, V)
    idx = np.clip(base_idx + d, 0, 5)
    rgba = np.zeros((h, w, 4), np.uint8)
    mask = np.zeros((h, w, 4), np.uint8)
    names = {CHANNELS[k]: k for k in CHANNELS}
    for c in np.unique(ch[inside]):
        cname = names[int(c)]
        ramp = defaults.get(cname, defaults.get('floor'))
        pal = np.array(RAMPS[ramp], np.uint8)
        m = inside & (ch == c)
        rgba[m] = pal[idx[m]]
        mask[m, 0] = c
        mask[m, 1] = idx[m]
        mask[m, 3] = 255
    return rgba, mask


# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
PIECES_1T = ['plain', 'window', 'door', 'opening']
NAMES = {'plain': 'Mur plein', 'window': 'Mur avec fenetre', 'door': 'Mur avec porte',
         'opening': 'Mur avec ouverture (passage)', 'window_wide': 'Mur avec grande fenetre (2 cases)'}


def _save_layer(root, rel, arr):
    save_png(os.path.join(root, rel), arr)


@custom_builder('architecture', order=10)
def build_architecture(root, log=None):
    entries = []
    fam = 'architecture'
    scenes = {}
    owner = owner_fn_factory()
    # rendu des scenes 1 case
    raw = {}
    for kind in PIECES_1T:
        lp = {} if kind == 'plain' else {1: kind}
        m = make_scene(lp, dict(lp))
        res, own = render_owned(m, owner)
        raw[kind] = (res, own)
    # rect commun par orientation
    rects = {'se': [], 'sw': []}
    for kind, (res, own) in raw.items():
        rects['se'].append(bbox_rel(own, [11], anchor_px(res, 0, 32)))
        rects['sw'].append(bbox_rel(own, [31], anchor_px(res, 32, 0)))
    common = {k: union_rect(v) for k, v in rects.items()}
    face_info = {}
    for kind, (res, own) in raw.items():
        for orient, code, (au, av) in (('se', 11, (0, 32)), ('sw', 31, (32, 0))):
            rgba, mask, anc, rc = extract(res, own, [code], anchor_px(res, au, av), common[orient])
            aid = f"wall_{kind}"
            png = f"assets/{fam}/{aid}__{orient}.png"; msk = f"masks/{fam}/{aid}__{orient}__mask.png"
            _save_layer(root, png, rgba); _save_layer(root, msk, mask)
            e = _find_or_new(entries, aid, fam, 'wall', NAMES[kind], 'wall_piece')
            e['orientations'][orient] = {
                'size': [rgba.shape[1], rgba.shape[0]], 'anchor': list(anc),
                'layers': {'base': {'file': png, 'mask': msk, 'mask_channels': _chans(mask)}},
                'wall_side': 'left' if orient == 'se' else 'right',
                'span_tiles': 1,
                'placement': 'ancre = coin interieur bas de la case (U=0,V=j) pour se, (U=i,V=0) pour sw',
            }
            e['channels'] = {'wall': 'cream', 'trim': 'plastic', 'glass': 'glass', 'wood': 'birch', 'metal': 'brass'}
            e['dims'] = {'height_px': H, 'thickness_su': T, 'baseboard_px': BB_H}
            if kind == 'window':
                e['opening'] = {'s_su': [WIN['s0'], WIN['s1']], 'z_px': [WIN['z0'], WIN['z1']], 'walkable': False}
            if kind in ('door', 'opening'):
                e['opening'] = {'s_su': [DOORW['s0'], DOORW['s1']], 'z_px': [0, DOORW['z1']], 'walkable': True,
                                'passage_tile_rule': 'la case de piece adjacente au segment est un acces'}
            if kind == 'plain':
                face_info[orient] = (res, own, code, (au, av), common[orient])
    # angle interieur + terminaisons (depuis la scene pleine)
    res, own = raw['plain']
    rgba, mask, anc, _ = extract(res, own, [1], anchor_px(res, 0, 0))
    png = f"assets/{fam}/wall_corner_inner__all.png"; msk = f"masks/{fam}/wall_corner_inner__all__mask.png"
    _save_layer(root, png, rgba); _save_layer(root, msk, mask)
    e = _find_or_new(entries, 'wall_corner_inner', fam, 'wall', 'Angle interieur (coin du fond)', 'wall_piece')
    e['orientations']['all'] = {'size': [rgba.shape[1], rgba.shape[0]], 'anchor': list(anc),
                                'layers': {'base': {'file': png, 'mask': msk, 'mask_channels': _chans(mask)}},
                                'placement': 'ancre = coin (U=0,V=0) de la piece'}
    for orient, code, (au, av) in (('se', 9, (0, N * TILE)), ('sw', 29, (N * TILE, 0))):
        rgba, mask, anc, _ = extract(res, own, [code], anchor_px(res, au, av))
        png = f"assets/{fam}/wall_end__{orient}.png"; msk = f"masks/{fam}/wall_end__{orient}__mask.png"
        _save_layer(root, png, rgba); _save_layer(root, msk, mask)
        e = _find_or_new(entries, 'wall_end', fam, 'wall', 'Terminaison de mur (chant visible)', 'wall_piece')
        e['orientations'][orient] = {'size': [rgba.shape[1], rgba.shape[0]], 'anchor': list(anc),
                                     'layers': {'base': {'file': png, 'mask': msk, 'mask_channels': _chans(mask)}},
                                     'placement': ('ancre = (U=0, V=Vmax) : extremite avant du mur gauche' if orient == 'se'
                                                   else 'ancre = (U=Umax, V=0) : extremite avant du mur droit')}
    # grande fenetre (2 cases)
    m = make_scene({1: 'window_wide'}, {1: 'window_wide'})
    res, own = render_owned(m, owner)
    for orient, codes, (au, av) in (('se', [11, 12], (0, 32)), ('sw', [31, 32], (32, 0))):
        base_rect = common[orient]
        rc = bbox_rel(own, codes, anchor_px(res, au, av))
        rc = union_rect([rc, base_rect])
        rgba, mask, anc, _ = extract(res, own, codes, anchor_px(res, au, av), rc)
        png = f"assets/{fam}/wall_window_wide__{orient}.png"; msk = f"masks/{fam}/wall_window_wide__{orient}__mask.png"
        _save_layer(root, png, rgba); _save_layer(root, msk, mask)
        e = _find_or_new(entries, 'wall_window_wide', fam, 'wall', NAMES['window_wide'], 'wall_piece')
        e['orientations'][orient] = {'size': [rgba.shape[1], rgba.shape[0]], 'anchor': list(anc),
                                     'layers': {'base': {'file': png, 'mask': msk, 'mask_channels': _chans(mask)}},
                                     'span_tiles': 2,
                                     'placement': 'ancre = coin interieur bas de la PREMIERE case couverte'}
        e['channels'] = {'wall': 'cream', 'trim': 'plastic', 'glass': 'glass'}
        e['opening'] = {'s_su': [5, 59], 'z_px': [WIN['z0'], WIN['z1']], 'walkable': False}
    # angle exterieur (scene en L inversee) : voir _outer_corner
    entries.append(_outer_corner(root, fam))
    # calques de motifs muraux
    entries += _build_patterns(root, face_info)
    # sols
    entries += _build_floors(root)
    entries += _build_edges(root)
    return entries


def _find_or_new(entries, aid, fam, sub, name, kind):
    for e in entries:
        if e['id'] == aid:
            return e
    e = {'id': aid, 'category': fam, 'subcategory': sub, 'name_fr': name, 'pack_version': PACK_VERSION,
         'usage': 'shared', 'kind': kind, 'render_layer': 'walls', 'orientations': {}}
    entries.append(e)
    return e


def _chans(mask):
    from srgen.palette import CHANNEL_NAMES
    c = mask[..., 0][mask[..., 3] > 0]
    return [CHANNEL_NAMES[int(x)] for x in sorted(set(np.unique(c).tolist()))] if c.size else []


def _outer_corner(root, fam):
    """Angle saillant : mur 'se' (u in [88,96]) finissant en v=128 et mur 'sw' (v in [120,128])."""
    m = Model(0, 0, outline='full', shadow=False, edge_hl=True)
    U3, V4 = 96, 128
    # pan gauche (face +U a u=96), pan droit (face +V a v=128)
    for (za, zb) in ((0, H),):
        m.add(Box(U3 - T, -64, za, U3, V4, zb, {'top': CAP, 'default': WALL}, name='L', flags=MASK_FLAG_PATTERN, flag_faces=['right']))
        m.add(Box(-64, V4 - T, za, U3, V4, zb, {'top': CAP, 'default': WALL}, name='R', flags=MASK_FLAG_PATTERN, flag_faces=['back']))
    m.add(Box(U3, -64, 0, U3 + BB_D, V4 + BB_D, BB_H, TRIM, name='Lb'))
    m.add(Box(-64, V4, 0, U3 + BB_D, V4 + BB_D, BB_H, TRIM, name='Rb'))

    def fn(name, u, v, z):
        out = np.zeros(u.shape, np.int32)
        corner = (u >= U3 - T - 1e-6) & (v >= V4 - T - 1e-6)
        out[:] = 2
        out[corner] = 1
        return out
    res, own = render_owned(m, fn)
    rgba, mask, anc, _ = extract(res, own, [1], anchor_px(res, U3, V4))
    png = f"assets/{fam}/wall_corner_outer__all.png"; msk = f"masks/{fam}/wall_corner_outer__all__mask.png"
    _save_layer(root, png, rgba); _save_layer(root, msk, mask)
    return {'id': 'wall_corner_outer', 'category': fam, 'subcategory': 'wall', 'name_fr': 'Angle saillant (retour)',
            'pack_version': PACK_VERSION, 'usage': 'shared', 'kind': 'wall_piece', 'render_layer': 'walls',
            'orientations': {'all': {'size': [rgba.shape[1], rgba.shape[0]], 'anchor': list(anc),
                                     'layers': {'base': {'file': png, 'mask': msk, 'mask_channels': _chans(mask)}},
                                     'placement': ("ancre = point (U=a, V=b) ou un mur gauche (face +U a U=a) rencontre "
                                                   "un mur droit (face +V a V=b) en angle saillant ; dessiner APRES les "
                                                   "deux segments adjacents (il recouvre leur extremite).")}},
            'channels': {'wall': 'cream', 'trim': 'plastic'}}


def _build_patterns(root, face_info):
    entries = []
    fam = 'wall_patterns'
    for pname, (fn, label, defaults) in PATTERNS.items():
        aid = f"pattern_{pname}"
        e = {'id': aid, 'category': fam, 'subcategory': 'wall_pattern', 'name_fr': label,
             'pack_version': PACK_VERSION, 'usage': 'shared', 'kind': 'wall_pattern_overlay',
             'defaults': defaults, 'orientations': {},
             'encoding': 'R = canal (11 wall, 14 wall2, 3 wood), G = delta d\'indice + 8, A = 255 sur la face murale',
             'period_su': 32}
        for orient, (res, own, code, (au, av), rc) in face_info.items():
            lay = res.layers['base']
            msel = (own == code) & ((lay.mask[..., 2] & MASK_FLAG_PATTERN) > 0) & (lay.mask[..., 3] > 0)
            t = res.depth['base']
            u = res.a + t; v = res.b + t
            s = (v - 32) if orient == 'se' else (u - 32)
            with np.errstate(invalid='ignore'):   # NaN hors surface (pixels sans impact) : sans effet
                ch, d = fn(np.nan_to_num(s), np.nan_to_num(t))
            ov = np.zeros(lay.rgba.shape, np.uint8)
            ov[msel, 0] = np.asarray(ch)[msel]
            ov[msel, 1] = np.clip(np.asarray(d)[msel] + 8, 0, 255)
            ov[msel, 3] = 255
            ax, ay = anchor_px(res, au, av)
            dx0, dy0, dx1, dy1 = rc
            out = ov[ay + dy0:ay + dy1, ax + dx0:ax + dx1]
            png = f"assets/{fam}/{aid}__{orient}.png"
            save_png(os.path.join(root, png), out)
            # apercu colore (non destine a la production)
            e['orientations'][orient] = {'size': [out.shape[1], out.shape[0]], 'anchor': [-dx0, -dy0],
                                         'layers': {'overlay': {'file': png}}}
        # texture source a plat (32 x 112) en couleurs par defaut, documentation
        ss, tt = np.meshgrid(np.arange(32) + 0.5, np.arange(H)[::-1] + 0.5)
        ch, d = fn(ss, tt)
        flat = np.zeros((H, 32, 4), np.uint8)
        names = {CHANNELS[k]: k for k in CHANNELS}
        for c in np.unique(ch):
            cname = names[int(c)]
            ramp = defaults.get(cname, 'cream' if cname != 'wood' else 'oak')
            pal = np.array(RAMPS[ramp], np.uint8)
            mm = ch == c
            flat[mm] = pal[np.clip(3 + np.asarray(d)[mm], 0, 5)]
        save_png(os.path.join(root, f"sources/wall_patterns/{aid}_flat.png"), flat)
        e['source_flat'] = f"sources/wall_patterns/{aid}_flat.png"
        entries.append(e)
    return entries


def _build_floors(root):
    entries = []
    fam = 'floors'
    for fid, (fn, label, (pu, pv), defaults) in FLOORS.items():
        e = {'id': fid, 'category': fam, 'subcategory': 'floor', 'name_fr': label, 'pack_version': PACK_VERSION,
             'usage': 'shared', 'kind': 'floor_tile', 'render_layer': 'floor', 'period_tiles': [pu, pv],
             'variant_rule': 'variante = (U mod pu, V mod pv)', 'channels': defaults, 'variants': {}}
        for iu in range(pu):
            for iv in range(pv):
                rgba, mask = render_floor_tile(fn, iu, iv, defaults)
                key = f"u{iu}v{iv}"
                png = f"assets/{fam}/{fid}__{key}.png"; msk = f"masks/{fam}/{fid}__{key}__mask.png"
                save_png(os.path.join(root, png), rgba); save_png(os.path.join(root, msk), mask)
                e['variants'][key] = {'size': [64, 32], 'anchor': [32, 0],
                                      'layers': {'base': {'file': png, 'mask': msk, 'mask_channels': _chans(mask)}}}
        entries.append(e)
    return entries


def _build_edges(root):
    """Bords avant de dalle, seuils de changement de sol et marquages au sol."""
    entries = []
    fam = 'floors'
    SLAB = M('fixed', 'concrete')
    m = Model(0, 0, outline='full', shadow=False, edge_hl=True)
    m.add(Box(0, 0, -10, 96, 96, 0, {'top': M('fixed', 'concrete'), 'default': SLAB}, name='slab'))
    p = m.prims[-1]
    for face in ('right', 'back'):
        p.decal(face, rect(0, 8, 999, 10), Use(M('fixed', 'concrete'), idx=4))
        p.decal(face, rect(0, 0, 999, 2), Use(M('fixed', 'concrete'), idx=1))

    def fn(name, u, v, z):
        out = np.zeros(u.shape, np.int32)
        onU = u >= 96 - 1e-6
        onV = v >= 96 - 1e-6
        out[onU] = 10 + np.clip(np.floor(v[onU] / 32), 0, 2).astype(np.int32)
        out[onV & ~onU] = 20 + np.clip(np.floor(u[onV & ~onU] / 32), 0, 2).astype(np.int32)
        return out
    res, own = render_owned(m, fn)
    e = {'id': 'floor_edge', 'category': fam, 'subcategory': 'floor_edge', 'name_fr': 'Bord avant de dalle (coupe)',
         'pack_version': PACK_VERSION, 'usage': 'shared', 'kind': 'floor_edge', 'render_layer': 'floor_edge',
         'orientations': {}, 'channels': {}}
    for orient, code, (au, av), note in (('se', 11, (96, 32), 'le long du bord U=Umax ; ancre = (Umax, V)'),
                                         ('sw', 21, (32, 96), 'le long du bord V=Vmax ; ancre = (U, Vmax)')):
        rgba, mask, anc, _ = extract(res, own, [code], anchor_px(res, au, av))
        png = f"assets/{fam}/floor_edge__{orient}.png"
        save_png(os.path.join(root, png), rgba)
        e['orientations'][orient] = {'size': [rgba.shape[1], rgba.shape[0]], 'anchor': list(anc),
                                     'layers': {'base': {'file': png}}, 'placement': note}
    # coin avant : les deux faces se rejoignent ; on exporte les pixels du dernier module de chaque bord
    rgba, mask, anc, _ = extract(res, own, [12, 22], anchor_px(res, 96, 96))
    png = f"assets/{fam}/floor_edge_corner__all.png"
    save_png(os.path.join(root, png), rgba)
    entries.append(e)
    entries.append({'id': 'floor_edge_corner', 'category': fam, 'subcategory': 'floor_edge',
                    'name_fr': 'Coin avant de dalle', 'pack_version': PACK_VERSION, 'usage': 'shared',
                    'kind': 'floor_edge', 'render_layer': 'floor_edge',
                    'orientations': {'all': {'size': [rgba.shape[1], rgba.shape[0]], 'anchor': list(anc),
                                             'layers': {'base': {'file': png}},
                                             'placement': "ancre = coin avant (Umax, Vmax) ; remplace les deux derniers modules de bord"}}})
    # seuils (barres de transition) et marquages
    entries += _build_overlays_floor(root)
    return entries


def _build_overlays_floor(root):
    from srgen.iso import render
    fam = 'floors'
    out = []
    specs = [
        ('floor_threshold', 'Seuil de transition (barre)', 'threshold',
         lambda m, along: m.add(Box(0, -1, 0, 32, 1, 1, M('metal', 'brass'), name='t') if along == 'u'
                                else Box(-1, 0, 0, 1, 32, 1, M('metal', 'brass'), name='t'))),
    ]
    e = {'id': 'floor_threshold', 'category': fam, 'subcategory': 'floor_overlay', 'name_fr': 'Seuil de transition (barre laiton)',
         'pack_version': PACK_VERSION, 'usage': 'shared', 'kind': 'floor_overlay', 'render_layer': 'floor_overlay',
         'orientations': {}, 'channels': {'metal': 'brass'}}
    for along, orient in (('u', 'along_u'), ('v', 'along_v')):
        m = Model(0, 0, outline='none', shadow=False, edge_hl=True)
        if along == 'u':
            m.add(Box(0, -1, 0, 32, 1, 1, M('metal', 'brass')))
        else:
            m.add(Box(-1, 0, 0, 1, 32, 1, M('metal', 'brass')))
        res = render(m, 'world', pad=1)
        lay = res.layers['base']
        png = f"assets/{fam}/floor_threshold__{orient}.png"; msk = f"masks/{fam}/floor_threshold__{orient}__mask.png"
        save_png(os.path.join(root, png), lay.rgba); save_png(os.path.join(root, msk), lay.mask)
        e['orientations'][orient] = {'size': list(res.size), 'anchor': list(res.anchor),
                                     'layers': {'base': {'file': png, 'mask': msk, 'mask_channels': ['metal']}},
                                     'placement': ("sur l'arete V=j entre deux cases (le long de U) ; ancre = (U, V=j)" if along == 'u'
                                                   else "sur l'arete U=i entre deux cases (le long de V) ; ancre = (U=i, V)")}
    out.append(e)
    # marquages au sol (industrie) : bande jaune le long d'une arete, zone hachuree
    mk = {'id': 'floor_marking', 'category': fam, 'subcategory': 'floor_overlay', 'name_fr': 'Marquage au sol (atelier)',
          'pack_version': PACK_VERSION, 'usage': 'shared', 'kind': 'floor_overlay', 'render_layer': 'floor_overlay',
          'orientations': {}, 'channels': {'accent': 'hazard'}}
    HZ = M('accent', 'hazard')

    def tile_overlay(fn):
        w, h = 64, 32
        xs = np.arange(w) + 0.5 - 32; ys = np.arange(h) + 0.5
        SX, SY = np.meshgrid(xs, ys)
        u = SX / 2 + SY; v = SY - SX / 2
        inside = (u >= 0) & (u < 32) & (v >= 0) & (v < 32)
        sel, idx = fn(u, v)
        sel &= inside
        rgba = np.zeros((h, w, 4), np.uint8); mask = np.zeros((h, w, 4), np.uint8)
        pal = np.array(RAMPS['hazard'], np.uint8)
        rgba[sel] = pal[idx[sel]]
        dark = sel & (idx < 0)
        mask[sel & ~dark, 0] = CHANNELS['accent']; mask[sel & ~dark, 1] = idx[sel & ~dark]; mask[sel & ~dark, 3] = 255
        rgba[dark] = RAMPS['graphite'][2]
        return rgba, mask
    variants = {
        'line_u': ('bande le long de l\'arete V=0 de la case', lambda u, v: ((v >= 1) & (v < 4), np.full(u.shape, 3))),
        'line_v': ('bande le long de l\'arete U=0 de la case', lambda u, v: ((u >= 1) & (u < 4), np.full(u.shape, 3))),
        'hatch': ('zone hachuree (case entiere)', lambda u, v: ((u >= 1) & (u < 31) & (v >= 1) & (v < 31),
                                                                 np.where(((np.floor(u + v)).astype(int) % 8) < 4, 3, -1))),
        'walkway_dot': ('repere de cheminement', lambda u, v: (((u - 16) ** 2 + (v - 16) ** 2) < 12, np.full(u.shape, 4))),
    }
    for key, (note, fn) in variants.items():
        rgba, mask = tile_overlay(fn)
        png = f"assets/{fam}/floor_marking__{key}.png"; msk = f"masks/{fam}/floor_marking__{key}__mask.png"
        save_png(os.path.join(root, png), rgba); save_png(os.path.join(root, msk), mask)
        mk['orientations'][key] = {'size': [64, 32], 'anchor': [32, 0],
                                   'layers': {'base': {'file': png, 'mask': msk, 'mask_channels': ['accent']}},
                                   'placement': note}
    out.append(mk)
    return out
