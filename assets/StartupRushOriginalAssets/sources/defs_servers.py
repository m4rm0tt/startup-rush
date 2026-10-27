"""Datacenter et infrastructure (famille servers).

Systeme de baies (racks) :
  - unite de slot = 4 px de haut (1U). Point de slot local : x0 = 6 su (bord gauche
    de l'ouverture), y_face = 4 su (plan de facade des equipements), z_top = haut du
    slot 0 ; slot i : z = z_top - 4*i. Largeur de facade d'un module : 20 su.
  - x0 + y_face pair => les positions ecran des slots sont des pixels entiers exacts
    en se et sw (aucun arrondi entre la baie et ses modules).
  - les modules (assets 'module_*') sont des plaques de facade : leurs pixels sont
    exactement le parallelogramme de leur(s) slot(s) (verifie par previews/check_racks.png).
  - modules visibles uniquement en se/sw (facade vers la camera) ; en nw/ne la baie
    montre sa porte arriere et les modules ne sont pas dessines.
Modeles poses (centered=True) : origine = centre de la base, sur le support.
"""
import os
import numpy as np

from srgen.iso import Model, Fix, Use, Delta, rect, hline, vline, frame, disc, render, TILE
from srgen.palette import M, RAMPS, CHANNEL_NAMES
from srgen.registry import iso_asset, Socket, Access, ALL4, FRONT2, ONE, custom_builder, PACK_VERSION
from srgen.shapes import chamfer_box, code_screen, ui_screen, led, vents
from srgen.imgio import save_png
from srgen.lot_dc_shapes import (strut, fan_grill, drive_bays, port_row, perforation, mesh, digits,
                                 knobs, hazard_triangle, ramp_col)

FRAME = M('primary', 'graphite')
LIGHT = M('primary', 'plastic')
STEELP = M('primary', 'steel')
ACC = M('accent', 'teal')
CORAL = M('accent', 'coral')
STEEL = M('metal', 'steel')
GRAPH = M('metal', 'graphite')
COPPER = M('metal', 'copper')
SCREEN = M('screen', 'display')
LCD = M('screen', 'screen')

UNIT = 4
X0, YF, OPEN_W = 6, 4, 20

RACKS = {
    # id: (nombre de slots, z_top, type)
    'rack_compact': (8, 41, 'compact'),
    'rack_standard': (20, 88, 'standard'),
    'rack_high_density': (20, 89, 'hd'),
}


def rack_slots(count, z_top):
    return {'unit_px': UNIT, 'count': count, 'x0': X0, 'y_face': YF, 'z_top': z_top,
            'accepts': ['rack_module'], 'face': 'front'}


def _side_panel_decals(p, face, depth, height, heavy=False):
    p.decal(face, frame(2, 3, depth - 2, height - 3), Delta(-1))
    p.decal(face, rect(2, height - 3, depth - 2, height - 2), Delta(+1))
    if heavy:
        for s in (8, depth / 2.0, depth - 8):
            p.decal(face, rect(s, 4, s + 1, height - 4), Delta(+1))
    else:
        vents(p, face, depth / 2.0 - 4, depth / 2.0 + 4, height - 12, height - 5, step=2)


