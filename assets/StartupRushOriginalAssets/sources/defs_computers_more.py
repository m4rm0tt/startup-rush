"""Postes informatiques (suite) : portables, peripheriques, impression, visio,
kits de test, reseau. Sans marque ni logo."""
from srgen.iso import Fix, Use, Delta, rect, hline, vline, frame, disc
from srgen.palette import M, RAMPS
from srgen.registry import iso_asset, Socket, Access, ALL4, FRONT2, ONE
from srgen.shapes import code_screen, ui_screen, led, vents, rbox, drawers

GRAPH = M('metal', 'graphite')
STEELM = M('metal', 'steel')
CASE_DARK = M('primary', 'graphite')
CASE_LIGHT = M('primary', 'plastic')
SCREEN = M('screen', 'display')


# ---------------------------------------------------------------------------
# Portables (origine = centre de la base ; ecran tourne vers l'utilisateur)
# ---------------------------------------------------------------------------
def _kb(w, d):
    return ('fn', lambda s, t: ((t.astype(int) % 2) == 0) & ((s.astype(int) % 2) == 0) & (s > 1) & (s < w - 1)
            & (t > d * 0.45) & (t < d - 1.5))


def _laptop(m, w, d, base_h, lid_h, case, thick_lid=1.0, tilt=2.5, seed=0, vents_side=False, accent=None, content='code'):
    hw, hd = w / 2.0, d / 2.0
    b = m.box(-hw, -hd, 0, hw, hd, base_h, case)
    b.decal('top', _kb(w, d), Delta(-1))
    b.decal('top', rect(w / 2 - 2.5, 1, w / 2 + 2.5, d * 0.38), Delta(-1))
    if vents_side:
        vents(b, 'right', 2, d - 2, 0.5, base_h, step=2, vertical=True)
        vents(b, 'left', 2, d - 2, 0.5, base_h, step=2, vertical=True)
    if accent:
        b.decal('front', hline(0, w, base_h - 1), Use(accent, idx=3))
    lid = m.slab_tilt(-hw, hw, hd - 0.5, base_h, hd + tilt, base_h + lid_h, thick_lid, case)
    if content == 'code':
        code_screen(lid, 'front', w, lid_h + 1, seed=seed, margin=1)
    else:
        ui_screen(lid, 'front', w, lid_h + 1, seed=seed, margin=1)
    lid.decal('back', rect(w / 2 - 2, lid_h / 2 - 1.5, w / 2 + 2, lid_h / 2 + 1.5), Delta(+1))
    return b, lid


def laptop_quad(w, d, base_h, lid_h, tilt):
    hw, hd = w / 2.0, d / 2.0
    return {'name': 'screen', 'quad': [(-hw + 1, hd + tilt - 0.2, base_h + lid_h - 1), (hw - 1, hd + tilt - 0.2, base_h + lid_h - 1),
                                        (hw - 1, hd - 0.4, base_h + 1), (-hw + 1, hd - 0.4, base_h + 1)]}


@iso_asset('laptop_old', 'computers', 'laptop', 'Portable ancien', centered=True, size_su=(16, 12), facings=ALL4, height=13,
           provides=['laptop'], screens=[laptop_quad(16, 12, 2.5, 10, 2.5)], notes='Epais, beige, petit ecran : point de depart.')
def _(m):
    _laptop(m, 16, 12, 2.5, 10, M('primary', 'beige_pc'), thick_lid=1.5, seed=7)


@iso_asset('laptop_standard', 'computers', 'laptop', 'Portable standard', centered=True, size_su=(18, 12), facings=ALL4,
           height=13, provides=['laptop'], screens=[laptop_quad(18, 12, 1.5, 11, 3)])
def _(m):
    _laptop(m, 18, 12, 1.5, 11, M('primary', 'steel'), seed=8, tilt=3)


@iso_asset('laptop_pro', 'computers', 'laptop', 'Portable professionnel', centered=True, size_su=(20, 13), facings=ALL4,
           height=14, provides=['laptop'], screens=[laptop_quad(20, 13, 1.2, 12.5, 3)])
def _(m):
    _laptop(m, 20, 13, 1.2, 12.5, M('primary', 'graphite'), thick_lid=0.8, seed=9, tilt=3, content='ui')


@iso_asset('laptop_gaming', 'computers', 'laptop', 'Portable puissant', centered=True, size_su=(22, 15), facings=ALL4,
           height=15, provides=['laptop'], screens=[laptop_quad(22, 15, 3, 12, 2.5)],
           notes="Chassis epais, grilles laterales, un seul filet d'accent (pas de RGB partout).")
