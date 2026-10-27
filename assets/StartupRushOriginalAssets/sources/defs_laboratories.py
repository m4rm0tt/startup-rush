"""Laboratoires et metiers tech (famille laboratories).

Etablis : plateau a 30 px (comme les bureaux), sockets 'bench_*' (objets d'etabli)
et 'shelf_*' (etagere haute), socket 'chair' (poste de travail).
Objets d'etabli : modeles poses (centered=True), provides ['bench_item', 'desk_item'].
Zone quantique : cryostat ouvert (etages dores suspendus), cryostat ferme, baie de
controle micro-ondes, unite de support cryogenique. Aucun cristal, aucun neon.
"""
import math
import numpy as np

from srgen.iso import Fix, Use, Delta, rect, hline, vline, frame, disc, TILE
from srgen.palette import M, RAMPS
from srgen.registry import iso_asset, Socket, Access, ALL4, FRONT2, ONE
from srgen.shapes import chamfer_box, code_screen, ui_screen, led, vents, rbox
from srgen.lot_dc_shapes import (strut, tripod, fan_grill, drive_bays, port_row, perforation, mesh, waveform,
                                 digits, knobs, hazard_triangle, ramp_col)

BENCH_H = 30
GRAPH = M('metal', 'graphite')
STEEL = M('metal', 'steel')
BRASS = M('metal', 'brass')
COPPER = M('metal', 'copper')
BODY_L = M('primary', 'plastic')
BODY_D = M('primary', 'graphite')
BODY_S = M('primary', 'steel')
ACC = M('accent', 'teal')
SUN = M('accent', 'sun')
CORAL = M('accent', 'coral')
SCREEN = M('screen', 'display')
LCD = M('screen', 'screen')
GLASS = M('glass', 'glass')
WOOD = M('wood', 'birch')
PCB = M('fixed', 'leaf')
ITEM = ['bench_item', 'desk_item']
ITEM_ACCEPT = ['bench_item', 'desk_item', 'instrument']


def bench_sockets(w_su, depth=32, top=BENCH_H, shelf_z=None, chair=True):
    s = [Socket('bench_left', ITEM_ACCEPT, w_su * 0.22, 12, top, 'gauche du plan'),
         Socket('bench_center', ITEM_ACCEPT, w_su * 0.5, 14, top, 'centre du plan'),
         Socket('bench_right', ITEM_ACCEPT, w_su * 0.78, 12, top, 'droite du plan')]
    if shelf_z:
        s += [Socket('shelf_left', ITEM_ACCEPT, w_su * 0.25, depth - 7, shelf_z, 'etagere haute gauche'),
              Socket('shelf_right', ITEM_ACCEPT, w_su * 0.75, depth - 7, shelf_z, 'etagere haute droite')]
    if chair:
        s.append(Socket('chair', ['chair'], w_su * 0.5, -12, 0, 'technicien', facing_rel='opposite', group='workstation'))
    return s


def bench_frame(m, w, top_mat, top_z=BENCH_H, shelf=True, leg_mat=STEEL):
    top = m.box(1, 2, top_z - 3, w - 1, 30, top_z, top_mat)
    top.decal('front', rect(0, 0, w, 1), Delta(-1))
    for x in (3, w - 5):
        m.box(x, 4, 0, x + 2, 6, top_z - 3, leg_mat)
        m.box(x, 26, 0, x + 2, 28, top_z - 3, leg_mat)
    if shelf:
        m.box(3, 4, 7, w - 3, 28, 8.5, leg_mat)
    return top


def screen_quad(x0, x1, y, z0, z1, name='screen'):
    return {'name': name, 'quad': [(x0, y, z1), (x1, y, z1), (x1, y, z0), (x0, y, z0)]}


# ---------------------------------------------------------------------------
# Etablis et postes
# ---------------------------------------------------------------------------
@iso_asset('workbench_electronics', 'laboratories', 'bench', 'Etabli electronique', footprint=(2, 1), facings=ALL4,
           height=72, sockets=bench_sockets(64, shelf_z=58), access=[Access('work', 0, -1), Access('work', 1, -1)],
           tags=['workstation', 'lab'],
           notes='Tapis antistatique, panneau perfore avec outils, etagere a bacs, bandeau de prises.')
def _(m):
    top = bench_frame(m, 64, BODY_L)
    top.decal('top', rect(4, 3, 50, 22), Use(M('fabric', 'teal'), idx=3))
    top.decal('top', frame(4, 3, 50, 22), Use(M('fabric', 'teal'), idx=2))
    top.decal('top', rect(46, 18, 48, 20), Use(STEEL, idx=4))
    peg = m.box(2, 27, BENCH_H, 62, 29, 72, M('secondary', 'kraft'))
    perforation(peg, 'front', 1, 2, 59, 40, step=3, delta=-1)
    peg.decal('back', frame(0, 0, 60, 42, 2, 2), Delta(-1))
    for sb in (15, 30, 45):
        peg.decal('back', rect(sb, 2, sb + 2, 40), Delta(+1))
    peg.decal('back', rect(2, 20, 58, 22), Delta(+1))
    # outils suspendus (silhouettes)
    tools = [(6, 26, 'coral'), (10, 28, 'sun'), (14, 25, 'sky')]
    for (s, t, col) in tools:
        peg.decal('front', rect(s, t, s + 1, t + 9), Fix(ramp_col('steel', 4)))
        peg.decal('front', rect(s - 0.5, t + 7, s + 1.5, t + 12), Fix(ramp_col(col, 3)))
    peg.decal('front', ('fn', lambda s, t: (np.abs(np.hypot(s - 24, t - 31) - 3.2) < 0.7)), Fix(ramp_col('copper', 3)))
    peg.decal('front', rect(32, 24, 33, 36), Fix(ramp_col('graphite', 3)))
    peg.decal('front', rect(34, 24, 35, 36), Fix(ramp_col('graphite', 3)))
    peg.decal('front', rect(32, 34, 35, 38), Fix(ramp_col('coral', 3)))
    peg.decal('front', rect(40, 27, 48, 33), Fix(ramp_col('sun', 3)))
    peg.decal('front', rect(41, 28, 47, 32), Fix(ramp_col('graphite', 1)))
    strip = m.box(6, 26, BENCH_H + 2, 58, 27, BENCH_H + 6, BODY_L)
    for k in range(6):
        strip.decal('front', rect(3 + k * 8, 1, 5 + k * 8, 3), Fix('ink'))
    led(strip, 'front', 50, 1.5, 'led_red', 2, 1)
    # etagere haute + bacs
    m.box(2, 20, 56, 62, 29, 58, BODY_L)
    for x in (4, 58):
        m.box(x, 26, 48, x + 2, 27, 56, STEEL)
    for k, col in enumerate(['sun', 'coral', 'teal', 'sky']):
        b = m.box(6 + k * 13, 21, 58, 16 + k * 13, 28, 63, M('accent', col))
        b.decal('front', rect(1, 3, 9, 5), Delta(-2))
        b.decal('top', rect(1, 1, 9, 6), Delta(-2))


@iso_asset('test_bench_pc', 'laboratories', 'bench', 'Banc de test PC', footprint=(2, 1), facings=ALL4, height=52,
           sockets=bench_sockets(64)[:1] + [Socket('chair', ['chair'], 32, -12, 0, '', facing_rel='opposite', group='workstation')],
           screens=[screen_quad(43, 59, 23.5, 36, 50)], access=[Access('work', 0, -1), Access('work', 1, -1)],
           tags=['workstation', 'lab'],
           notes='PC a cadre ouvert (carte mere, ventirad, carte graphique, alimentation), ecran de benchmark, multimetre.')
