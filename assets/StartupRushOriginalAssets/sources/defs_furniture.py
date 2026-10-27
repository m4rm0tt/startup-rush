"""Mobilier de travail et vie des locaux (famille furniture).

Repere local : x = largeur (droite de l'observateur face a l'objet), y = profondeur
depuis la face avant (y=0), z = hauteur en px. 32 su = 1 case.
Hauteurs de reference : plateau de bureau 30 px, assise 19 px, mur 112 px.
"""
from srgen.iso import Fix, Use, Delta, rect, hline, vline, frame, disc, FRONT_IF_BACK, TILE
from srgen.palette import M, OAK, WALNUT, BIRCH, STEEL
from srgen.registry import iso_asset, Socket, Access, ALL4, FRONT2, ONE
from srgen.shapes import rbox, chamfer_box, star_base, drawers, vents, led

GRAPH = M('metal', 'graphite')
STEELM = M('metal', 'steel')
FAB = M('fabric', 'teal')
PRIM_W = M('primary', 'plastic')
DESK_H = 30

# ---------------------------------------------------------------------------
# Sockets standards d'un bureau
# ---------------------------------------------------------------------------

def desk_sockets(w_su, depth=32, top=DESK_H, tower=True, chair_y=-12):
    c = w_su / 2.0
    s = [Socket('monitor_main', ['monitor', 'laptop_closed'], c, depth - 9, top, 'ecran principal'),
         Socket('monitor_second', ['monitor', 'monitor_arm'], c - 18 if w_su >= 48 else c - 8, depth - 9, top, 'ecran secondaire'),
         Socket('keyboard', ['keyboard'], c, 11, top, 'clavier + souris'),
         Socket('laptop', ['laptop'], c, 13, top, 'portable ouvert (remplace ecran+clavier)'),
         Socket('desk_left', ['desk_item'], 7, 13, top, 'accessoire gauche'),
         Socket('desk_right', ['desk_item'], w_su - 7, 13, top, 'accessoire droit'),
         Socket('desk_back_left', ['desk_item', 'monitor'], 8, depth - 7, top, 'arriere gauche'),
         Socket('desk_back_right', ['desk_item', 'monitor'], w_su - 8, depth - 7, top, 'arriere droit'),
         Socket('chair', ['chair'], c, chair_y, 0, 'chaise (regarde le bureau)', facing_rel='opposite', group='workstation')]
    if tower:
        s.append(Socket('tower_floor', ['tower'], w_su - 9, depth / 2.0 + 2, 0, 'tour sous le plateau',
                        group='before_parent'))
    return s


def _legs(m, x0, x1, y0, y1, z, mat=GRAPH, inset=2, w=2):
    for (x, y) in ((x0 + inset, y0 + inset), (x1 - inset - w, y0 + inset),
                   (x0 + inset, y1 - inset - w), (x1 - inset - w, y1 - inset - w)):
        m.box(x, y, 0, x + w, y + w, z, mat)


def _top_grain(p, w, d):
    p.decal('top', rect(4, 6, w * 0.45, 7), Delta(-1))
    p.decal('top', rect(w * 0.55, 15, w - 6, 16), Delta(-1))
    p.decal('top', rect(8, 21, w * 0.3, 22), Delta(-1))


# ---------------------------------------------------------------------------
# Bureaux
# ---------------------------------------------------------------------------
@iso_asset('desk_simple', 'furniture', 'desk', 'Bureau simple', footprint=(2, 1), facings=ALL4, height=DESK_H,
           sockets=desk_sockets(64), access=[Access('work', 0, -1), Access('work', 1, -1)], provides=['floor'],
           notes='Poste standard : plateau bois, pietement metal, voile de fond.')
def _(m):
    top = m.box(1, 2, DESK_H - 3, 63, 30, DESK_H, OAK)
    _top_grain(top, 62, 28)
    _legs(m, 1, 63, 2, 30, DESK_H - 3)
    m.box(5, 26, 13, 59, 27, DESK_H - 3, GRAPH)


