"""Decoration et personnalite (famille decoration).

Objets muraux : emprise (1,1), orientation se = mur gauche, sw = mur droit ;
la geometrie est collee au mur (y local in [32 - epaisseur, 32]), ancre = coin
de la case de mur (comme les segments de mur). Pas d'ombre au sol, pas de
collision. Placer : ancre de l'objet = coin (U=0, V=j) [gauche] ou (U=i, V=0) [droit].
Objets de bureau : centres (origine = point de pose sur le plateau), provides
['desk_item'] ; orientation = celle du bureau.
Toutes les illustrations sont originales : projet fictif, schema generique,
paysage abstrait ; references cachees discretes (canard de debogage, croquis
d'idee, invite '>_' generique, mini-projet).
"""
import math
import numpy as np

from srgen.iso import Fix, Use, Delta, rect, hline, vline, frame, disc, TILE
from srgen.palette import M
from srgen.registry import iso_asset, Socket, Access, ALL4, FRONT2, ONE
from srgen.shapes import rbox, chamfer_box
from srgen.lot_ind_helpers import stamp, speckle, hsh, label

GRAPH = M('fixed', 'graphite')   # petites pieces sombres non recolorables
STEEL = M('metal', 'steel')
INK = Fix('ink')
PLANT = M('plant', 'leaf')
SAGE_P = M('plant', 'sage')
SOIL = M('fixed', 'walnut')
WALLKW = dict(footprint=(1, 1), facings=FRONT2, kind='wall', wall_mount=True, collision=[],
              model_kw={'shadow': False}, layer='wall_items')
DESKKW = dict(centered=True, provides=['desk_item'], model_kw={'shadow_k': 0.3})


def U_(ramp, rel=0, ch='fixed'):
    return Use(M(ch, ramp), rel=rel)


def A_(ramp, idx, ch='fixed'):
    return Use(M(ch, ramp), idx=idx)


def _pad(rows, w):
    out = []
    for r in rows:
        assert len(r) <= w, (r, len(r), w)
        out.append(r.ljust(w, '.'))
    return out


# ---------------------------------------------------------------------------
# Plantes
# ---------------------------------------------------------------------------
def leafy(p, seed):
    """Texture de feuillage : petites taches claires/sombres (cellules 2x2)."""
    p.decal('surface', ('fn', lambda s, t: hsh(np.floor(s / 1.6), np.floor(t / 1.6), seed) % 5 == 0), Delta(+1))
    p.decal('surface', ('fn', lambda s, t: hsh(np.floor(s / 1.6) + 7, np.floor(t / 1.6), seed + 3) % 6 == 0), Delta(-1))


@iso_asset('plant_floor_medium', 'decoration', 'plant', 'Plante en pot (moyenne)', footprint=(1, 1), facings=ONE,
           symmetric=True, height=40, notes='Buisson en pot terre cuite ; feuillage recolorable (canal plant).')
def _(m):
    pot = m.cyl(16, 16, 6, 0, 12, M('primary', 'brick'))
    pot.decal('side', rect(0, 9, 360, 10), Delta(-1))
    m.cyl(16, 16, 6.8, 10.5, 13.5, M('primary', 'brick'))
    m.cyl(16, 16, 6.0, 13.0, 13.6, SOIL)
    for (cx, cy, cz, rx, ry, rz, sd) in ((16, 16, 24, 9, 9, 8, 1), (10, 18, 21, 5, 5, 5, 2), (22, 13, 22, 5.5, 5.5, 5, 3),
                                         (15, 15, 31, 6.5, 6.5, 5.5, 4), (17, 10, 19, 5, 4, 4, 5), (21, 20, 27, 4.5, 4.5, 4.5, 6)):
        e = m.ell(cx, cy, cz, rx, ry, rz, PLANT)
        leafy(e, sd)
    for (cx, cy, cz) in ((7, 15, 25), (25, 18, 26), (12, 9, 25), (19, 22, 33), (14, 18, 36), (24, 11, 18)):
        m.ell(cx, cy, cz, 1.8, 1.8, 1.5, PLANT)