def _(m):
    bench_frame(m, 64, BODY_D)
    tray = m.box(5, 9, BENCH_H, 29, 27, BENCH_H + 2, STEEL)
    for (x, y) in ((6, 10), (27, 10), (6, 25), (27, 25)):
        m.box(x, y, BENCH_H - 0.1, x + 1, y + 1, BENCH_H + 0.1, GRAPH)
    mb = m.box(7, 11, BENCH_H + 2, 27, 25, BENCH_H + 3, PCB)
    mb.decal('top', rect(0, 0, 20, 14), Use(PCB, idx=1))
    mb.decal('top', rect(12, 9, 18, 13), Use(GRAPH, idx=1))
    cool = m.box(10, 16, BENCH_H + 3, 16, 22, BENCH_H + 12, STEEL)
    for face in ('front', 'right', 'left', 'back'):
        cool.decal(face, ('fn', lambda s, t: (np.floor(t).astype(int) % 2 == 0)), Delta(-1))
    m.box(10, 15, BENCH_H + 4, 16, 16, BENCH_H + 11, GRAPH)
    gpu = m.box(7, 11, BENCH_H + 3, 27, 13, BENCH_H + 12, BODY_D)
    fan_grill(gpu, 'front', 6, 4.5, 3.2, spokes=5)
    fan_grill(gpu, 'front', 14, 4.5, 3.2, spokes=5)
    gpu.decal('front', rect(0, 8, 20, 9), Use(CORAL, idx=3))
    for k, col in enumerate(('sky', 'sky')):
        m.box(19 + k * 2, 17, BENCH_H + 3, 20 + k * 2, 24, BENCH_H + 7, M('accent', col))
    psu = m.box(30, 14, BENCH_H, 40, 26, BENCH_H + 8, BODY_D)
    fan_grill(psu, 'top', 5, 6, 3.5, spokes=6)
    vents(psu, 'front', 1, 9, 2, 7, step=2)
    m.box(49, 25, BENCH_H, 53, 27, BENCH_H + 6, GRAPH)
    sc = m.box(42, 23.5, BENCH_H + 5, 60, 25.5, BENCH_H + 21, BODY_D)
    ui_screen(sc, 'front', 18, 16, seed=4)
    mm = m.box(46, 6, BENCH_H, 52, 12, BENCH_H + 2, SUN)
    mm.decal('top', rect(1, 3, 5, 5), Use(LCD, idx=4))
    mm.decal('top', disc(3, 1.5, 1.2, 1.2), Use(GRAPH, idx=2))


@iso_asset('test_bench_embedded', 'laboratories', 'bench', 'Banc de test mobile / embarque', footprint=(2, 1),
           facings=ALL4, height=46, access=[Access('work', 0, -1), Access('work', 1, -1)], tags=['workstation', 'lab'],
           sockets=[Socket('bench_right', ITEM_ACCEPT, 52, 10, BENCH_H),
                    Socket('chair', ['chair'], 32, -12, 0, '', facing_rel='opposite', group='workstation')],
           notes='Cartes de developpement sur entretoises, telephones et tablette en pinces, analyseur logique et fils.')
def _(m):
    bench_frame(m, 64, WOOD)
    # cartes de dev
    for (x, y, w, d, col) in ((5, 6, 10, 8, 'leaf'), (17, 5, 8, 6, 'navy'), (6, 16, 8, 7, 'leaf')):
        for (sx, sy) in ((x, y), (x + w - 1, y), (x, y + d - 1), (x + w - 1, y + d - 1)):
            m.box(sx, sy, BENCH_H, sx + 1, sy + 1, BENCH_H + 1.5, BRASS)
        b = m.box(x, y, BENCH_H + 1.5, x + w, y + d, BENCH_H + 2.3, M('fixed', col))
        b.decal('top', rect(2, 2, 5, 5), Use(GRAPH, idx=1))
        b.decal('top', rect(w - 2, 1, w - 1, 2), Fix('led_green'))
        b.decal('top', rect(1, d - 2, w - 1, d - 1), Fix(ramp_col('brass', 4)))
    # telephones dans des pinces
    for k in range(3):
        x = 30 + k * 6
        m.box(x + 1, 20, BENCH_H, x + 4, 24, BENCH_H + 1.5, GRAPH)
        ph = m.box(x, 21, BENCH_H + 1.5, x + 5, 22, BENCH_H + 12, BODY_D)
        if k == 1:
            code_screen(ph, 'front', 5, 10.5, seed=k + 3, margin=0.5)
        else:
            ui_screen(ph, 'front', 5, 10.5, seed=k, margin=0.5)
    tab = m.slab_tilt(48, 62, 22, BENCH_H + 1, 25, BENCH_H + 14, 1, BODY_D)
    tab.decal('front', rect(1, 1, 13, 99), Use(SCREEN, idx=2))
    tab.decal('front', rect(2, 4, 8, 10), Fix(ramp_col('teal', 3)))
    tab.decal('front', rect(9, 4, 12, 7), Fix(ramp_col('sun', 4)))
    la = m.box(40, 6, BENCH_H, 48, 12, BENCH_H + 3, BODY_L)
    la.decal('front', rect(1, 1, 7, 2), Fix('ink'))
    for k, col in enumerate(['coral', 'sun', 'leaf', 'sky', 'plastic']):
        m.hcyl('y', 41.5 + k * 1.3, BENCH_H + 0.6, 0.5, -0.5, 6, M('fixed', col), outline=False)
    m.box(26, 6, BENCH_H, 30, 9, BENCH_H + 1, GRAPH)
    strut(m, (28, 7.5, BENCH_H + 1), (24, 12, BENCH_H + 10), 1.0, GRAPH)
    strut(m, (24, 12, BENCH_H + 10), (19, 10, BENCH_H + 6), 0.8, CORAL)


@iso_asset('research_station', 'laboratories', 'bench', 'Poste de recherche (paillasse)', footprint=(2, 1), facings=ALL4,
           height=70, access=[Access('work', 0, -1), Access('work', 1, -1)], tags=['workstation', 'lab'],
           sockets=bench_sockets(64, shelf_z=None)[1:2] + [
               Socket('keyboard', ['keyboard'], 32, 10, BENCH_H),
               Socket('chair', ['chair'], 32, -12, 0, '', facing_rel='opposite', group='workstation')],
           screens=[screen_quad(9, 27, 23.5, 37, 51, 'screen_left'), screen_quad(37, 55, 23.5, 37, 51, 'screen_right')],
           notes='Plan epoxy noir, caissons blancs, etagere vitree a flacons, deux ecrans integres.')
