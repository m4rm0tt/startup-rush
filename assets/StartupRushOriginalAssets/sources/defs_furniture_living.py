"""Vie des locaux : luminaires, detente, kitchenette, sanitaires, chambre, tapis, stores."""
import random

from srgen.iso import Fix, Use, Delta, rect, hline, vline, frame, disc, FRONT_IF_BACK, TILE
from srgen.palette import M, OAK, WALNUT, BIRCH, RAMPS
from srgen.registry import iso_asset, Socket, Access, ALL4, FRONT2, ONE
from srgen.shapes import rbox, chamfer_box, drawers, vents, led

GRAPH = M('metal', 'graphite')
STEELM = M('metal', 'steel')
WHITE = M('primary', 'plastic')
GLOW = Fix('lamp_glow')
COUNTER_H = 30


# ---------------------------------------------------------------------------
# Luminaires
# ---------------------------------------------------------------------------
@iso_asset('lamp_ceiling', 'furniture', 'lighting', 'Luminaire suspendu', centered=True, size_su=(14, 14), facings=ONE,
           symmetric=True, height=112, kind='ceiling', collision=[],
           notes='Suspension au plafond (cable jusqu\'a z=112). Ancre au sol sous la lampe ; aucune collision.')
def _(m):
    m.model_shadow = False
    m.cyl(0, 0, 0.6, 86, 112, GRAPH, shadow=False)
    m.cyl(0, 0, 2.2, 84, 87, GRAPH, shadow=False)
    sh = m.ell(0, 0, 80, 8, 8, 6, M('primary', 'sun'), zclip=(77, 86), shadow=False)
    m.cyl(0, 0, 6.5, 76.5, 77.2, Fix(GLOW.color), shadow=False, outline=False)


@iso_asset('lamp_desk', 'furniture', 'lighting', 'Lampe de bureau', centered=True, size_su=(10, 12), facings=ALL4, height=22,
           provides=['desk_item'], notes='Lampe articulee ; tete orientee vers l\'avant.')
def _(m):
    m.cyl(0, 3, 3.5, 0, 1.5, GRAPH, ry=3)
    rbox(m, 0, 2, 2, 1.4, 90, 1.5, 14, M('accent', 'coral'))
    m.box(-0.7, -4, 13, 0.7, 3, 14.4, M('accent', 'coral'))
    head = m.box(-2.5, -6.5, 10.5, 2.5, -2.5, 14, M('accent', 'coral'))
    head.decal('bottom', rect(0, 0, 5, 4), Fix(GLOW.color))
    m.box(-1.8, -6, 10, 1.8, -3, 10.6, Fix(GLOW.color), outline=False)


@iso_asset('lamp_floor', 'furniture', 'lighting', 'Lampadaire', centered=True, size_su=(14, 14), facings=ONE, symmetric=True,
           height=74)
def _(m):
    m.cyl(0, 0, 5.5, 0, 2, GRAPH)
    m.cyl(0, 0, 0.9, 2, 60, GRAPH)
    s = m.cyl(0, 0, 6.5, 58, 72, M('fabric', 'cream'))
    s.decal('side', ('fn', lambda a, t: (t.astype(int) == 1)), Delta(-1))
    s.decal('top', disc(6.5, 6.5, 5, 5), Fix(GLOW.color))


# ---------------------------------------------------------------------------
# Detente
# ---------------------------------------------------------------------------
def _sofa(m, w, fab):
    m.box(2, 4, 3, w - 2, 28, 12, fab)
    for (x, y) in ((4, 6), (w - 6, 6), (4, 25), (w - 6, 25)):
        m.box(x, y, 0, x + 2, y + 2, 3, M('wood', 'walnut'))
    n = 2 if w > 40 else 1
    cw = (w - 12) / n
    for k in range(n):
        c = m.box(6 + k * cw, 5, 12, 6 + (k + 1) * cw - 0.5, 22, 16, fab)
        c.decal('top', rect(0, 0, cw, 1), Delta(+1))
    back = m.box(4, 21, 12, w - 4, 28, 30, fab, layer=FRONT_IF_BACK)
    back.decal('front', ('fn', lambda s, t: ((s.astype(int) % int(cw)) == int(cw) - 1) & (t > 2)), Delta(-1))
    for x0 in (2, w - 6):
        a = m.box(x0, 4, 3, x0 + 4, 28, 21, fab)
        a.decal('top', rect(0, 0, 4, 24), Delta(+1))
    for k in range(n):
        p = m.box(8 + k * cw, 18, 16, 8 + k * cw + 9, 21, 26, M('accent', 'sun'))


