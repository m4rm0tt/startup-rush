"""Mobilier de bureau : reunion, accueil, rangement, tableaux, ecrans, cloisons, divers.

Objets muraux : emprise (L, 1), geometrie contre le mur en y in [32-prof, 32],
orientation se = mur gauche, sw = mur droit, collision vide.
"""
import random

from srgen.iso import Fix, Use, Delta, rect, hline, vline, frame, disc, FRONT_IF_BACK, TILE
from srgen.palette import M, OAK, WALNUT, BIRCH, RAMPS
from srgen.registry import iso_asset, Socket, Access, ALL4, FRONT2, ONE
from srgen.shapes import rbox, chamfer_box, star_base, drawers, vents, led, ui_screen, code_screen

GRAPH = M('metal', 'graphite')
STEELM = M('metal', 'steel')
WHITE = M('primary', 'plastic')
DESK_H = 30
BOOK_RAMPS = ['coral', 'teal', 'sun', 'sky', 'sage', 'rose', 'navy', 'cream', 'red', 'leaf']


def chair_ring(cx, cy, r_x, r_y, n_side, ends=True):
    """Sockets de chaises autour d'une table rectangulaire (x sur la longueur)."""
    out = []
    xs = [cx - r_x + (2 * r_x) * (k + 0.5) / n_side for k in range(n_side)]
    i = 0
    for x in xs:
        out.append(Socket(f'chair_{i}', ['chair'], x, cy - r_y, 0, 'cote avant', facing_rel='opposite', group='workstation')); i += 1
        out.append(Socket(f'chair_{i}', ['chair'], x, cy + r_y, 0, 'cote arriere', facing_rel='same', group='workstation')); i += 1
    if ends:
        out.append(Socket(f'chair_{i}', ['chair'], cx - r_x - 8, cy, 0, 'bout gauche', facing_rel='ccw', group='workstation')); i += 1
        out.append(Socket(f'chair_{i}', ['chair'], cx + r_x + 8, cy, 0, 'bout droit', facing_rel='cw', group='workstation'))
    return out


# ---------------------------------------------------------------------------
# Reunion et accueil
# ---------------------------------------------------------------------------
@iso_asset('table_meeting_small', 'furniture', 'table', 'Petite table de reunion (ronde)', footprint=(2, 2), facings=ONE,
           symmetric=True, height=DESK_H,
           sockets=[Socket('chair_0', ['chair'], 32, -2, 0, '', facing_rel='opposite', group='workstation'),
                    Socket('chair_1', ['chair'], 32, 66, 0, '', facing_rel='same', group='workstation'),
                    Socket('chair_2', ['chair'], -2, 32, 0, '', facing_rel='ccw', group='workstation'),
                    Socket('chair_3', ['chair'], 66, 32, 0, '', facing_rel='cw', group='workstation'),
                    Socket('center', ['desk_item', 'speakerphone'], 32, 32, DESK_H)],
           notes='Table ronde 4 places, pied central. Symetrique : une seule orientation.')
def _(m):
    top = m.cyl(32, 32, 25, DESK_H - 3, DESK_H, BIRCH)
    top.decal('top', disc(25, 25, 21, 21), Delta(+1))
    top.decal('top', disc(25, 25, 20, 20), Delta(0))
    m.cyl(32, 32, 2.5, 3, DESK_H - 3, GRAPH)
    m.cyl(32, 32, 11, 0, 3, GRAPH)


@iso_asset('table_meeting_large', 'furniture', 'table', 'Grande table de reunion', footprint=(4, 2), facings=FRONT2,
           height=DESK_H, sockets=chair_ring(64, 32, 48, 34, 3) + [Socket('center', ['desk_item', 'speakerphone'], 64, 32, DESK_H)],
           notes='8 places (3 par cote + 2 bouts), boitier de prises central.')
def _(m):
    top = chamfer_box(m, 4, 8, DESK_H - 3, 124, 56, DESK_H, 6, WALNUT)
    top.decal('top', rect(54, 20, 74, 28), Use(GRAPH, idx=2))
    top.decal('top', rect(55, 21, 73, 27), Use(GRAPH, idx=3))
    top.decal('top', rect(10, 9, 50, 10), Delta(-1))
    top.decal('top', rect(78, 38, 116, 39), Delta(-1))
    for x in (22, 102):
        m.box(x - 3, 14, 0, x + 3, 50, 3, GRAPH)
        m.box(x - 2, 28, 3, x + 2, 36, DESK_H - 3, GRAPH)


