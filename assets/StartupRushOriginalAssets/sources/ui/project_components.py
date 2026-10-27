"""Composants de projet (le joueur assemble les fonctionnalites de son produit).

Pour chaque composant : icone 16x16 (famille HUD), apercu abstrait 48x32 (maquette
fil de fer, aucune vraie page web), carte 9-slice avec emplacements de texte
dynamique, ports de connexion et liens de dependance graphiques.
Les mots d'etat (« en developpement », « termine »...) sont des donnees de l'UI :
la carte n'a que des variantes de COULEUR d'en-tete, pas d'etat de machine.
"""
import numpy as np

from srgen.lot_ui_core import canvas, fill, hline, vline, rect, rrect, rfill, put, tok
from ui.hud import spec, spec_r, glyph

INK = tok('ink')
PW, PH = 48, 32

# id -> (nom FR, groupe, icone)
COMPONENTS = {
    'home':          ('Accueil', 'app', 'home'),
    'auth':          ('Compte / authentification', 'app', 'comp_auth'),
    'profile':       ('Profil', 'app', 'comp_profile'),
    'catalog':       ('Catalogue', 'app', 'comp_catalog'),
    'search':        ('Recherche', 'app', 'search'),
    'feed':          ('Fil de contenu', 'app', 'comp_feed'),
    'messaging':     ('Messagerie', 'app', 'comp_messaging'),
    'notifications': ('Notifications', 'app', 'bell'),
    'subscription':  ('Abonnement', 'app', 'subscription'),
    'payment':       ('Paiement simulé', 'app', 'comp_payment'),
    'reviews':       ('Avis', 'app', 'comp_reviews'),
    'support':       ('Support', 'app', 'support'),
    'analytics':     ('Analytics', 'app', 'statistics'),
    'storage':       ('Stockage', 'app', 'storage'),
    'api':           ('API', 'app', 'comp_api'),
    'admin':         ('Administration', 'app', 'comp_admin'),
    'security':      ('Sécurité', 'app', 'security'),
    'game_menu':     ('Menu (jeu)', 'game', 'comp_game_menu'),
    'progression':   ('Progression', 'game', 'comp_progression'),
    'inventory':     ('Inventaire', 'game', 'comp_inventory'),
    'savegame':      ('Sauvegarde', 'game', 'save'),
    'leaderboard':   ('Classement', 'game', 'comp_leaderboard'),
    'netplay':       ('Réseau (multijoueur)', 'game', 'comp_netplay'),
    'processing':    ('Traitement', 'other', 'comp_processing'),
    'integration':   ('Intégration', 'other', 'comp_integration'),
    'engine':        ('Moteur', 'other', 'comp_engine'),
    'automation':    ('Automatisation', 'other', 'comp_automation'),
    'infrastructure': ('Infrastructure', 'other', 'comp_infrastructure'),
}


# ---------------------------------------------------------------------------
# Apercus abstraits 48x32
# ---------------------------------------------------------------------------
def _frame(dark=False):
    img = canvas(PW, PH)
    rfill(img, 0, 0, PW - 1, PH - 1, tok('navy') if dark else tok('paper'), 1)
    fill(img, 1, 1, PW - 2, 4, tok('navy_2') if dark else tok('paper_3'))
    for k, c in enumerate(('coral', 'sun', 'success')):
        put(img, 3 + k * 3, 2, tok(c)); put(img, 4 + k * 3, 2, tok(c)); put(img, 3 + k * 3, 3, tok(c)); put(img, 4 + k * 3, 3, tok(c))
    rrect(img, 0, 0, PW - 1, PH - 1, INK, 1)
    return img


def _lines(img, x, y, widths, color, step=2):
    for k, w in enumerate(widths):
        hline(img, x, x + w - 1, y + k * step, color)


def _disc(img, cx, cy, r, color):
    yy, xx = np.mgrid[0:PH, 0:PW]
    img[((xx - cx) ** 2 + (yy - cy) ** 2) <= r * r] = color