@iso_asset('sofa', 'furniture', 'lounge', 'Canape', footprint=(2, 1), facings=ALL4, height=30,
           sockets=[Socket('seat_l', ['character_lounge'], 22, 12, 16, 'place assise gauche'),
                    Socket('seat_r', ['character_lounge'], 42, 12, 16, 'place assise droite')],
           notes='2 places, coussins + coussin deco ; dossier au premier plan vu de dos.')
def _(m):
    _sofa(m, 64, M('fabric', 'teal'))


@iso_asset('armchair', 'furniture', 'lounge', 'Fauteuil', footprint=(1, 1), facings=ALL4, height=30)
def _(m):
    _sofa(m, 32, M('fabric', 'coral'))


@iso_asset('table_coffee', 'furniture', 'lounge', 'Table basse', footprint=(1, 1), facings=ONE, symmetric=True, height=14,
           sockets=[Socket('top', ['desk_item'], 16, 16, 14)])
def _(m):
    t = m.cyl(16, 16, 11, 11, 14, OAK)
    t.decal('top', disc(11, 11, 8, 8), Delta(+1))
    t.decal('top', disc(11, 11, 7, 7), Delta(0))
    for (dx, dy) in ((-6, -6), (6, -6), (-6, 6), (6, 6)):
        m.box(16 + dx - 0.8, 16 + dy - 0.8, 0, 16 + dx + 0.8, 16 + dy + 0.8, 11, GRAPH)


# ---------------------------------------------------------------------------
# Kitchenette (modules d'une case contre un mur)
# ---------------------------------------------------------------------------
def _base_cabinet(m, top_mat=None):
    b = m.box(1, 8, 2, 31, 30, COUNTER_H - 2, WHITE)
    b.decal('front', vline(15, 0, 26), Delta(-1))
    b.decal('front', hline(0, 30, 20), Delta(-1))
    b.decal('front', rect(12, 22, 14, 23), Use(STEELM, idx=4))
    b.decal('front', rect(17, 22, 19, 23), Use(STEELM, idx=4))
    b.decal('front', rect(13, 14, 14, 18), Use(STEELM, idx=4))
    b.decal('front', rect(17, 14, 18, 18), Use(STEELM, idx=4))
    m.box(2, 9, 0, 30, 29, 2, M('fixed', 'graphite'))
    top = m.box(0, 7, COUNTER_H - 2, 32, 31, COUNTER_H + 1, top_mat or M('secondary', 'graphite'))
    return top


@iso_asset('kitchen_counter', 'furniture', 'kitchen', 'Element de cuisine', footprint=(1, 1), facings=FRONT2,
           height=COUNTER_H + 1, sockets=[Socket('counter', ['desk_item', 'microwave', 'coffee_machine'], 16, 18, COUNTER_H + 1)],
           notes='Module bas avec plan de travail ; se raboute (meme hauteur que evier).')
def _(m):
    _base_cabinet(m)


@iso_asset('kitchen_sink', 'furniture', 'kitchen', 'Evier', footprint=(1, 1), facings=FRONT2, height=COUNTER_H + 10)
def _(m):
    top = _base_cabinet(m)
    top.decal('top', rect(6, 5, 26, 18), Use(STEELM, idx=3))
    top.decal('top', rect(7, 6, 25, 17), Use(STEELM, idx=1))
    top.decal('top', rect(15.5, 10, 16.5, 12), Fix('ink'))
    m.box(15, 25, COUNTER_H + 1, 17, 27, COUNTER_H + 9, STEELM)
    m.box(15, 19, COUNTER_H + 7, 17, 27, COUNTER_H + 9, STEELM)


@iso_asset('kitchen_wall_cabinet', 'furniture', 'kitchen', 'Placard haut de cuisine', footprint=(1, 1), facings=FRONT2,
           height=86, kind='wall', wall_mount=True, collision=[],
           notes='Extra justifie : placard mural au-dessus des modules bas (silhouette differente).')
def _(m):
    b = m.box(1, 20, 60, 31, 32, 84, WHITE)
    b.decal('front', vline(15, 0, 24), Delta(-1))
    b.decal('front', rect(12, 2, 14, 3), Use(STEELM, idx=4))
    b.decal('front', rect(17, 2, 19, 3), Use(STEELM, idx=4))