@iso_asset('desk_compact', 'furniture', 'desk', 'Bureau compact', footprint=(1, 1), facings=ALL4, height=DESK_H,
           sockets=desk_sockets(32), access=[Access('work', 0, -1)],
           notes='Une case : etudiant / chambre. Caisson a gauche.')
def _(m):
    top = m.box(1, 3, DESK_H - 3, 31, 29, DESK_H, BIRCH)
    top.decal('top', rect(4, 8, 14, 9), Delta(-1))
    cab = m.box(2, 4, 0, 13, 28, DESK_H - 3, M('primary', 'plastic'))
    drawers(cab, 'front', 1, 10, 1, DESK_H - 4, 3)
    m.box(27, 4, 0, 29, 6, DESK_H - 3, GRAPH)
    m.box(27, 26, 0, 29, 28, DESK_H - 3, GRAPH)


def bench_sockets(n=3, w=96):
    out = []
    for k in range(n):
        x = (k + 0.5) * w / n
        out += [Socket(f'monitor_{k}', ['monitor', 'laptop_closed'], x, 23, DESK_H, f'ecran place {k}'),
                Socket(f'keyboard_{k}', ['keyboard'], x, 11, DESK_H, f'clavier place {k}'),
                Socket(f'laptop_{k}', ['laptop'], x, 13, DESK_H, f'portable place {k}'),
                Socket(f'chair_{k}', ['chair'], x, -12, 0, f'chaise place {k}', facing_rel='opposite', group='workstation')]
    out += [Socket('desk_left', ['desk_item'], 5, 26, DESK_H), Socket('desk_right', ['desk_item'], w - 5, 26, DESK_H)]
    return out


@iso_asset('desk_long', 'furniture', 'desk', 'Bureau long (bench)', footprint=(3, 1), facings=ALL4, height=DESK_H,
           sockets=bench_sockets(),
           access=[Access('work', 0, -1), Access('work', 1, -1), Access('work', 2, -1)],
           notes='Plan de 3 cases ; supporte un 3e ecran. Goulotte de cables sous le plateau.')
def _(m):
    top = m.box(1, 2, DESK_H - 3, 95, 30, DESK_H, OAK)
    _top_grain(top, 94, 28)
    for x in (3, 46, 91):
        m.box(x, 4, 0, x + 2, 28, 3, GRAPH)
        m.box(x, 15, 3, x + 2, 17, DESK_H - 3, GRAPH)
    tray = m.box(8, 22, DESK_H - 8, 88, 27, DESK_H - 4, GRAPH)
    vents(tray, 'front', 2, 78, 1, 4, step=2)


@iso_asset('desk_corner', 'furniture', 'desk', "Bureau d'angle", footprint=(2, 2), facings=ALL4, height=DESK_H,
           sockets=[Socket('monitor_main', ['monitor'], 50, 52, DESK_H, 'ecran dans l\'angle'),
                    Socket('monitor_second', ['monitor', 'monitor_arm'], 24, 54, DESK_H),
                    Socket('keyboard', ['keyboard'], 44, 40, DESK_H),
                    Socket('desk_left', ['desk_item'], 8, 54, DESK_H),
                    Socket('desk_right', ['desk_item'], 56, 14, DESK_H),
                    Socket('chair', ['chair'], 40, 22, 0, 'chaise dans le creux du L', facing_rel='opposite', group='workstation'),
                    Socket('tower_floor', ['tower'], 56, 30, 0, 'tour', group='before_parent')],
           access=[Access('work', 1, 0)], collision=[(0, 1), (1, 1), (1, 0)],
           notes='Plateau en L (2x2) ; la case avant-gauche reste libre pour la chaise.')
def _(m):
    a = m.box(1, 34, DESK_H - 3, 63, 62, DESK_H, OAK)
    b = m.box(35, 2, DESK_H - 3, 63, 34, DESK_H, OAK)
    _top_grain(a, 62, 28)
    for (x, y) in ((3, 36), (3, 58), (59, 4), (37, 4), (59, 58)):
        m.box(x, y, 0, x + 2, y + 2, DESK_H - 3, GRAPH)
    m.box(5, 58, 12, 59, 59, DESK_H - 3, GRAPH)