def _rack(m, n, z_top, kind):
    heavy = kind == 'hd'
    zb = z_top - n * UNIT
    xl0 = 3 if heavy else 4
    xr1 = 29 if heavy else 28
    y0 = 1 if heavy else 2
    y1 = 29 if heavy else 30
    base_top = zb - 3
    roof0 = z_top + 4
    roof1 = roof0 + (5 if heavy else 4)
    xr0 = X0 + OPEN_W
    # socle / roulettes
    if kind == 'compact':
        for (x, y) in ((xl0 + 2, y0 + 2), (xr1 - 2, y0 + 2), (xl0 + 2, y1 - 2), (xr1 - 2, y1 - 2)):
            m.cyl(x, y, 1.3, 0, 2.5, GRAPH)
        plinth = m.box(xl0, y0, 2.5, xr1, y1, base_top, FRAME)
    else:
        plinth = m.box(xl0, y0, 0, xr1, y1, base_top, FRAME)
        plinth.decal('front', rect(0, 0, 99, 1), Delta(-1))
    for face in ('left', 'right'):
        plinth.decal(face, rect(0, 0, 99, 1), Delta(-1))
    # panneaux lateraux
    h = roof0 - base_top
    d = y1 - y0
    lp = m.box(xl0, y0, base_top, X0, y1, roof0, FRAME)
    rp = m.box(xr0, y0, base_top, xr1, y1, roof0, FRAME)
    _side_panel_decals(lp, 'left', d, h, heavy)
    _side_panel_decals(rp, 'right', d, h, heavy)
    # traverses avant (devant le plan des modules)
    m.box(X0, 2, base_top, xr0, YF, zb, FRAME)
    top_rail = m.box(X0, 2, z_top + 1, xr0, YF, roof0, FRAME)
    top_rail.decal('front', rect(1, 1, OPEN_W - 1, 2), Use(ACC, idx=3))
    top_rail.decal('front', rect(OPEN_W - 3, 1, OPEN_W - 2, 2), Fix('led_green'))
    # toit
    roof = m.box(xl0, y0, roof0, xr1, y1, roof1, FRAME)
    roof.decal('top', frame(1, 1, xr1 - xl0 - 1, d - 1), Delta(+1))
    roof.decal('top', rect(6, d - 8, 12, d - 5), Use(GRAPH, idx=0))
    roof.decal('top', rect(xr1 - xl0 - 12, d - 8, xr1 - xl0 - 6, d - 5), Use(GRAPH, idx=0))
    # rails de fixation (visibles dans les slots vides)
    for (a0, a1) in ((X0, X0 + 2), (xr0 - 2, xr0)):
        r = m.box(a0, YF, zb, a1, YF + 1, z_top, STEEL)
        r.decal('front', ('fn', lambda s, t: (np.floor(t).astype(int) % 4 == 1) & (s >= 0.5) & (s < 1.5)), Delta(-2))
    # porte arriere (face interieure visible dans l'ouverture, face exterieure en nw/ne)
    rear = m.box(X0, y1 - 1, base_top, xr0, y1, roof0, FRAME)
    rear.decal('front', rect(0, 0, 99, 999), Use(FRAME, idx=0))
    perforation(rear, 'front', 1, 2, OPEN_W - 1, h - 2, step=2, delta=+1)
    perforation(rear, 'back', 1, 3, OPEN_W - 1, h - 3, step=2, delta=-1)
    rear.decal('back', rect(OPEN_W - 3, h / 2.0 - 6, OPEN_W - 2, h / 2.0 + 6), Use(STEEL, idx=4))
    for t in (6, h - 8):
        rear.decal('back', rect(1, t, 2, t + 3), Use(STEEL, idx=3))
    if heavy:
        # porte arriere refroidissante (echangeur) + conduites
        cool = m.box(xl0, y1, base_top, xr1, 32, roof0, FRAME)
        cool.decal('back', frame(1, 1, xr1 - xl0 - 1, h - 1), Delta(+1))
        for k in range(3):
            fan_grill(cool, 'back', (xr1 - xl0) / 2.0, h - 14 - k * 22, 6.5, spokes=5)
        cool.decal('back', rect(2, 4, 6, 6), Fix('led_blue'))
        m.hcyl('x', 31, 3, 1.2, xl0 + 3, xr1 - 3, M('accent', 'sky'))
        m.hcyl('x', 31, 7, 1.2, xl0 + 3, xr1 - 3, COPPER)
        # panier a cables sur le toit
        bk = m.box(8, 8, roof1, 24, 24, roof1 + 3, GRAPH)
        bk.decal('top', rect(1, 1, 15, 15), Delta(-1))
        m.hcyl('y', 13, roof1 + 3, 1.2, 9, 23, M('accent', 'navy'))
        m.hcyl('y', 18, roof1 + 3, 1.2, 9, 23, M('accent', 'sun'))
        # bandeau d'etat sur la traverse
        top_rail.decal('front', rect(2, 2, 7, 3), Fix('led_blue'))


for _rid, (_n, _zt, _kind) in RACKS.items():
    _names = {'rack_compact': 'Baie compacte (8U, roulettes)', 'rack_standard': 'Baie standard (20U)',
              'rack_high_density': 'Baie haute densite (20U, porte arriere refroidie)'}
    _notes = ("Facade ouverte. Slots de 4 px (1U), facade des modules 20 su. Modules dessines seulement en se/sw ; "
              "en nw/ne la baie montre sa porte arriere perforee.")

    def _mk(n=_n, zt=_zt, kind=_kind):
        def b(m):
            _rack(m, n, zt, kind)
        return b
    iso_asset(_rid, 'servers', 'rack', _names[_rid], footprint=(1, 1), facings=ALL4,
              height=_zt + (14 if _kind == 'hd' else 8), slots=rack_slots(_n, _zt),
              access=[Access('service', 0, -1)], notes=_notes,
              occludes=40 if _kind != 'compact' else None, tags=['rack', 'datacenter'])(_mk())


# ---------------------------------------------------------------------------
# Modules de baie : plaques de facade (constructeur specialise)
# ---------------------------------------------------------------------------
MOD = M('primary', 'graphite')
MOD_L = M('primary', 'steel')


def _plate(m, units, mat=MOD):
    h = units * UNIT
    p = m.box(0, 0, -h, OPEN_W, 2, 0, mat)
    p.decal('front', rect(0, h - 1, OPEN_W, h), Delta(+1))
    p.decal('front', rect(0, 0, OPEN_W, 1), Delta(-2))
    for s0 in (0, OPEN_W - 1):
        p.decal('front', rect(s0, 1, s0 + 1, h - 1), Delta(-1))
    return p, h


def mod_server_1u(m):
    p, h = _plate(m, 1)
    p.decal('front', ('fn', lambda s, t: (s >= 2) & (s < 10) & (t >= 1) & (t < 3) &
                      ((np.floor(s).astype(int) + np.floor(t).astype(int)) % 2 == 0)), Delta(-1))
    for k in range(2):
        p.decal('front', rect(11 + k * 3, 1, 13 + k * 3, 3), Delta(-1))
        p.decal('front', rect(12 + k * 3, 2, 13 + k * 3, 3), Fix('led_green'))
    p.decal('front', rect(18, 2, 19, 3), Fix('led_blue'))