def _(m):
    top = m.box(1, 2, BENCH_H - 3, 63, 30, BENCH_H, BODY_D)
    top.decal('top', rect(1, 1, 61, 2), Delta(+1))
    for x0 in (2, 44):
        c = m.box(x0, 4, 0, x0 + 18, 29, BENCH_H - 3, BODY_L)
        c.decal('front', frame(1, 1, 17, 26), Delta(-1))
        c.decal('front', rect(7, 20, 11, 21), Use(STEEL, idx=4))
    m.box(20, 26, 0, 44, 28, BENCH_H - 3, BODY_L)
    for x in (2, 60):
        m.box(x, 27, BENCH_H, x + 2, 29, 70, STEEL)
    m.box(2, 22, 60, 62, 29, 61, GLASS, alpha=170)
    for k, col in enumerate(['sky', 'leaf', 'sun', 'coral', 'sky', 'rose', 'leaf']):
        x = 6 + k * 8
        lq = m.cyl(x, 25.5, 2, 61, 64.5, M('fixed', col))
        lq.decal('side', rect(300, 0, 340, 3.5), Delta(+2))
        gl = m.cyl(x, 25.5, 2, 64.5, 67, GLASS, alpha=190)
        gl.decal('side', rect(300, 0, 340, 3), Use(GLASS, idx=5))
        m.cyl(x, 25.5, 1.0, 67, 69, GRAPH)
    for (x0, seed) in ((8, 21), (36, 22)):
        m.box(x0 + 8, 25, BENCH_H, x0 + 12, 27, BENCH_H + 7, GRAPH)
        sc = m.box(x0, 23.5, BENCH_H + 6, x0 + 20, 25.5, BENCH_H + 22, BODY_D)
        if seed == 21:
            ui_screen(sc, 'front', 20, 16, seed=9)
        else:
            sc.decal('front', rect(1, 1, 19, 15), Use(SCREEN, idx=1))
            waveform(sc, 'front', 2, 3, 16, 10, ramp_col('screen', 4), periods=2.5)
            waveform(sc, 'front', 2, 4, 16, 8, ramp_col('sun', 4), periods=1.5)
    sen = m.box(4, 6, BENCH_H, 12, 14, BENCH_H + 6, BODY_S)
    sen.decal('front', rect(1, 3, 7, 5), Use(LCD, idx=3))
    sen.decal('top', disc(4, 4, 2.5, 2.5), Use(GLASS, idx=3))


@iso_asset('calibration_bench', 'laboratories', 'bench', 'Etabli de calibration (marbre)', footprint=(2, 1),
           facings=FRONT2, height=56, access=[Access('work', 0, -1), Access('work', 1, -1)], tags=['lab'],
           sockets=[Socket('bench_left', ITEM_ACCEPT, 14, 14, 30)],
           notes='Marbre de granit sur bati acier, trusquin a colonne, coffret de cales etalon, comparateur.')
def _(m):
    for x in (4, 56):
        m.box(x, 6, 0, x + 4, 26, 3, STEEL)
        m.box(x + 1, 13, 3, x + 3, 19, 22, STEEL)
    m.box(6, 14, 8, 58, 18, 10, STEEL)
    g = m.box(3, 4, 22, 61, 28, 30, M('fixed', 'graphite'))
    for face in ('top', 'front', 'left', 'right'):
        g.decal(face, ('fn', lambda s, t: (((np.floor(s * 1.7) + np.floor(t * 2.3) * 7).astype(int) % 11) == 0)), Delta(+1))
        g.decal(face, ('fn', lambda s, t: (((np.floor(s * 2.1) + np.floor(t * 1.3) * 5).astype(int) % 13) == 0)), Delta(-1))
    m.box(44, 10, 30, 52, 18, 32, STEEL)
    m.box(47, 13, 32, 49, 15, 56, STEEL)
    sl = m.box(45, 12, 44, 51, 16, 47, BODY_D)
    m.box(51, 13, 45, 56, 15, 46, STEEL)
    m.cyl(48, 11.5, 2.2, 49, 50, BODY_L)
    box = m.box(12, 8, 30, 30, 18, 33, WOOD)
    box.decal('top', rect(1, 1, 17, 9), Use(M('fabric', 'navy'), idx=1))
    for k in range(7):
        box.decal('top', rect(2 + k * 2.2, 3, 3.2 + k * 2.2, 7 - (k % 3)), Use(STEEL, idx=5))
    m.box(34, 18, 30, 39, 23, 33, M('accent', 'sun'))
    strut(m, (36.5, 20.5, 33), (36.5, 20.5, 42), 1.0, STEEL)
    strut(m, (36.5, 20.5, 42), (31, 14, 40), 0.8, STEEL)
    m.cyl(30.5, 13.5, 1.6, 37, 40, BODY_L)


# ---------------------------------------------------------------------------
# Objets d'etabli (poses)
# ---------------------------------------------------------------------------
@iso_asset('soldering_station', 'laboratories', 'bench_item', 'Station de soudure', centered=True, size_su=(14, 10),
           facings=ALL4, height=8, provides=ITEM, tags=['bench_item'],
           notes='Unite de controle (afficheur, bouton), support de fer en spirale, eponge.')
def _(m):
    b = m.box(-7, -4, 0, -1, 4, 5, BODY_D)
    b.decal('front', rect(0.5, 2.5, 4, 4.3), Use(GRAPH, idx=0))
    digits(b, 'front', 0.8, 2.8, 2, ramp_col('red', 4), w=1, gap=0.6)
    b.decal('front', disc(4.8, 2.5, 0.9, 0.9), Use(STEEL, idx=4))
    m.cyl(3.5, 0, 2, 0, 1, GRAPH)
    strut(m, (3.5, 0, 1), (4.5, 1.5, 6), 1.3, STEEL)
    strut(m, (1.5, -3, 1.2), (5.5, 2.5, 6.2), 0.8, STEEL)
    strut(m, (0.5, -4.2, 0.8), (1.8, -2.6, 1.6), 1.0, CORAL)
    sp = m.box(1, 2, 0, 6, 4, 1, GRAPH)
    m.box(1.5, 2.5, 1, 5.5, 3.5, 1.6, M('fixed', 'sun'))


@iso_asset('microscope', 'laboratories', 'bench_item', 'Microscope binoculaire', centered=True, size_su=(10, 12),
           facings=ALL4, height=19, provides=ITEM, tags=['bench_item'])
def _(m):
    m.box(-4, -3, 0, 4, 5, 1.5, BODY_L)
    m.box(-1, 3, 1.5, 1, 5, 14, BODY_L)
    m.box(-3, -2, 5, 3, 2, 6, GRAPH)
    m.box(-1.5, -1, 6, 1.5, 1, 6.4, GLASS)
    m.box(-3, -1, 11, 3, 4.5, 15, BODY_L)
    m.cyl(0, 0.5, 1.3, 8, 11, GRAPH)
    for sx in (-1.4, 1.4):
        strut(m, (sx, 0.5, 15), (sx * 1.25, -2.0, 18.6), 1.3, GRAPH)
    m.cyl(-1.3, 4, 1.4, 7, 9, BODY_D)


@iso_asset('oscilloscope', 'laboratories', 'instrument', 'Oscilloscope', centered=True, size_su=(16, 12), facings=ALL4,
           height=12, provides=ITEM + ['instrument'], tags=['bench_item'],
           screens=[screen_quad(-7, 1, -5, 2, 9)],
           notes='Ecran avec courbe, boutons, entrees BNC, poignee.')