def pv_home(i):
    fill(i, 3, 7, 44, 15, tok('teal')); _lines(i, 6, 9, [18, 12], tok('paper')); fill(i, 6, 13, 13, 14, tok('coral'))
    for k in range(3):
        rect(i, 3 + k * 14, 18, 15 + k * 14, 28, tok('paper_4')); fill(i, 5 + k * 14, 20, 13 + k * 14, 23, tok('paper_3'))
        hline(i, 5 + k * 14, 11 + k * 14, 26, tok('ink_soft'))


def pv_auth(i):
    rect(i, 13, 7, 34, 29, tok('paper_4')); _disc(i, 23.5, 11, 2.5, tok('teal'))
    for y in (16, 21):
        rect(i, 16, y, 31, y + 3, tok('ink_soft'))
    fill(i, 16, 26, 31, 27, tok('coral'))


def pv_profile(i):
    _disc(i, 11, 14, 5.5, tok('sun')); _disc(i, 11, 12.5, 2.2, tok('paper')); fill(i, 8, 15, 14, 17, tok('paper'))
    _lines(i, 20, 9, [20, 14, 18], tok('ink_soft'), 3)
    for k in range(3):
        fill(i, 4 + k * 14, 23, 14 + k * 14, 28, tok('paper_3'))


def pv_catalog(i):
    for r in range(2):
        for c in range(4):
            x = 3 + c * 11; y = 7 + r * 11
            rect(i, x, y, x + 9, y + 9, tok('paper_4')); fill(i, x + 1, y + 1, x + 8, y + 5, (tok('teal'), tok('coral'), tok('sun'), tok('info'))[(r + c) % 4])
            hline(i, x + 1, x + 6, y + 7, tok('ink_soft'))


def pv_search(i):
    rrect(i, 4, 7, 43, 12, tok('ink_soft'), 1); _disc(i, 8, 9.5, 1.6, tok('teal'))
    for k in range(4):
        y = 15 + k * 4
        fill(i, 4, y, 6, y + 2, tok('paper_3')); hline(i, 9, 40 - k * 5, y, tok('teal')); hline(i, 9, 30 - k * 3, y + 2, tok('ink_soft'))


def pv_feed(i):
    for k in range(3):
        y = 7 + k * 8
        rect(i, 3, y, 44, y + 6, tok('paper_4')); _disc(i, 7, y + 3, 2, (tok('coral'), tok('teal'), tok('sun'))[k])
        hline(i, 11, 34 - k * 4, y + 2, tok('ink_soft')); hline(i, 11, 26, y + 4, tok('paper_3'))


def pv_messaging(i):
    rfill(i, 4, 7, 26, 11, tok('paper_3'), 1); rfill(i, 20, 13, 43, 17, tok('teal'), 1)
    rfill(i, 4, 19, 30, 23, tok('paper_3'), 1); rfill(i, 26, 25, 43, 28, tok('teal'), 1)


def pv_notifications(i):
    for k in range(4):
        y = 7 + k * 6
        fill(i, 4, y, 43, y + 4, tok('paper_2')); _disc(i, 7, y + 2, 1.6, (tok('coral'), tok('sun'), tok('info'), tok('success'))[k])
        hline(i, 11, 36 - k * 4, y + 2, tok('ink_soft'))


def pv_subscription(i):
    for k in range(3):
        x = 4 + k * 14
        hl = k == 1
        rfill(i, x, 8 if hl else 10, x + 11, 29, tok('teal') if hl else tok('paper_2'), 1)
        rrect(i, x, 8 if hl else 10, x + 11, 29, INK if hl else tok('paper_4'), 1)
        fill(i, x + 2, 12, x + 9, 13, tok('sun') if hl else tok('ink_soft'))
        for y in (17, 20, 23):
            hline(i, x + 2, x + 8, y, tok('paper') if hl else tok('paper_4'))


def pv_payment(i):
    rfill(i, 5, 8, 24, 20, tok('navy_3'), 1); fill(i, 5, 11, 24, 12, tok('ink')); fill(i, 7, 15, 10, 17, tok('sun'))
    for y in (9, 15):
        rect(i, 28, y, 43, y + 3, tok('ink_soft'))
    fill(i, 28, 24, 43, 26, tok('coral'))


