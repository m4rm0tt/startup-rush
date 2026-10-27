"""Industrie informatique future (famille industry_future, usage 'future').

Les machines sont statiques ; les produits et leur deplacement seront geres par
le jeu. Hauteur de bande commune : 12 px (dessus de bande des convoyeurs, des
rebords a rouleaux et des passages de bande des machines en ligne).

Ports (manifeste, champ 'ports' par orientation) :
  tile      = case (monde, relative a la case d'ancrage) qui porte le port
  world_dir = direction monde de l'arete d'echange (+U, -U, +V, -V)
  kind      = in / out / inout
Un produit passe d'un module A a un module B quand le port 'out' de A et un port
'in'/'inout' de B sont sur des cases voisines et de directions opposees.
Repere local : x = largeur, y = profondeur (avant y=0), z = hauteur px.
"""
import math
import numpy as np

from srgen.iso import Fix, Use, Delta, rect, hline, vline, frame, disc, TILE
from srgen.palette import M
from srgen.registry import iso_asset, Socket, Access, ALL4, FRONT2, ONE
from srgen.shapes import rbox, chamfer_box, code_screen, ui_screen, led, vents
from srgen.lot_ind_helpers import (stamp, speckle, mesh, perforation, hazard_stripes, fan_disc, stack_light,
                                   screen_panel, ring_segments, label)

BELT_Z = 12
GRAPH = M('fixed', 'graphite')     # structure sombre non recolorable
STEEL = M('metal', 'steel')
NAVY_M = M('metal', 'navy')
BELT = M('secondary', 'rubber')
BODY = M('primary', 'plastic')
BODY_G = M('secondary', 'steel')   # habillage acier (jamais avec une bande sur le meme asset)
ACC = M('accent', 'teal')
HAZ = M('accent', 'hazard')
DARK = M('fixed', 'graphite')
GLASS = M('glass', 'glass')
KRAFT = M('primary', 'kraft')
WOODP = M('wood', 'birch')

PORT_NOTE = ("ports : 'tile' = case du module portant le port, 'world_dir' = direction monde de l'arete "
             "d'echange ; dessus de bande a 12 px pour tous les modules et machines en ligne.")
EXTRA_BELT = {'belt_height_px': BELT_Z, 'port_rule': PORT_NOTE}


def P(name, d, kind, cx=0, cy=0):
    return dict(name=name, dir=d, kind=kind, cx=cx, cy=cy)


# ---------------------------------------------------------------------------
# Convoyeurs
# ---------------------------------------------------------------------------
def _ridges_y(prim):
    prim.decal('top', ('fn', lambda s, t: (np.floor(t) % 4) == 1), Delta(-1))


def _ridges_x(prim):
    prim.decal('top', ('fn', lambda s, t: (np.floor(s) % 4) == 1), Delta(-1))


def belt_y(m, x0, x1, y0, y1):
    """Bande dont le flux suit y local."""
    b = m.box(x0, y0, 8, x1, y1, BELT_Z, {'top': BELT, 'default': GRAPH})
    _ridges_y(b)
    return b


def belt_x(m, x0, x1, y0, y1):
    b = m.box(x0, y0, 8, x1, y1, BELT_Z, {'top': BELT, 'default': GRAPH})
    _ridges_x(b)
    return b


def legs(m, pts, z=7, w=2, mat=GRAPH):
    for (x, y) in pts:
        m.box(x, y, 0, x + w, y + w, z, mat)
        m.box(x - 0.5, y - 0.5, 0, x + w + 0.5, y + w + 0.5, 0.8, mat)


def side_frame_y(m, x0, y0, y1):
    """Longeron + garde (rail) le long de y."""
    m.box(x0, y0, 6, x0 + 2, y1, 13, STEEL)
    m.box(x0 + 0.5, y0, 14, x0 + 2, y1, 15.5, STEEL)


def side_frame_x(m, y0, x0, x1):
    m.box(x0, y0, 6, x1, y0 + 2, 13, STEEL)
    m.box(x0, y0 + 0.5, 14, x1, y0 + 2, 15.5, STEEL)


def rail_posts_y(m, x0, ys):
    for y in ys:
        m.box(x0 + 0.75, y - 0.5, 13, x0 + 1.75, y + 0.5, 14, GRAPH)