@iso_asset('plant_floor_tall', 'decoration', 'plant', 'Grande plante (ficus lyre)', footprint=(1, 1), facings=ONE,
           symmetric=True, height=76, occludes=50, notes='Plante haute a grandes feuilles, cache-pot moderne.')
def _(m):
    pot = chamfer_box(m, 10, 10, 0, 22, 22, 17, 2, M('primary', 'plastic'))
    pot.decal('front', rect(0, 14, 12, 15), Delta(-1))
    m.box(10.5, 10.5, 16, 21.5, 21.5, 17.2, SOIL)
    m.cyl(16, 16, 1.1, 17, 50, M('wood', 'walnut'))
    m.cyl(15, 17, 0.8, 34, 44, M('wood', 'walnut'))
    m.cyl(18, 14, 0.8, 40, 52, M('wood', 'walnut'))
    for (cx, cy, cz, rx, ry, rz, sd) in ((12, 18, 40, 5, 4, 4.5, 11), (21, 13, 46, 5.5, 4.5, 5, 12), (12, 12, 53, 6, 5, 5, 13),
                                         (20, 20, 58, 5, 5, 5, 14), (16, 15, 67, 6.5, 6.5, 6, 15), (10, 16, 61, 4, 4, 4, 16),
                                         (23, 15, 64, 4, 4, 4, 17)):
        e = m.ell(cx, cy, cz, rx, ry, rz, PLANT)
        e.decal('surface', ('fn', lambda s, t: np.abs((s - t * 0.5) % 7 - 3.5) < 0.5), Delta(-1))
        leafy(e, sd)


@iso_asset('plant_succulent', 'decoration', 'plant', 'Succulente de bureau', size_su=(10, 10), facings=ONE, symmetric=True,
           height=9, **DESKKW)
def _(m):
    pot = m.cyl(0, 0, 3.2, 0, 4, M('primary', 'sky'))
    pot.decal('side', rect(0, 2.8, 360, 3.4), Delta(+1))
    m.cyl(0, 0, 2.8, 3.6, 4.0, SOIL)
    # rosette : 8 feuilles charnues pointues vers l'exterieur + coeur
    for k in range(8):
        a = math.radians(k * 45 + 22)
        lf = m.ell(math.cos(a) * 3.0, math.sin(a) * 3.0, 4.9, 1.6, 1.6, 1.2, SAGE_P)
        lf.decal('surface', rect(-99, -99, 99, 0.9), Delta(-1))
    for k in range(5):
        a = math.radians(k * 72)
        lf = m.ell(math.cos(a) * 1.5, math.sin(a) * 1.5, 6.1, 1.25, 1.25, 1.3, SAGE_P)
        lf.decal('surface', rect(-99, 1.2, 99, 99), Delta(+1))
    tip = m.ell(0, 0, 7.0, 1.0, 1.0, 1.1, SAGE_P)
    tip.decal('surface', rect(-99, -99, 99, 99), Delta(+1))


@iso_asset('plant_trailing', 'decoration', 'plant', 'Plante retombante (etagere)', size_su=(12, 12), facings=ONE,
           symmetric=True, height=8, centered=True, provides=['desk_item', 'shelf_item'], model_kw={'shadow_k': 0.3},
           notes="Les tiges retombent SOUS le point de pose (bord d'etagere) : le sprite depasse sous l'ancre.")
def _(m):
    pot = m.cyl(0, 0, 4, 0, 6, M('primary', 'plastic'))
    pot.decal('side', rect(0, 4.5, 360, 5.2), Delta(-1))
    m.cyl(0, 0, 3.6, 5.6, 6.0, SOIL)
    e = m.ell(0, 0, 7, 4.8, 4.8, 2.6, PLANT)
    leafy(e, 21)
    # tiges retombantes : feuilles serrees en zigzag (brin continu, pas de chapelet)
    strands = [((-4.4, -2.4), 5.5, -12.0, 1), ((3.6, -4.0), 5.0, -9.0, 2), ((0.4, -5.0), 4.5, -6.5, 3), ((5.2, 0.8), 5.0, -3.0, 4)]
    for (bx, by), z0, z1, sd in strands:
        z = z0; k = 0
        while z >= z1:
            dx = 0.8 if k % 2 == 0 else -0.8
            drift = (z0 - z) * 0.06
            lf = m.ell(bx + dx + (drift if bx < 0 else -drift), by - (z0 - z) * 0.05, z, 1.25, 1.1, 1.1, PLANT)
            if (k + sd) % 3 == 0:
                lf.decal('surface', rect(-99, -99, 99, 99), Delta(+1))
            z -= 1.5; k += 1
        m.ell(bx, by - (z0 - z1) * 0.05, z1 - 1.0, 0.9, 0.9, 0.9, M('plant', 'leaf'))
    m.bounds_hint(-8, -8, -14, 8, 8, 10)