def mod_compute_2u(m):
    p, h = _plate(m, 2)
    for t in (2, 4, 6):
        p.decal('front', rect(2, t, 14, t + 1), Delta(-1))
    p.decal('front', rect(15, 1, 16, h - 1), Use(ACC, idx=3))
    p.decal('front', rect(17, 5, 18, 6), Fix('led_green'))
    p.decal('front', rect(17, 3, 18, 4), Fix('led_amber'))


def mod_storage_2u(m):
    p, h = _plate(m, 2)
    drive_bays(p, 'front', 1.5, 1.5, 6, 2, 2, 2, gs=1, gt=1)


def mod_storage_jbod_4u(m):
    p, h = _plate(m, 4, MOD)
    p.decal('front', rect(1, h - 3, OPEN_W - 1, h - 2), Use(ACC, idx=2))
    drive_bays(p, 'front', 1.5, 1.5, 6, 3, 2, 3, gs=1, gt=1, led_col='led_green', led_every=1)
    p.decal('front', rect(OPEN_W - 3, h - 3, OPEN_W - 2, h - 2), Fix('led_blue'))


def mod_gpu_4u(m):
    p, h = _plate(m, 4, MOD)
    p.decal('front', rect(1, h - 3, OPEN_W - 1, h - 1), Use(CORAL, idx=3))
    for cs in (4.0, 10.0, 16.0):
        fan_grill(p, 'front', cs, 6.5, 2.9, spokes=4)
    p.decal('front', rect(2, 1, 3, 2), Fix('led_green'))
    p.decal('front', rect(4, 1, 5, 2), Fix('led_green'))
    p.decal('front', rect(17, 1, 18, 2), Fix('led_amber'))


def mod_network_switch_1u(m):
    p, h = _plate(m, 1)
    port_row(p, 'front', 2, 15, 1, step=2, led='led_green', led_step=1)
    p.decal('front', rect(15.5, 1, 18.5, 3), Delta(-2))
    p.decal('front', rect(16, 2, 17, 3), Fix('led_amber'))


def mod_patch_panel_1u(m):
    p, h = _plate(m, 1, MOD_L)
    port_row(p, 'front', 2, 18, 1, step=2)
    plug = [ramp_col('sky', 3), ramp_col('sun', 4), ramp_col('coral', 3), ramp_col('leaf', 3), ramp_col('sky', 3),
            ramp_col('plastic', 4), ramp_col('sun', 4), ramp_col('coral', 3)]
    for k, c in enumerate(plug):
        if k % 3 == 2:
            continue
        s = 2 + k * 2
        p.decal('front', rect(s, 2, s + 1, 3), Fix(c))


def mod_compute_node_2u(m):
    p, h = _plate(m, 2)
    p.decal('front', rect(10, 1, 11, h - 1), Delta(-2))
    for s0 in (1.5, 11.5):
        p.decal('front', ('fn', lambda s, t, a=s0: (s >= a + 0.5) & (s < a + 6.5) & (t >= 2) & (t < 6) &
                          ((np.floor(t).astype(int)) % 2 == 0)), Delta(-1))
        p.decal('front', rect(s0 + 7, 5, s0 + 8, 6), Fix('led_green'))
        p.decal('front', rect(s0 + 7, 2, s0 + 8, 4), Use(STEEL, idx=4))


def mod_ups_2u(m):
    p, h = _plate(m, 2, MOD_L)
    p.decal('front', rect(2, 3, 8, 6), Use(LCD, idx=3))
    p.decal('front', rect(3, 4, 6, 5), Use(LCD, idx=5))
    knobs(p, 'front', [(10, 4.5), (12, 4.5)], delta=-2, r=0.6)
    p.decal('front', rect(14, 2, 18, 3), Delta(-1))
    p.decal('front', rect(14, 4, 18, 5), Delta(-1))
    p.decal('front', rect(14, 6, 15, 7), Fix('led_green'))


def mod_blank_1u(m):
    p, h = _plate(m, 1)
    p.decal('front', rect(2, 2, 18, 3), Delta(+1))
    p.decal('front', rect(0, 2, 1, 3), Use(STEEL, idx=4))
    p.decal('front', rect(19, 2, 20, 3), Use(STEEL, idx=4))


def mod_cable_manager_1u(m):
    p, h = _plate(m, 1)
    p.decal('front', rect(3, 1, 17, 3), Use(M('fixed', 'rubber'), idx=0))
    p.decal('front', ('fn', lambda s, t: (s >= 3) & (s < 17) & (t >= 1) & (t < 3) &
                      (np.floor(s).astype(int) % 2 == 0)), Use(M('fixed', 'rubber'), idx=2))
    p.decal('front', rect(1, 1, 2.5, 3), Use(STEEL, idx=4))
    p.decal('front', rect(17.5, 1, 19, 3), Use(STEEL, idx=4))