@iso_asset('reception_desk', 'furniture', 'desk', "Banque d'accueil", footprint=(3, 2), facings=FRONT2, height=40,
           sockets=[Socket('chair', ['chair'], 48, 46, 0, 'hote(sse) d\'accueil, regarde l\'entree', facing_rel='same',
                           group='workstation'),
                    Socket('monitor_main', ['monitor'], 48, 22, DESK_H, 'ecran tourne vers l\'hote', facing_rel='opposite'),
                    Socket('counter_left', ['desk_item'], 20, 7, 41), Socket('counter_right', ['desk_item'], 78, 7, 41)],
           access=[Access('visit', 1, -1), Access('work', 1, 1, 'same')],
           notes='Comptoir haut cote visiteurs (avant), plan de travail bas cote hote.')
def _(m):
    front = m.box(2, 4, 0, 94, 12, 38, M('primary', 'teal'))
    front.decal('front', rect(0, 8, 92, 11), Use(M('accent', 'sun'), idx=3))
    front.decal('front', rect(0, 11, 92, 12), Use(M('accent', 'sun'), idx=2))
    front.decal('front', frame(4, 16, 88, 34), Delta(-1))
    m.box(0, 2, 38, 96, 14, 41, OAK)
    m.box(2, 12, 0, 12, 60, 38, M('primary', 'teal'))
    m.box(0, 12, 38, 14, 62, 41, OAK)
    work = m.box(12, 12, DESK_H - 3, 94, 40, DESK_H, WHITE)
    work.decal('top', rect(2, 2, 80, 3), Delta(-1))
    m.box(90, 14, 0, 92, 38, DESK_H - 3, GRAPH)
    m.box(4, 2, 0, 92, 4, 2, M('fixed', 'graphite'))


# ---------------------------------------------------------------------------
# Rangements
# ---------------------------------------------------------------------------
@iso_asset('wardrobe_office', 'furniture', 'storage', 'Armoire', footprint=(1, 1), facings=FRONT2, height=78, occludes=40,
           notes='Armoire haute a deux portes, socle.')
def _(m):
    b = m.box(2, 6, 2, 30, 28, 78, BIRCH)
    b.decal('front', vline(14, 1, 75), Delta(-1))
    b.decal('front', rect(11, 36, 12, 46), Use(STEELM, idx=4))
    b.decal('front', rect(16, 36, 17, 46), Use(STEELM, idx=4))
    b.decal('front', hline(0, 28, 74), Delta(-1))
    m.box(3, 7, 0, 29, 27, 2, M('fixed', 'graphite'))


@iso_asset('locker', 'furniture', 'storage', 'Casiers', footprint=(1, 1), facings=FRONT2, height=76, occludes=40,
           notes='4 casiers metal, aerations, porte-etiquettes.')
def _(m):
    b = m.box(2, 8, 0, 30, 28, 76, M('primary', 'teal'))
    b.decal('front', vline(14, 0, 76), Use(GRAPH, idx=1))
    b.decal('front', hline(0, 28, 38), Use(GRAPH, idx=1))
    for (s0, t0) in ((1, 1), (15, 1), (1, 39), (15, 39)):
        for k in range(3):
            b.decal('front', hline(s0 + 3, s0 + 10, t0 + 28 + k * 2), Delta(-1))
        b.decal('front', rect(s0 + 9, t0 + 14, s0 + 11, t0 + 20), Use(STEELM, idx=4))
        b.decal('front', rect(s0 + 3, t0 + 22, s0 + 7, t0 + 24), Use(M('fixed', 'cream'), idx=5))
    b.decal('top', frame(0, 0, 28, 20), Delta(+1))