# ---------------------------------------------------------------------------
# Objets muraux (affiches, horloge, plaques, notes)
# ---------------------------------------------------------------------------
POSTER_ROCKET = [
    "...................",
    ".s..........s......",
    ".........k.......s.",
    "........kck........",
    ".......kccck.......",
    ".......krrrk.......",
    "......krrrrrk......",
    "......krwwwrk......",
    "......krwwwrk......",
    "......krrrrrk......",
    "......krrrrrk......",
    ".....kcrrrrrck.....",
    "....kcckrrrkcck....",
    "....kkk.kkk.kkk....",
    "........fof........",
    "........ofo........",
    ".........f.........",
    "..s......t.........",
    "...................",
    ".........t........s",
    "...................",
    ".........t.........",
    "....kkkkkkkkkkk....",
    "....kdddddddddk....",
    "....kdllldddddk....",
    "....kddyyyydddk....",
    "....kdddllldddk....",
    "....kkkkkkkkkkk....",
    "..kgggggggggggggk..",
    "...kkkkkkkkkkkkk...",
    "...................",
    "bbbbbbbbbbbbbbbbbbb",
    "bBbbBbbBbbBbbBbbBbb",
]


@iso_asset('poster_project', 'decoration', 'wall_art', 'Affiche de projet fictif (fusee)', height=82,
           notes="Affiche encadree : une fusee decolle d'un portable (projet fictif), sans texte.", **WALLKW)
def _(m):
    p = m.box(5, 30, 44, 27, 32, 80, M('wood', 'oak'))
    p.decal('front', rect(1.5, 1.5, 20.5, 34.5), U_('cream', +1))
    stamp(p, 'front', _pad(POSTER_ROCKET, 19),
          {'k': INK, 'c': U_('coral', +1), 'r': U_('plastic', +2), 'w': U_('sky', +1), 'f': U_('sun', +2),
           'o': U_('coral', +2), 's': U_('sun', +1), 't': U_('steel', 0), 'd': U_('navy', -1), 'l': U_('teal', +2),
           'y': U_('sun', +2), 'g': U_('steel', +1), 'b': U_('teal', 0), 'B': U_('teal', +2)},
          s0=1.5, t_top=34.5)


POSTER_SCHEMATIC = [
    "...................",
    ".wwwwwww...ww.ww...",
    "...................",
    "...wwwwwwww........",
    "...w......w...ccc..",
    "..ww.wwww.ww..c.c..",
    "...w.w..w.w...ccc..",
    "..ww.wwww.ww...w...",
    "...w......w....w...",
    "...wwwwwwww....w...",
    "......w........w...",
    "......w...wwwwww...",
    "......w...w........",
    "......wwwww........",
    "..........w........",
    "..ww....w.w.w.w....",
    "..ww...w.w.w.w.w...",
    "..ww..w........w...",
    "......w........w...",
    "......wwwwwwwwww...",
    "..........w........",
    "........wwwww......",
    ".........www.......",
    "..........w........",
    "...................",
    ".wwwwwwwwwwwwwwwww.",
    ".w...w...........w.",
    ".wwwwwwwwwwwwwwwww.",
]


@iso_asset('poster_schematic', 'decoration', 'wall_art', 'Schema technique (plan bleu)', height=78,
           notes='Plan bleu generique : puce, pistes, resistance, masse, cartouche ; aucune inscription.', **WALLKW)