MODULES = [
    ('module_server_1u', 1, 'Serveur 1U', mod_server_1u),
    ('module_compute_2u', 2, 'Serveur de calcul 2U', mod_compute_2u),
    ('module_storage_2u', 2, 'Stockage / NAS 2U (12 baies)', mod_storage_2u),
    ('module_storage_jbod_4u', 4, 'Tiroir de disques 4U (JBOD)', mod_storage_jbod_4u),
    ('module_gpu_4u', 4, 'Serveur GPU 4U', mod_gpu_4u),
    ('module_network_switch_1u', 1, 'Switch reseau 1U', mod_network_switch_1u),
    ('module_patch_panel_1u', 1, 'Panneau de brassage 1U', mod_patch_panel_1u),
    ('module_compute_node_2u', 2, 'Noeuds de calcul 2U (2 demi-largeurs)', mod_compute_node_2u),
    ('module_ups_2u', 2, 'Onduleur de baie 2U', mod_ups_2u),
    ('module_blank_1u', 1, 'Obturateur 1U', mod_blank_1u),
    ('module_cable_manager_1u', 1, 'Passe-cables 1U', mod_cable_manager_1u),
]


def _chans(mask):
    c = mask[..., 0][mask[..., 3] > 0]
    return [CHANNEL_NAMES[int(x)] for x in sorted(set(np.unique(c).tolist()))] if c.size else []


def render_front_plate(model, facing):
    """Rend le modele et ne garde que les pixels du plan de facade (y local = 0)."""
    res = render(model, facing, pad=1)
    lay = res.layers['base']
    t = res.depth['base']
    with np.errstate(invalid='ignore'):
        u = res.a + t
        v = res.b + t
        Rt = res.R.T
        yl = Rt[1, 0] * (u - res.T[0]) + Rt[1, 1] * (v - res.T[1])
    keep = (lay.pid >= 0) & (np.abs(yl) < 1e-6)
    rgba = np.where(keep[..., None], lay.rgba, 0).astype(np.uint8)
    mask = np.where(keep[..., None], lay.mask, 0).astype(np.uint8)
    ys, xs = np.nonzero(keep)
    x0, y0, x1, y1 = xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
    ax, ay = res.anchor
    return rgba[y0:y1, x0:x1], mask[y0:y1, x0:x1], (ax - x0, ay - y0)


@custom_builder('servers_modules', order=60)
def build_rack_modules(root, log=None):
    entries = []
    for (mid, units, name, fn) in MODULES:
        e = {'id': mid, 'category': 'servers', 'subcategory': 'rack_module', 'name_fr': name,
             'pack_version': PACK_VERSION, 'usage': 'current', 'kind': 'rack_module', 'render_layer': 'objects',
             'silhouette_group': 'rack_module', 'units': units, 'unit_px': UNIT, 'face_width_su': OPEN_W,
             'fits': list(RACKS.keys()), 'provides': ['rack_module'],
             'placement': ("ancre du module = slots.positions[i].offset_px de la baie (meme orientation) ; "
                           "occupe les slots i .. i+units-1 ; dessiner apres la baie, du bas vers le haut ; "
                           "orientations se/sw uniquement"),
             'orientations': {}, 'channels': {}}
        for f in ('se', 'sw'):
            m = Model(0, 0, centered=True, outline='none', shadow=False, edge_hl=False)
            fn(m)
            for p in m.prims:
                mats = p.mat.values() if isinstance(p.mat, dict) else [p.mat]
                for mt in mats:
                    if hasattr(mt, 'channel') and mt.channel != 'fixed':
                        e['channels'].setdefault(mt.channel, mt.ramp)
                for (_, _, paint) in p.decals:
                    mt = getattr(paint, 'mat', None)
                    if hasattr(mt, 'channel') and mt.channel != 'fixed':
                        e['channels'].setdefault(mt.channel, mt.ramp)
            rgba, mask, anc = render_front_plate(m, f)
            png = f"assets/servers/{mid}__{f}.png"
            msk = f"masks/servers/{mid}__{f}__mask.png"
            save_png(os.path.join(root, png), rgba)
            L = {'file': png}
            if (mask[..., 3] > 0).any():
                save_png(os.path.join(root, msk), mask)
                L['mask'] = msk
                L['mask_channels'] = _chans(mask)
            e['orientations'][f] = {'size': [rgba.shape[1], rgba.shape[0]], 'anchor': list(anc), 'layers': {'base': L}}
        entries.append(e)
        if log:
            log(f"  {mid}")
    return entries


# ---------------------------------------------------------------------------
# Serveurs et stockage
# ---------------------------------------------------------------------------
@iso_asset('server_tower_classic', 'servers', 'server', 'Serveur tour (pied)', footprint=(1, 1), facings=ALL4, height=34,
           access=[Access('service', 0, -1)], tags=['server'],
           notes='Serveur sur pied : 8 baies remplacables a chaud, ecran d\'etat ; plus haut et profond qu\'une tour PC.')