def _(m):
    b = m.box(-8, -5, 0.6, 8, 6, 10.6, BODY_L)
    for (x, y) in ((-7, -4), (6, -4), (-7, 4), (6, 4)):
        m.box(x, y, 0, x + 1, y + 1, 0.6, GRAPH)
    b.decal('front', rect(0.5, 1.5, 9.5, 9), Use(GRAPH, idx=1))
    b.decal('front', rect(1, 2, 9, 8.5), Use(SCREEN, idx=1))
    b.decal('front', ('fn', lambda s, t: (((np.floor(s).astype(int) - 1) % 2 == 0) & (np.floor(t).astype(int) == 5))), Use(SCREEN, idx=3))
    waveform(b, 'front', 1, 2.5, 8, 5.5, ramp_col('screen', 5), periods=2)
    waveform(b, 'front', 1, 3.5, 8, 3.5, ramp_col('sun', 4), periods=1, kind='square')
    knobs(b, 'front', [(11, 8), (13.5, 8), (11, 5.5), (13.5, 5.5)], delta=-2, r=0.8)
    for k in range(3):
        b.decal('front', rect(10.5 + k * 1.6, 2, 11.5 + k * 1.6, 3), Fix(ramp_col('brass', 4)))
    b.decal('front', rect(0.5, 0.5, 15.5, 1), Delta(-1))
    m.box(-6, -0.5, 10.6, -5, 0.5, 12, GRAPH)
    m.box(5, -0.5, 10.6, 6, 0.5, 12, GRAPH)
    m.box(-6, -0.5, 11.5, 6, 0.5, 12.3, GRAPH)
    vents(b, 'left', 2, 9, 2, 9, step=2)
    vents(b, 'right', 2, 9, 2, 9, step=2)
    fan_grill(b, 'back', 11, 5.5, 2.8, spokes=4)
    b.decal('back', rect(2, 2, 5, 4), Use(GRAPH, idx=1))
    vents(b, 'back', 2, 7, 5, 9, step=2)


@iso_asset('lab_power_supply', 'laboratories', 'instrument', 'Alimentation de laboratoire', centered=True,
           size_su=(14, 10), facings=ALL4, height=8, provides=ITEM + ['instrument'], tags=['bench_item'])
def _(m):
    b = m.box(-7, -5, 0, 7, 5, 8, BODY_D)
    b.decal('front', rect(1, 4.5, 6.5, 7), Use(GRAPH, idx=0))
    digits(b, 'front', 1.5, 5, 3, ramp_col('red', 4), w=1, gap=0.7)
    b.decal('front', rect(7.5, 4.5, 13, 7), Use(GRAPH, idx=0))
    digits(b, 'front', 8, 5, 3, ramp_col('leaf', 5), w=1, gap=0.7)
    b.decal('front', disc(3.5, 2, 1.3, 1.3), Use(STEEL, idx=4))
    b.decal('front', disc(10.5, 2, 1.3, 1.3), Use(STEEL, idx=4))
    b.decal('front', disc(6, 1.5, 0.7, 0.7), Fix(ramp_col('red', 3)))
    b.decal('front', disc(8, 1.5, 0.7, 0.7), Fix(ramp_col('graphite', 0)))
    b.decal('top', frame(1, 1, 13, 9), Delta(+1))
    vents(b, 'left', 3, 8, 2, 7, step=2)
    fan_grill(b, 'back', 4, 4, 2.6, spokes=4)
    b.decal('back', rect(9, 2, 12, 4), Use(GRAPH, idx=1))
    b.decal('back', disc(10.5, 6, 0.7, 0.7), Fix(ramp_col('red', 3)))


@iso_asset('cnc_mill_desktop', 'laboratories', 'bench_item', 'Fraiseuse CNC de table', centered=True, size_su=(18, 16),
           facings=ALL4, height=17, provides=ITEM, tags=['bench_item', 'prototyping'],
           notes='Petit equipement de prototypage : portique, broche, piece en cours (statique).')
def _(m):
    bed = m.box(-9, -8, 0, 9, 8, 2, GRAPH)
    bed.decal('top', ('fn', lambda s, t: (np.floor(t).astype(int) % 4 == 1)), Delta(-1))
    for x in (-9, 7):
        m.box(x, 2, 2, x + 2, 5, 16, STEEL)
    m.box(-9, 2, 14, 9, 5, 17, STEEL)
    car = m.box(-2.5, 0, 9, 2.5, 2, 15, SUN)
    car.decal('front', rect(1, 4, 4, 5), Delta(-1))
    m.cyl(0, 1, 1.1, 5, 9, STEEL)
    m.cyl(0, 1, 0.5, 3.6, 5, GRAPH)
    wp = m.box(-5, -5, 2, 4, 0, 4, WOOD)
    wp.decal('top', rect(2, 1, 6, 3), Delta(-1))


@iso_asset('studio_monitors', 'laboratories', 'bench_item', 'Enceintes de studio (paire)', centered=True, size_su=(22, 7),
           facings=ALL4, height=11, provides=ITEM, tags=['bench_item', 'av'],
           notes='Ecoutes de creation audio/jeu ; se posent sur un bureau ou un etabli.')
def _(m):
    for cx in (-7, 7):
        b = m.box(cx - 3, -3, 0, cx + 3, 3, 10, BODY_D)
        b.decal('front', disc(3, 3.5, 2.3, 2.3), Delta(-2))
        b.decal('front', ('fn', lambda s, t: np.abs(np.hypot(s - 3, t - 3.5) - 1.6) < 0.45), Delta(-1))
        b.decal('front', disc(3, 3.5, 0.7, 0.7), Delta(+1))
        b.decal('front', disc(3, 8, 0.9, 0.9), Delta(-2))
        b.decal('front', rect(2.5, 0.5, 3.5, 1.5), Fix('led_blue'))


# ---------------------------------------------------------------------------
# Equipements au sol
# ---------------------------------------------------------------------------
@iso_asset('instrument_rack', 'laboratories', 'instrument', "Chariot d'instruments de mesure", footprint=(1, 1),
           facings=FRONT2, height=66, access=[Access('work', 0, -1)], tags=['lab'],
           screens=[screen_quad(10, 18, 7, 9, 17, 'analyzer')],
           notes='Chariot ouvert a roulettes : analyseur de spectre, generateur, petit oscilloscope.')
def _(m):
    for (x, y) in ((7, 7), (25, 7), (7, 25), (25, 25)):
        m.cyl(x, y, 1.2, 0, 2.4, GRAPH)
    for (x, y) in ((6, 6), (24, 6), (6, 24), (24, 24)):
        m.box(x, y, 2.4, x + 2, y + 2, 58, STEEL)
    for z in (4, 30, 54):
        m.box(6, 6, z, 26, 26, z + 2, STEEL)
    an = m.box(8, 7, 6, 24, 24, 21, BODY_L)
    an.decal('front', rect(1.5, 2, 10.5, 12), Use(GRAPH, idx=1))
    an.decal('front', rect(2, 2.5, 10, 11.5), Use(SCREEN, idx=1))
    an.decal('front', ('fn', lambda s, t: (s >= 2) & (s < 10) & (t >= 2.5) & (t < 2.5 + 7 * np.exp(-((s - 6) ** 2) / 1.5))),
              Fix(ramp_col('screen', 4)))
    knobs(an, 'front', [(12.5, 10), (14.5, 10), (12.5, 7), (14.5, 7)], delta=-2, r=0.8)
    an.decal('front', rect(12, 2, 15, 4), Fix(ramp_col('brass', 4)))
    gen = m.box(8, 8, 32, 24, 24, 41, BODY_D)
    gen.decal('front', rect(1, 4, 9, 7), Use(GRAPH, idx=0))
    digits(gen, 'front', 1.5, 4.5, 4, ramp_col('screen', 4), w=1, gap=0.8)
    knobs(gen, 'front', [(11, 5.5), (13.5, 5.5)], delta=+2, r=1.0)
    gen.decal('front', rect(14, 1.5, 15, 2.5), Fix(ramp_col('brass', 4)))
    sc = m.box(10, 9, 56, 22, 22, 64, BODY_L)
    sc.decal('front', rect(1, 1.5, 7, 7), Use(SCREEN, idx=1))
    waveform(sc, 'front', 1.3, 2, 5.5, 4.5, ramp_col('screen', 5), periods=1.5)
    knobs(sc, 'front', [(9, 5), (10.5, 3)], delta=-2, r=0.7)
    m.hcyl('x', 9, 28, 0.6, 8, 24, COPPER, outline=False)