def _(m):
    p = m.box(5, 30.5, 46, 27, 32, 78, M('metal', 'graphite'))
    p.decal('front', rect(1.5, 1.5, 20.5, 30.5), U_('navy', 0))
    p.decal('front', ('fn', lambda s, t: ((np.floor(s) % 4 == 1) | (np.floor(t) % 4 == 1)) & (s > 1.5) & (s < 20.5)
                      & (t > 1.5) & (t < 30.5)), U_('navy', +1))
    stamp(p, 'front', _pad(POSTER_SCHEMATIC, 19), {'w': U_('sky', +2), 'c': U_('coral', +2)}, s0=1.5, t_top=30.5)


PHOTO = [
    "yyyyyyyyyyyyyyy",
    "yyyyyyyyyyssyyy",
    "yyyyyyyyyssssyy",
    "yyyyyyyyyssssyy",
    "yyyyyyyyyyssyyy",
    "yyyyyyyyyyyyyyy",
    "zzzzzyyyyyyyyyy",
    "hhhzzzzyyyyyzzz",
    "hhhhhhzzzzzzzhh",
    "gghhhhhhhhhhhhh",
    "ggggghhhhhhgggg",
    "llllllgggggglll",
    "lLllllllllLllll",
]


@iso_asset('photo_abstract', 'decoration', 'wall_art', 'Photo abstraite encadree', height=70,
           notes='Paysage abstrait non personnel (soleil, collines, lac), cadre blanc et passe-partout.', **WALLKW)
def _(m):
    p = m.box(7, 30.5, 50, 25, 32, 71, M('primary', 'plastic'))
    p.decal('front', rect(1, 1, 17, 20), U_('cream', +1))
    stamp(p, 'front', PHOTO, {'y': U_('sky', +2), 's': U_('sun', +2), 'z': U_('sky', +1), 'h': U_('sage', +1),
                              'g': U_('leaf', 0), 'l': U_('sky', 0), 'L': U_('sky', +2)}, s0=1.5, t_top=17.5)


@iso_asset('clock_wall', 'decoration', 'wall_art', 'Horloge murale', height=78,
           notes='Cadran rond (aiguilles fixes, le jeu peut superposer des aiguilles), lunette recolorable.', **WALLKW)
def _(m):
    c = m.hcyl('y', 16, 70, 6.5, 29, 32, M('primary', 'coral'))
    # cadran : coordonnees de la face 'end' : s = x - 9.5, t = z - 63.5 (centre 6.5, 6.5)
    c.decal('end', disc(6.5, 6.5, 5.3), U_('cream', +2))
    for (ss, tt) in ((6, 11), (11, 6), (6, 1.5), (1.5, 6)):
        c.decal('end', rect(ss, tt, ss + 1, tt + 1), INK)
    c.decal('end', rect(6, 6, 7, 10.5), INK)
    c.decal('end', rect(6, 6, 9.5, 7), INK)
    c.decal('end', rect(6, 6, 7, 7), U_('coral', +1))


@iso_asset('award_plaque', 'decoration', 'wall_art', 'Plaque de reussite', height=78,
           notes='Plaque bois et laiton avec embleme etoile (petite decoration de reussite).', **WALLKW)
def _(m):
    p = m.box(9, 30, 56, 23, 32, 76, M('wood', 'walnut'))
    p.decal('front', frame(0, 0, 14, 20), Delta(+1))
    p.decal('front', rect(2, 3, 12, 17), A_('brass', 3, 'metal'))
    p.decal('front', frame(2, 3, 12, 17), A_('brass', 4, 'metal'))
    stamp(p, 'front', ['...k...', '..kyk..', 'kkkyykk', '.kyyyk.', '.kyk yk', 'kk...kk'],
          {'k': A_('brass', 1, 'metal'), 'y': A_('brass', 5, 'metal')}, s0=3.5, t_top=15)
    p.decal('front', rect(4, 5, 10, 6), A_('brass', 2, 'metal'))
    p.decal('front', rect(5, 7, 9, 8), A_('brass', 2, 'metal'))


@iso_asset('sticky_notes', 'decoration', 'wall_art', 'Notes adhesives (mur)', height=78,
           notes='Grappe de notes colorees avec griffonnages (texte suggere, illisible).', **WALLKW)