@iso_asset('bookcase', 'furniture', 'storage', 'Bibliotheque', footprint=(2, 1), facings=FRONT2, height=72, occludes=40,
           sockets=[Socket(f'shelf_{k}', ['desk_item'], 16 + 32 * (k % 2), 10, 20 + 17 * (k // 2), 'objet deco sur etagere')
                    for k in range(4)],
           notes='Cadre bois, 4 niveaux, livres de hauteurs variees.')
def _(m):
    W, D, H = 62, 30, 72
    x0, x1, y0, y1 = 1, 63, 14, 30
    m.box(x0, y0, 0, x0 + 3, y1, H, WALNUT)
    m.box(x1 - 3, y0, 0, x1, y1, H, WALNUT)
    m.box(x0, y0, H - 3, x1, y1, H, WALNUT)
    m.box(x0, y0, 0, x1, y1, 4, WALNUT)
    m.box(x0 + 3, y1 - 2, 4, x1 - 3, y1, H - 3, M('wood', 'walnut'))
    rnd = random.Random(7)
    shelves = [4, 20, 37, 54]
    for z in shelves[1:]:
        m.box(x0 + 3, y0, z - 2, x1 - 3, y1 - 2, z, WALNUT)
    for si, z in enumerate(shelves):
        x = x0 + 4
        top = (shelves[si + 1] - 2) if si + 1 < len(shelves) else H - 3
        while x < x1 - 6:
            if rnd.random() < 0.12 and x < x1 - 14:
                x += rnd.randint(4, 7)
                continue
            w = rnd.choice([2, 2, 3, 3, 4])
            h = min(top - z - 1, rnd.randint(9, 14))
            col = rnd.choice(BOOK_RAMPS)
            d0 = rnd.choice([16, 17, 18])
            bk = m.box(x, d0, z, x + w, y1 - 3, z + h, M('fixed', col), outline=False)
            bk.decal('front', hline(0, w, h - 3), Delta(+1))
            x += w
        if si == 2:
            m.box(x1 - 14, 18, z, x1 - 6, 26, z + 6, M('fixed', 'kraft'))


@iso_asset('shelf_metal', 'furniture', 'storage', 'Etagere metallique', footprint=(2, 1), facings=FRONT2, height=70, occludes=40,
           notes='Rayonnage ouvert acier : cartons, classeurs, pieces.')
def _(m):
    for (x, y) in ((2, 10), (60, 10), (2, 28), (60, 28)):
        m.box(x, y, 0, x + 2, y + 2, 70, STEELM)
    rnd = random.Random(3)
    for z in (6, 26, 46, 66):
        p = m.box(2, 10, z, 62, 30, z + 2, STEELM)
        p.decal('front', hline(0, 60, 1), Delta(-1))
        if z < 66:
            x = 6
            while x < 54:
                kind = rnd.random()
                if kind < 0.45:
                    w = rnd.randint(10, 14)
                    m.box(x, 13, z + 2, x + w, 27, z + 2 + rnd.randint(8, 12), M('fixed', 'kraft'))
                    x += w + 2
                elif kind < 0.8:
                    for k in range(rnd.randint(3, 5)):
                        m.box(x, 15, z + 2, x + 2, 27, z + 14, M('fixed', rnd.choice(['navy', 'teal', 'coral', 'graphite'])), outline=False)
                        x += 2
                    x += 3
                else:
                    x += rnd.randint(6, 10)


@iso_asset('pedestal_drawer', 'furniture', 'storage', 'Caisson a roulettes', centered=True, size_su=(12, 18), facings=ALL4,
           height=22, provides=['under_desk'], notes='Se glisse sous un bureau (socket libre) ou se pose au sol.')
def _(m):
    b = m.box(-6, -9, 2, 6, 9, 22, WHITE)
    drawers(b, 'front', 1, 11, 1, 19, 3)
    b.decal('top', frame(0, 0, 12, 18), Delta(+1))
    for (x, y) in ((-5, -8), (4, -8), (-5, 7), (4, 7)):
        m.cyl(x + 0.5, y + 0.5, 0.9, 0, 2, GRAPH)


@iso_asset('filing_cabinet', 'furniture', 'storage', 'Classeur a tiroirs', footprint=(1, 1), facings=FRONT2, height=52,
           notes='4 tiroirs, porte-etiquettes, poignees.')
def _(m):
    b = m.box(6, 6, 0, 26, 28, 52, M('primary', 'steel'))
    for i in range(4):
        t0 = 1 + i * 12.75
        b.decal('front', hline(0, 20, t0), Use(GRAPH, idx=1))
        b.decal('front', rect(6, t0 + 7, 14, t0 + 8), Use(GRAPH, idx=4))
        b.decal('front', rect(7, t0 + 9, 13, t0 + 11), Fix('paper'))
    b.decal('top', frame(0, 0, 20, 22), Delta(+1))


@iso_asset('sideboard_low', 'furniture', 'storage', 'Meuble bas', footprint=(2, 1), facings=FRONT2, height=27,
           sockets=[Socket('top_left', ['desk_item', 'microwave', 'coffee_machine', 'printer_small'], 16, 18, 27),
                    Socket('top_right', ['desk_item', 'coffee_machine', 'microwave'], 48, 18, 27)],
           notes='Bas, plateau bois, 3 portes : supporte imprimante, cafe, deco.')
def _(m):
    b = m.box(2, 8, 3, 62, 28, 25, WHITE)
    for k in (20, 40):
        b.decal('front', vline(k, 0, 22), Delta(-1))
    for k in (17, 23, 37, 43):
        b.decal('front', rect(k, 11, k + 1, 16), Use(STEELM, idx=4))
    top = m.box(1, 7, 25, 63, 29, 27, OAK)
    for (x, y) in ((3, 9), (60, 9), (3, 26), (60, 26)):
        m.box(x, y, 0, x + 1.5, y + 1.5, 3, GRAPH)


@iso_asset('display_case', 'furniture', 'storage', 'Presentoir vitre', footprint=(1, 1), facings=FRONT2, height=68,
           notes='Vitrine : prototypes et trophees. Verre semi-transparent.')
def _(m):
    m.box(4, 6, 0, 28, 28, 22, WALNUT)
    m.prims[-1].decal('front', frame(2, 2, 22, 20), Delta(-1))
    g = m.box(5, 7, 22, 27, 27, 64, M('glass', 'glass'), alpha=150, outline=True)
    g.decal('front', ('fn', lambda s, t: ((s + t * 0.8) % 18 < 2)), Use(M('glass', 'glass'), idx=5))
    for z in (36, 50):
        m.box(5, 7, z, 27, 27, z + 1, M('glass', 'glass'), alpha=190)
    m.box(4, 6, 64, 28, 28, 68, WALNUT)
    m.cyl(16, 17, 3, 23, 25, M('metal', 'brass'))
    m.box(15, 16, 25, 17, 18, 30, M('metal', 'brass'))
    m.ell(16, 17, 32, 3, 3, 2.5, M('metal', 'brass'))
    m.box(10, 14, 37, 22, 22, 41, M('fixed', 'graphite'))
    m.prims[-1].decal('top', rect(2, 2, 10, 6), Fix(RAMPS['leaf'][3]))


# ---------------------------------------------------------------------------
# Tableaux et ecrans
# ---------------------------------------------------------------------------
def _scribbles(p, face, w, h, seed):
    rnd = random.Random(seed)
    cols = [RAMPS['sky'][2], RAMPS['coral'][2], RAMPS['leaf'][2], RAMPS['navy'][3]]
    # boites reliees par des fleches (schema d'idee, sans texte)
    boxes = []
    for k in range(4):
        bw = rnd.randint(7, 11); bh = rnd.randint(5, 8)
        s0 = 3 + k * (w - 6) / 4.0 + rnd.randint(0, 2); t0 = rnd.randint(4, h - bh - 4)
        p.decal(face, frame(s0, t0, s0 + bw, t0 + bh), Fix(cols[k % len(cols)]))
        boxes.append((s0 + bw, t0 + bh / 2.0, s0))
    for k in range(len(boxes) - 1):
        a = boxes[k]; b = boxes[k + 1]
        p.decal(face, hline(a[0], b[2], int(a[1])), Fix(RAMPS['graphite'][3]))
    for k in range(3):
        s0 = rnd.randint(3, w - 20)
        p.decal(face, hline(s0, s0 + rnd.randint(8, 16), 2 + k * 2), Fix(RAMPS['graphite'][2]))


@iso_asset('whiteboard_mobile', 'furniture', 'board', 'Tableau blanc mobile', footprint=(2, 1), facings=FRONT2, height=64,
           notes='Pietement a roulettes ; schema d\'idee dessine (aucun texte).')
def _(m):
    for x in (3, 59):
        m.box(x, 14, 2, x + 2, 18, 64, STEELM)
        m.box(x - 2, 8, 0, x + 4, 24, 2, GRAPH)
    b = m.box(5, 15, 24, 59, 17, 62, M('primary', 'plastic'))
    b.decal('front', frame(0, 0, 54, 38), Use(STEELM, idx=3))
    _scribbles(b, 'front', 54, 38, 11)
    m.box(8, 12, 24, 56, 15, 25, STEELM)
    m.box(14, 12.5, 25, 18, 14, 26, M('fixed', 'sky'))
    m.box(20, 12.5, 25, 24, 14, 26, M('fixed', 'coral'))


@iso_asset('planning_board', 'furniture', 'board', 'Tableau de planning mural', footprint=(2, 1), facings=FRONT2, height=88,
           kind='wall', wall_mount=True, collision=[],
           notes='Tableau kanban mural (colonnes + post-it). Mur gauche = se, mur droit = sw.')
def _(m):
    b = m.box(4, 30, 42, 60, 32, 86, M('primary', 'plastic'))
    b.decal('front', frame(0, 0, 56, 44), Use(WALNUT, idx=3))
    for k in (19, 37):
        b.decal('front', vline(k, 2, 40), Fix(RAMPS['graphite'][3]))
    b.decal('front', rect(1, 39, 55, 43), Use(M('accent', 'teal'), idx=3))
    rnd = random.Random(5)
    cols = ['sun', 'coral', 'sky', 'leaf', 'rose']
    for col in range(3):
        n = [4, 3, 2][col]
        for k in range(n):
            s0 = 3 + col * 18 + rnd.randint(0, 3)
            t0 = 33 - k * 8 - rnd.randint(0, 1)
            c = RAMPS[rnd.choice(cols)]
            b.decal('front', rect(s0, t0, s0 + 6, t0 + 5), Fix(c[4]))
            b.decal('front', rect(s0, t0, s0 + 6, t0 + 1), Fix(c[3]))
            b.decal('front', hline(s0 + 1, s0 + 5, t0 + 3), Fix(c[2]))


@iso_asset('presentation_screen', 'furniture', 'av', 'Ecran de presentation sur pied', footprint=(2, 1), facings=FRONT2,
           height=66, screens=[{'name': 'screen', 'quad': [(9, 13, 63), (55, 13, 63), (55, 13, 33), (9, 13, 33)]}],
           sockets=[Socket('top', ['videoconf_bar', 'webcam'], 32, 15, 65, 'camera de visio sur le dessus')],
           notes='Grand ecran sur chariot ; zone screen pour le contenu dynamique.')
def _(m):
    m.box(18, 10, 0, 46, 26, 3, GRAPH)
    for (x, y) in ((19, 11), (44, 11), (19, 24), (44, 24)):
        m.cyl(x, y, 1, 0, 1.5, GRAPH)
    m.box(30, 16, 3, 34, 19, 40, STEELM)
    p = m.box(7, 13, 31, 57, 16, 65, M('primary', 'graphite'))
    ui_screen(p, 'front', 50, 34, seed=4, margin=2)
    p.decal('back', rect(18, 10, 32, 24), Delta(-1))


# ---------------------------------------------------------------------------
# Cloisons : segment d'une case pose sur l'arete arriere de sa case
# ---------------------------------------------------------------------------
PART_NOTE = ("Segment d'une case pose sur l'arete arriere de sa case (y local 28..32) : se = sur l'arete U min de la "
             "case, sw = sur l'arete V min. Les lignes se raboutent et se rejoignent exactement aux angles.")


@iso_asset('partition_low', 'furniture', 'partition', 'Cloison basse', footprint=(1, 1), facings=FRONT2, height=40,
           collision=[(0, 0)], notes=PART_NOTE)
def _(m):
    p = m.box(0, 28, 2, 32, 32, 38, M('fabric', 'sage'))
    p.decal('front', frame(0, 0, 32, 36), Delta(-1))
    p.decal('back', frame(0, 0, 32, 36), Delta(-1))
    m.box(0, 27.5, 38, 32, 32.5, 40, STEELM)
    for x in (1, 29):
        m.box(x, 24, 0, x + 2, 32, 2, STEELM)


@iso_asset('partition_acoustic', 'furniture', 'partition', 'Separation acoustique', footprint=(1, 1), facings=FRONT2,
           height=64, collision=[(0, 0)], notes=PART_NOTE + ' Feutre a coutures, hauteur debout.')
def _(m):
    p = m.box(0, 26, 2, 32, 32, 64, M('fabric', 'navy'))
    for face in ('front', 'back'):
        p.decal(face, ('fn', lambda s, t: ((t.astype(int) % 10) == 0) & (s > 1) & (s < 31)), Delta(-1))
        p.decal(face, ('fn', lambda s, t: ((t.astype(int) % 10) == 1) & (s > 1) & (s < 31)), Delta(+1))
    p.decal('top', rect(0, 0, 32, 6), Delta(+1))
    for x in (2, 27):
        m.box(x, 22, 0, x + 3, 32, 2, GRAPH)


@iso_asset('partition_glass', 'furniture', 'partition', 'Vitrage interieur', footprint=(1, 1), facings=FRONT2, height=96,
           collision=[(0, 0)], notes=PART_NOTE + ' Verre semi-transparent + bande depolie.')
def _(m):
    g = m.box(1, 29, 2, 31, 31, 94, M('glass', 'glass'), alpha=140, outline=False)
    for face in ('front', 'back'):
        g.decal(face, rect(0, 40, 30, 50), Use(M('glass', 'glass'), idx=4))
        g.decal(face, ('fn', lambda s, t: ((s + t * 0.7) % 22 < 2)), Use(M('glass', 'glass'), idx=5))
    m.box(0, 28, 0, 32, 32, 2, GRAPH)
    m.box(0, 28, 94, 32, 32, 96, GRAPH)
    m.box(0, 28, 0, 1.5, 32, 96, GRAPH)
    m.box(30.5, 28, 0, 32, 32, 96, GRAPH)


@iso_asset('partition_glass_door', 'furniture', 'partition', 'Vitrage interieur avec porte', footprint=(1, 1),
           facings=FRONT2, height=96, collision=[(0, 0)],
           notes=PART_NOTE + " Porte vitree fermee (pas d'etat ouvert) ; la case est un passage.")
def _(m):
    g = m.box(4, 29, 2, 28, 31, 84, M('glass', 'glass'), alpha=150, outline=False)
    for face in ('front', 'back'):
        g.decal(face, rect(0, 38, 24, 46), Use(M('glass', 'glass'), idx=4))
        g.decal(face, ('fn', lambda s, t: ((s + t * 0.7) % 20 < 2)), Use(M('glass', 'glass'), idx=5))
    m.box(0, 28, 84, 32, 32, 96, GRAPH)
    m.box(0, 28, 0, 4, 32, 96, GRAPH)
    m.box(28, 28, 0, 32, 32, 96, GRAPH)
    m.box(4, 28, 0, 28, 32, 2, GRAPH)
    m.box(22, 27, 38, 23, 33, 50, STEELM)


# ---------------------------------------------------------------------------
# Divers
# ---------------------------------------------------------------------------
@iso_asset('trash_bin', 'furniture', 'misc', 'Poubelle', centered=True, size_su=(10, 10), facings=ONE, symmetric=True,
           height=16)
def _(m):
    c = m.cyl(0, 0, 4.5, 0, 15, M('primary', 'graphite'))
    c.decal('side', ('fn', lambda s, t: (t.astype(int) % 4 == 2)), Delta(-1))
    r = m.cyl(0, 0, 5, 15, 16.5, M('metal', 'steel'))
    r.decal('top', disc(5, 5, 3.6, 3.6), Fix(RAMPS['graphite'][0]))
    m.box(-1, -1, 15.5, 2, 1.5, 17, Fix('paper'))


@iso_asset('recycling_station', 'furniture', 'misc', 'Bacs de tri', footprint=(1, 1), facings=FRONT2, height=26,
           notes='3 bacs (couvercles bleu / jaune / vert), pictogrammes simplifies.')
def _(m):
    for k, col in enumerate(['sky', 'sun', 'leaf']):
        x = 2 + k * 9.5
        b = m.box(x, 10, 0, x + 9, 28, 24, M('primary', 'plastic'))
        lid = m.box(x - 0.5, 9.5, 24, x + 9.5, 28.5, 26, M('fixed', col))
        lid.decal('top', rect(2.5, 4, 6.5, 12), Fix(RAMPS[col][1]))
        b.decal('front', rect(3, 12, 6, 15), Fix(RAMPS[col][3]))


@iso_asset('coat_rack', 'furniture', 'misc', 'Porte-manteau', centered=True, size_su=(12, 12), facings=ONE, height=70,
           notes='Pied central, crochets, une veste et une echarpe accrochees.')
def _(m):
    m.cyl(0, 0, 6, 0, 2, WALNUT)
    m.cyl(0, 0, 1.2, 2, 68, WALNUT)
    m.ell(0, 0, 69, 1.8, 1.8, 1.5, WALNUT)
    for (dx, dy) in ((3, 0), (-3, 0), (0, 3), (0, -3)):
        rbox(m, dx / 2.0, dy / 2.0, 4, 1, 0 if dy == 0 else 90, 63, 64, WALNUT)
    m.box(-6, -1, 36, -2, 3, 62, M('fabric', 'navy'))
    m.box(-7, -1.5, 52, -1.5, 3.5, 62, M('fabric', 'navy'))
    m.box(1.5, -2, 44, 3, 1, 62, M('fabric', 'coral'))