def _(m):
    for (x, y) in ((8, 4), (22, 4), (8, 26), (22, 26)):
        m.box(x, y, 0, x + 2, y + 2, 1.5, GRAPH)
    body = m.box(7, 3, 1.5, 25, 29, 34, FRAME)
    h = 32.5
    body.decal('front', rect(1, h - 6, 17, h - 1), Delta(-1))
    body.decal('front', rect(2, h - 5, 8, h - 2), Use(LCD, idx=3))
    body.decal('front', rect(3, h - 4, 6, h - 3), Use(LCD, idx=5))
    for k in range(3):
        body.decal('front', rect(10 + k * 2, h - 4, 11 + k * 2, h - 3), Fix(['led_green', 'led_green', 'led_amber'][k]))
    drive_bays(body, 'front', 1.5, 8, 2, 4, 7, 3, gs=1, gt=1)
    vents(body, 'front', 2, 16, 2, 6, step=2)
    body.decal('front', rect(0, 0, 1, h), Use(ACC, idx=3))
    for face in ('left', 'right'):
        body.decal(face, frame(3, 3, 23, h - 3), Delta(-1))
        vents(body, face, 18, 23, 6, h - 8, step=2)
    body.decal('top', rect(5, 9, 13, 17), Delta(-1))
    # face arriere : alimentation, ports, ventilation
    fan_grill(body, 'back', 13, h - 6, 3.6, spokes=5)
    port_row(body, 'back', 3, 9, h - 8, step=2)
    port_row(body, 'back', 3, 9, h - 11, step=2, led='led_green', led_step=2)
    body.decal('back', rect(2, 4, 16, 18), Delta(-1))
    vents(body, 'back', 3, 15, 5, 17, step=2)
    body.decal('back', rect(11, h - 13, 15, h - 11), Use(GRAPH, idx=0))


@iso_asset('nas_desktop', 'servers', 'server', 'NAS de bureau (4 baies)', centered=True, size_su=(10, 14), facings=ALL4,
           height=12, provides=['desk_item', 'shelf_item'], tags=['storage'],
           notes='Petit serveur de stockage pose sur un bureau ou une etagere.')
def _(m):
    b = m.box(-5, -7, 0.5, 5, 7, 12, FRAME)
    for (x, y) in ((-4, -6), (3, -6), (-4, 5), (3, 5)):
        m.box(x, y, 0, x + 1, y + 1, 0.5, GRAPH)
    for k in range(4):
        s = 0.8 + k * 2.2
        b.decal('front', rect(s, 1.5, s + 1.6, 9.5), Delta(-1))
        b.decal('front', rect(s, 8.5, s + 1.6, 9.5), Delta(+1))
        b.decal('front', rect(s + 0.6, 10, s + 1.6, 11), Fix('led_green' if k < 3 else 'led_amber'))
    b.decal('top', frame(1, 1, 9, 13), Delta(+1))
    fan_grill(b, 'back', 5, 6.5, 3.4, spokes=5)
    port_row(b, 'back', 1.5, 4, 2, step=2)
    vents(b, 'left', 3, 11, 3, 10, step=2)
    vents(b, 'right', 3, 11, 3, 10, step=2)


# ---------------------------------------------------------------------------
# Energie et refroidissement
# ---------------------------------------------------------------------------
@iso_asset('ups_floor', 'servers', 'power', 'Onduleur au sol', footprint=(1, 1), facings=FRONT2, height=26,
           access=[Access('service', 0, -1)], tags=['power'],
           notes='Bloc trapu : afficheur, jauge de batterie a LED, poignees de levage.')
def _(m):
    body = m.box(5, 6, 0, 27, 27, 24, STEELP)
    fp = m.box(7, 5, 2, 25, 6, 22, FRAME)
    fp.decal('front', rect(2, 12, 11, 18), Use(LCD, idx=2))
    fp.decal('front', rect(3, 13, 10, 17), Use(LCD, idx=4))
    fp.decal('front', rect(4, 15, 7, 16), Use(LCD, idx=1))
    for k in range(5):
        fp.decal('front', rect(2 + k * 2, 9, 3 + k * 2, 10), Fix('led_green' if k < 4 else 'led_amber'))
    knobs(fp, 'front', [(14, 15), (16, 15)], delta=+2, r=0.8)
    vents(fp, 'front', 2, 16, 2, 7, step=2)
    for face in ('left', 'right'):
        body.decal(face, frame(2, 2, 19, 22), Delta(-1))
        vents(body, face, 12, 18, 4, 18, step=2)
    for x in (8, 20):
        m.box(x, 14, 24, x + 4, 15, 26, GRAPH)
        m.box(x, 14, 25, x + 4, 19, 26, GRAPH)
        m.box(x, 18, 24, x + 4, 19, 26, GRAPH)


@iso_asset('power_distribution_cabinet', 'servers', 'power', 'Armoire de distribution electrique', footprint=(1, 1),
           facings=FRONT2, height=84, access=[Access('service', 0, -1)], occludes=50, tags=['power'],
           notes='Armoire murale profonde de 20 su : porte, fenetre de disjoncteurs, pictogramme sans texte, conduits.')
def _(m):
    m.box(5, 10, 0, 27, 28, 4, FRAME)
    cab = m.box(5, 10, 4, 27, 28, 84, LIGHT)
    door = m.box(6, 9, 6, 26, 10, 82, LIGHT)
    door.decal('front', frame(1, 1, 19, 75), Delta(-1))
    door.decal('front', rect(3, 44, 17, 58), Use(GRAPH, idx=1))
    cols = [ramp_col('plastic', 4), ramp_col('plastic', 4), ramp_col('red', 3), ramp_col('plastic', 4),
            ramp_col('sun', 4), ramp_col('plastic', 4)]
    for row in range(2):
        for k, c in enumerate(cols):
            door.decal('front', rect(4 + k * 2.2, 46 + row * 6, 5.2 + k * 2.2, 49 + row * 6), Fix(c))
    door.decal('front', rect(16, 30, 17, 40), Use(GRAPH, idx=2))
    door.decal('front', rect(15, 34, 17, 36), Use(GRAPH, idx=3))
    hazard_triangle(door, 'front', 6.5, 62, 7)
    vents(door, 'front', 3, 17, 4, 10, step=2)
    for face in ('left', 'right'):
        cab.decal(face, frame(2, 2, 16, 78), Delta(-1))
    for x in (10, 18):
        c = m.cyl(x, 20, 1.6, 84, 112, STEEL)
        m.cyl(x, 20, 2.2, 84, 86, GRAPH)