def _(m):
    notes = [(6, 64, 'sun'), (12, 66, 'coral'), (18, 63, 'sky'), (8, 56, 'leaf'), (15, 57, 'sun'), (21, 55, 'rose')]
    for k, (x, z, ramp) in enumerate(notes):
        n = m.box(x, 31.2 - k * 0.15, z, x + 5, 32, z + 5, M('fixed', ramp))
        n.decal('front', rect(1, 3, 4, 4), Delta(-2))
        n.decal('front', rect(1, 1.5, 3 + (k % 2), 2.5), Delta(-2))
        n.decal('front', rect(0, 4, 5, 5), Delta(+1))


@iso_asset('idea_sketch', 'decoration', 'wall_art', "Croquis d'idee punaise", height=76,
           notes="Feuille punaisee : ampoule dessinee + fleches (reference cachee discrete).", **WALLKW)
def _(m):
    p = m.box(9, 31, 52, 23, 32, 72, M('secondary', 'cream'))
    p.decal('front', rect(0, 0, 14, 20), Use(M('secondary', 'cream'), rel=+2))
    stamp(p, 'front', _pad([
        "..............",
        ".k....kk...k..",
        "..k..k..k.k...",
        ".....k..k.....",
        "kk..k....k..kk",
        "....k....k....",
        "....k.yy.k....",
        ".....k..k.....",
        "......kk......",
        "......kk......",
        "......kk......",
        ".......k......",
        "..............",
        ".kkkk...kkk...",
        "..............",
        ".kkkkkk..k....",
        ".........kk...",
        ".kkk.....k....",
        "..............",
    ], 14), {'k': U_('graphite', +1), 'y': U_('sun', +1)}, s0=0, t_top=19.5)
    m.ell(16, 30.5, 70, 0.9, 0.9, 0.9, M('fixed', 'red'))


# ---------------------------------------------------------------------------
# Objets de bureau
# ---------------------------------------------------------------------------
@iso_asset('figurine_robot', 'decoration', 'desk_decor', 'Petite figurine robot', size_su=(8, 8), facings=ALL4, height=14,
           notes='Robot jouet original (tete carree, antenne, ecran de poitrine).', **DESKKW)
def _(m):
    for x in (-2.5, 0.5):
        m.box(x, -1.5, 0, x + 2, 1.5, 1.2, GRAPH)
        m.box(x + 0.4, -0.8, 1.2, x + 1.6, 0.8, 2.2, GRAPH)
    b = m.box(-2.5, -1.8, 2.2, 2.5, 1.8, 7, M('primary', 'sky'))
    b.decal('front', rect(1, 1.2, 4, 3.6), A_('display', 1, 'screen'))
    b.decal('front', rect(1.5, 2.3, 2.5, 3.3), Fix('led_green'))
    for x in (-3.6, 2.5):
        m.box(x, -0.8, 3.2, x + 1.1, 0.8, 6.6, M('secondary', 'plastic'))
    h = m.box(-2.8, -2.2, 7, 2.8, 2.2, 11, M('secondary', 'plastic'))
    h.decal('front', rect(1.2, 1.4, 2.2, 2.6), Fix('led_blue'))
    h.decal('front', rect(3.4, 1.4, 4.4, 2.6), Fix('led_blue'))
    h.decal('front', rect(2.0, 0.5, 3.6, 1.5), INK)
    m.cyl(0, 0, 0.4, 11, 13, GRAPH)
    m.ell(0, 0, 13.6, 0.9, 0.9, 0.9, M('accent', 'coral'))


def _book(m, x0, y0, z0, x1, y1, z1, ramp, ch='accent'):
    b = m.box(x0, y0, z0, x1, y1, z1, M(ch, ramp))
    w = x1 - x0; d = y1 - y0; h = z1 - z0
    for f, ww in (('front', w), ('right', d), ('back', w)):
        b.decal(f, rect(0.5, 0.5, ww - 0.2, h - 0.5), Use(M('fixed', 'cream'), idx=4))
    b.decal('left', rect(0, h * 0.3, d, h * 0.3 + 0.6), Delta(+1))
    return b


