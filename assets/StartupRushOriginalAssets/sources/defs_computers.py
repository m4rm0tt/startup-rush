"""Postes informatiques et accessoires (famille computers).

Objets poses (centered=True) : origine locale = centre de la base, sur le support.
Ecrans : la zone d'affichage est declaree (quad) pour recevoir une texture du jeu.
Aucune marque : pas de logo, pas de nom de fabricant.
"""
from srgen.iso import Fix, Use, Delta, rect, hline, vline, frame, disc
from srgen.palette import M, RAMPS
from srgen.registry import iso_asset, Socket, Access, ALL4, FRONT2, ONE
from srgen.shapes import code_screen, ui_screen, led, vents, rbox, chamfer_box

GRAPH = M('metal', 'graphite')
STEELM = M('metal', 'steel')
CASE_DARK = M('primary', 'graphite')
CASE_LIGHT = M('primary', 'plastic')
SCREEN = M('screen', 'display')


def screen_quad(x0, x1, y, z0, z1):
    return {'name': 'screen', 'quad': [(x0, y, z1), (x1, y, z1), (x1, y, z0), (x0, y, z0)]}


# ---------------------------------------------------------------------------
# Ecrans
# ---------------------------------------------------------------------------
def _monitor(m, half_w, z0, z1, depth=2.0, stand='round', seed=0, content='code', bezel=CASE_DARK):
    # pied
    if stand == 'round':
        m.cyl(0, 2, 5, 0, 1.5, GRAPH, ry=3.5)
    else:
        m.box(-6, -1, 0, 6, 5, 1.5, GRAPH)
    m.box(-1, 2, 1, 1, 4, z0 + 3, GRAPH)
    m.box(-2.5, 2.5, z0 + 1, 2.5, 4.5, z0 + 6, GRAPH)
    p = m.box(-half_w, -0.5, z0, half_w, depth - 0.5, z1, bezel)
    w = 2 * half_w; h = z1 - z0
    if content == 'code':
        code_screen(p, 'front', w, h, seed=seed)
    else:
        ui_screen(p, 'front', w, h, seed=seed)
    p.decal('back', rect(w / 2 - 4, h / 2 - 3, w / 2 + 4, h / 2 + 3), Delta(-1))
    p.decal('back', frame(1, 1, w - 1, h - 1), Delta(+1))
    return p


@iso_asset('monitor_basic', 'computers', 'monitor', 'Ecran simple', centered=True, size_su=(24, 8), facings=ALL4,
           height=22, provides=['monitor'], screens=[screen_quad(-10, 10, -0.5, 9, 21)],
           sockets=[Socket('top', ['webcam'], 0, 0.5, 22, 'webcam sur le bord superieur')],
           notes="Ecran 22 px ; la zone 'screen' recoit l'affichage dynamique.")
def _(m):
    _monitor(m, 11, 8, 22, seed=1)


@iso_asset('monitor_large', 'computers', 'monitor', 'Grand ecran', centered=True, size_su=(32, 8), facings=ALL4,
           height=26, provides=['monitor'], screens=[screen_quad(-14, 14, -0.5, 9, 25)],
           sockets=[Socket('top', ['webcam'], 0, 0.5, 26, 'webcam')])
def _(m):
    _monitor(m, 15, 8, 26, seed=2, stand='flat', content='ui')


@iso_asset('monitor_ultrawide', 'computers', 'monitor', 'Ecran ultralarge incurve', centered=True, size_su=(44, 10),
           facings=ALL4, height=24, provides=['monitor'], screens=[screen_quad(-20, 20, -0.5, 9, 23)],
           sockets=[Socket('top', ['webcam'], 0, 0.5, 24, 'webcam')],
           notes='Ecran ultralarge (2 cases de bureau) ; legere courbure suggeree par les ailes.')
def _(m):
    m.box(-7, -1, 0, 7, 5, 1.5, GRAPH)
    m.box(-1, 2, 1, 1, 4, 11, GRAPH)
    p = m.box(-21, -0.5, 8, 21, 1.5, 24, CASE_DARK)
    code_screen(p, 'front', 42, 16, seed=5)
    m.box(-23, 0.5, 8, -21, 3, 24, CASE_DARK)
    m.box(21, 0.5, 8, 23, 3, 24, CASE_DARK)


@iso_asset('monitor_arm_second', 'computers', 'monitor', "Module de deuxieme ecran (bras)", centered=True,
           size_su=(24, 10), facings=ALL4, height=30, provides=['monitor_arm', 'monitor'],
           screens=[screen_quad(-10, 10, -2.5, 15, 27)],
           notes="Amelioration visible : bras articule + ecran, se pose sur le socket monitor_second.")
def _(m):
    m.box(-2, 3, 0, 2, 7, 3, GRAPH)
    m.box(-1, 4, 3, 1, 6, 22, STEELM)
    rbox(m, 0, 2, 6, 2, 90, 20, 22, STEELM)
    p = m.box(-11, -2.5, 14, 11, -0.5, 28, CASE_DARK)
    ui_screen(p, 'front', 22, 14, seed=3)