def _(m):
    _laptop(m, 22, 15, 3, 12, M('primary', 'graphite'), thick_lid=1.2, seed=10, vents_side=True, accent=M('accent', 'teal'))


# ---------------------------------------------------------------------------
# Peripheriques et accessoires (objets poses sur un bureau)
# ---------------------------------------------------------------------------
@iso_asset('graphics_tablet', 'computers', 'peripheral', 'Tablette graphique', centered=True, size_su=(16, 11), facings=ALL4,
           height=2, provides=['desk_item'])
def _(m):
    t = m.box(-8, -5.5, 0, 8, 5.5, 1, CASE_DARK)
    t.decal('top', rect(2.5, 1.5, 14, 9.5), Delta(+1))
    t.decal('top', rect(0.5, 1.5, 1.5, 9.5), Delta(-1))
    rbox(m, 3, -1, 9, 1, 30, 1, 2, M('accent', 'coral'))


@iso_asset('headset_stand', 'computers', 'peripheral', 'Casque sur support', centered=True, size_su=(10, 10), facings=ALL4,
           height=16, provides=['desk_item'])
def _(m):
    m.cyl(0, 0, 3.5, 0, 1, GRAPH)
    m.box(-0.6, -0.6, 1, 0.6, 0.6, 13, GRAPH)
    m.box(-4.5, -1.2, 13, 4.5, 1.2, 14.5, CASE_DARK)
    m.box(-5.5, -1.2, 10, -4.5, 1.2, 14.5, CASE_DARK)
    m.box(4.5, -1.2, 10, 5.5, 1.2, 14.5, CASE_DARK)
    m.ell(-5.5, 0, 8, 1.5, 3, 3, M('fabric', 'teal'))
    m.ell(5.5, 0, 8, 1.5, 3, 3, M('fabric', 'teal'))


@iso_asset('webcam', 'computers', 'peripheral', 'Webcam', centered=True, size_su=(6, 4), facings=ALL4, height=3,
           provides=['webcam'], notes="Se clipse sur le socket 'top' d'un ecran.")
def _(m):
    w = m.box(-3, -1.5, 0, 3, 1.5, 2.5, CASE_DARK)
    w.decal('front', rect(2.5, 0.5, 3.5, 1.5), Fix(RAMPS['sky'][4]))
    w.decal('front', rect(4.5, 1, 5, 1.5), Fix('led_green'))


@iso_asset('docking_station', 'computers', 'peripheral', "Station d'accueil", centered=True, size_su=(10, 6), facings=ALL4,
           height=3, provides=['desk_item'])
def _(m):
    d = m.box(-5, -3, 0, 5, 3, 2.5, CASE_DARK)
    for k in range(3):
        d.decal('front', rect(2 + k * 2, 0.8, 3 + k * 2, 1.6), Fix('ink'))
    d.decal('front', rect(8.5, 1, 9.5, 1.7), Fix('led_white'))


@iso_asset('printer_mfp', 'computers', 'office', 'Imprimante multifonction', footprint=(1, 1), facings=FRONT2, height=37,
           notes='Imprimante/scanner au sol : bacs, panneau de commande, sortie papier.')
def _(m):
    b = m.box(4, 6, 2, 28, 28, 30, CASE_LIGHT)
    drawers(b, 'front', 1, 23, 1, 13, 2)
    b.decal('front', rect(2, 15, 22, 17), Use(GRAPH, idx=2))
    m.box(5, 7, 0, 27, 27, 2, GRAPH)
    m.box(7, 9, 20, 22, 22, 21.5, Fix('paper'))
    sc = m.box(4, 8, 30, 28, 28, 36, CASE_LIGHT)
    sc.decal('front', hline(0, 24, 4), Delta(-1))
    panel = m.box(18, 2, 31, 28, 8, 34, M('primary', 'graphite'))
    panel.decal('top', rect(1, 1, 6, 4), Use(SCREEN, idx=3))
    panel.decal('top', rect(7, 2, 8, 3), Fix('led_green'))
    m.box(8, 9, 36, 24, 26, 37, M('glass', 'glass'))


@iso_asset('videoconf_bar', 'computers', 'av', 'Barre de visioconference', centered=True, size_su=(24, 5), facings=FRONT2,
           height=4, provides=['videoconf_bar', 'webcam'], notes='Camera + haut-parleurs, se pose sur un ecran de presentation.')