@iso_asset('books_stack', 'decoration', 'desk_decor', 'Pile de livres', size_su=(12, 9), facings=ALL4, height=6, **DESKKW)
def _(m):
    _book(m, -6, -4, 0, 6, 4, 2, 'coral', 'primary')
    _book(m, -5.5, -3.5, 2, 5, 3.5, 3.6, 'teal', 'secondary')
    _book(m, -6, -4.2, 3.6, 5.5, 4, 5.4, 'sun', 'accent')


@iso_asset('storage_boxes', 'decoration', 'desk_decor', 'Boites de rangement', size_su=(14, 10), facings=ALL4, height=11,
           centered=True, provides=['desk_item', 'shelf_item'], model_kw={'shadow_k': 0.3})
def _(m):
    a = m.box(-7, -5, 0, 7, 5, 6, M('primary', 'kraft'))
    label(a, 'front', 4, 1.5, 6, 3, 1)
    a.decal('right', rect(3, 3.5, 7, 4.5), Delta(-2))
    b = m.box(-6, -4.5, 6, 6, 4.5, 11, M('secondary', 'sky'))
    b.decal('front', rect(4, 2, 8, 3), Delta(-2))
    b.decal('top', frame(0, 0, 12, 9), Delta(+1))


@iso_asset('mug', 'decoration', 'desk_decor', 'Tasse', size_su=(7, 6), facings=ALL4, height=5, **DESKKW)
def _(m):
    c = m.cyl(0, 0, 2.2, 0, 4.6, M('primary', 'coral'))
    c.decal('top', disc(2.2, 2.2, 1.6), A_('walnut', 1))
    c.decal('side', rect(0, 3.6, 360, 4.6), Delta(+1))
    m.box(2.0, -0.45, 1.0, 3.4, 0.45, 1.7, M('primary', 'coral'))
    m.box(2.8, -0.45, 1.0, 3.5, 0.45, 3.9, M('primary', 'coral'))
    m.box(2.0, -0.45, 3.2, 3.4, 0.45, 3.9, M('primary', 'coral'))


@iso_asset('bottle', 'decoration', 'desk_decor', "Bouteille d'eau", size_su=(4, 4), facings=ONE, symmetric=True, height=10,
           **DESKKW)
def _(m):
    b = m.cyl(0, 0, 1.5, 0, 7, M('glass', 'glass'))
    b.decal('side', rect(0, 2, 360, 4.5), A_('teal', 3, 'accent'))
    b.decal('side', rect(200, 4.5, 230, 7), Use(M('glass', 'glass'), idx=5))
    m.cyl(0, 0, 0.8, 7, 8.4, M('glass', 'glass'))
    m.cyl(0, 0, 0.95, 8.4, 9.6, M('primary', 'sky'))


@iso_asset('snack_bowl', 'decoration', 'desk_decor', 'Bol de snacks', size_su=(8, 8), facings=ONE, symmetric=True, height=5,
           **DESKKW)
def _(m):
    m.ell(0, 0, 3, 3.8, 3.8, 3, M('primary', 'sun'), zclip=(0.4, 3))
    m.cyl(0, 0, 1.6, 0, 0.6, M('primary', 'sun'))
    for k, (x, y, ramp) in enumerate(((-1.4, -0.6, 'kraft'), (1.2, 0.8, 'coral'), (0.2, -1.6, 'kraft'),
                                      (-0.6, 1.4, 'sun'), (1.6, -1.0, 'kraft'), (0, 0, 'coral'))):
        m.ell(x, y, 3.3 + (k % 2) * 0.4, 1.1, 1.1, 0.9, M('fixed', ramp))


@iso_asset('notebook', 'decoration', 'desk_decor', 'Carnet et stylo', size_su=(12, 10), facings=ALL4, height=2, **DESKKW)
def _(m):
    n = m.box(-4, -5, 0, 3, 5, 1.6, M('primary', 'navy'))
    for f, w in (('front', 7), ('right', 10)):
        n.decal(f, rect(0.3, 0.4, w - 0.3, 1.1), Use(M('fixed', 'cream'), idx=4))
    n.decal('top', rect(4.6, 0, 5.4, 10), A_('coral', 3, 'accent'))
    rbox(m, 5.2, 0, 8, 1, 80, 0, 1.0, M('secondary', 'sky'))