# ---------------------------------------------------------------------------
# Unites centrales
# ---------------------------------------------------------------------------
def _tower(m, w, d, h, mat, front='plain', glass=False, gpu=False, seed=0):
    p = m.box(-w / 2, -d / 2, 1, w / 2, d / 2, h, mat)
    for (x, y) in ((-w / 2 + 1, -d / 2 + 1), (w / 2 - 2, -d / 2 + 1), (-w / 2 + 1, d / 2 - 2), (w / 2 - 2, d / 2 - 2)):
        m.box(x, y, 0, x + 1, y + 1, 1, GRAPH)
    hh = h - 1
    if front == 'plain':
        p.decal('front', rect(1, hh - 4, w - 1, hh - 3), Delta(-1))
        led(p, 'front', w / 2 - 0.5, hh - 2, 'led_blue')
        p.decal('front', rect(1, 2, w - 1, 6), Delta(-1))
    elif front == 'mesh':
        p.decal('front', ('fn', lambda s, t: ((s.astype(int) + t.astype(int)) % 2 == 0) & (t > 2) & (t < hh - 4)), Delta(-1))
        led(p, 'front', w / 2 - 0.5, hh - 2, 'led_white')
    elif front == 'retro':
        p.decal('front', rect(1, hh - 6, w - 1, hh - 4), Delta(-1))
        p.decal('front', rect(1, hh - 9, w - 1, hh - 8), Delta(-1))
        p.decal('front', rect(2, 3, 4, 5), Fix('led_amber'))
        led(p, 'front', w - 3, 3, 'led_green')
    for side in ('right', 'left'):
        if glass:
            p.decal(side, rect(2, 2, d - 2, hh - 2), Use(M('glass', 'glass'), idx=1))
            if gpu:
                p.decal(side, rect(4, hh * 0.45, d - 3, hh * 0.45 + 3), Use(M('accent', 'sky'), idx=3))
                p.decal(side, rect(4, hh * 0.45 + 4, d - 3, hh * 0.45 + 7), Use(M('accent', 'sky'), idx=3))
                p.decal(side, rect(d - 7, hh - 8, d - 3, hh - 4), Use(GRAPH, idx=3))
            p.decal(side, rect(3, hh - 4, 6, hh - 3), Fix('led_white'))
        else:
            vents(p, side, d - 6, d - 2, 3, hh - 3, step=2)
    return p


@iso_asset('tower_used', 'computers', 'tower', "Tour d'occasion", centered=True, size_su=(10, 18), facings=ALL4, height=22,
           provides=['tower'], notes='Vieille tour beige, lecteur en facade : point de depart.')
def _(m):
    _tower(m, 10, 18, 22, M('primary', 'beige_pc'), front='retro')


@iso_asset('tower_office', 'computers', 'tower', 'Tour bureautique', centered=True, size_su=(8, 16), facings=ALL4, height=20,
           provides=['tower'])
def _(m):
    _tower(m, 8, 16, 20, CASE_DARK, front='plain')


@iso_asset('workstation_dev', 'computers', 'tower', 'Station de developpement', centered=True, size_su=(10, 20),
           facings=ALL4, height=26, provides=['tower'])
def _(m):
    _tower(m, 10, 20, 26, CASE_DARK, front='mesh')


@iso_asset('workstation_creative', 'computers', 'tower', 'Station creative', centered=True, size_su=(10, 18),
           facings=ALL4, height=26, provides=['tower'], notes='Boitier alu clair, grille ronde en facade.')
def _(m):
    p = _tower(m, 10, 18, 26, M('primary', 'steel'), front='plain')
    p.decal('front', ('fn', lambda s, t: (((s - 5) ** 2 + (t - 13) ** 2) < 12) & ((s.astype(int) + t.astype(int)) % 2 == 0)), Delta(-1))


@iso_asset('workstation_gpu', 'computers', 'tower', 'Station GPU / IA', centered=True, size_su=(12, 22), facings=ALL4,
           height=30, provides=['tower'], notes='Grand boitier a paroi vitree : cartes GPU visibles (sans RGB).')
def _(m):
    _tower(m, 12, 22, 30, CASE_DARK, front='mesh', glass=True, gpu=True)


@iso_asset('workstation_compute', 'computers', 'tower', 'Poste de calcul avance', centered=True, size_su=(18, 22),
           facings=ALL4, height=32, provides=['tower'], notes='Double unite au pied du bureau, refroidissement liquide.')
def _(m):
    a = _tower(m, 8, 22, 32, CASE_DARK, front='mesh', glass=True, gpu=True)
    m.box(4, -11, 1, 13, 11, 28, M('primary', 'steel'))
    q = m.prims[-1]
    vents(q, 'front', 1, 8, 3, 25, step=2)
    led(q, 'front', 4, 24, 'led_green')
    m.hcyl('y', 5.5, 24, 1, -8, 8, M('accent', 'sky'))


@iso_asset('mini_pc', 'computers', 'tower', 'Mini-PC', centered=True, size_su=(8, 8), facings=ALL4, height=3,
           provides=['desk_item', 'tower_mini'], notes='Se pose sur le bureau.')
def _(m):
    p = m.box(-4, -4, 0, 4, 4, 3, CASE_DARK)
    p.decal('top', frame(1, 1, 7, 7), Delta(+1))
    led(p, 'front', 6, 1, 'led_blue')


# ---------------------------------------------------------------------------
# Peripheriques de bureau
# ---------------------------------------------------------------------------
@iso_asset('keyboard_mouse', 'computers', 'peripheral', 'Clavier et souris', centered=True, size_su=(22, 8), facings=ALL4,
           height=2, provides=['keyboard'])
def _(m):
    k = m.box(-9, -3, 0, 6, 3, 1.5, CASE_DARK)
    k.decal('top', ('fn', lambda s, t: ((t.astype(int) % 2) == 1) & ((s.astype(int) % 2) == 0) & (s > 0.5) & (s < 14.5)), Delta(+2))
    pad = m.box(8, -3.5, 0, 14, 3.5, 0.5, M('fabric', 'navy'), outline=False)
    m.ell(11, -0.5, 0.5, 1.6, 2.2, 1.4, CASE_DARK, zclip=(0.5, 2))