@iso_asset('printer_3d', 'laboratories', 'prototyping', 'Imprimante 3D fermee', footprint=(1, 1), facings=FRONT2,
           height=46, access=[Access('work', 0, -1)], tags=['lab', 'prototyping'],
           notes='Sur sa table : caisson vitre, plateau, piece imprimee, chariot d\'extrusion, bobine de filament.')
def _(m):
    top = m.box(4, 5, 15, 28, 27, 18, GRAPH)
    for (x, y) in ((5, 6), (25, 6), (5, 24), (25, 24)):
        m.box(x, y, 0, x + 2, y + 2, 15, GRAPH)
    m.box(6, 7, 6, 26, 25, 7, GRAPH)
    for (x, y) in ((7, 7), (24, 7), (7, 24), (24, 24)):
        m.box(x, y, 18, x + 1, y + 1, 44, BODY_D)
    m.box(7, 7, 18, 25, 25, 21, BODY_D)
    ui = m.box(9, 6.5, 18.5, 17, 7, 20.5, LCD)
    m.box(9, 9, 24, 23, 23, 25, STEEL)
    m.cyl(16, 16, 3, 25, 31, CORAL)
    m.cyl(16, 16, 2.2, 31, 32, CORAL)
    m.hcyl('x', 16, 36, 0.6, 8, 24, STEEL, outline=False)
    car = m.box(13, 14, 33, 19, 18, 37, SUN)
    car.decal('front', rect(1, 1, 5, 2), Delta(-1))
    m.box(7, 7, 44, 25, 25, 46, BODY_D)
    # vitrage : reflets fins opaques (le moteur ne compose pas un pixel derriere une vitre du meme sprite)
    GL = Fix(ramp_col('glass', 5))
    for (a, b) in ((10, 30), (14, 30), (12, 38)):
        strut(m, (a, 7.2, b), (a + 5, 7.2, b + 6), 0.6, GL, outline=False, shadow=False, edges=False)
    for (a, b) in ((10, 30), (16, 32)):
        strut(m, (7.2, a, b), (7.2, a + 5, b + 6), 0.6, GL, outline=False, shadow=False, edges=False)
        strut(m, (24.8, a, b), (24.8, a + 5, b + 6), 0.6, GL, outline=False, shadow=False, edges=False)
    m.hcyl('x', 16, 36, 4.5, 25.2, 28, CORAL)
    m.hcyl('x', 16, 36, 1.4, 28, 28.6, GRAPH)


@iso_asset('laser_cutter', 'laboratories', 'prototyping', 'Decoupeuse laser', footprint=(2, 1), facings=FRONT2, height=40,
           access=[Access('work', 0, -1), Access('work', 1, -1)], tags=['lab', 'prototyping'],
           notes='Capot a fenetre teintee, pupitre avec arret d\'urgence, gaine d\'extraction a l\'arriere.')
def _(m):
    for (x, y) in ((4, 6), (58, 6), (4, 26), (58, 26)):
        m.cyl(x, y, 1.3, 0, 2.5, GRAPH)
    body = m.box(2, 4, 2.5, 62, 30, 26, BODY_L)
    body.decal('front', rect(1, 1, 59, 2), Delta(-1))
    vents(body, 'front', 4, 20, 4, 12, step=2)
    body.decal('front', rect(44, 6, 56, 20), Use(GRAPH, idx=2))
    body.decal('front', rect(45, 13, 51, 19), Use(LCD, idx=3))
    body.decal('front', rect(46, 16, 50, 17), Use(LCD, idx=5))
    body.decal('front', disc(53.5, 16, 1.6, 1.6), Fix(ramp_col('red', 3)))
    body.decal('front', disc(53.5, 16, 0.8, 0.8), Fix(ramp_col('red', 5)))
    knobs(body, 'front', [(47, 9), (50, 9), (53, 9)], delta=+3, r=0.7)
    lid = m.wedge_y(3, 5, 26, 61, 29, 30, 33, BODY_L)
    lid.decal('top', rect(4, 4, 40, 20), Use(M('glass', 'copper'), idx=2))
    lid.decal('top', ('fn', lambda s, t: (s >= 4) & (s < 40) & (t >= 4) & (t < 20) & ((np.floor(s + t).astype(int) % 14) < 2)),
              Use(M('glass', 'copper'), idx=4))
    lid.decal('front', rect(8, 1, 36, 2), Use(STEEL, idx=4))
    m.cyl(56, 27, 2.6, 26, 42, GRAPH)
    m.box(53, 26, 40, 59, 30, 42, GRAPH)


@iso_asset('cobot_cell', 'laboratories', 'robotics', 'Cellule robotisee (cobot)', footprint=(2, 2), facings=FRONT2,
           height=66, access=[Access('work', 0, -1), Access('work', 1, -1)], occludes=40, tags=['lab', 'robotics'],
           notes='Bras collaboratif sur table, bacs de pieces, cadre de securite (poteaux jaunes, panneaux translucides, barrage lumineux).')
def _(m):
    t = m.box(8, 8, 0, 56, 56, 22, BODY_D)
    t.decal('front', frame(2, 2, 46, 20), Delta(-1))
    t.decal('front', rect(20, 12, 28, 13), Use(STEEL, idx=4))
    tp = m.box(7, 7, 22, 57, 57, 24, STEEL)
    tp.decal('top', ('fn', lambda s, t_: (np.floor(t_).astype(int) % 8 == 0)), Delta(-1))
    WHITE = M('primary', 'plastic')
    JOINT = ACC
    m.cyl(32, 38, 6.5, 24, 28, WHITE)
    m.cyl(32, 38, 5.4, 28, 30, JOINT)
    m.cyl(32, 38, 4.2, 30, 42, WHITE)
    m.ell(32, 38, 43, 5.0, 5.0, 4.6, JOINT)
    strut(m, (32, 38, 43), (22, 30, 60), 5.0, WHITE)
    m.ell(22, 30, 60, 4.0, 4.0, 3.8, JOINT)
    strut(m, (22, 30, 60), (30, 18, 46), 4.0, WHITE)
    m.ell(30, 18, 46, 3.0, 3.0, 2.8, JOINT)
    strut(m, (30, 18, 46), (31, 17, 38), 2.6, GRAPH)
    m.box(28.5, 15, 33, 30, 19.5, 38, GRAPH)
    m.box(32, 15, 33, 33.5, 19.5, 38, GRAPH)
    m.box(29, 15.5, 37, 33, 19, 39, M('accent', 'teal'))
    for k, col in enumerate(['sun', 'sky', 'coral']):
        b = m.box(12 + k * 13, 11, 24, 22 + k * 13, 19, 27, GRAPH)
        b.decal('top', rect(1, 1, 9, 7), Use(GRAPH, idx=0))
        for j in range(3):
            m.box(14 + k * 13 + j * 2.5, 13, 25, 15.5 + k * 13 + j * 2.5, 16, 27.5, M('accent', col))
    YEL = M('accent', 'hazard')
    for (x, y) in ((3, 3), (59, 3), (3, 59), (59, 59)):
        p = m.box(x, y, 0, x + 2, y + 2, 66, YEL)
        p.decal('sides', ('fn', lambda s, t_: (np.floor(t_).astype(int) % 10) < 2), Use(M('fixed', 'graphite'), idx=1))
    for (a, b, c, d) in ((3, 3, 61, 5), (3, 59, 61, 61), (3, 3, 5, 61), (59, 3, 61, 61)):
        m.box(a, b, 64, c, d, 66, YEL)
    # panneau translucide uniquement au fond (toujours derriere le robot) ; cotes : lisses intermediaires
    m.box(5, 59.5, 4, 59, 60.5, 64, GLASS, alpha=80, outline=False, shadow=False)

    for x in (3, 59):
        bar = m.box(x, 1, 6, x + 2, 3, 40, GRAPH)
        bar.decal('sides', ('fn', lambda s, t_: (np.floor(t_).astype(int) % 6) == 0), Fix('led_red'))