@iso_asset('fridge', 'furniture', 'kitchen', 'Refrigerateur', footprint=(1, 1), facings=FRONT2, height=78, occludes=40)
def _(m):
    b = chamfer_box(m, 3, 6, 1, 29, 30, 78, 1.5, M('primary', 'plastic'))
    b.decal('front', hline(0, 26, 50), Delta(-1))
    b.decal('front', rect(21, 54, 22, 70), Use(STEELM, idx=4))
    b.decal('front', rect(21, 30, 22, 46), Use(STEELM, idx=4))
    b.decal('front', rect(4, 40, 9, 45), Fix(RAMPS['sun'][4]))
    b.decal('front', rect(10, 36, 14, 40), Fix(RAMPS['sky'][4]))
    m.box(4, 7, 0, 28, 29, 1, M('fixed', 'graphite'))


@iso_asset('microwave', 'furniture', 'kitchen', 'Micro-ondes', centered=True, size_su=(16, 12), facings=FRONT2, height=9,
           provides=['microwave', 'desk_item'])
def _(m):
    b = m.box(-8, -6, 0, 8, 6, 9, WHITE)
    b.decal('front', rect(1, 1.5, 11, 8), Use(M('fixed', 'graphite'), idx=1))
    b.decal('front', rect(2, 2.5, 6, 4.5), Use(M('fixed', 'graphite'), idx=2))
    b.decal('front', rect(12, 6, 15, 7), Fix('led_green'))
    for k in range(2):
        b.decal('front', rect(12, 2 + k * 2, 15, 3 + k * 2), Delta(-1))


@iso_asset('coffee_machine', 'furniture', 'kitchen', 'Machine a cafe', centered=True, size_su=(10, 12), facings=FRONT2,
           height=14, provides=['coffee_machine', 'desk_item'], notes='Espresso : reservoir, bec, tasse, egouttoir.')
def _(m):
    b = m.box(-5, -2, 0, 5, 6, 14, M('primary', 'graphite'))
    b.decal('front', rect(1, 11, 9, 12), Use(STEELM, idx=4))
    led(b, 'front', 8, 9, 'led_amber')
    m.box(-4, -6, 0, 4, -2, 1.5, STEELM)
    m.box(-2.5, -4, 8, 2.5, -2, 10, STEELM)
    m.cyl(0, -4, 1.6, 1.5, 4, M('fixed', 'plastic'))
    m.box(-4.5, 4, 1, -1.5, 6.5, 13, M('glass', 'glass'), alpha=170)


@iso_asset('water_cooler', 'furniture', 'kitchen', 'Fontaine a eau', centered=True, size_su=(12, 12), facings=FRONT2,
           height=56)
def _(m):
    b = m.box(-6, -6, 0, 6, 6, 36, WHITE)
    b.decal('front', rect(2, 20, 10, 30), Delta(-1))
    b.decal('front', rect(3, 26, 4, 28), Fix(RAMPS['sky'][3]))
    b.decal('front', rect(8, 26, 9, 28), Fix(RAMPS['coral'][3]))
    m.cyl(0, 0, 5, 36, 52, M('glass', 'sky'), alpha=200)
    m.cyl(0, 0, 2, 52, 55, M('glass', 'sky'), alpha=200)
    m.box(-4, -7, 0, 4, -6, 2, GRAPH)


def _vending(m, kind):
    b = m.box(2, 6, 0, 30, 28, 80, M('primary', 'navy' if kind == 'drinks' else 'red'))
    win = rect(2, 18, 20, 76)
    b.decal('front', win, Use(M('glass', 'glass'), idx=1))
    rnd = random.Random(9 if kind == 'drinks' else 13)
    for r in range(6):
        t = 22 + r * 9
        b.decal('front', hline(2, 20, t - 2), Use(STEELM, idx=3))
        for c in range(4):
            s = 3 + c * 4.3
            col = RAMPS[rnd.choice(['sun', 'coral', 'leaf', 'sky', 'rose', 'plastic'])]
            if kind == 'drinks':
                b.decal('front', rect(s + 0.5, t, s + 2.5, t + 6), Fix(col[3]))
                b.decal('front', rect(s + 1, t + 5, s + 2, t + 6), Fix(col[5]))
            else:
                b.decal('front', rect(s, t, s + 3.5, t + 4), Fix(col[3]))
                b.decal('front', rect(s, t + 3, s + 3.5, t + 4), Fix(col[2]))
    b.decal('front', rect(22, 50, 27, 62), Use(GRAPH, idx=1))
    for k in range(3):
        b.decal('front', rect(23, 52 + k * 3, 26, 53 + k * 3), Fix(RAMPS['steel'][4]))
    b.decal('front', rect(22, 6, 27, 12), Use(GRAPH, idx=0))
    b.decal('front', rect(2, 4, 20, 10), Use(GRAPH, idx=0))