def roller_ledge_x(m, x0, x1, y0=6, y1=26):
    """Rebord a rouleaux (port) a hauteur de bande ; rouleaux d'axe y."""
    m.box(x0, y0, 8, x1, y1, 10, STEEL)
    n = max(1, int((x1 - x0) // 2))
    for k in range(n):
        cx = x0 + (x1 - x0) * (k + 0.5) / n
        m.hcyl('y', cx, 10.8, 1.2, y0 + 0.5, y1 - 0.5, STEEL)


def belt_stub_x(m, x0, x1, y0=6, y1=26):
    """Bout de bande sortant d'une machine (passage en ligne)."""
    b = m.box(x0, y0, 8, x1, y1, BELT_Z, {'top': BELT, 'default': GRAPH})
    _ridges_x(b)
    m.box(x0, y0 - 2, 6, x1, y0, 13, STEEL)
    m.box(x0, y1, 6, x1, y1 + 2, 13, STEEL)
    return b


@iso_asset('conveyor_straight', 'industry_future', 'conveyor', 'Convoyeur droit', footprint=(1, 1), facings=FRONT2,
           height=16, usage='future',
           ports=[P('in', 'back', 'in'), P('out', 'front', 'out')],
           sockets=[Socket('belt', ['product', 'tote'], 16, 16, BELT_Z, 'produit au centre de la bande')],
           extra=dict(EXTRA_BELT, flow_reversible=True),
           notes=("Module 1 case. Flux le long de y local : se = le long de U, sw = le long de V. "
                  "Symetrique (tambours aux deux bouts) : le flux peut etre inverse sans autre sprite."))
def _(m):
    legs(m, [(5, 4), (25, 4), (5, 26), (25, 26)])
    m.box(7, 4.5, 2, 25, 5.5, 3, GRAPH)
    m.box(7, 26.5, 2, 25, 27.5, 3, GRAPH)
    side_frame_y(m, 4, 0, 32)
    side_frame_y(m, 26, 0, 32)
    rail_posts_y(m, 4, (8, 24)); rail_posts_y(m, 26, (8, 24))
    belt_y(m, 6, 26, 2, 30)
    m.hcyl('x', 2, 10, 2, 6, 26, STEEL)
    m.hcyl('x', 30, 10, 2, 6, 26, STEEL)


@iso_asset('conveyor_curve', 'industry_future', 'conveyor', 'Convoyeur courbe (90 degres)', footprint=(1, 1), facings=ALL4,
           height=16, usage='future',
           ports=[P('a', 'back', 'inout'), P('b', 'right', 'inout')],
           sockets=[Socket('belt', ['product', 'tote'], 16, 16, BELT_Z)],
           extra=dict(EXTRA_BELT, flow_reversible=True,
                      curve_rule="relie les aretes 'back' et 'right' locales ; les 4 orientations couvrent les 4 virages"),
           notes='Bande en quart de couronne (rayons 6 a 26 su) autour du coin arriere-droit, plaque interieure.')
def _(m):
    m.clip = (0, 32, 0, 32)
    PX, PY = 32.0, 32.0
    b = m.cyl(PX, PY, 26, 8, BELT_Z, {'top': BELT, 'side': GRAPH, 'default': GRAPH}, shadow=False)

    def ridges(s, t):
        x = s + PX - 26; y = t + PY - 26
        ang = np.degrees(np.arctan2(y - PY, x - PX)) % 360
        r = np.hypot(x - PX, y - PY)
        return ((ang % 8.0) < 1.5) & (r > 6.5) & (r < 25.5)
    b.decal('top', ('fn', ridges), Delta(-1))
    # longerons exterieur / interieur (couronnes approchees par segments)
    ring_segments(m, PX, PY, 27, 2, 180, 270, 12, 6, 13, STEEL)
    ring_segments(m, PX, PY, 26.75, 1.5, 180, 270, 12, 14, 15.5, STEEL)
    m.cyl(PX, PY, 6, 6, 13, STEEL)
    ring_segments(m, PX, PY, 5.25, 1.5, 180, 270, 4, 14, 15.5, STEEL)
    for a in (205, 245):
        r = math.radians(a)
        m.box(PX + math.cos(r) * 26.75 - 0.5, PY + math.sin(r) * 26.75 - 0.5, 13,
              PX + math.cos(r) * 26.75 + 0.5, PY + math.sin(r) * 26.75 + 0.5, 14, GRAPH)
    for a, rr in ((200, 16), (250, 16), (225, 23)):
        r = math.radians(a)
        x, y = PX + math.cos(r) * rr, PY + math.sin(r) * rr
        m.box(x - 1, y - 1, 0, x + 1, y + 1, 8, GRAPH)
        m.box(x - 1.5, y - 1.5, 0, x + 1.5, y + 1.5, 0.8, GRAPH)
    # plaques de raccord aux deux aretes (affleurantes a la bande, masquent la coupe)
    for (x0, y0, x1, y1) in ((6, 30, 26, 32), (30, 6, 32, 26)):
        pl = m.box(x0, y0, 6, x1, y1, BELT_Z, STEEL)
        pl.decal('top', ('fn', lambda s, t: (np.floor(s + t) % 3) == 0), Delta(-1))
    for (x0, y0, x1, y1) in ((4, 30, 6, 32), (26, 30, 28, 32), (30, 4, 32, 6), (30, 26, 32, 28)):
        m.box(x0, y0, 6, x1, y1, 13, STEEL)


@iso_asset('conveyor_junction_t', 'industry_future', 'conveyor', 'Convoyeur jonction en T (insertion)', footprint=(1, 1),
           facings=ALL4, height=16, usage='future',
           ports=[P('in_main', 'back', 'in'), P('in_side', 'left', 'in'), P('out', 'front', 'out')],
           sockets=[Socket('belt', ['product', 'tote'], 16, 16, BELT_Z)],
           extra=EXTRA_BELT, notes="Ligne principale le long de y ; arrivee laterale par l'arete gauche, guide d'insertion.")
def _(m):
    legs(m, [(25, 4), (25, 26), (1, 14), (5, 4), (5, 26)])
    side_frame_y(m, 26, 0, 32)
    rail_posts_y(m, 26, (8, 24))
    m.box(4, 0, 6, 6, 4, 13, STEEL); m.box(4.5, 0, 14, 6, 4, 15.5, STEEL)
    m.box(4, 28, 6, 6, 32, 13, STEEL); m.box(4.5, 28, 14, 6, 32, 15.5, STEEL)
    belt_y(m, 6, 26, 2, 30)
    m.hcyl('x', 2, 10, 2, 6, 26, STEEL)
    m.hcyl('x', 30, 10, 2, 6, 26, STEEL)
    belt_x(m, 2, 6.5, 6, 26)
    m.box(0, 4, 6, 6, 6, 13, STEEL); m.box(0, 4.5, 14, 6, 6, 15.5, STEEL)
    m.box(0, 26, 6, 6, 28, 13, STEEL); m.box(0, 26, 14, 6, 27.5, 15.5, STEEL)
    m.hcyl('y', 2, 10, 2, 6, 26, STEEL)
    rbox(m, 9.5, 22.5, 10, 1.4, -45, 14, 15.5, STEEL)
    m.box(9.5 - 0.5, 22.5 - 0.5, 12, 9.5 + 0.5, 22.5 + 0.5, 14, GRAPH)


@iso_asset('conveyor_end', 'industry_future', 'conveyor', 'Convoyeur fin de ligne (butee)', footprint=(1, 1), facings=ALL4,
           height=20, usage='future',
           ports=[P('in', 'back', 'in')],
           sockets=[Socket('belt', ['product', 'tote'], 16, 18, BELT_Z)],
           extra=EXTRA_BELT, notes='Butee rayee jaune/noir et cellule de detection a l\'avant ; entree par l\'arriere.')
def _(m):
    legs(m, [(5, 10), (25, 10), (5, 26), (25, 26)])
    side_frame_y(m, 4, 6, 32)
    side_frame_y(m, 26, 6, 32)
    belt_y(m, 6, 26, 8, 30)
    m.hcyl('x', 30, 10, 2, 6, 26, STEEL)
    m.hcyl('x', 8, 10, 2, 6, 26, STEEL)
    m.box(4, 4, 3, 28, 6, 13, STEEL)
    stop = m.box(5, 4.5, 13, 27, 6.5, 19, HAZ)
    for f in ('front', 'back'):
        stop.decal(f, ('fn', lambda s, t: (np.floor(s + t) % 6) < 3), Use(DARK, idx=1))
    post = m.box(26.5, 12, 13, 28, 13.5, 21, GRAPH)
    head = m.box(25, 11.5, 19, 28.5, 14, 22, GRAPH)
    head.decal('sides', rect(0.5, 0.5, 1.5, 1.5), Fix('led_red'))


@iso_asset('conveyor_transfer_table', 'industry_future', 'conveyor', 'Table de transfert a angle droit', footprint=(1, 1),
           facings=FRONT2, height=14, usage='future',
           ports=[P('back', 'back', 'inout'), P('front', 'front', 'inout'), P('left', 'left', 'inout'), P('right', 'right', 'inout')],
           sockets=[Socket('belt', ['product', 'tote'], 16, 16, BELT_Z)],
           extra=EXTRA_BELT, notes='Rouleaux d\'axe x + deux bandes de transfert d\'axe y : echange dans les 4 directions.')
def _(m):
    legs(m, [(4, 4), (26, 4), (4, 26), (26, 26)], z=6)
    fr = m.box(3, 3, 6, 29, 29, 9, STEEL)
    fr.decal('top', frame(0, 0, 26, 26, 1, 1), Delta(+1))
    m.box(12, 12, 2, 20, 20, 6, DARK)
    for y in range(6, 28, 4):
        m.hcyl('x', y, 10.5, 1.5, 4, 28, STEEL)
    for x in (10, 21):
        bb = m.box(x, 4, 9, x + 2, 28, 11.6, BELT)
        bb.decal('top', ('fn', lambda s, t: (np.floor(t) % 3) == 0), Delta(-1))
    for (x, y) in ((3, 3), (27, 3), (3, 27), (27, 27)):
        m.box(x, y, 9, x + 2, y + 2, 13, HAZ)


# ---------------------------------------------------------------------------
# Postes et machines
# ---------------------------------------------------------------------------
def bench_frame(m, w=64, top_mat=None, z=30):
    for x in (2, w - 5):
        m.box(x, 3, 0, x + 3, 6, z - 3, STEEL)
        m.box(x, 25, 0, x + 3, 28, z - 3, STEEL)
        m.box(x, 3, 4, x + 3, 28, 6, STEEL)
    m.box(5, 25.5, 4, w - 5, 27.5, 6, STEEL)
    top = m.box(1, 2, z - 3, w - 1, 30, z, {'top': top_mat or M('secondary', 'teal'), 'default': GRAPH})
    top.decal('top', frame(1, 1, w - 3, 27), Delta(-1))
    return top


@iso_asset('assembly_station_manual', 'industry_future', 'assembly', "Poste d'assemblage manuel", footprint=(2, 1),
           facings=ALL4, height=64, usage='future',
           sockets=[Socket('chair', ['chair'], 32, -12, 0, 'operateur assis', facing_rel='opposite', group='workstation'),
                    Socket('bench_left', ['desk_item', 'product'], 14, 12, 30),
                    Socket('bench_center', ['product', 'desk_item'], 32, 11, 30),
                    Socket('bench_right', ['desk_item', 'product'], 50, 12, 30)],
           access=[Access('work', 0, -1), Access('work', 1, -1)],
           ports=[P('in', 'left', 'in', 0, 0), P('out', 'right', 'out', 1, 0)],
           extra=EXTRA_BELT,
           notes="Etabli ESD, rampe d'outils, lampe, bacs de pieces. Rebords a rouleaux aux deux bouts (ports a 12 px).")
def _(m):
    bench_frame(m)
    m.box(3, 26, 30, 5, 29, 63, STEEL)
    m.box(59, 26, 30, 61, 29, 63, STEEL)
    m.box(3, 25.5, 61, 61, 29, 64, STEEL)
    lamp = m.box(18, 22, 57, 46, 28, 60, BODY)
    lamp.decal('front', rect(1, 0, 27, 1), Fix((248, 251, 255, 255)))
    m.box(31, 26, 60, 33, 28, 61, STEEL)
    m.box(5, 27, 38, 59, 29, 39.5, STEEL)
    for x, ramp in zip((7, 15, 23, 31, 39, 47), ('sun', 'teal', 'coral', 'sky', 'sun', 'leaf')):
        bn = m.box(x, 22.5, 34, x + 6.5, 27, 40, M('fixed', ramp))
        bn.decal('top', rect(0.5, 0.5, 6, 4), Delta(-2))
        bn.decal('front', rect(0, 0, 6.5, 1), Delta(-1))
    screen_panel(m, 6, 26.5, 44, 17, 28, 53, 'ui', seed=4)
    m.box(55.5, 25, 47, 57, 26.5, 55, M('fixed', 'coral'))
    m.box(56, 25.5, 42, 56.5, 26, 47, STEEL)
    m.box(51.5, 25, 49, 53, 26.5, 55, M('fixed', 'sun'))
    m.box(52, 25.5, 45, 52.5, 26, 49, STEEL)
    mat = m.box(22, 6, 30, 42, 18, 30.6, M('fixed', 'graphite'), outline=False)
    mat.decal('top', frame(0, 0, 20, 12), Use(M('fixed', 'graphite'), idx=3))
    roller_ledge_x(m, 0, 5)
    roller_ledge_x(m, 59, 64)


@iso_asset('assembly_cell_robotic', 'industry_future', 'assembly', "Cellule d'assemblage robotisee", footprint=(2, 2),
           facings=FRONT2, height=50, usage='future', occludes=40,
           ports=[P('in', 'left', 'in', 0, 0), P('out', 'right', 'out', 1, 0)],
           access=[Access('maintenance', 1, 2, 'same')],
           sockets=[Socket('belt', ['product'], 32, 16, BELT_Z, 'poste de prise du robot')],
           extra=EXTRA_BELT,
           notes=("Bras robotise (6 axes stylise) au-dessus d'une bande traversante (rangee avant). Grillage "
                  "a l'arriere et sur les cotes seulement : la cellule reste lisible depuis la camera."))
def _(m):
    pl = m.box(0.5, 0.5, 0, 63.5, 63.5, 1.5, M('fixed', 'concrete'))
    edge = lambda s, t: (s < 2.5) | (s > 60.5) | (t < 2.5) | (t > 60.5)
    pl.decal('top', ('fn', lambda s, t: edge(s, t) & ((np.floor(s + t) % 6) < 3)), Use(HAZ, idx=3))
    pl.decal('top', ('fn', lambda s, t: edge(s, t) & ((np.floor(s + t) % 6) >= 3)), Use(DARK, idx=1))
    # bande traversante (rangee avant, flux le long de x)
    belt_x(m, 2, 62, 6, 26)
    m.box(0, 4, 6, 64, 6, 13, STEEL); m.box(0, 26, 6, 64, 28, 13, STEEL)
    m.hcyl('y', 2, 10, 2, 6, 26, STEEL); m.hcyl('y', 62, 10, 2, 6, 26, STEEL)
    for x in (8, 30, 52):
        m.box(x, 4.5, 1.5, x + 2, 6.5, 7, GRAPH); m.box(x, 25.5, 1.5, x + 2, 27.5, 7, GRAPH)
    # robot
    m.cyl(36, 46, 6.5, 1.5, 5, DARK)
    m.cyl(36, 46, 5, 5, 11, HAZ)
    tur = m.cyl(36, 46, 4.2, 11, 16, HAZ)
    tur.decal('side', rect(0, 1.5, 360, 2.5), Use(DARK, idx=2))
    m.hcyl('x', 46, 16, 3.2, 31.5, 40.5, DARK)
    m.slab_tilt(33, 39, 46, 15, 39, 35, 5, HAZ)
    m.hcyl('x', 38, 36, 3, 32, 40, DARK)
    m.slab_tilt(34, 38, 39, 37.5, 22, 31, 3.5, HAZ)
    m.cyl(36, 21, 2.2, 25, 31, DARK)
    m.box(34, 19.5, 20, 35, 22.5, 25, STEEL)
    m.box(37, 19.5, 20, 38, 22.5, 25, STEEL)
    m.bounds_hint(28, 14, 0, 44, 54, 44)
    # grillage arriere + cotes (moitie arriere)
    PANEL = M('fixed', 'graphite')
    back = m.box(3, 61, 2, 61, 62.5, 46, PANEL)
    lft = m.box(1, 33, 2, 2.5, 61, 46, PANEL)
    rgt = m.box(61.5, 33, 2, 63, 61, 46, PANEL)
    for p in (back, lft, rgt):
        mesh(p, 'sides', 3, Use(M('fixed', 'steel'), idx=2))
    for (x, y) in ((0.5, 31), (0.5, 61), (61.5, 31), (61.5, 61), (31, 61)):
        m.box(x, y, 0, x + 2, y + 2, 48, HAZ)
    m.box(0.5, 31, 46, 63.5, 33, 48, HAZ)
    m.box(0.5, 61, 46, 63.5, 63, 48, HAZ)
    m.box(0.5, 33, 46, 2.5, 61, 48, HAZ)
    m.box(61.5, 33, 46, 63.5, 61, 48, HAZ)
    # coffret de commande avec arret d'urgence
    cab = m.box(52, 34, 1.5, 60, 40, 30, M('primary', 'steel'))
    cab.decal('front', rect(1, 18, 7, 25), Use(M('screen', 'display'), idx=1))
    cab.decal('front', rect(2, 22, 6, 23), Fix((143, 227, 204, 255)))
    cab.decal('front', disc(4, 12, 2.2), Use(M('fixed', 'red'), idx=3))
    cab.decal('front', disc(4, 12, 1.2), Use(M('fixed', 'red'), idx=4))
    cab.decal('front', rect(2, 6, 3, 7), Fix('led_green'))
    cab.decal('front', rect(5, 6, 6, 7), Fix('led_amber'))
    stack_light(m, 62.5, 62, 48, lit='leaf')


@iso_asset('pick_and_place_machine', 'industry_future', 'smt', 'Machine de placement de composants', footprint=(2, 1),
           facings=FRONT2, height=48, usage='future',
           ports=[P('in', 'left', 'in', 0, 0), P('out', 'right', 'out', 1, 0)],
           access=[Access('operate', 0, -1), Access('operate', 1, -1)],
           extra=EXTRA_BELT,
           notes="Carrosserie fermee, bancs de chargeurs a bobines en facade, fenetre sur le portique, passage de bande.")
def _(m):
    m.box(1, 2, 0, 63, 30, 6, DARK)
    body = m.box(2, 3, 6, 62, 29, 34, BODY)
    body.decal('front', rect(3, 0.5, 57, 13), Use(M('fixed', 'graphite'), idx=2))
    for k in range(9):
        cs = 6.5 + k * 6
        body.decal('front', disc(cs, 7, 2.7), Use(M('fixed', 'graphite'), idx=4))
        body.decal('front', disc(cs, 7, 1.0), Use(M('fixed', 'steel'), idx=5))
        body.decal('front', rect(cs - 0.5, 10.5, cs + 0.5, 11.5), Fix('led_amber' if k % 4 == 1 else 'led_green'))
    body.decal('front', rect(0, 14, 60, 16), Use(ACC, rel=0))
    body.decal('front', rect(5, 17.5, 55, 26.5), Use(GLASS, idx=2))
    body.decal('front', rect(5, 22, 55, 23), Use(DARK, idx=2))
    body.decal('front', rect(27, 18.5, 32, 22), Use(M('fixed', 'sun'), idx=3))
    body.decal('front', ('fn', lambda s, t: (((s + t) % 13) < 1.5) & (s > 5) & (s < 55) & (t > 17.5) & (t < 26.5)), Use(GLASS, idx=4))
    body.decal('top', rect(8, 6, 52, 20), Use(GLASS, idx=3))
    body.decal('top', rect(8, 12, 52, 14), Use(DARK, idx=2))
    body.decal('top', rect(27, 10, 32, 16), Use(M('fixed', 'sun'), idx=4))
    for f in ('left', 'right'):
        body.decal(f, rect(2, 1, 24, 8), Fix('void'))
        vents(body, f, 4, 22, 14, 24, step=2)
    belt_stub_x(m, 0, 2.5)
    belt_stub_x(m, 61.5, 64)
    stack_light(m, 58, 25, 34, lit='leaf')
    m.box(46, 9, 34, 48, 11, 38, STEEL)
    screen_panel(m, 40, 7, 38, 54, 9, 47, 'ui', seed=7)


@iso_asset('reflow_oven', 'industry_future', 'smt', 'Four de refusion (tunnel)', footprint=(3, 1), facings=FRONT2,
           height=46, usage='future',
           ports=[P('in', 'left', 'in', 0, 0), P('out', 'right', 'out', 2, 0)],
           access=[Access('operate', 2, -1)],
           extra=EXTRA_BELT,
           notes="Tunnel a 6 zones de chauffe : temoins par zone et hublots chauds stylises (aucune flamme).")
def _(m):
    for x in (4, 46, 88):
        m.box(x, 4, 0, x + 4, 7, 8, DARK); m.box(x, 25, 0, x + 4, 28, 8, DARK)
    m.box(3, 4, 6, 93, 28, 8, DARK)
    body = m.box(3, 4, 8, 93, 28, 26, BODY)
    hood = m.box(6, 6, 26, 90, 26, 33, STEEL)
    for k in range(1, 6):
        hood.decal('front', vline(k * 14, 0, 7), Delta(-1))
        hood.decal('top', vline(k * 14, 0, 20), Delta(-1))
    for k in range(6):
        hood.decal('front', rect(6 + k * 14, 3, 8 + k * 14, 5), Fix('led_amber' if k in (2, 3) else 'led_green'))
    for k in range(5):
        x0 = 9 + k * 15
        body.decal('front', rect(x0 - 1, 7, x0 + 7, 12), Delta(-1))
        body.decal('front', rect(x0, 8, x0 + 6, 11), Use(M('fixed', 'coral'), idx=4))
        body.decal('front', rect(x0, 10, x0 + 6, 11), Use(M('fixed', 'sun'), idx=4))
    body.decal('front', rect(0, 14, 90, 15.5), Use(ACC, rel=0))
    panel = m.box(74, 1.5, 10, 90, 4, 24, DARK)
    panel.decal('front', rect(1, 7, 11, 13), Use(M('screen', 'display'), idx=1))
    panel.decal('front', rect(2, 9, 9, 10), Fix((143, 227, 204, 255)))
    panel.decal('front', rect(2, 11, 6, 12), Fix((251, 215, 105, 255)))
    for k in range(3):
        panel.decal('front', rect(12.5, 3 + k * 3, 14.5, 4 + k * 3), Fix(['led_green', 'led_amber', 'led_red'][k]))
    for x in (22, 70):
        m.cyl(x, 16, 2.5, 33, 44, STEEL)
        m.cyl(x, 16, 3.2, 43, 45, STEEL)
    for f in ('left', 'right'):
        body.decal(f, rect(2, 0, 22, 6), Fix('void'))
        vents(body, f, 4, 20, 8, 16, step=2)
    belt_stub_x(m, 0, 3.5)
    belt_stub_x(m, 92.5, 96)


@iso_asset('aoi_inspection', 'industry_future', 'smt', 'Inspection optique automatique', footprint=(1, 1), facings=FRONT2,
           height=44, usage='future',
           ports=[P('in', 'left', 'in'), P('out', 'right', 'out')],
           access=[Access('operate', 0, -1)], extra=EXTRA_BELT,
           notes='Capot camera surmonte d\'un objectif, hublot sur la zone d\'inspection, petit ecran sur bras.')
def _(m):
    m.box(2, 3, 0, 30, 29, 7, DARK)
    body = m.box(3, 4, 7, 29, 28, 24, BODY)
    body.decal('front', rect(2, 2, 24, 12), Use(GLASS, idx=2))
    body.decal('front', rect(7, 4, 17, 6), Use(M('fixed', 'leaf'), idx=3))
    body.decal('front', rect(11, 6, 13, 10), Use(M('fixed', 'sky'), idx=5))
    body.decal('front', rect(0, 13.5, 26, 15), Use(ACC, rel=0))
    for f in ('left', 'right'):
        body.decal(f, rect(2, 0, 22, 6), Fix('void'))
    hood = m.box(8, 8, 24, 24, 24, 34, BODY)
    hood.decal('front', rect(1, 7, 15, 8.5), Use(ACC, rel=0))
    m.cyl(16, 16, 4, 34, 38, DARK)
    m.cyl(16, 16, 2.6, 38, 41, STEEL)
    m.cyl(16, 16, 1.4, 41, 41.6, M('fixed', 'sky'))
    m.box(26, 22, 24, 28, 24, 34, STEEL)
    screen_panel(m, 20, 20, 33, 32, 22, 41, 'ui', seed=11)
    belt_stub_x(m, 0, 3)
    belt_stub_x(m, 29, 32)
    body.decal('front', rect(22, 13.5, 24, 15), Fix('led_green'))


@iso_asset('pcb_tester', 'industry_future', 'test', 'Testeur de cartes (sondes mobiles)', footprint=(2, 1), facings=FRONT2,
           height=44, usage='future',
           ports=[P('in', 'left', 'in', 0, 0), P('out', 'right', 'out', 1, 0)],
           access=[Access('operate', 0, -1)], extra=EXTRA_BELT,
           notes='Baie inferieure, plateau vitre au-dessus : carte verte et sondes visibles par la fenetre.')
def _(m):
    base = m.box(2, 3, 0, 62, 29, 24, BODY)
    base.decal('front', vline(30, 1, 22), Delta(-1))
    base.decal('front', rect(26, 11, 27, 16), Use(STEEL, idx=4))
    base.decal('front', rect(33, 11, 34, 16), Use(STEEL, idx=4))
    vents(base, 'front', 4, 24, 2, 7, step=2)
    vents(base, 'front', 36, 56, 2, 7, step=2)
    for f in ('left', 'right'):
        base.decal(f, rect(2, 7, 24, 13), Fix('void'))
    top = m.box(4, 5, 24, 60, 27, 33, STEEL)
    top.decal('top', rect(3, 3, 53, 19), Use(GLASS, idx=3))
    top.decal('top', rect(10, 6, 44, 16), Use(M('fixed', 'leaf'), idx=3))
    top.decal('top', rect(14, 8, 20, 12), Use(DARK, idx=1))
    top.decal('top', rect(26, 9, 30, 13), Use(DARK, idx=1))
    top.decal('top', rect(3, 4, 53, 5), Use(DARK, idx=3))
    top.decal('top', rect(3, 17, 53, 18), Use(DARK, idx=3))
    top.decal('top', rect(22, 4, 24, 18), Use(DARK, idx=2))
    top.decal('top', rect(38, 4, 40, 18), Use(DARK, idx=2))
    top.decal('front', rect(3, 2, 53, 8), Use(GLASS, idx=2))
    for k in range(4):
        top.decal('front', vline(10 + k * 11, 3, 8), Use(DARK, idx=1))
        top.decal('front', rect(9 + k * 11, 3, 12 + k * 11, 4), Use(M('fixed', 'leaf'), idx=3))
    belt_stub_x(m, 0, 2.5)
    belt_stub_x(m, 61.5, 64)
    stack_light(m, 6.5, 25, 33, lit='sun')
    m.box(58, 24, 33, 60, 26, 39, STEEL)
    screen_panel(m, 50, 22, 38, 62, 24, 46, 'code', seed=9)


@iso_asset('chip_fab_tool', 'industry_future', 'fab', 'Outil de traitement microelectronique (ferme)', footprint=(2, 2),
           facings=FRONT2, height=64, usage='future', occludes=40,
           ports=[P('load_a', 'front', 'inout', 0, 0), P('load_b', 'front', 'inout', 1, 0)],
           access=[Access('operate', 0, -1), Access('operate', 1, -1)],
           extra={'port_rule': PORT_NOTE, 'feed': 'boites de plaquettes (product_wafer) aux deux ports de chargement'},
           notes=("Machine de salle propre fermee (gravure/depot stylise) : module frontal avec 2 ports de chargement, "
                  "ecran tactile sur bras, panneaux de service. Aucun procede reel represente."))
def _(m):
    main = chamfer_box(m, 4, 20, 0, 60, 61, 54, 5, BODY)
    main.decal('front', rect(0, 46, 60, 48), Use(ACC, rel=0))
    main.decal('right', rect(0, 46, 60, 48), Use(ACC, rel=0))
    main.decal('left', rect(0, 46, 60, 48), Use(ACC, rel=0))
    for f in ('right', 'left'):
        main.decal(f, frame(8, 6, 26, 40), Delta(-1))
        main.decal(f, frame(28, 6, 34, 40), Delta(-1))
        vents(main, f, 10, 24, 8, 16, step=2)
    efem = m.box(6, 3, 0, 58, 21, 36, BODY)
    efem.decal('front', rect(0, 32, 52, 34), Use(ACC, rel=0))
    efem.decal('front', rect(4, 2, 48, 3), Delta(-1))
    for x0 in (10, 34):
        efem.decal('front', frame(x0 - 6, 10, x0 + 8, 24), Delta(-1))
        efem.decal('front', rect(x0 - 5, 11, x0 + 7, 23), Delta(-1))
        m.box(x0, 0, 21, x0 + 14, 4, 22.5, DARK)
        foup = m.box(x0 + 1, 0.5, 22.5, x0 + 13, 4.5, 30.5, M('primary', 'plastic'))
        foup.decal('front', rect(1, 1, 11, 7), Use(M('fixed', 'steel'), idx=3))
        foup.decal('front', rect(4, 3, 8, 4), Use(DARK, idx=2))
        m.box(x0 + 5, 1.5, 30.5, x0 + 9, 3.5, 32, DARK)
        efem.decal('front', rect(x0 - 3, 5, x0 - 1, 7), Fix('led_green'))
    m.box(58, 8, 30, 60, 10, 40, STEEL)
    screen_panel(m, 52, 5, 38, 63, 7, 48, 'ui', seed=13)
    m.box(14, 30, 54, 28, 46, 60, BODY_G)
    m.cyl(44, 40, 3, 54, 62, STEEL)
    m.cyl(44, 40, 3.6, 61, 62.5, STEEL)
    m.hcyl('x', 52, 57, 1.5, 10, 56, STEEL)
    stack_light(m, 56, 57, 54, lit='leaf')


@iso_asset('precision_station', 'industry_future', 'fab', 'Station de precision avancee', footprint=(2, 1), facings=FRONT2,
           height=54, usage='future',
           access=[Access('operate', 0, -1), Access('operate', 1, -1)],
           sockets=[Socket('stage', ['product'], 32, 13, 28, 'piece sur la platine')],
           notes="Socle granit sur bati, portique et tete optique sous capot transparent (aretes), console a manette.")
def _(m):
    for (x, y) in ((4, 5), (56, 5), (4, 23), (56, 23)):
        m.box(x, y, 0, x + 3, y + 3, 18, STEEL)
    m.box(7, 6, 6, 56, 7.5, 8, STEEL)
    m.box(7, 24, 6, 56, 25.5, 8, STEEL)
    gran = m.box(2, 3, 18, 62, 29, 26, M('fixed', 'graphite'))
    for f, seed in (('top', 3), ('front', 5), ('right', 7), ('left', 9), ('back', 11)):
        speckle(gran, f, seed, 6, Delta(+1))
        speckle(gran, f, seed + 20, 9, Delta(+2))
    m.box(8, 20, 26, 12, 26, 46, STEEL)
    m.box(52, 20, 26, 56, 26, 46, STEEL)
    br = m.box(8, 18.5, 42, 56, 26, 47, STEEL)
    br.decal('front', rect(2, 2, 46, 3), Delta(-1))
    car = m.box(28, 13, 33, 36, 20, 46, DARK)
    car.decal('front', rect(1, 9, 7, 11), Use(M('fixed', 'sky'), idx=4))
    m.cyl(32, 15, 1.6, 28.5, 33, STEEL)
    m.cyl(32, 15, 2.2, 27.5, 28.5, DARK)
    stage = m.box(20, 7, 26, 44, 19, 28, STEEL)
    stage.decal('top', frame(0, 0, 24, 12), Delta(-1))
    m.cyl(32, 13, 3.5, 28, 28.8, M('fixed', 'sky'))
    G2 = M('glass', 'glass')
    for (x, y) in ((3, 4), (59.8, 4), (3, 26.8), (59.8, 26.8)):
        m.box(x, y, 26, x + 1.2, y + 1.2, 52, G2)
    m.box(3, 4, 51, 61, 5.2, 52.2, G2)
    m.box(3, 26.8, 51, 61, 28, 52.2, G2)
    m.box(3, 4, 51, 4.2, 28, 52.2, G2)
    m.box(59.8, 4, 51, 61, 28, 52.2, G2)
    con = m.box(46, 3.5, 26, 58, 9, 29, DARK)
    con.decal('top', rect(1, 1, 3, 3), Fix('led_green'))
    m.cyl(52, 6, 0.6, 29, 32, DARK)
    m.ell(52, 6, 32.5, 1.2, 1.2, 1.0, M('fixed', 'red'))
    m.box(5, 22, 26, 7, 24, 33, STEEL)
    screen_panel(m, 1, 21, 33, 13, 23, 41, 'ui', seed=21)


@iso_asset('flash_station', 'industry_future', 'test', "Station d'installation / flash logiciel", footprint=(2, 1),
           facings=ALL4, height=48, usage='future',
           sockets=[Socket('chair', ['chair'], 32, -12, 0, 'operateur', facing_rel='opposite', group='workstation')],
           access=[Access('work', 0, -1), Access('work', 1, -1)],
           ports=[P('in', 'left', 'in', 0, 0), P('out', 'right', 'out', 1, 0)], extra=EXTRA_BELT,
           notes="Bacs d'appareils raccordes a un concentrateur, terminal (ecran de code) et unite centrale.")
def _(m):
    bench_frame(m, top_mat=M('secondary', 'steel'))
    t1 = m.box(6, 5, 30, 26, 15, 31.5, DARK)
    t1.decal('top', ('fn', lambda s, t: ((np.floor(s) % 4) < 2.5) & ((np.floor(t) % 5) < 3.5) & (s > 0.8) & (s < 19.5) & (t > 0.8) & (t < 9.5)),
             Use(M('fixed', 'graphite'), idx=4))
    t1.decal('top', ('fn', lambda s, t: ((np.floor(s) % 4) == 1) & ((np.floor(t) % 5) == 3) & (s < 19.5) & (t < 9.5)), Fix('led_green'))
    t2 = m.box(28, 5, 30, 42, 15, 31.5, DARK)
    t2.decal('top', ('fn', lambda s, t: ((np.floor(s) % 5) < 3.5) & ((np.floor(t) % 5) < 3.5) & (s > 0.8) & (s < 13.5) & (t > 0.8) & (t < 9.5)),
             Use(M('fixed', 'leaf'), idx=3))
    t2.decal('top', ('fn', lambda s, t: ((np.floor(s) % 5) == 2) & ((np.floor(t) % 5) == 1) & (s < 13.5) & (t < 9.5)), Fix('led_amber'))
    hub = m.box(24, 21, 30, 38, 27, 34, BODY)
    for k in range(6):
        hub.decal('front', rect(1.5 + k * 2, 1.5, 2.5 + k * 2, 2.5), Fix('led_green' if k != 4 else 'led_amber'))
    for (x, col) in ((12, 'sky'), (18, 'coral'), (33, 'sun'), (38, 'sky')):
        rbox(m, (x + 31) / 2.0, 18, abs(31 - x) + 6, 0.8, math.degrees(math.atan2(3, (31 - x) or 1)), 30, 30.8,
             M('fixed', col), outline=False)
    m.box(51, 23, 30, 53, 25, 34, STEEL)
    screen_panel(m, 44, 22, 34, 60, 24, 46, 'code', seed=17)
    kb = m.box(45, 7, 30, 58, 12, 31.2, DARK)
    kb.decal('top', ('fn', lambda s, t: ((np.floor(t) % 2) == 1) & ((np.floor(s) % 2) == 0) & (s > 0.5) & (s < 12.5)), Delta(+2))
    tw = m.box(51, 10, 0, 59, 25, 20, DARK)
    tw.decal('front', rect(3.5, 16, 4.5, 17), Fix('led_blue'))
    vents(tw, 'front', 1, 7, 3, 12, step=2)
    roller_ledge_x(m, 0, 5)
    roller_ledge_x(m, 59, 64)


@iso_asset('stress_test_rack', 'industry_future', 'test', 'Banc de stress-test (rodage)', footprint=(2, 1), facings=FRONT2,
           height=76, usage='future', occludes=40,
           access=[Access('maintenance', 0, -1), Access('maintenance', 1, -1)],
           notes=("Etagere ouverte de cartes en rodage : ventilateurs et temoins visibles. Volontairement differente "
                  "d'une baie serveur (cadre ouvert, cartes exposees)."))
def _(m):
    for x in (2, 60):
        for y in (3, 27):
            p = m.box(x, y, 0, x + 2, y + 2, 76, STEEL)
            perforation(p, 'sides', 0, 3, 2, 74, step=3, paint=Delta(-1))
    for i, z in enumerate((3, 21, 39, 57)):
        sh = m.box(2, 3, z, 62, 29, z + 1.5, STEEL)
        for k in range(4):
            x = 4 + k * 14.5
            u = m.box(x, 6, z + 1.5, x + 12.5, 25, z + 15, DARK)
            fan_disc(u, 'front', 6.25, 6.8, 4.6)
            u.decal('front', rect(0, 12, 12.5, 13.5), Use(M('fixed', 'leaf'), idx=3))
            colors = ['led_green', 'led_green', 'led_amber', 'led_green', 'led_red', 'led_green']
            u.decal('front', rect(11, 2, 12, 3), Fix(colors[(i * 4 + k) % 6]))
            u.decal('front', rect(11, 4.5, 12, 5.5), Fix(colors[(i * 4 + k + 2) % 6]))
            u.decal('top', ('fn', lambda s, t: (np.floor(s) % 3) == 0), Delta(+1))
    top = m.box(2, 3, 74, 62, 29, 76, STEEL)
    ps = m.box(62, 10, 6, 63.5, 20, 70, BODY_G)
    for k in range(8):
        ps.decal('right', rect(4, 6 + k * 8, 6, 7 + k * 8), Fix('led_green'))
        ps.decal('front', rect(0.3, 6 + k * 8, 1.2, 7 + k * 8), Fix('led_green'))
    m.cyl(61, 27.5, 1.3, 3, 72, M('fixed', 'graphite'))
    m.cyl(3, 27.5, 1.3, 3, 72, M('fixed', 'graphite'))


@iso_asset('packing_station', 'industry_future', 'logistics', "Poste d'emballage et d'etiquetage", footprint=(2, 1),
           facings=FRONT2, height=48, usage='future',
           access=[Access('work', 0, -1), Access('work', 1, -1)],
           ports=[P('in', 'left', 'in', 0, 0), P('out', 'right', 'out', 1, 0)],
           sockets=[Socket('table_center', ['product', 'box'], 26, 15, 30)], extra=EXTRA_BELT,
           notes="Table bois : carton ouvert, devidoir, imprimante d'etiquettes, rouleau de calage ; cartons plats dessous.")
def _(m):
    for x in (2, 59):
        m.box(x, 3, 0, x + 3, 6, 27, STEEL); m.box(x, 25, 0, x + 3, 28, 27, STEEL)
    sh = m.box(3, 4, 6, 61, 28, 7.5, STEEL)
    m.box(1, 2, 27, 63, 30, 30, WOODP)
    flat = m.box(8, 6, 7.5, 32, 26, 12, M('fixed', 'kraft'))
    for f in ('front', 'right', 'left', 'back'):
        flat.decal(f, ('fn', lambda s, t: (np.floor(t) % 2) == 1), Delta(-1))
    bx = m.box(16, 9, 30, 32, 23, 38, KRAFT)
    bx.decal('top', rect(1, 1, 15, 13), Delta(-2))
    m.slab_tilt(16, 32, 9, 38, 5.5, 44, 0.8, KRAFT)
    m.slab_tilt(16, 32, 23, 38, 26, 44, 0.8, KRAFT)
    m.box(36, 14, 30, 42, 19, 33, DARK)
    m.hcyl('x', 16.5, 34.5, 2, 36.5, 41.5, M('fixed', 'sand'))
    lp = m.box(46, 18, 30, 56, 27, 36, M('secondary', 'plastic'))
    lp.decal('front', rect(2, 2, 8, 3), Use(DARK, idx=1))
    lp.decal('top', rect(1, 1, 4, 3), Fix('led_green'))
    m.box(47.5, 15.5, 30, 54.5, 18, 30.5, M('fixed', 'cream'), outline=False)
    m.box(5, 26, 30, 6.5, 27.5, 44, STEEL)
    m.box(29.5, 26, 30, 31, 27.5, 44, STEEL)
    roll = m.hcyl('x', 26.5, 42, 3, 5.5, 30.5, M('fixed', 'sky'))
    roll.decal('side', ('fn', lambda s, t: ((np.floor(s) % 2) == 0) & ((np.floor(t) % 2) == 0)), Use(M('fixed', 'sky'), idx=5))
    b1 = m.box(54, 4, 30, 62, 12, 36, KRAFT)
    b1.decal('top', rect(3, 0, 5, 8), Use(M('fixed', 'sand'), idx=5))
    b2 = m.box(55, 5, 36, 61, 11, 40.5, KRAFT)
    b2.decal('top', rect(2, 0, 4, 6), Use(M('fixed', 'sand'), idx=5))
    roller_ledge_x(m, 0, 5)
    roller_ledge_x(m, 59, 64)


@iso_asset('storage_rack_industrial', 'industry_future', 'logistics', 'Rack de stockage a palettes', footprint=(2, 1),
           facings=FRONT2, height=86, usage='future', occludes=40,
           access=[Access('load', 0, -1), Access('load', 1, -1)],
           sockets=[Socket('level_0', ['pallet', 'tote', 'box'], 32, 16, 0),
                    Socket('level_1', ['pallet', 'tote', 'box'], 32, 16, 30),
                    Socket('level_2', ['pallet', 'tote', 'box'], 32, 16, 58)],
           notes='Echelles bleu acier, lisses orange, platelages grillages ; chargement par defaut dessine.')
def _(m):
    BEAM = M('accent', 'coral')
    for x in (1, 61):
        for y in (2, 27):
            p = m.box(x, y, 0, x + 2.5, y + 2.5, 86, NAVY_M)
            perforation(p, 'sides', 0.5, 2.5, 2, 84, step=3, paint=Delta(+1))
        for z in (12, 42, 72):
            m.box(x + 0.5, 4.5, z, x + 2, 27, z + 1, NAVY_M)
    for z in (28, 56):
        for y in (2, 27):
            m.box(3.5, y, z, 61, y + 2.5, z + 3, BEAM)
        dk = m.box(3.5, 4.5, z + 1, 61, 27, z + 2, M('fixed', 'steel'))
        mesh(dk, 'top', 3, Delta(-1))
    for y in (2, 27):
        m.box(3.5, y, 83, 61, y + 2.5, 86, BEAM)
    W_ = M('wood', 'birch')
    m.box(6, 6, 0, 30, 26, 3, W_)
    l0 = m.box(7, 7, 3, 29, 25, 19, M('fixed', 'kraft'))
    l0.decal('sides', vline(11, 0, 16), Delta(-1)); l0.decal('sides', hline(0, 22, 8), Delta(-1))
    l0.decal('top', rect(10, 0, 12, 18), Use(M('fixed', 'sand'), idx=5))
    for k in range(2):
        tb = m.box(35, 8, 0 + k * 10, 57, 24, 10 + k * 10, M('fixed', 'teal'))
        tb.decal('top', rect(1, 1, 21, 15), Delta(-2))
        tb.decal('sides', rect(8, 6, 14, 8), Delta(-2))
    m.box(6, 6, 31, 30, 26, 34, W_)
    l1 = m.box(7, 8, 34, 28, 24, 48, M('fixed', 'kraft'))
    l1.decal('top', rect(9, 0, 11, 16), Use(M('fixed', 'sand'), idx=5))
    l1b = m.box(36, 7, 31, 56, 24, 44, M('fixed', 'sky'))
    l1b.decal('sides', rect(2, 4, 10, 9), Use(M('fixed', 'cream'), idx=5))
    for k, (x, ramp) in enumerate(((8, 'kraft'), (21, 'kraft'), (40, 'sun'))):
        bb = m.box(x, 8, 59, x + 11, 22, 70 - (k % 2) * 3, M('fixed', ramp))
        bb.decal('top', rect(4.5, 0, 6.5, 14), Use(M('fixed', 'sand'), idx=5))


@iso_asset('pallet', 'industry_future', 'logistics', 'Palette de manutention', footprint=(1, 1), facings=FRONT2, height=6,
           usage='future',
           sockets=[Socket('load', ['product', 'tote', 'box'], 16, 16, 5.5, 'charge posee sur la palette')],
           notes='Palette bois vide (planches, des) ; la charge se pose via le socket load.')
def _(m):
    W_ = M('wood', 'birch')
    for y in (4, 14.5, 25):
        m.box(4, y, 0, 28, y + 3, 1.5, W_)
    for x in (4, 14.5, 25):
        for y in (4, 14.5, 25):
            b = m.box(x, y, 1.5, x + 3, y + 3, 4, W_)
            b.decal('sides', rect(0, 0, 3, 1), Delta(-1))
    for x in (4, 9, 14, 19, 24):
        p = m.box(x, 3.5, 4, x + 4, 28.5, 5.5, W_)
        p.decal('top', rect(1.5, 2, 2.5, 3), Use(DARK, idx=2))
        p.decal('top', rect(1.5, 22, 2.5, 23), Use(DARK, idx=2))


@iso_asset('tote_bin', 'industry_future', 'logistics', 'Bac de manutention', centered=True, size_su=(20, 14), facings=FRONT2,
           height=10, usage='future', provides=['tote', 'product'],
           notes='Bac plastique ouvert, poignees evidees ; empilable (+10 px).')
def _(m):
    b = m.box(-10, -7, 0, 10, 7, 9, M('primary', 'teal'))
    b.decal('top', rect(1, 1, 19, 13), Delta(-2))
    b.decal('top', rect(1, 1, 19, 2), Delta(-3))
    for f, w in (('front', 20), ('back', 20), ('left', 14), ('right', 14)):
        b.decal(f, rect(w / 2 - 3, 5.5, w / 2 + 3, 7), Delta(-2))
        b.decal(f, rect(0, 2, w, 2.8), Delta(-1))
    m.box(-10.5, -7.5, 8.5, 10.5, 7.5, 10, M('primary', 'teal'), shadow=False)
    rim = m.prims[-1]
    rim.decal('top', rect(1.2, 1.2, 19.8, 13.8), Delta(-2))


@iso_asset('dust_extraction', 'industry_future', 'utility', "Centrale d'aspiration / filtration", footprint=(1, 1),
           facings=FRONT2, height=74, usage='future', occludes=40,
           access=[Access('maintenance', 0, -1)],
           notes='Deux cartouches filtrantes, bacs de collecte, moteur et gaine verticale vers le plafond.')
def _(m):
    m.box(3, 4, 0, 29, 28, 5, DARK)
    for x in (10, 22):
        m.cyl(x, 16, 4.6, 5, 14, M('fixed', 'graphite'))
        f = m.cyl(x, 16, 5.5, 14, 45, BODY)
        for t in (8, 16, 24):
            f.decal('side', rect(0, t, 360, t + 1), Delta(-1))
        f.decal('side', rect(150, 24, 200, 27), Use(M('fixed', 'cream'), idx=5))
        m.cyl(x, 16, 6, 44, 46.5, STEEL)
    mot = m.box(5, 8, 46.5, 27, 24, 56, ACC)
    vents(mot, 'front', 3, 19, 2, 7, step=2)
    vents(mot, 'right', 3, 13, 2, 7, step=2)
    m.cyl(16, 16, 3, 56, 74, STEEL)
    for z in (60, 68):
        m.cyl(16, 16, 3.5, z, z + 1.2, STEEL)
    m.hcyl('x', 16, 32, 2.5, 26, 32, STEEL)
    cb = m.box(25, 23, 18, 30, 27, 30, STEEL)
    cb.decal('front', rect(1.5, 8, 2.5, 9), Fix('led_green'))
    cb.decal('front', rect(1.5, 5, 2.5, 6), Fix('led_amber'))


@iso_asset('industrial_chiller', 'industry_future', 'utility', 'Groupe de refroidissement (chiller)', footprint=(2, 1),
           facings=FRONT2, height=34, usage='future',
           access=[Access('maintenance', 0, -1)],
           notes='Deux ventilateurs sur le dessus, persiennes, panneau de commande, departs eau froide/chaude a l\'arriere.')
def _(m):
    m.box(2, 3, 0, 62, 29, 2, DARK)
    body = m.box(2, 3, 2, 62, 29, 32, BODY)
    vents(body, 'front', 22, 58, 3, 26, step=2)
    body.decal('front', rect(3, 12, 17, 24), Use(M('secondary', 'graphite'), idx=2))
    body.decal('front', rect(4, 18, 16, 23), Use(M('screen', 'display'), idx=1))
    body.decal('front', rect(5, 20, 12, 21), Fix((143, 227, 204, 255)))
    body.decal('front', rect(5, 14, 6, 15), Fix('led_green'))
    body.decal('front', rect(8, 14, 9, 15), Fix('led_green'))
    body.decal('front', rect(0, 27, 60, 28.5), Use(ACC, rel=0))
    for f in ('right', 'left'):
        vents(body, f, 3, 23, 3, 24, step=2)
    fan_disc(body, 'top', 15, 13, 9.5)
    fan_disc(body, 'top', 45, 13, 9.5)
    for (x, ramp) in ((20, 'red'), (30, 'sky')):
        m.hcyl('y', x, 10, 1.6, 29, 35, M('fixed', ramp))
        m.box(x - 2, 31, 8, x + 2, 33, 12, STEEL)


@iso_asset('control_cabinet', 'industry_future', 'utility', 'Armoire de controle', footprint=(1, 1), facings=FRONT2,
           height=76, usage='future', occludes=40,
           access=[Access('operate', 0, -1)],
           notes='Armoire acier : voyants, petit ecran IHM, poignee, etiquette, presse-etoupe et goulotte en haut.')
def _(m):
    m.box(4, 8, 0, 28, 26, 4, DARK)
    cab = m.box(4, 8, 4, 28, 26, 70, M('primary', 'steel'))
    cab.decal('front', frame(1, 1, 23, 65), Delta(-1))
    cab.decal('front', vline(12, 2, 64), Delta(-1))
    for k, col in enumerate(('led_red', 'led_amber', 'led_green')):
        cab.decal('front', disc(4.5 + k * 3.5, 56, 1.4), Use(DARK, idx=1))
        cab.decal('front', disc(4.5 + k * 3.5, 56, 0.9), Fix(col))
    cab.decal('front', rect(14, 52, 21, 60), Use(M('screen', 'display'), idx=1))
    cab.decal('front', rect(15, 57, 20, 58), Fix((143, 227, 204, 255)))
    cab.decal('front', rect(15, 54, 18, 55), Fix((251, 215, 105, 255)))
    cab.decal('front', rect(10, 28, 11, 36), Use(STEEL, idx=5))
    cab.decal('front', rect(13, 28, 14, 36), Use(STEEL, idx=5))
    label(cab, 'front', 3, 42, 7, 4, 1)
    stamp(cab, 'front', ['..s..', '.sKs.', 'sssss'], {'s': Use(M('fixed', 'hazard'), idx=4), 'K': Fix('ink')},
          s0=15, t_top=46)
    vents(cab, 'front', 3, 21, 3, 10, step=2)
    vents(cab, 'right', 3, 15, 3, 10, step=2)
    m.box(10, 12, 70, 22, 22, 76, DARK)
    m.box(14, 14, 76, 18, 20, 78, DARK)


@iso_asset('cleanroom_airlock', 'industry_future', 'cleanroom', 'Sas de salle propre (douche a air)', footprint=(1, 2),
           facings=FRONT2, height=90, usage='future', occludes=40,
           access=[Access('enter', 0, -1), Access('exit', 0, 2, 'same')],
           notes="Cabine traversante : portes vitrees avant/arriere, grilles de soufflage laterales, caisson filtrant.")
def _(m):
    booth = m.box(1, 2, 0, 31, 62, 84, BODY)
    for f in ('front', 'back'):
        booth.decal(f, rect(5, 0, 25, 72), Delta(-1))
        booth.decal(f, frame(5, 0, 25, 72), Use(BODY_G, rel=0))
        booth.decal(f, rect(9, 38, 21, 66), Use(GLASS, idx=2))
        booth.decal(f, ('fn', lambda s, t: (((s + t) % 11) < 1.5) & (s > 9) & (s < 21) & (t > 38) & (t < 66)), Use(GLASS, idx=4))
        booth.decal(f, rect(21, 28, 22.5, 36), Use(M('metal', 'steel'), idx=5))
        booth.decal(f, rect(12, 76, 14, 78), Fix('led_green'))
        booth.decal(f, rect(16, 76, 18, 78), Fix('led_red'))
    for f in ('right', 'left'):
        perforation(booth, f, 8, 52, 18, 70, step=2)
        booth.decal(f, frame(7, 17, 53, 71), Delta(-1))
    hepa = m.box(3, 6, 84, 29, 58, 90, BODY_G)
    perforation(hepa, 'top', 2, 24, 4, 48, step=2)
    hepa.decal('front', rect(2, 2, 6, 3), Fix('led_green'))
    m.box(4, 0, 0, 28, 2, 1.5, DARK)
    m.box(4, 62, 0, 28, 64, 1.5, DARK)


@iso_asset('cleanroom_partition', 'industry_future', 'cleanroom', 'Cloison de salle propre', footprint=(1, 1),
           facings=FRONT2, height=86, usage='future', occludes=40, collision=[(0, 0)],
           notes="Panneau blanc sur toute la largeur de case (raboutable), fenetre affleurante, plinthe arrondie.")
def _(m):
    m.box(0, 12, 0, 32, 20, 4, BODY_G)
    pnl = m.box(0, 13, 4, 32, 19, 84, BODY)
    for f in ('front', 'back'):
        pnl.decal(f, vline(0, 0, 80), Delta(-1))
        pnl.decal(f, rect(4, 38, 28, 68), Use(BODY_G, rel=0))
        pnl.decal(f, rect(5, 39, 27, 67), Use(GLASS, idx=2))
        pnl.decal(f, ('fn', lambda s, t: (((s + t) % 12) < 1.5) & (s > 5) & (s < 27) & (t > 39) & (t < 67)), Use(GLASS, idx=4))
    m.box(0, 12.5, 84, 32, 19.5, 86, BODY_G)


# ---------------------------------------------------------------------------
# Produits (intermediaires et finis) : objets poses, centre = point de pose
# ---------------------------------------------------------------------------
PCB = M('fixed', 'leaf')
GOLD = M('fixed', 'brass')
PROD = dict(usage='future', provides=['product'], model_kw={'shadow_k': 0.3})


@iso_asset('product_pcb', 'industry_future', 'product', 'Carte electronique', centered=True, size_su=(14, 10), facings=FRONT2,
           height=3, **PROD)
def _(m):
    b = m.box(-7, -5, 0, 7, 5, 1, PCB)
    b.decal('top', rect(0, 0, 14, 10), Use(PCB, idx=2))
    b.decal('top', rect(1, 7, 13, 8), Use(PCB, idx=4))
    b.decal('top', rect(8, 2, 9, 7), Use(PCB, idx=4))
    b.decal('top', rect(0, 0, 14, 1.5), Use(GOLD, idx=4))
    b.decal('top', rect(10, 8, 12, 10), Use(GOLD, idx=4))
    m.box(-4, -2, 1, 0, 2, 2, DARK)
    m.box(2.5, 1, 1, 5, 3, 1.8, DARK)
    m.cyl(3.5, -2.5, 0.9, 1, 2.6, M('fixed', 'sky'))


@iso_asset('product_cpu', 'industry_future', 'product', 'Processeur (microcomposant)', centered=True, size_su=(10, 10),
           facings=ONE, symmetric=True, height=2, **PROD)
def _(m):
    s = m.box(-5, -5, 0, 5, 5, 1, PCB)
    s.decal('top', rect(0, 0, 10, 10), Use(PCB, idx=1))
    s.decal('top', rect(0, 0, 1.5, 1.5), Use(GOLD, idx=5))
    lid = m.box(-3.5, -3.5, 1, 3.5, 3.5, 2, STEEL)
    lid.decal('top', frame(0, 0, 7, 7), Delta(+1))


@iso_asset('product_ram', 'industry_future', 'product', 'Module memoire', centered=True, size_su=(16, 4), facings=FRONT2,
           height=6, **PROD)
def _(m):
    st = m.box(-8, -1, 0, 8, 1, 6, PCB)
    for k in range(4):
        st.decal('sides', rect(1 + k * 3.7, 1.5, 3.6 + k * 3.7, 4.5), Use(DARK, idx=1))
    st.decal('sides', rect(0, 0, 16, 1), Use(GOLD, idx=4))
    st.decal('top', rect(0, 0, 16, 2), Use(PCB, idx=3))


@iso_asset('product_gpu', 'industry_future', 'product', 'Carte graphique', centered=True, size_su=(20, 8), facings=FRONT2,
           height=5, **PROD)
def _(m):
    pcb = m.box(-9, -4, 0, 9, 4, 1, PCB)
    pcb.decal('front', rect(4, 0, 12, 1), Use(GOLD, idx=4))
    sh = m.box(-8.5, -3.5, 1, 8.5, 3.5, 4, M('primary', 'graphite'))
    fan_disc(sh, 'top', 4.5, 3.5, 3.1)
    fan_disc(sh, 'top', 12.5, 3.5, 3.1)
    sh.decal('front', rect(0, 1, 17, 2), Use(ACC, rel=0))
    m.box(-10, -4, 0, -9, 4, 5, STEEL)


@iso_asset('product_case', 'industry_future', 'product', 'Boitier vide', centered=True, size_su=(10, 18), facings=FRONT2,
           height=21, **PROD)
def _(m):
    c = m.box(-5, -9, 1, 5, 9, 21, M('primary', 'graphite'))
    for f in ('left', 'right'):
        c.decal(f, rect(1, 1, 17, 19), Use(DARK, idx=0))
        c.decal(f, rect(3, 4, 14, 17), Use(DARK, idx=2))
        for (ss, tt) in ((4, 5), (12, 5), (4, 15), (12, 15), (8, 10)):
            c.decal(f, rect(ss, tt, ss + 1, tt + 1), Use(M('fixed', 'steel'), idx=4))
        c.decal(f, rect(1, 1, 17, 2), Use(DARK, idx=3))
        c.decal(f, rect(15, 2, 16, 18), Use(DARK, idx=3))
    c.decal('front', ('fn', lambda s, t: ((np.floor(s) + np.floor(t)) % 2 == 0) & (t > 2) & (t < 16)), Delta(-1))
    for (x, y) in ((-4.5, -8.5), (3.5, -8.5), (-4.5, 7.5), (3.5, 7.5)):
        m.box(x, y, 0, x + 1, y + 1, 1, DARK)


@iso_asset('product_pc_assembled', 'industry_future', 'product', 'Ordinateur assemble', centered=True, size_su=(10, 18),
           facings=FRONT2, height=21, **PROD)
def _(m):
    c = m.box(-5, -9, 1, 5, 9, 21, M('primary', 'graphite'))
    for f in ('left', 'right'):
        c.decal(f, rect(1.5, 1.5, 16.5, 18.5), Use(GLASS, idx=1))
        c.decal(f, rect(3, 8, 14, 17), Use(PCB, idx=2))
        c.decal(f, rect(3, 5, 15, 7), Use(M('fixed', 'sky'), idx=3))
        c.decal(f, disc(12, 14, 2), Use(DARK, idx=2))
    c.decal('front', rect(4.5, 17, 5.5, 18), Fix('led_blue'))
    c.decal('front', ('fn', lambda s, t: ((np.floor(s) + np.floor(t)) % 2 == 0) & (t > 2) & (t < 14)), Delta(-1))
    for (x, y) in ((-4.5, -8.5), (3.5, -8.5), (-4.5, 7.5), (3.5, 7.5)):
        m.box(x, y, 0, x + 1, y + 1, 1, DARK)


@iso_asset('product_box', 'industry_future', 'product', 'Carton de produit', centered=True, size_su=(16, 12), facings=FRONT2,
           height=10, usage='future', provides=['product', 'box'], model_kw={'shadow_k': 0.3})
def _(m):
    b = m.box(-8, -6, 0, 8, 6, 10, KRAFT)
    b.decal('top', rect(0, 5, 16, 7), Use(M('fixed', 'sand'), idx=5))
    for f in ('left', 'right'):
        b.decal(f, rect(5, 6, 7, 10), Use(M('fixed', 'sand'), idx=5))
    label(b, 'front', 2, 2, 7, 5, 2)
    stamp(b, 'front', ['.k..k.', 'kkkkkk', '.k..k.', '.k..k.'],
          {'k': Use(M('fixed', 'graphite'), idx=2)}, s0=10, t_top=8)


@iso_asset('product_wafer', 'industry_future', 'product', 'Plaquette de silicium', centered=True, size_su=(14, 14),
           facings=ONE, symmetric=True, height=1, **PROD)
def _(m):
    w = m.cyl(0, 0, 7, 0, 0.8, M('fixed', 'sky'))
    w.decal('top', ('fn', lambda s, t: ((np.floor(s) % 2) == 0) | ((np.floor(t) % 2) == 0)), Use(M('fixed', 'navy'), idx=4))
    w.decal('top', ('fn', lambda s, t: np.abs((s - t)) < 1.6), Use(M('fixed', 'screen'), idx=4))
    w.decal('top', ('fn', lambda s, t: np.abs((s - t) - 3.5) < 0.8), Use(M('fixed', 'sky'), idx=5))
    w.decal('top', rect(6, 0, 8, 1), Use(M('fixed', 'graphite'), idx=2))