@iso_asset('rack_cooler_inrow', 'servers', 'cooling', 'Refroidissement de rangee (in-row)', footprint=(1, 1), facings=ALL4,
           height=96, access=[Access('service', 0, -1)], occludes=40, tags=['cooling', 'datacenter'],
           notes='Demi-largeur de baie, meme hauteur que rack_standard : facade perforee, ventilateurs, ecran de controle.')
def _(m):
    m.box(8, 2, 0, 24, 30, 5, FRAME)
    body = m.box(8, 2, 5, 24, 30, 92, FRAME)
    m.box(8, 2, 92, 24, 30, 96, FRAME)
    h = 87
    body.decal('front', frame(1, 1, 15, h - 1), Delta(+1))
    perforation(body, 'front', 2, 3, 14, h - 14, step=2, delta=-1)
    for k in range(3):
        fan_grill(body, 'front', 8, 14 + k * 22, 5, spokes=5, dark=-1)
    body.decal('front', rect(3, h - 11, 13, h - 4), Use(GRAPH, idx=0))
    body.decal('front', rect(4, h - 10, 12, h - 5), Use(M('screen', 'sky'), idx=3))
    body.decal('front', rect(5, h - 8, 10, h - 7), Use(M('screen', 'sky'), idx=5))
    body.decal('front', rect(13, h - 13, 14, h - 12), Fix('led_blue'))
    perforation(body, 'back', 2, 3, 14, h - 3, step=2, delta=-1)
    for face in ('left', 'right'):
        body.decal(face, frame(2, 3, 26, h - 3), Delta(-1))
    m.hcyl('x', 29, 2, 1.2, 10, 22, M('accent', 'sky'))
    m.hcyl('x', 29, 4.5, 1.2, 10, 22, COPPER)


@iso_asset('crac_unit', 'servers', 'cooling', 'Climatiseur de salle informatique', footprint=(2, 1), facings=FRONT2,
           height=76, access=[Access('service', 0, -1), Access('service', 1, -1)], occludes=40, tags=['cooling'],
           notes='Armoire de climatisation : grandes persiennes, panneau de commande, reprise d\'air sur le dessus.')
def _(m):
    m.box(2, 4, 0, 62, 30, 4, FRAME)
    body = m.box(2, 4, 4, 62, 30, 76, LIGHT)
    h = 72
    for k in range(2):
        s0 = 3 + k * 30
        body.decal('front', frame(s0, 6, s0 + 24, 52), Delta(-1))
        body.decal('front', ('fn', lambda s, t, a=s0: (s >= a + 2) & (s < a + 22) & (t >= 8) & (t < 50) &
                             (np.floor(t).astype(int) % 3 == 0)), Delta(-1))
        body.decal('front', ('fn', lambda s, t, a=s0: (s >= a + 2) & (s < a + 22) & (t >= 8) & (t < 50) &
                             (np.floor(t).astype(int) % 3 == 1)), Delta(+1))
    body.decal('front', rect(20, 56, 40, 68), Use(GRAPH, idx=2))
    body.decal('front', rect(22, 58, 32, 66), Use(LCD, idx=3))
    body.decal('front', rect(23, 63, 30, 64), Use(LCD, idx=5))
    body.decal('front', rect(23, 60, 28, 61), Use(LCD, idx=5))
    knobs(body, 'front', [(35, 63), (37.5, 63)], delta=+2, r=0.8)
    body.decal('front', rect(35, 59, 36, 60), Fix('led_green'))
    body.decal('front', rect(0, 0, 60, 1), Delta(-1))
    body.decal('top', frame(3, 3, 57, 23), Delta(-1))
    perforation(body, 'top', 5, 5, 55, 21, step=2, delta=-1)
    for face in ('left', 'right'):
        body.decal(face, frame(2, 4, 24, 66), Delta(-1))


# ---------------------------------------------------------------------------
# Cheminement de cables
# ---------------------------------------------------------------------------
TRAY_Z = 98