@iso_asset('vending_drinks', 'furniture', 'kitchen', 'Distributeur de boissons', footprint=(1, 1), facings=FRONT2,
           height=80, occludes=40)
def _(m):
    _vending(m, 'drinks')


@iso_asset('vending_snacks', 'furniture', 'kitchen', 'Distributeur de snacks', footprint=(1, 1), facings=FRONT2,
           height=80, occludes=40)
def _(m):
    _vending(m, 'snacks')


# ---------------------------------------------------------------------------
# Sanitaires
# ---------------------------------------------------------------------------
@iso_asset('toilet', 'furniture', 'sanitary', 'WC', footprint=(1, 1), facings=ALL4, height=34,
           notes='Cuvette suspendue + reservoir ; contre un mur (dos = arriere).')
def _(m):
    m.box(8, 22, 0, 24, 30, 34, WHITE)
    m.prims[-1].decal('front', rect(5, 26, 11, 29), Use(STEELM, idx=4))
    m.cyl(16, 14, 6, 4, 13, WHITE, ry=8)
    s = m.cyl(16, 14, 6.5, 13, 15, WHITE, ry=8.5)
    s.decal('top', disc(6.5, 8.5, 4.5, 6.5), Use(M('primary', 'plastic'), idx=2))
    m.box(10, 20, 13, 22, 23, 26, WHITE)


@iso_asset('washbasin', 'furniture', 'sanitary', 'Lavabo', footprint=(1, 1), facings=FRONT2, height=86,
           notes='Vasque murale + miroir (miroir integre au module).')
def _(m):
    b = m.box(6, 16, 26, 26, 30, 32, WHITE)
    b.decal('top', rect(3, 2, 17, 11), Use(M('primary', 'plastic'), idx=2))
    m.box(14, 26, 32, 18, 29, 36, STEELM)
    m.box(15, 22, 34, 17, 29, 36, STEELM)
    m.box(11, 20, 0, 21, 26, 26, WHITE)
    mir = m.box(5, 30, 44, 27, 32, 80, M('glass', 'glass'))
    mir.decal('front', ('fn', lambda s, t: ((s + t * 0.8) % 16 < 2)), Use(M('glass', 'glass'), idx=5))
    mir.decal('front', frame(0, 0, 22, 36), Use(STEELM, idx=3))


@iso_asset('toilet_stall', 'furniture', 'sanitary', 'Cabine sanitaire', footprint=(1, 1), facings=FRONT2, height=70,
           notes='Cloisons + porte fermee (statique) ; a placer autour d\'un WC.')
def _(m):
    m.box(0, 2, 4, 2, 32, 70, M('primary', 'sage'))
    m.box(30, 2, 4, 32, 32, 70, M('primary', 'sage'))
    d = m.box(2, 2, 6, 30, 4, 68, M('primary', 'sage'))
    d.decal('front', rect(22, 30, 24, 34), Use(STEELM, idx=4))
    d.decal('front', rect(4, 54, 7, 57), Fix('led_green'))
    for x in (0, 30):
        m.box(x, 2, 0, x + 2, 4, 4, STEELM)


# ---------------------------------------------------------------------------
# Chambre
# ---------------------------------------------------------------------------
@iso_asset('bed_single', 'furniture', 'bedroom', 'Lit simple', footprint=(2, 1), facings=FRONT2, height=26,
           notes='Tete de lit a gauche (x=0) ; couette recolorable (fabric).')
def _(m):
    m.box(2, 3, 2, 62, 29, 8, WALNUT)
    for (x, y) in ((2, 3), (60, 3), (2, 27), (60, 27)):
        m.box(x, y, 0, x + 2, y + 2, 2, WALNUT)
    m.box(1, 2, 2, 5, 30, 26, WALNUT)
    m.prims[-1].decal('right', frame(2, 6, 26, 22), Delta(-1))
    m.box(5, 4, 8, 61, 28, 12, M('secondary', 'plastic'))
    du = m.box(18, 3.5, 11, 61.5, 28.5, 15, M('fabric', 'sky'))
    du.decal('top', rect(0, 0, 4, 25), Delta(+1))
    du.decal('front', ('fn', lambda s, t: ((s.astype(int) % 8) == 0)), Delta(-1))
    du.decal('top', ('fn', lambda s, t: ((s.astype(int) % 8) == 0) & (s > 4)), Delta(-1))
    pl = m.box(6, 6, 12, 16, 26, 16, M('accent', 'cream'))
    pl.decal('top', rect(0, 0, 10, 1), Delta(+1))
    m.box(58, 3, 2, 62, 29, 14, WALNUT)