@iso_asset('paper_stack', 'decoration', 'desk_decor', 'Pile de papiers', size_su=(10, 12), facings=ALL4, height=3, **DESKKW)
def _(m):
    for k in range(3):
        o = (k % 2) * 0.6
        p = m.box(-4.5 + o, -6 + o * 0.5, k * 0.9, 4.5 + o, 6 - o * 0.5, k * 0.9 + 0.9, M('secondary', 'plastic'))
    top = m.prims[-1]
    for k in range(4):
        top.decal('top', rect(1.5, 2 + k * 2.2, 7.5 - (k % 2) * 2, 3 + k * 2.2), Use(M('fixed', 'steel'), idx=3))
    m.box(-1.5, 4.8, 2.7, 1.5, 6.4, 3.4, GRAPH)


@iso_asset('cable_organizer', 'decoration', 'desk_decor', 'Rangement de cables', size_su=(12, 8), facings=ALL4, height=5,
           **DESKKW)
def _(m):
    b = m.box(-5, -3, 0, 3, 3, 4, M('primary', 'graphite'))
    b.decal('top', rect(1, 1, 7, 5), Delta(-2))
    for k, ramp in enumerate(('sky', 'coral', 'sun')):
        b.decal('left', rect(1.2 + k * 1.6, 2.2, 2.0 + k * 1.6, 3.2), A_(ramp, 3))
    coil = m.cyl(6, 0, 2.6, 0, 1.2, M('fixed', 'sky'))
    coil.decal('top', ('fn', lambda s, t: (np.floor(np.hypot(s - 2.6, t - 2.6) * 1.4) % 2) == 0), Delta(-1))
    rbox(m, 2.5, -1.5, 4, 0.7, 20, 0, 0.7, M('fixed', 'coral'), outline=False)


@iso_asset('trophy_project', 'decoration', 'desk_decor', 'Trophee de projet', size_su=(8, 8), facings=ALL4, height=14,
           notes='Coupe originale surmontee d\'une etoile, socle bois et plaque.', **DESKKW)
def _(m):
    base = m.box(-3, -3, 0, 3, 3, 2, M('wood', 'walnut'))
    base.decal('front', rect(1.5, 0.5, 4.5, 1.5), A_('brass', 4, 'metal'))
    m.box(-2, -2, 2, 2, 2, 3.4, GRAPH)
    m.cyl(0, 0, 0.7, 3.4, 6, M('metal', 'brass'))
    cup = m.ell(0, 0, 8.6, 2.8, 2.8, 2.8, M('metal', 'brass'), zclip=(6, 9.8))
    m.box(-3.8, -0.4, 7.4, -2.6, 0.4, 9.2, M('metal', 'brass'))
    m.box(2.6, -0.4, 7.4, 3.8, 0.4, 9.2, M('metal', 'brass'))
    st = m.box(-1.6, -0.3, 10.2, 1.6, 0.3, 13.4, M('metal', 'brass'), outline=True)
    stamp(st, 'front', ['.k.', 'kkk', '.k.'], {'k': A_('sun', 5)}, s0=0.1, t_top=3.1)
    stamp(st, 'back', ['.k.', 'kkk', '.k.'], {'k': A_('sun', 5)}, s0=0.1, t_top=3.1)


@iso_asset('rubber_duck_debug', 'decoration', 'desk_decor', 'Canard de debogage', size_su=(7, 8), facings=ALL4, height=7,
           notes='Canard en caoutchouc (jouet) : reference discrete au debogage par explication.', **DESKKW)
def _(m):
    m.ell(0, 0.6, 2.0, 2.8, 3.2, 2.0, M('accent', 'sun'))
    m.ell(0, 3.0, 3.0, 1.2, 1.2, 1.2, M('accent', 'sun'))
    m.ell(0, -1.4, 4.6, 1.9, 1.9, 1.9, M('accent', 'sun'))
    m.box(-0.9, -4.3, 3.9, 0.9, -2.9, 4.7, M('fixed', 'coral'))
    for x in (-1.5, 0.7):
        m.box(x, -3.05, 5.2, x + 0.8, -2.6, 6.1, INK, outline=False, edges=False)