@iso_asset('camera_tripod', 'laboratories', 'av', 'Camera sur trepied', centered=True, size_su=(16, 16), facings=ALL4,
           height=38, provides=['floor_item'], tags=['av'],
           notes='Capture video pour creation de contenus/jeux (statique).')
def _(m):
    tripod(m, 0, 0, 30, 7, GRAPH, leg_w=1.2)
    m.cyl(0, 0, 1.6, 28, 31, GRAPH)
    cam = m.box(-3, -2, 31, 3, 4, 36, BODY_D)
    cam.decal('back', rect(1, 1, 5, 4), Use(LCD, idx=3))
    cam.decal('left', rect(1, 1, 3, 2), Fix('led_red'))
    m.hcyl('y', 0, 33.5, 2.2, -5, -2, GRAPH)
    m.hcyl('y', 0, 33.5, 1.6, -5.6, -5, GLASS)
    m.box(-1, 0, 36, 1, 3, 37.5, GRAPH)


@iso_asset('softbox_light', 'laboratories', 'av', 'Eclairage softbox', centered=True, size_su=(18, 18), facings=ALL4,
           height=64, provides=['floor_item'], tags=['av'])
def _(m):
    tripod(m, 0, 2, 30, 8, GRAPH, leg_w=1.1)
    m.box(-0.7, 1.3, 28, 0.7, 2.7, 48, STEEL)
    m.box(-7, 1, 46, 7, 7, 59, M('fabric', 'graphite'))
    pan = m.slab_tilt(-9, 9, -1, 43, 1, 62, 1.2, Fix(ramp_col('plastic', 5)))
    pan.decal('front', frame(0, 0, 18, 99, 1, 1), Fix(ramp_col('plastic', 3)))
    m.box(-1, 1, 46, 1, 3, 49, GRAPH)


@iso_asset('mic_stand', 'laboratories', 'av', 'Pied de micro avec perche', centered=True, size_su=(14, 18), facings=ALL4,
           height=44, provides=['floor_item'], tags=['av'])
def _(m):
    m.cyl(0, 0, 5, 0, 1.5, GRAPH)
    m.box(-0.6, -0.6, 1.5, 0.6, 0.6, 34, STEEL)
    m.cyl(0, 0, 1.2, 33, 35, GRAPH)
    strut(m, (0, 2, 35), (0, -9, 40), 0.9, STEEL)
    m.ell(0, -10, 40.5, 1.6, 1.6, 2.6, GRAPH)
    m.ell(0, -12.5, 40.5, 2.6, 0.5, 2.6, M('fabric', 'graphite'))
    m.cyl(0, 3.5, 1.3, 34, 36, GRAPH)


@iso_asset('greenscreen_backdrop', 'laboratories', 'av', 'Fond vert sur portique', footprint=(2, 1), facings=FRONT2,
           height=82, collision=[(0, 0), (1, 0)], tags=['av'],
           notes='Toile verte sur portique avec retombee au sol (franchissable visuellement, bloquante pour le jeu).')
def _(m):
    for x in (2, 60):
        m.box(x, 26, 0, x + 2, 28, 82, GRAPH)
        strut(m, (x + 1, 27, 0), (x + 1, 20, 0.8), 1.2, GRAPH)
        strut(m, (x + 1, 27, 6), (x + 1, 20, 0.6), 0.8, GRAPH)
    m.box(2, 26, 80, 62, 28, 82, GRAPH)
    m.cyl(32, 27, 1.6, 77, 80, GRAPH)
    GREEN = M('fabric', 'leaf')
    sh = m.box(5, 27, 3, 59, 28, 79, GREEN)
    sh.decal('front', ('fn', lambda s, t: (np.floor(s).astype(int) % 9 == 4)), Delta(-1))
    sh.decal('front', ('fn', lambda s, t: (np.floor(s).astype(int) % 9 == 5)), Delta(+1))
    fl = m.box(5, 12, 0, 59, 28, 1, GREEN)
    fl.decal('top', rect(0, 13, 54, 16), Delta(+1))


@iso_asset('xr_base_station', 'laboratories', 'xr', 'Balise de suivi XR sur pied', centered=True, size_su=(12, 12),
           facings=ALL4, height=70, provides=['floor_item'], tags=['xr'])
def _(m):
    tripod(m, 0, 0, 22, 5, GRAPH, leg_w=1.0)
    m.box(-0.6, -0.6, 20, 0.6, 0.6, 64, STEEL)
    h = m.box(-2.5, -2.5, 64, 2.5, 2.5, 69, BODY_D)
    h.decal('front', rect(1, 1, 4, 4), Use(GLASS, idx=1))
    h.decal('front', rect(1.5, 3, 3.5, 3.8), Use(GLASS, idx=3))
    h.decal('top', rect(1, 1, 2, 2), Fix('led_green'))


@iso_asset('xr_play_mat', 'laboratories', 'xr', 'Tapis de zone de jeu XR', footprint=(2, 2), facings=ONE, height=1,
           walkable=True, collision=[], symmetric=True, layer='floor_decor', tags=['xr', 'floor_decor'],
           model_kw={'shadow': False}, notes='Zone de test XR au sol : bordure de securite et cible centrale.')
def _(m):
    p = m.box(2, 2, 0, 62, 62, 1, M('fabric', 'navy'), shadow=False)
    p.decal('top', frame(2, 2, 58, 58, 2, 2), Use(ACC, idx=4))
    p.decal('top', ('fn', lambda s, t: np.abs(np.hypot(s - 30, t - 30) - 12) < 1.0), Use(ACC, idx=3))
    p.decal('top', disc(30, 30, 3, 3), Use(ACC, idx=4))
    for (s, t) in ((8, 8), (52, 8), (8, 52), (52, 52)):
        p.decal('top', rect(s - 1, t - 1, s + 2, t + 2), Use(SUN, idx=4))