def _(m):
    b = m.box(-12, -2.5, 0, 12, 2.5, 3.5, CASE_DARK)
    b.decal('front', ('fn', lambda s, t: (((s.astype(int) % 2) == 0) & ((s < 8) | (s > 16)) & (t > 0.5) & (t < 3))), Delta(+1))
    b.decal('front', rect(11, 1, 13, 3), Use(M('fixed', 'sky'), idx=2))
    b.decal('front', rect(11.5, 1.5, 12.5, 2.5), Fix(RAMPS['sky'][5]))
    b.decal('front', rect(14, 2, 15, 3), Fix('led_green'))


@iso_asset('speakerphone', 'computers', 'av', 'Pieuvre de conference', centered=True, size_su=(10, 10), facings=ONE,
           symmetric=True, height=2, provides=['speakerphone', 'desk_item'])
def _(m):
    c = m.cyl(0, 0, 5, 0, 1.6, CASE_DARK)
    c.decal('top', disc(5, 5, 4, 4), Delta(+1))
    c.decal('top', ('fn', lambda s, t: (((s - 5) ** 2 + (t - 5) ** 2) < 9) & (((s.astype(int) + t.astype(int)) % 2) == 0)), Delta(-1))
    c.decal('top', disc(5, 5, 1.2, 1.2), Fix('led_green'))


@iso_asset('device_test_rack', 'computers', 'test', 'Poste de test mobile', centered=True, size_su=(24, 10), facings=ALL4,
           height=14, provides=['desk_item'], notes='Support incline : 3 telephones + 1 tablette branches.')
def _(m):
    base = m.box(-12, -5, 0, 12, 5, 2, GRAPH)
    base.decal('front', hline(1, 23, 1), Delta(+1))
    devs = [(-11, 4, 9), (-6, 4, 9), (-1, 4, 9), (4, 7, 11)]
    for k, (x0, w, h) in enumerate(devs):
        d = m.slab_tilt(x0, x0 + w, -1, 2, 1.5, 2 + h, 1, M('primary', 'graphite'))
        d.decal('front', rect(0.5, 1, w - 0.5, h - 0.5), Use(SCREEN, idx=[3, 2, 4, 3][k]))
        d.decal('front', rect(1, h - 3, w - 1, h - 2), Fix(RAMPS['sun'][4]))
        m.box(x0 + w / 2.0 - 0.5, 2, 0.5, x0 + w / 2.0 + 0.5, 4, 2.5, M('fixed', 'plastic'))


@iso_asset('console_test_kit', 'computers', 'test', 'Kit de test console generique', centered=True, size_su=(22, 12),
           facings=ALL4, height=6, provides=['desk_item'], notes='Boitier de console generique + manette + console portable.')
def _(m):
    c = m.box(-11, -2, 0, 1, 6, 4, CASE_LIGHT)
    c.decal('top', rect(1, 1, 11, 2), Delta(-1))
    c.decal('front', rect(9, 1, 10, 2), Fix('led_blue'))
    m.ell(6, -2, 0.8, 4, 2.5, 1.2, CASE_DARK)
    m.ell(3.5, -2, 0.8, 1.6, 2, 1.2, CASE_DARK)
    m.ell(8.5, -2, 0.8, 1.6, 2, 1.2, CASE_DARK)
    hh = m.box(3, 2, 0, 11, 6, 1.5, M('primary', 'coral'))
    hh.decal('top', rect(2, 0.5, 6, 3.5), Use(SCREEN, idx=3))


@iso_asset('xr_headset_stand', 'computers', 'test', 'Casque XR sur support', centered=True, size_su=(10, 10), facings=ALL4,
           height=16, provides=['desk_item'])
def _(m):
    m.cyl(0, 0, 3.5, 0, 1, GRAPH)
    m.box(-0.6, -0.6, 1, 0.6, 0.6, 9, GRAPH)
    v = m.box(-4.5, -3, 9, 4.5, 1, 14, M('primary', 'plastic'))
    v.decal('front', rect(1, 1, 8, 4), Use(M('fixed', 'graphite'), idx=1))
    v.decal('front', rect(2, 3, 3, 4), Fix(RAMPS['sky'][4]))
    m.box(-4, 1, 10.5, 4, 4, 12.5, M('fabric', 'graphite'))


@iso_asset('capture_device', 'computers', 'test', 'Boitier de capture', centered=True, size_su=(10, 7), facings=ALL4,
           height=3, provides=['desk_item'])