@iso_asset('retro_message', 'decoration', 'desk_decor', 'Petit ecran retro (invite)', size_su=(10, 10), facings=ALL4,
           height=10, screens=[{'name': 'screen', 'quad': [(-3.5, -4, 6.5), (3.5, -4, 6.5), (3.5, -4, 2), (-3.5, -4, 2)]}],
           notes="Mini ecran retro affichant une invite generique '>_' (message informatique generique).", **DESKKW)
def _(m):
    m.box(-4, -3, 0, 4, 4, 1, M('primary', 'beige_pc'))
    b = m.box(-5, -4, 1, 5, 4, 9, M('primary', 'beige_pc'))
    b.decal('front', rect(1, 1, 9, 7), A_('graphite', 2))
    b.decal('front', rect(1.5, 1.5, 8.5, 6.5), A_('display', 1, 'screen'))
    stamp(b, 'front', ['k....', '.k...', 'k.kkk'], {'k': Fix('led_green')}, s0=2.5, t_top=5.5)
    b.decal('back', rect(2, 2, 8, 6), Delta(-1))
    for k in range(3):
        b.decal('right', rect(2 + k * 2, 3, 3 + k * 2, 6), Delta(-1))


@iso_asset('side_project', 'decoration', 'desk_decor', 'Mini-projet personnel (platine)', size_su=(16, 10), facings=ALL4,
           height=3, notes='Platine d\'essai avec fils, carte microcontroleur generique, LED et piles.', **DESKKW)
def _(m):
    bb = m.box(-8, -4, 0, 2, 4, 1.2, M('primary', 'plastic'))
    bb.decal('top', ('fn', lambda s, t: ((np.floor(s) % 2) == 0) & ((np.floor(t) % 2) == 0) & (t > 1) & (t < 7)), Delta(-1))
    bb.decal('top', rect(0, 3.6, 10, 4.4), Delta(-1))
    for (s0, t0, s1, t1, ramp) in ((1, 2, 6, 3, 'coral'), (3, 5, 4, 7.5, 'sky'), (6, 1, 9, 2, 'sun'), (7, 5, 8, 7, 'leaf')):
        bb.decal('top', rect(s0, t0, s1, t1), A_(ramp, 4))
    m.box(3, -3, 0, 8, 2, 0.8, M('fixed', 'leaf'))
    m.box(4.5, -1.5, 0.8, 6.5, 0.5, 1.6, GRAPH)
    m.box(7, -2.5, 0.8, 8, -1.5, 1.6, M('metal', 'steel'))
    m.cyl(-2, 0, 0.6, 1.2, 2.4, M('fixed', 'red'))
    m.box(-7.5, -4, 1.2, -6.5, 4, 1.6, GRAPH, outline=False)
    for x in (4, 6.2):
        m.hcyl('y', x, 1.0, 1.0, 3, 8, M('fixed', 'sun'))


@iso_asset('moving_box', 'decoration', 'floor_decor', 'Cartons de demenagement', footprint=(1, 1), facings=FRONT2, height=26,
           notes="Deux cartons scotches (fleches 'haut' pictogrammes) : chambre de depart, emmenagement.")
def _(m):
    a = m.box(6, 7, 0, 26, 25, 16, M('primary', 'kraft'))
    a.decal('top', rect(9, 0, 11, 18), Use(M('fixed', 'sand'), idx=5))
    for f in ('front', 'back'):
        a.decal(f, rect(9, 12, 11, 16), Use(M('fixed', 'sand'), idx=5))
    for f, w in (('left', 18), ('right', 18)):
        a.decal(f, rect(w / 2 - 3, 10, w / 2 + 3, 11.5), Delta(-2))
    stamp(a, 'front', ['.k..k.', 'kkkkkk', '.k..k.', '.k..k.'], {'k': Use(M('fixed', 'graphite'), idx=2)}, s0=2.5, t_top=9)
    b = m.box(9, 10, 16, 22, 21, 24.5, M('primary', 'kraft'))
    b.decal('top', rect(0, 4.5, 13, 6.5), Use(M('fixed', 'sand'), idx=5))
    label(b, 'front', 3, 2, 7, 4, 1)