@iso_asset('desk_tech', 'furniture', 'desk', 'Bureau technique renforce', footprint=(2, 1), facings=ALL4, height=DESK_H,
           sockets=desk_sockets(64) + [Socket('shelf_top', ['monitor', 'desk_item', 'instrument'], 32, 26, 46, 'etagere haute')],
           access=[Access('work', 0, -1), Access('work', 1, -1)],
           notes='Cadre acier epais, etagere haute, bandeau de prises : labos, salle serveur, atelier.')
def _(m):
    top = m.box(1, 2, DESK_H - 4, 63, 30, DESK_H, M('primary', 'graphite'))
    top.decal('top', frame(1, 1, 61, 27), Delta(+1))
    top.decal('front', rect(0, 0, 62, 1), Delta(-1))
    for x in (2, 59):
        m.box(x, 3, 0, x + 3, 6, DESK_H - 4, STEELM)
        m.box(x, 25, 0, x + 3, 28, 46, STEELM)
        m.box(x, 3, 3, x + 3, 28, 6, STEELM)
    m.box(2, 25, 44, 62, 29, 46, M('primary', 'graphite'))
    strip = m.box(6, 26, 34, 58, 28, 38, M('primary', 'plastic'))
    for k in range(6):
        strip.decal('front', rect(3 + k * 8, 1, 5 + k * 8, 3), Fix('ink'))
    led(strip, 'front', 49, 1, 'led_red', 2, 2)
    m.box(4, 6, 8, 60, 25, 9, STEELM)


@iso_asset('table_collab', 'furniture', 'table', 'Table de travail collective', footprint=(3, 2), facings=FRONT2, height=DESK_H,
           sockets=[Socket(f'seat_{i}', ['chair'], x, y, 0, '', facing_rel=fr, group='workstation')
                    for i, (x, y, fr) in enumerate([(24, -10, 'opposite'), (72, -10, 'opposite'),
                                                    (24, 74, 'same'), (72, 74, 'same')])] +
           [Socket('laptop_a', ['laptop'], 24, 14, DESK_H), Socket('laptop_b', ['laptop'], 72, 14, DESK_H),
            Socket('center', ['desk_item'], 48, 32, DESK_H)],
           access=[Access('work', 0, -1), Access('work', 2, -1), Access('work', 0, 2, 'same'), Access('work', 2, 2, 'same')],
           notes='Grande table partagee (4 places), passe-cables central.')
def _(m):
    top = m.box(2, 3, DESK_H - 3, 94, 61, DESK_H, BIRCH)
    top.decal('top', rect(40, 26, 56, 32), Use(GRAPH, idx=2))
    top.decal('top', rect(41, 27, 55, 31), Use(GRAPH, idx=1))
    _top_grain(top, 92, 58)
    for x in (10, 82):
        m.box(x, 8, 0, x + 4, 56, 3, GRAPH)
        m.box(x, 30, 3, x + 4, 34, DESK_H - 3, GRAPH)
    m.box(14, 31, 14, 82, 33, 16, GRAPH)


# ---------------------------------------------------------------------------
# Chaises (modele centre : origine = centre d'assise au sol, avant = -y)
# ---------------------------------------------------------------------------
CHAIR_OCC = (0, 5, 19, 'same')


@iso_asset('chair_office', 'furniture', 'chair', 'Chaise de bureau', centered=True, size_su=(22, 22), facings=ALL4,
           height=36, provides=['chair'], occupant=CHAIR_OCC,
           notes='Dossier en couche "front" quand la chaise tourne le dos a la camera (nw/ne).')
def _(m):
    star_base(m, 0, 9)
    m.cyl(0, 0, 1.2, 4, 14, STEELM)
    m.box(-4, -3, 13, 4, 4, 15, GRAPH)
    seat = m.cyl(0, 0.5, 9.5, 15, 19, FAB, ry=9)
    seat.decal('top', disc(9.5, 9, 6.5, 6), Delta(+1))
    m.box(-1, 7, 14, 1, 10, 22, GRAPH, layer=FRONT_IF_BACK)
    back = m.slab_tilt(-8, 8, 8, 21, 10, 37, 2.5, FAB, layer=FRONT_IF_BACK)
    back.decal('front', frame(0, 0, 16, 99, 1, 1), Delta(-1))
    for sx in (-1, 1):
        x0 = 9 if sx > 0 else -10.5
        m.box(x0, -1, 17, x0 + 1.5, 2, 24, GRAPH)
        m.box(x0 - 0.5, -4, 24, x0 + 2, 4, 25.5, GRAPH)