@iso_asset('dresser', 'furniture', 'bedroom', 'Commode', footprint=(1, 1), facings=FRONT2, height=40,
           sockets=[Socket('top', ['desk_item'], 16, 18, 40, 'objet sur la commode')])
def _(m):
    b = m.box(2, 8, 2, 30, 28, 38, OAK)
    drawers(b, 'front', 1, 27, 1, 35, 4, handle_mat=M('metal', 'brass'))
    m.box(1, 7, 38, 31, 29, 40, OAK)
    for (x, y) in ((3, 9), (28, 9), (3, 26), (28, 26)):
        m.box(x, y, 0, x + 1, y + 1, 2, GRAPH)


@iso_asset('nightstand', 'furniture', 'bedroom', 'Table de chevet', centered=True, size_su=(14, 14), facings=FRONT2,
           height=20, sockets=[Socket('top', ['desk_item'], 0, 0, 20)])
def _(m):
    b = m.box(-7, -7, 2, 7, 7, 18, OAK)
    b.decal('front', hline(0, 14, 9), Delta(-1))
    b.decal('front', rect(6, 12, 8, 13), Use(M('metal', 'brass'), idx=4))
    m.box(-7.5, -7.5, 18, 7.5, 7.5, 20, OAK)
    for (x, y) in ((-6, -6), (5, -6), (-6, 5), (5, 5)):
        m.box(x, y, 0, x + 1, y + 1, 2, GRAPH)


# ---------------------------------------------------------------------------
# Tapis et stores
# ---------------------------------------------------------------------------
def _rug(m, w, d, fab, acc):
    r = m.box(2, 2, 0, w - 2, d - 2, 0.6, fab, shadow=False)
    r.decal('top', frame(2, 2, w - 6, d - 6, 2, 2), Use(acc, rel=0))
    r.decal('top', frame(5, 5, w - 9, d - 9, 1, 1), Delta(-1))
    r.decal('top', ('fn', lambda s, t: (((s.astype(int) // 4) + (t.astype(int) // 4)) % 2 == 0) &
                     (s > 8) & (s < w - 12) & (t > 8) & (t < d - 12)), Delta(+1))


@iso_asset('rug_small', 'furniture', 'floor_decor', 'Tapis', footprint=(2, 2), facings=ONE, symmetric=True, height=1,
           walkable=True, collision=[], layer='floor_decor', model_kw={'shadow': False},
           notes='Couche floor_decor : dessine apres les sols, avant les ombres.')
def _(m):
    _rug(m, 64, 64, M('fabric', 'coral'), M('accent', 'cream'))


@iso_asset('rug_large', 'furniture', 'floor_decor', 'Grand tapis', footprint=(3, 2), facings=FRONT2, height=1,
           walkable=True, collision=[], layer='floor_decor', model_kw={'shadow': False})
def _(m):
    _rug(m, 96, 64, M('fabric', 'teal'), M('accent', 'sun'))


@iso_asset('window_blinds', 'furniture', 'window_dressing', 'Stores de fenetre', footprint=(1, 1), facings=FRONT2,
           height=90, kind='wall', wall_mount=True, collision=[], model_kw={'shadow': False},
           notes='Store venitien a mi-hauteur, a poser sur un segment wall_window (meme ancre de case).')
def _(m):
    # dans l'embrasure : entre la vitre (u=-4) et la face du mur (u=0) -> y local 33..35
    m.box(5, 33, 87, 27, 35.5, 89, M('primary', 'plastic'), shadow=False)
    for k in range(9):
        z = 85 - k * 3
        sl = m.box(7, 33.2, z, 25, 35, z + 2, M('primary', 'plastic'), shadow=False)
        sl.decal('front', hline(0, 18, 0), Delta(-1))
    m.box(6.5, 33, 58, 25.5, 35.5, 59.5, M('primary', 'plastic'), shadow=False)
    m.box(23, 34, 50, 24, 35, 58, M('fixed', 'graphite'), shadow=False)