@iso_asset('compute_cabinet', 'laboratories', 'compute', 'Calculateur avance (armoire)', footprint=(2, 1), facings=FRONT2,
           height=98, access=[Access('service', 0, -1), Access('service', 1, -1)], occludes=50, tags=['compute', 'lab'],
           screens=[screen_quad(25, 39, 3, 74, 84, 'status')],
           notes='Armoire de supercalcul : angles chanfreines, portes a lames, liseres d\'etat, capot debordant, ailettes.')
def _(m):
    NAVY = M('primary', 'navy')
    m.box(2, 3, 0, 62, 29, 6, BODY_D)
    body = chamfer_box(m, 3, 4, 6, 61, 28, 92, 4, NAVY)
    for k in range(6):
        s0 = 4 + k * 9
        body.decal('front', frame(s0, 4, s0 + 8, 64), Delta(-1))
        body.decal('front', rect(s0 + 1, 6, s0 + 2, 62), Use(ACC, idx=3))
        for j in range(8):
            body.decal('front', rect(s0 + 4, 10 + j * 6, s0 + 6, 11 + j * 6), Fix('led_green' if (j + k) % 5 else 'led_amber'))
    body.decal('front', rect(21, 66, 37, 80), Use(GRAPH, idx=1))
    body.decal('front', rect(22, 68, 36, 78), Use(SCREEN, idx=2))
    body.decal('front', rect(23, 74, 33, 75), Fix(ramp_col('teal', 4)))
    body.decal('front', rect(23, 71, 29, 72), Fix(ramp_col('sun', 4)))
    hood = m.box(1, 2, 92, 63, 30, 98, NAVY)
    hood.decal('front', rect(2, 1, 60, 2), Use(ACC, idx=4))
    hood.decal('top', frame(2, 2, 60, 26), Delta(+1))
    for face_x in ((1, 3), (61, 63)):
        for y in (8, 14, 20):
            m.box(face_x[0], y, 10, face_x[1], y + 1.5, 88, BODY_D)


# ---------------------------------------------------------------------------
# Zone quantique
# ---------------------------------------------------------------------------
STAGES = [(86, 16, COPPER), (74, 13.5, BRASS), (62, 11.5, BRASS), (51, 9.5, BRASS), (41, 7.5, BRASS)]


def _cryo_frame(m):
    m.box(4, 4, 0, 60, 60, 4, BODY_D)
    for (x, y) in ((6, 6), (56, 6), (6, 56), (56, 56)):
        m.box(x, y, 4, x + 2, y + 2, 98, STEEL)
    for (a, b, c, d) in ((6, 6, 58, 8), (6, 56, 58, 58), (6, 6, 8, 58), (56, 6, 58, 58)):
        m.box(a, b, 96, c, d, 98, STEEL)
    top = m.box(10, 10, 98, 54, 54, 104, BODY_S)
    top.decal('top', ('fn', lambda s, t: np.abs(np.hypot(s - 22, t - 22) - 15) < 0.8), Delta(-1))
    for (s, t) in ((8, 8), (36, 8), (8, 36), (36, 36)):
        top.decal('top', disc(s, t, 1.2, 1.2), Use(STEEL, idx=2))
    m.cyl(32, 32, 18, 94, 98, STEEL)
    for (x, y, h) in ((22, 22, 112), (42, 24, 110), (26, 42, 108)):
        m.cyl(x, y, 1.4, 104, h, STEEL)
        m.cyl(x, y, 2.0, 104, 106, GRAPH)


@iso_asset('quantum_cryostat_open', 'laboratories', 'quantum', 'Ordinateur quantique (cryostat ouvert)', footprint=(2, 2),
           facings=FRONT2, height=112, access=[Access('service', 0, -1), Access('service', 1, -1)], occludes=60,
           tags=['quantum', 'lab'],
           notes=('Refrigerateur a dilution stylise mais credible : etages dores suspendus a la plaque superieure, '
                  'tiges, lignes coaxiales en cuivre, processeur au bas de la chambre de melange.'))
def _(m):
    _cryo_frame(m)
    prev_z = 94
    for i, (z, r, mat) in enumerate(STAGES):
        pl = m.cyl(32, 32, r, z, z + 2, mat)
        pl.decal('top', ('fn', lambda s, t, rr=r: np.abs(np.hypot(s - rr, t - rr) - (rr - 1.5)) < 0.6), Delta(+1))
        pl.decal('side', rect(0, 0, 360, 1), Delta(-1))
        # tiges de suspension (dont deux cote camera, -y)
        for a_deg in (210, 330, 90):
            a = math.radians(a_deg)
            rr = r - 1.5
            m.cyl(32 + math.cos(a) * rr, 32 + math.sin(a) * rr, 0.9, z + 2, prev_z, STEEL)
        # lignes coaxiales en faisceau cote camera
        for k in range(5):
            a = math.radians(240 + k * 15)
            rr = r - 3.5
            m.cyl(32 + math.cos(a) * rr, 32 + math.sin(a) * rr, 0.55, z + 2, prev_z, COPPER, outline=False)
        # echangeur / attenuateurs
        if i in (1, 2, 3):
            a = math.radians(250)
            m.cyl(32 + math.cos(a) * (r - 4), 32 + math.sin(a) * (r - 4), 1.4, z - 3, z, COPPER)
        prev_z = z
    # boitier processeur sous la chambre de melange
    m.cyl(32, 32, 2.2, 34, 40, COPPER)
    pkg = m.box(28.5, 28.5, 28, 35.5, 35.5, 34, COPPER)
    pkg.decal('front', rect(1.5, 1.5, 5.5, 4.5), Use(GRAPH, idx=1))
    pkg.decal('front', rect(2.5, 2.5, 4.5, 3.5), Use(BRASS, idx=5))
    pkg.decal('left', rect(1.5, 1.5, 5.5, 4.5), Use(GRAPH, idx=1))
    for k in range(4):
        a = math.radians(45 + k * 90)
        m.cyl(32 + math.cos(a) * 4.5, 32 + math.sin(a) * 4.5, 0.6, 30, 40, COPPER, outline=False)


@iso_asset('quantum_cryostat_closed', 'laboratories', 'quantum', 'Ordinateur quantique (cryostat ferme)', footprint=(2, 2),
           facings=FRONT2, height=112, access=[Access('service', 0, -1), Access('service', 1, -1)], occludes=60,
           tags=['quantum', 'lab'],
           notes='Meme chassis que la version ouverte, cuve cylindrique blanche, colliers, lignes de gaz, coffret electronique.')
def _(m):
    _cryo_frame(m)
    can = m.cyl(32, 32, 17, 26, 94, M('primary', 'plastic'))
    can.decal('side', rect(0, 50, 360, 53), Use(M('accent', 'sky'), idx=3))
    can.decal('side', rect(0, 10, 360, 11), Delta(-1))
    m.ell(32, 32, 26, 17, 17, 8, M('primary', 'plastic'), zclip=(18, 26))
    for z in (90, 60, 34):
        m.cyl(32, 32, 17.8, z, z + 2, STEEL)
    box = m.box(57, 22, 30, 61, 42, 60, BODY_D)
    box.decal('right', rect(4, 22, 16, 27), Use(LCD, idx=3))
    box.decal('right', rect(5, 18, 6, 19), Fix('led_green'))
    m.hcyl('y', 59, 70, 1.0, 22, 42, COPPER)
    m.hcyl('x', 16, 101, 1.0, 54, 62, STEEL)