def _tray_segment(m, x0, x1, axis='x', c=16):
    """Chemin de cables en echelle le long de x (ou y) local, centre en c."""
    SM = M('metal', 'steel')
    if axis == 'x':
        m.box(x0, c - 6, TRAY_Z, x1, c - 5, TRAY_Z + 4, SM)
        m.box(x0, c + 5, TRAY_Z, x1, c + 6, TRAY_Z + 4, SM)
        r = x0 + 2
        while r < x1 - 1:
            m.box(r, c - 5, TRAY_Z, r + 1, c + 5, TRAY_Z + 1, SM, outline=False)
            r += 5
        for (cc, col) in ((c - 3, 'navy'), (c - 1, 'sun'), (c + 1.5, 'teal'), (c + 3.5, 'navy')):
            m.hcyl('x', cc, TRAY_Z + 2, 1.0, x0, x1, M('accent', col), outline=False)
    else:
        m.box(c - 6, x0, TRAY_Z, c - 5, x1, TRAY_Z + 4, SM)
        m.box(c + 5, x0, TRAY_Z, c + 6, x1, TRAY_Z + 4, SM)
        r = x0 + 2
        while r < x1 - 1:
            m.box(c - 5, r, TRAY_Z, c + 5, r + 1, TRAY_Z + 1, SM, outline=False)
            r += 5
        for (cc, col) in ((c - 3, 'navy'), (c - 1, 'sun'), (c + 1.5, 'teal'), (c + 3.5, 'navy')):
            m.hcyl('y', cc, TRAY_Z + 2, 1.0, x0, x1, M('accent', col), outline=False)


@iso_asset('cable_tray_straight', 'servers', 'cabling', 'Chemin de cables suspendu (droit)', footprint=(1, 1),
           facings=FRONT2, height=112, collision=[], walkable=True, tags=['overhead'],
           model_kw={'shadow': False},
           notes=("Echelle a cables a z=98-104, tiges de suspension jusqu'a z=112 ; se = le long de V, sw = le long de U. "
                  "Tri : comme un objet au sol (tri topologique des emprises) ; sur une case partagee avec un objet "
                  "pose dessous, dessiner APRES cet objet. Ancre au sol (hors toile), aucune collision."))
def _(m):
    _tray_segment(m, 0, 32, 'x')
    for x in (5, 27):
        m.box(x, 9, TRAY_Z + 4, x + 1, 10, 112, M('metal', 'graphite'))
        m.box(x, 22, TRAY_Z + 4, x + 1, 23, 112, M('metal', 'graphite'))
        m.box(x - 1, 9, TRAY_Z - 1, x + 2, 23, TRAY_Z, M('metal', 'graphite'))
    m.box(0, 9, TRAY_Z, 1, 23, TRAY_Z + 4, M('metal', 'graphite'))


@iso_asset('cable_tray_corner', 'servers', 'cabling', 'Chemin de cables suspendu (angle)', footprint=(1, 1),
           facings=ALL4, height=112, collision=[], walkable=True, tags=['overhead'],
           model_kw={'shadow': False},
           notes=("Angle a 90 deg : bras vers l'avant local (-y) et vers la droite locale (+x), centres dans la case ; "
                  "se raccorde aux troncons droits voisins. Meme regle de tri que cable_tray_straight."))
def _(m):
    SM = M('metal', 'steel')
    GR = M('metal', 'graphite')
    c0, c1 = 10, 22                      # carre d'angle ; coin interieur (22, 10), exterieur (10, 22)
    m.box(c0, c1 - 1, TRAY_Z, 32, c1, TRAY_Z + 4, SM)      # rail exterieur du bras +x
    m.box(c1, c0, TRAY_Z, 32, c0 + 1, TRAY_Z + 4, SM)      # rail interieur du bras +x
    m.box(c0, 0, TRAY_Z, c0 + 1, c1, TRAY_Z + 4, SM)       # rail exterieur du bras -y
    m.box(c1 - 1, 0, TRAY_Z, c1, c0, TRAY_Z + 4, SM)       # rail interieur du bras -y
    m.box(c0 + 1, c0 + 1, TRAY_Z, c1 - 1, c1 - 1, TRAY_Z + 1, SM, outline=False)
    for r in (25, 30):
        m.box(r, c0 + 1, TRAY_Z, r + 1, c1 - 1, TRAY_Z + 1, SM, outline=False)
    for r in (2, 7):
        m.box(c0 + 1, r, TRAY_Z, c1 - 1, r + 1, TRAY_Z + 1, SM, outline=False)
    # cables en L emboites (chaque cable tourne dans l'angle)
    for (yc, xc, col) in ((13, 19.5, 'navy'), (15, 17.5, 'sun'), (17.5, 15, 'teal'), (19.5, 13, 'navy')):
        m.hcyl('x', yc, TRAY_Z + 2, 1.0, xc - 1, 32, M('accent', col), outline=False)
        m.hcyl('y', xc, TRAY_Z + 2, 1.0, 0, yc + 1, M('accent', col), outline=False)
    m.box(15, 15, TRAY_Z + 1, 17, 17, 112, GR)
    m.box(31, 9, TRAY_Z, 32, 23, TRAY_Z + 4, GR)
    m.box(9, 0, TRAY_Z, 23, 1, TRAY_Z + 4, GR)


@iso_asset('cable_conduit', 'servers', 'cabling', 'Passe-cable au sol', footprint=(1, 1), facings=FRONT2, height=3,
           collision=[], walkable=True, tags=['floor_decor'],
           notes='Protection de cables franchissable (rampe). se = le long de V, sw = le long de U.')