def _(m):
    b = m.box(-5, -3.5, 0, 5, 3.5, 2.5, M('primary', 'graphite'))
    b.decal('top', rect(1, 1, 9, 6), Delta(+1))
    b.decal('front', rect(1, 0.6, 3, 1.8), Fix('ink'))
    b.decal('front', rect(4, 0.6, 6, 1.8), Fix('ink'))
    b.decal('front', rect(8, 1, 9, 1.8), Fix('led_red'))
    m.hcyl('y', 2, 0.6, 0.5, -9, -3.5, M('fixed', 'graphite'))


# ---------------------------------------------------------------------------
# Reseau
# ---------------------------------------------------------------------------
@iso_asset('network_cabinet', 'computers', 'network', 'Baie de rangement technique', footprint=(1, 1), facings=FRONT2,
           height=47, notes='Demi-baie vitree : panneau de brassage, switch, box.')
def _(m):
    b = m.box(3, 8, 0, 29, 28, 46, M('primary', 'graphite'))
    b.decal('front', frame(1, 2, 25, 44), Delta(+1))
    b.decal('front', rect(2, 3, 24, 43), Use(GRAPH, idx=0))
    for k, z in enumerate((36, 30, 22)):
        b.decal('front', rect(3, z, 23, z + 4), Use(M('fixed', 'steel'), idx=3))
        for j in range(8):
            b.decal('front', rect(4.5 + j * 2.4, z + 1.5, 5.5 + j * 2.4, z + 2.5), Fix('ink'))
        b.decal('front', rect(21, z + 1, 22, z + 2), Fix(['led_green', 'led_amber', 'led_green'][k]))
    b.decal('front', ('fn', lambda s, t: ((s + t * 0.9) % 20 < 1.2) & (s > 2) & (s < 24) & (t > 3) & (t < 43)),
            Fix(RAMPS['glass'][4]))
    b.decal('front', rect(24, 18, 25, 26), Use(STEELM, idx=4))
    m.box(4, 9, 46, 28, 27, 47, M('primary', 'graphite'))


@iso_asset('router', 'computers', 'network', 'Routeur', centered=True, size_su=(12, 8), facings=ALL4, height=10,
           provides=['desk_item'])
def _(m):
    b = m.box(-6, -4, 0, 6, 4, 2.5, CASE_LIGHT)
    for k in range(4):
        b.decal('front', rect(2 + k * 2, 1, 3 + k * 2, 1.8), Fix('led_green' if k < 3 else 'led_amber'))
    for x in (-4.5, 0, 4.5):
        m.box(x - 0.5, 2.5, 2.5, x + 0.5, 3.5, 10, CASE_DARK)


@iso_asset('network_switch_desk', 'computers', 'network', 'Switch de bureau', centered=True, size_su=(14, 8), facings=ALL4,
           height=3, provides=['desk_item'])
def _(m):
    b = m.box(-7, -4, 0, 7, 4, 2.5, M('primary', 'steel'))
    for k in range(6):
        b.decal('front', rect(1.5 + k * 2, 0.6, 2.5 + k * 2, 1.6), Fix('ink'))
        b.decal('front', rect(1.5 + k * 2, 1.9, 2.5 + k * 2, 2.4), Fix('led_green'))


@iso_asset('access_point', 'computers', 'network', "Point d'acces Wi-Fi (mural)", footprint=(1, 1), facings=FRONT2,
           height=96, kind='wall', wall_mount=True, collision=[], notes='Disque mural en hauteur.')
def _(m):
    m.box(13, 30, 86, 19, 32, 94, CASE_LIGHT)
    d = m.box(11, 28.5, 84, 21, 30, 96, CASE_LIGHT)
    d.decal('front', rect(4.5, 5.5, 5.5, 6.5), Fix('led_blue'))


@iso_asset('fiber_box', 'computers', 'network', 'Boitier fibre (mural)', footprint=(1, 1), facings=FRONT2, height=58,
           kind='wall', wall_mount=True, collision=[], notes='Boitier de raccordement + gaine descendant au sol.')
def _(m):
    b = m.box(10, 28, 44, 22, 32, 58, CASE_LIGHT)
    b.decal('front', rect(2, 10, 4, 11), Fix('led_green'))
    b.decal('front', hline(1, 11, 4), Delta(-1))
    m.box(15, 30, 0, 17, 32, 44, M('fixed', 'plastic'))