@iso_asset('quantum_control_rack', 'laboratories', 'quantum', 'Baie de controle micro-ondes', footprint=(1, 1), facings=ALL4,
           height=100, access=[Access('service', 0, -1)], occludes=40, tags=['quantum', 'lab'],
           screens=[screen_quad(10, 22, 3, 80, 88, 'awg')],
           notes='Instruments micro-ondes (connecteurs SMA dores), faisceaux coaxiaux cuivre en boucle : distinct des baies serveurs.')
def _(m):
    LT = M('primary', 'steel')
    m.box(4, 2, 0, 28, 30, 5, BODY_D)
    for (a0, a1) in ((4, 6), (26, 28)):
        p = m.box(a0, 2, 5, a1, 30, 94, LT)
        p.decal('left' if a0 == 4 else 'right', frame(2, 3, 26, 86), Delta(-1))
    m.box(4, 2, 94, 28, 30, 98, LT)
    rear = m.box(6, 28, 5, 26, 30, 94, BODY_D)
    perforation(rear, 'back', 2, 4, 18, 84, step=2, delta=-1)
    for k, x in enumerate((5, 8, 11)):
        rear.decal('back', rect(x, 30, x + 1, 88), Fix(ramp_col('copper', 3 + (k % 2))))
    rear.decal('back', rect(14, 40, 17, 46), Use(GRAPH, idx=4))
    rows = [(78, 12, 'awg'), (64, 12, 'mw'), (50, 12, 'mw'), (36, 12, 'mw'), (20, 14, 'dc'), (8, 10, 'pwr')]
    for (z0, h, kind) in rows:
        ins = m.box(6, 3, z0, 26, 27, z0 + h, BODY_L if kind != 'pwr' else BODY_D)
        ins.decal('front', rect(0, h - 1, 20, h), Delta(+1))
        ins.decal('front', rect(0, 0, 20, 1), Delta(-2))
        if kind == 'awg':
            ins.decal('front', rect(4, 2, 16, 10), Use(SCREEN, idx=1))
            waveform(ins, 'front', 4.5, 3, 11, 6, ramp_col('screen', 5), periods=3, kind='square')
        elif kind == 'mw':
            for k in range(8):
                ins.decal('front', disc(2 + k * 2.3, 4, 0.8, 0.8), Fix(ramp_col('brass', 4)))
            ins.decal('front', rect(2, 8, 8, 10), Use(LCD, idx=3))
            ins.decal('front', rect(16, 8.5, 17, 9.5), Fix('led_green'))
        elif kind == 'dc':
            for k in range(6):
                ins.decal('front', rect(2 + k * 3, 4, 3.5 + k * 3, 10), Delta(-1))
                ins.decal('front', rect(2.5 + k * 3, 9, 3 + k * 3, 10), Fix('led_blue'))
        else:
            vents(ins, 'front', 2, 14, 2, 8, step=2)
            ins.decal('front', rect(16, 6, 17, 7), Fix('led_green'))
    # faisceaux coaxiaux en boucle devant la baie
    for k, x in enumerate((9, 12, 15, 18, 21)):
        m.cyl(x, 1.5, 0.5, 40 - k * 3, 76, COPPER, outline=False)
    m.hcyl('x', 1.5, 40, 0.6, 9, 21.5, COPPER, outline=False)
    m.hcyl('y', 16, 99, 1.6, 6, 30, COPPER)


@iso_asset('cryo_support_unit', 'laboratories', 'quantum', 'Unite de support cryogenique', footprint=(2, 1),
           facings=FRONT2, height=72, access=[Access('service', 0, -1), Access('service', 1, -1)], tags=['quantum', 'lab'],
           screens=[screen_quad(36, 46, 7, 52, 60, 'ghs')],
           notes="Compresseur a helium (ventilateur, manometres) + armoire de gestion des gaz (synoptique de vannes, ecran).")
def _(m):
    comp = m.box(2, 6, 0, 28, 28, 40, M('primary', 'navy'))
    comp.decal('front', frame(2, 2, 24, 38), Delta(-1))
    for (s, t) in ((7, 30), (13, 30), (19, 30)):
        comp.decal('front', disc(s, t, 2.2, 2.2), Use(BODY_L, idx=4))
        comp.decal('front', rect(s - 0.2, t, s + 0.6, t + 1.6), Fix('ink'))
    comp.decal('front', rect(6, 10, 20, 22), Delta(-1))
    perforation(comp, 'front', 7, 11, 19, 21, step=2, delta=-1)
    fan_grill(comp, 'left', 11, 20, 7, spokes=5)
    fan_grill(comp, 'right', 11, 20, 7, spokes=5)
    for x in (10, 18):
        m.cyl(x, 17, 1.4, 40, 48, STEEL)
    m.hcyl('x', 17, 48, 1.4, 9, 40, STEEL)
    gh = m.box(32, 8, 0, 62, 28, 70, BODY_L)
    gh.decal('front', frame(1, 1, 29, 69), Delta(-1))
    pipe = ramp_col('copper', 3)
    for t in (14, 26, 38):
        gh.decal('front', rect(3, t, 27, t + 1), Fix(pipe))
    for s in (6, 15, 24):
        gh.decal('front', rect(s, 14, s + 1, 39), Fix(pipe))
    for (s, t) in ((6, 26), (15, 14), (24, 38), (15, 38), (24, 14)):
        gh.decal('front', disc(s + 0.5, t + 0.5, 1.5, 1.5), Use(BODY_D, idx=2))
        gh.decal('front', disc(s + 0.5, t + 0.5, 0.7, 0.7), Fix(ramp_col('red', 3)))
    gh.decal('front', rect(4, 44, 14, 52), Use(LCD, idx=3))
    gh.decal('front', rect(5, 47, 12, 48), Use(LCD, idx=5))
    for (s, t) in ((20, 47), (25, 47)):
        gh.decal('front', disc(s, t, 2.2, 2.2), Use(BODY_L, idx=5))
        gh.decal('front', rect(s, t, s + 1, t + 1.6), Fix('ink'))
    hazard_triangle(gh, 'front', 12, 58, 6)


@iso_asset('supervision_terminal', 'laboratories', 'console', 'Terminal de supervision (pupitre debout)', footprint=(1, 1),
           facings=FRONT2, height=48, access=[Access('work', 0, -1)], tags=['console', 'lab'],
           screens=[{'name': 'screen', 'quad': [(9, 8 + 12 * 0.9, 31 + 15 * 0.9), (23, 8 + 12 * 0.9, 31 + 15 * 0.9),
                                                (23, 8 + 12 * 0.12, 31 + 15 * 0.12), (9, 8 + 12 * 0.12, 31 + 15 * 0.12)]}],
           notes='Pupitre sur colonne, ecran incline, tablette clavier ; supervision d\'un labo ou d\'une ligne.')
def _(m):
    base = m.box(9, 10, 0, 23, 24, 4, BODY_D)
    m.box(13, 14, 4, 19, 21, 31, BODY_D)
    led(m.prims[-1], 'front', 2, 20, 'led_green')
    head = m.slab_tilt(8, 24, 8, 31, 20, 46, 3, BODY_D)
    ui_screen(head, 'front', 16, 16, seed=12, margin=1)
    kb = m.box(9, 5, 29, 23, 10, 31, BODY_D)
    kb.decal('top', ('fn', lambda s, t: ((np.floor(t).astype(int) % 2) == 1) & ((np.floor(s).astype(int) % 2) == 0) & (s > 1) & (s < 13)), Delta(+2))