def pv_reviews(i):
    for k in range(3):
        y = 7 + k * 8
        for s in range(5):
            c = tok('sun') if s < 5 - k else tok('paper_3')
            fill(i, 4 + s * 4, y, 6 + s * 4, y + 2, c)
        hline(i, 26, 42, y + 1, tok('ink_soft')); hline(i, 4, 38, y + 5, tok('paper_3'))


def pv_support(i):
    rfill(i, 4, 7, 22, 28, tok('paper_2'), 1)
    for k in range(4):
        hline(i, 6, 18, 10 + k * 4, tok('ink_soft'))
    _disc(i, 33, 15, 6.5, tok('info')); glyph(i, 31, 11, ['.##.', '#..#', '...#', '..#.', '.#..', '....', '.#..'], tok('paper'))


def pv_analytics(i):
    vline(i, 4, 7, 28, tok('ink_soft')); hline(i, 4, 44, 28, tok('ink_soft'))
    for k, h in enumerate((6, 10, 8, 14, 12, 18)):
        fill(i, 7 + k * 6, 27 - h, 10 + k * 6, 27, tok('teal') if k < 5 else tok('coral'))
    pts = [(7, 22), (13, 19), (19, 20), (25, 14), (31, 15), (37, 9), (43, 8)]
    for (a, b) in zip(pts[:-1], pts[1:]):
        n = max(abs(b[0] - a[0]), abs(b[1] - a[1]))
        for t in range(n + 1):
            put(i, a[0] + (b[0] - a[0]) * t // n, a[1] + (b[1] - a[1]) * t // n, tok('sun'))


def pv_storage(i):
    for r in range(2):
        for c in range(4):
            x = 4 + c * 10; y = 8 + r * 10
            fill(i, x, y, x + 3, y + 1, tok('sun')); fill(i, x, y + 2, x + 7, y + 7, tok('sun_hi')); rect(i, x, y + 2, x + 7, y + 7, tok('sun_lo'))


def pv_api(i):
    fill(i, 2, 6, 45, 29, tok('term_bg'))
    widths = [10, 22, 16, 26, 12, 20, 8]
    cols = [tok('sun'), tok('term_text'), tok('coral'), tok('term_text'), tok('info'), tok('term_text'), tok('sun')]
    for k, (w, c) in enumerate(zip(widths, cols)):
        x = 5 + (2 if 0 < k < 6 else 0) * (1 + k % 2)
        hline(i, x, x + w, 8 + k * 3, c)


def pv_admin(i):
    fill(i, 2, 6, 11, 29, tok('navy_2'))
    for k in range(5):
        hline(i, 4, 9, 9 + k * 4, tok('navy_hi'))
    for r in range(5):
        y = 8 + r * 4
        fill(i, 14, y, 44, y + 2, tok('paper_2') if r % 2 else tok('paper'))
        hline(i, 16, 24, y + 1, tok('ink_soft')); hline(i, 30, 36, y + 1, tok('ink_soft'))
        fill(i, 40, y, 42, y + 2, (tok('success'), tok('sun'), tok('danger'), tok('success'), tok('success'))[r])


def pv_security(i):
    sh = ['....######....', '..##......##..', '.#..........#.', '.#....##....#.', '.#...#..#...#.', '.#...####...#.',
          '.#...####...#.', '..#........#..', '...#......#...', '....##..##....', '......##......']
    glyph(i, 4, 9, sh, tok('teal'))
    _lines(i, 22, 10, [20, 14, 18, 10], tok('ink_soft'), 4)
    fill(i, 22, 26, 30, 27, tok('success'))


def pv_game_menu(i):
    fill(i, 2, 6, 45, 29, tok('navy'))
    fill(i, 12, 8, 35, 11, tok('sun'))
    for k in range(3):
        rfill(i, 16, 15 + k * 5, 31, 18 + k * 5, tok('coral') if k == 0 else tok('navy_3'), 1)
    glyph(i, 12, 15, ['#..', '##.', '#..'], tok('sun'))


def pv_progression(i):
    rfill(i, 4, 8, 43, 12, tok('paper_3'), 1); rfill(i, 4, 8, 30, 12, tok('success'), 1)
    for k in range(4):
        x = 6 + k * 11
        _disc(i, x + 3, 22, 4, tok('sun') if k < 3 else tok('paper_3'))
        hline(i, x + 7, x + 10, 22, tok('ink_soft')) if k < 3 else None


def pv_inventory(i):
    fill(i, 2, 6, 45, 29, tok('walnut' if False else 'paper_2'))
    for r in range(3):
        for c in range(6):
            x = 4 + c * 7; y = 8 + r * 7
            rect(i, x, y, x + 5, y + 5, tok('paper_4'))
            if (r * 6 + c) % 3 != 2:
                fill(i, x + 1, y + 1, x + 4, y + 4, (tok('coral'), tok('teal'), tok('sun'), tok('info'))[(r + c) % 4])


def pv_savegame(i):
    for k in range(3):
        y = 7 + k * 8
        rect(i, 3, y, 44, y + 6, tok('paper_4') if k else tok('teal'))
        fill(i, 5, y + 1, 13, y + 5, (tok('sky'), tok('sage'), tok('sand'))[k] if False else tok('paper_3'))
        hline(i, 16, 34, y + 2, tok('ink_soft')); hline(i, 16, 26, y + 4, tok('paper_3'))


def pv_leaderboard(i):
    medals = (tok('sun'), tok('paper_4'), tok('coral'))
    for k in range(5):
        y = 7 + k * 5
        fill(i, 3, y, 44, y + 3, tok('paper_2') if k % 2 else tok('paper'))
        fill(i, 5, y, 7, y + 3, medals[k] if k < 3 else tok('paper_3'))
        hline(i, 10, 28, y + 1, tok('ink_soft')); hline(i, 36, 42, y + 1, tok('teal'))


def pv_netplay(i):
    for x0, c in ((5, tok('teal')), (31, tok('coral'))):
        rect(i, x0, 10, x0 + 11, 18, tok('ink_soft')); fill(i, x0 + 1, 11, x0 + 10, 17, c); fill(i, x0 + 4, 19, x0 + 7, 20, tok('ink_soft'))
    for x in range(17, 31, 2):
        put(i, x, 14, tok('sun')); put(i, x, 15, tok('sun'))
    _disc(i, 23.5, 25, 3.5, tok('info'))


def pv_processing(i):
    for k in range(3):
        x = 3 + k * 15
        rect(i, x, 12, x + 10, 22, tok('ink_soft')); fill(i, x + 1, 13, x + 9, 21, (tok('paper_3'), tok('teal'), tok('success'))[k])
        if k < 2:
            hline(i, x + 11, x + 14, 17, tok('ink')); put(i, x + 13, 16, tok('ink')); put(i, x + 13, 18, tok('ink'))


def pv_integration(i):
    for k, c in enumerate((tok('teal'), tok('coral'))):
        x = 6 + k * 18
        fill(i, x, 10, x + 14, 24, c)
        if k == 0:
            fill(i, x + 15, 15, x + 17, 19, c)
        else:
            fill(i, x, 15, x + 1, 19, tok('paper'))
    glyph(i, 21, 26, ['#.#.#'], tok('ink_soft'))


def pv_engine(i):
    fill(i, 2, 6, 33, 29, tok('navy_2'))
    for x in range(4, 33, 5):
        vline(i, x, 7, 28, tok('navy_3'))
    for y in range(8, 29, 5):
        hline(i, 3, 32, y, tok('navy_3'))
    vline(i, 17, 12, 22, tok('success')); hline(i, 17, 26, 22, tok('danger')); put(i, 16, 23, tok('info')); put(i, 15, 24, tok('info'))
    fill(i, 35, 7, 45, 29, tok('paper_2'))
    _lines(i, 37, 9, [6, 7, 5, 7, 6], tok('ink_soft'), 4)


def pv_automation(i):
    nodes = [(6, 10), (22, 10), (38, 10), (14, 22), (30, 22)]
    for (a, b) in ((0, 1), (1, 2), (1, 3), (1, 4)):
        (x0, y0), (x1, y1) = nodes[a], nodes[b]
        n = max(abs(x1 - x0), abs(y1 - y0))
        for t in range(n + 1):
            put(i, x0 + (x1 - x0) * t // n + 2, y0 + (y1 - y0) * t // n + 2, tok('ink_soft'))
    for k, (x, y) in enumerate(nodes):
        rfill(i, x, y, x + 5, y + 4, (tok('sun'), tok('teal'), tok('teal'), tok('success'), tok('coral'))[k], 1)
        rrect(i, x, y, x + 5, y + 4, INK, 1)


def pv_infrastructure(i):
    for k in range(3):
        x = 5 + k * 13
        rect(i, x, 13, x + 9, 28, tok('ink_soft'))
        for y in range(15, 27, 3):
            hline(i, x + 2, x + 6, y, tok('navy_3')); put(i, x + 8, y, tok('term_ok'))
    rfill(i, 14, 6, 33, 10, tok('paper_2'), 2); rrect(i, 14, 6, 33, 10, tok('info'), 2)


PREVIEWS = {k: globals()[f'pv_{k}'] for k in COMPONENTS}


def preview(cid):
    img = _frame(dark=cid in ('api',))
    PREVIEWS[cid](img)
    rrect(img, 0, 0, PW - 1, PH - 1, INK, 1)
    return img


# ---------------------------------------------------------------------------
# Carte de composant, ports, liens
# ---------------------------------------------------------------------------
CARD_STATES = {'planned': 'paper_3', 'in_dev': 'sun', 'done': 'success', 'bug': 'danger', 'locked': 'paper_4'}


def comp_card():
    states = {}
    w, h, th = 56, 44, 14
    for st, hc in CARD_STATES.items():
        img = canvas(w, h + 2)
        rfill(img, 1, 2, w - 1, h + 1, tok('shadow'), 2)
        rfill(img, 0, 0, w - 1, h - 1, tok('paper') if st != 'locked' else tok('paper_2'), 2)
        rfill(img, 1, 1, w - 2, th, tok(hc), 1)
        hline(img, 1, w - 2, th + 1, INK)
        rrect(img, 0, 0, w - 1, h - 1, INK, 2)
        states[st] = img
    sel = canvas(w + 2, h + 4)
    rrect(sel, 0, 0, w + 1, h + 1, tok('sun'), 2)
    rrect(sel, 1, 1, w, h, tok('sun'), 1)
    return states['planned'], spec('Carte de composant', (6, th + 3, 6, 6), (40, 32), (5, th + 3, 5, 6), states=states,
                                   slots={'title': [4, 2, w - 8, 12], 'icon': [4, th + 5, 16, 16],
                                          'state_label': [23, th + 4, w - 28, 12], 'cost': [23, th + 16, w - 28, 12],
                                          'quality_bar': [23, h - 8, w - 28, 3], 'requirements': 'sous la carte ou en infobulle'},
                                   tokens={'title_text': 'ink', 'text': 'ink'},
                                   extras={'comp_card_selected': sel}, selection_nine_slice=[4, 4, 4, 4],
                                   note=("Couleur d'en-tete = etat de developpement (donnee UI). Slots en px pour une "
                                         "carte a la taille native ; ils suivent le 9-slice (ancres en haut/gauche)."))


PORT_COL = {'idle': 'paper_4', 'connected': 'teal', 'invalid': 'danger'}


def ports():
    out = {}
    for side in ('left', 'right', 'top', 'bottom'):
        for kind in ('in', 'out'):
            states = {}
            for st, c in PORT_COL.items():
                img = canvas(8, 8)
                rfill(img, 1, 1, 6, 6, tok(c), 2 if kind == 'out' else 1)
                rrect(img, 1, 1, 6, 6, INK, 2 if kind == 'out' else 1)
                if kind == 'in':
                    fill(img, 3, 3, 4, 4, tok('paper'))
                states[st] = img
            out[f'port_{kind}_{side}'] = (states['idle'], spec(f'Port {kind} ({side})', (0, 0, 0, 0), (8, 8), (0, 0, 0, 0),
                                                               states=states, resize='none', anchor=[4, 4],
                                                               note="Centre (4,4) pose sur le bord de la carte."))
    return out


def links():
    out = {}
    h = canvas(4, 4)
    hline(h, 0, 3, 0, INK); fill(h, 0, 1, 3, 2, (255, 255, 255, 255)); hline(h, 0, 3, 3, INK)
    out['link_h'] = (h, spec('Lien horizontal (teinte par multiplication)', (0, 0, 0, 0), (1, 4), (0, 0, 0, 0),
                             stretch='tile', resize='horizontal', tint='multiply'))
    v = np.transpose(h, (1, 0, 2)).copy()
    out['link_v'] = (v, spec('Lien vertical', (0, 0, 0, 0), (4, 1), (0, 0, 0, 0), stretch='tile', resize='vertical', tint='multiply'))
    dh = h.copy(); dh[:, 2:] = 0
    out['link_dashed_h'] = (dh, spec('Lien optionnel (tirets) horizontal', (0, 0, 0, 0), (4, 4), (0, 0, 0, 0),
                                     stretch='tile', resize='horizontal', tint='multiply'))
    dv = np.transpose(dh, (1, 0, 2)).copy()
    out['link_dashed_v'] = (dv, spec('Lien optionnel (tirets) vertical', (0, 0, 0, 0), (4, 4), (0, 0, 0, 0),
                                     stretch='tile', resize='vertical', tint='multiply'))
    W = (255, 255, 255, 255)
    corner = canvas(4, 4)
    for (x, y) in [(1, 1), (2, 1), (3, 1), (1, 2), (2, 2), (3, 2), (1, 3), (2, 3)]:
        put(corner, x, y, W)
    for (x, y) in [(0, 0), (1, 0), (2, 0), (3, 0), (0, 1), (0, 2), (0, 3)]:
        put(corner, x, y, INK)
    put(corner, 3, 3, INK)
    for name, im in (('link_corner_se', corner), ('link_corner_sw', corner[:, ::-1].copy()),
                     ('link_corner_ne', corner[::-1].copy()), ('link_corner_nw', corner[::-1, ::-1].copy())):
        out[name] = (im, spec(f'Angle de lien ({name[-2:]})', (0, 0, 0, 0), (4, 4), (0, 0, 0, 0), resize='none', tint='multiply'))
    tee = canvas(4, 4)
    hline(tee, 0, 3, 0, INK); fill(tee, 0, 1, 3, 2, W); put(tee, 0, 3, INK); put(tee, 3, 3, INK); fill(tee, 1, 3, 2, 3, W)
    out['link_tee_down'] = (tee, spec('Jonction en T (vers le bas)', (0, 0, 0, 0), (4, 4), (0, 0, 0, 0), resize='none', tint='multiply'))
    out['link_tee_up'] = (tee[::-1].copy(), spec('Jonction en T (vers le haut)', (0, 0, 0, 0), (4, 4), (0, 0, 0, 0),
                                                 resize='none', tint='multiply'))
    arrow = canvas(7, 7)
    rows = ['#......', '##.....', '#W#....', '#WW#...', '#W#....', '##.....', '#......']
    for j, r in enumerate(rows):
        for i, c in enumerate(r):
            if c == '#':
                put(arrow, i, j, INK)
            elif c == 'W':
                put(arrow, i, j, W)
    arrow[2:5, 0] = W
    arrow[1:6, 0] = INK
    for name, im in (('link_arrow_right', arrow), ('link_arrow_left', arrow[:, ::-1].copy()),
                     ('link_arrow_down', np.transpose(arrow, (1, 0, 2)).copy()),
                     ('link_arrow_up', np.transpose(arrow, (1, 0, 2))[::-1].copy())):
        out[name] = (im, spec(f'Pointe de lien ({name.split("_")[-1]})', (0, 0, 0, 0), (7, 7), (0, 0, 0, 0),
                              resize='none', tint='multiply', anchor=[3, 3]))
    return out


LINK_STATES = {'required': 'teal', 'optional': 'paper_4', 'satisfied': 'success', 'broken': 'danger'}