def _(m):
    k = 1.2
    planes = [(1, 0, 0, 32, 'right'), (-1, 0, 0, 0, 'left'),
              (0, 0, 1, 3, 'top'), (0, 0, -1, 0, 'bottom'),
              (0, -k, 1, 3 - 12 * k, 'front'), (0, k, 1, 3 + 20 * k, 'back')]
    p = m.poly(planes, (0, 9, 0, 32, 23, 3), M('accent', 'hazard'))
    p.decal('top', ('fn', lambda s, t: (np.floor(s).astype(int) % 8) < 4), Use(M('fixed', 'rubber'), idx=2))
    for f in ('front', 'back'):
        p.decal(f, ('fn', lambda s, t: (np.floor(s).astype(int) % 8) < 4), Use(M('fixed', 'rubber'), idx=1))


# ---------------------------------------------------------------------------
# Supervision et securite
# ---------------------------------------------------------------------------
@iso_asset('supervision_console', 'servers', 'console', 'Console de supervision (NOC)', footprint=(3, 1), facings=ALL4,
           height=56, tags=['workstation'],
           screens=[{'name': f'screen_{k}', 'quad': [(6 + 30 * k, 23.5, 54), (28 + 30 * k, 23.5, 54),
                                                     (28 + 30 * k, 23.5, 38), (6 + 30 * k, 23.5, 38)]} for k in range(3)],
           sockets=[Socket('keyboard', ['keyboard'], 48, 10, 30, 'clavier central'),
                    Socket('desk_left', ['desk_item'], 10, 12, 30), Socket('desk_right', ['desk_item'], 86, 12, 30),
                    Socket('chair', ['chair'], 48, -12, 0, 'operateur', facing_rel='opposite', group='workstation')],
           access=[Access('work', 0, -1), Access('work', 1, -1), Access('work', 2, -1)],
           notes='Pupitre long, pont d\'ecrans integre (3 zones d\'affichage), caissons ventiles.')
def _(m):
    top = m.box(1, 2, 27, 95, 30, 30, M('primary', 'graphite'))
    top.decal('top', rect(1, 1, 93, 3), Delta(+1))
    top.decal('front', rect(0, 0, 94, 1), Delta(-1))
    for x0 in (2, 72):
        c = m.box(x0, 4, 0, x0 + 22, 29, 27, FRAME)
        vents(c, 'front', 3, 19, 4, 20, step=2)
        c.decal('front', rect(17, 22, 18, 23), Fix('led_green'))
    m.box(24, 26, 0, 72, 28, 27, FRAME)
    m.box(2, 23, 30, 94, 29, 34, FRAME)
    for k in range(3):
        x0 = 5 + 30 * k
        m.box(x0 + 10, 25, 34, x0 + 14, 27, 38, GRAPH)
        sc = m.box(x0, 23.5, 37, x0 + 24, 25.5, 55, M('primary', 'graphite'))
        if k == 1:
            ui_screen(sc, 'front', 24, 18, seed=7)
        else:
            code_screen(sc, 'front', 24, 18, seed=11 + k)
        sc.decal('back', rect(8, 6, 16, 12), Delta(-1))


@iso_asset('security_cage_panel', 'servers', 'security', 'Grille de securite (cage)', footprint=(1, 1), facings=FRONT2,
           height=88, collision=[(0, 0)], tags=['security'],
           notes='Panneau grillage soude centre dans la case ; se = le long de V, sw = le long de U ; poteaux partages.')
def _(m):
    _cage(m, door=False)


@iso_asset('security_cage_door', 'servers', 'security', 'Porte de cage grillagee', footprint=(1, 1), facings=FRONT2,
           height=88, collision=[], tags=['security'], access=[Access('pass', 0, -1)],
           notes='Porte grillagee fermee (statique) avec serrure a badge ; franchissable par le jeu.')
def _(m):
    _cage(m, door=True)


def _cage(m, door=False):
    POST = M('metal', 'graphite')
    WIRE = ramp_col('steel', 2)
    WIRE_HL = ramp_col('steel', 4)
    for (a0, a1) in ((0, 1.5), (30.5, 32)):
        m.box(a0, 15, 0, a1, 17, 88, POST)
    m.box(1.5, 15.5, 85, 30.5, 16.5, 88, POST)
    m.box(1.5, 15.5, 1, 30.5, 16.5, 4, POST)
    if door:
        m.box(4, 15.5, 4, 5.5, 16.5, 82, POST)
        m.box(26.5, 15.5, 4, 28, 16.5, 82, POST)
        m.box(5.5, 15.5, 80, 26.5, 16.5, 82, POST)
        m.box(5.5, 15.5, 42, 26.5, 16.5, 44, POST)
        lock = m.box(23, 14, 40, 26, 15.5, 47, M('primary', 'graphite'))
        lock.decal('front', rect(1, 4, 2, 5), Fix('led_red'))
        lock.decal('front', rect(0.5, 1, 2.5, 3), Use(GRAPH, idx=4))
    plate = m.box(1.5, 15.7, 4, 30.5, 16.3, 85, Fix((0, 0, 0, 0)), outline=False, shadow=False, edges=False)
    plate.decal('sides', ('fn', lambda s, t: ((np.floor(s).astype(int) % 3) == 1) | ((np.floor(t).astype(int) % 4) == 2)),
                Fix(WIRE))
    plate.decal('sides', ('fn', lambda s, t: ((np.floor(t).astype(int) % 4) == 2) & ((np.floor(s).astype(int) % 3) == 1)),
                Fix(WIRE_HL))