@iso_asset('chair_budget', 'furniture', 'chair', 'Chaise economique', centered=True, size_su=(20, 20), facings=ALL4,
           height=34, provides=['chair'], occupant=(0, 5, 18, 'same'),
           notes='Chaise empilable : coque plastique, 4 pieds tube.')
def _(m):
    for (x, y) in ((-7, -7), (6, -7), (-7, 6), (6, 6)):
        m.box(x, y, 0, x + 1.5, y + 1.5, 16, STEELM)
    seat = m.box(-8, -8, 16, 8, 8, 18, M('primary', 'coral'))
    seat.decal('top', frame(0, 0, 16, 16, 1, 1), Delta(+1))
    m.slab_tilt(-8, 8, 7, 18, 9, 34, 2, M('primary', 'coral'), layer=FRONT_IF_BACK)
    m.box(-7, 7, 16, -5.5, 9, 30, STEELM, layer=FRONT_IF_BACK)
    m.box(5.5, 7, 16, 7, 9, 30, STEELM, layer=FRONT_IF_BACK)


@iso_asset('chair_ergonomic', 'furniture', 'chair', 'Chaise ergonomique', centered=True, size_su=(22, 22), facings=ALL4,
           height=40, provides=['chair'], occupant=CHAIR_OCC,
           notes='Dossier resille haut et etroit (sans appui-tete pour ne pas masquer la tete), soutien lombaire.')
def _(m):
    star_base(m, 0, 10, mat=STEELM)
    m.cyl(0, 0, 1.3, 4, 14, GRAPH)
    m.box(-5, -4, 12, 5, 5, 15, GRAPH)
    seat = m.cyl(0, 0.5, 10, 15, 19, M('fabric', 'graphite'), ry=9.5)
    seat.decal('top', disc(10, 9.5, 7, 6.5), Delta(+1))
    m.box(-1.5, 7, 13, 1.5, 11, 24, GRAPH, layer=FRONT_IF_BACK)
    fr = m.slab_tilt(-7, 7, 9, 21, 11, 40, 2, GRAPH, layer=FRONT_IF_BACK)
    fr.decal('front', rect(1, 2, 13, 99), Use(M('fabric', 'graphite'), idx=3))
    fr.decal('front', ('fn', lambda s, t: ((s + t).astype(int) % 3 == 0) & (s >= 1) & (s < 13) & (t >= 2)), Delta(-1))
    fr.decal('front', rect(1, 5, 13, 7), Use(M('accent', 'teal'), idx=3))
    for sx in (-1, 1):
        x0 = 9.5 if sx > 0 else -11
        m.box(x0, -1, 17, x0 + 1.5, 2, 25, GRAPH)
        m.box(x0 - 0.5, -5, 25, x0 + 2, 4, 26.5, M('fabric', 'graphite'))


@iso_asset('chair_meeting', 'furniture', 'chair', 'Chaise de reunion', centered=True, size_su=(20, 20), facings=ALL4,
           height=34, provides=['chair'], occupant=(0, 5, 18, 'same'),
           notes='Pietement luge chrome, assise et dossier tissu.')
def _(m):
    for sx in (-7, 6):
        m.box(sx, -8, 0, sx + 1.5, 8, 1.5, STEELM)
        m.box(sx, -7, 1.5, sx + 1.5, -5.5, 16, STEELM)
        m.box(sx, 6, 1.5, sx + 1.5, 7.5, 16, STEELM, layer=FRONT_IF_BACK)
    seat = m.box(-8, -8, 15, 8, 8, 18, M('fabric', 'coral'))
    seat.decal('top', frame(0, 0, 16, 16, 1, 1), Delta(+1))
    m.slab_tilt(-8, 8, 7, 18, 9, 33, 2, M('fabric', 'coral'), layer=FRONT_IF_BACK)
