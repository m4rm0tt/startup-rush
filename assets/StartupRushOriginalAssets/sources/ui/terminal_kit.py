"""Kit terminal (mini-jeu de deploiement FICTIF, local) : cadre, barre de titre,
surface, curseurs, champ de commande, marqueurs de lignes, segments de progression,
defilement sombre. Le texte reste dessine par le jeu avec « StartupRush Mono »
(cellule 6 x 12 px). Aucune execution reelle de commandes.
"""
import numpy as np

from srgen.lot_ui_core import canvas, fill, hline, vline, rect, rrect, rfill, put, tok
from ui.hud import spec, spec_r, glyph

INK = tok('ink')
CELL_W, CELL_H = 6, 12


def term_frame():
    w, h, th = 40, 40, 12
    img = canvas(w, h + 2)
    rfill(img, 1, 2, w - 1, h + 1, tok('shadow'), 2)
    rfill(img, 0, 0, w - 1, h - 1, tok('term_bg'), 2)
    rfill(img, 1, 1, w - 2, th, tok('navy_2'), 1)
    hline(img, 2, w - 3, 1, tok('navy_3'))
    hline(img, 1, w - 2, th + 1, tok('navy_0'))
    for k, c in enumerate(('coral', 'sun', 'success')):
        fill(img, 4 + k * 5, 5, 6 + k * 5, 7, tok(c))
    rrect(img, 0, 0, w - 1, h - 1, INK, 2)
    return img, spec('Terminal : cadre + barre de titre', (20, th + 3, 4, 5), (28, th + 12), (5, th + 4, 5, 6),
                     tokens={'title_text': 'text_on_dark', 'text': 'term_text'},
                     title_bar={'height': th, 'title_padding': [20, 0]},
                     note="Les 3 pastilles sont decoratives (pas de commande systeme).")


def term_titlebar():
    img = canvas(16, 12)
    rfill(img, 0, 0, 15, 11, tok('navy_2'), 1)
    hline(img, 1, 14, 1, tok('navy_3'))
    rrect(img, 0, 0, 15, 11, INK, 1)
    return img, spec_r('horizontal', 'Terminal : barre de titre seule', (3, 3, 3, 3), (8, 12), (5, 0, 5, 0))


def term_surface():
    img = canvas(16, 16, tok('term_bg'))
    hline(img, 0, 15, 0, tok('navy_0')); vline(img, 0, 0, 15, tok('navy_0'))
    return img, spec('Terminal : surface de texte', (2, 2, 2, 2), (8, 8), (3, 2, 3, 2),
                     cell=[CELL_W, CELL_H], tokens={'text': 'term_text', 'dim': 'term_dim'})


def term_cursors():
    blk = canvas(CELL_W, CELL_H)
    fill(blk, 0, 2, CELL_W - 1, 10, tok('term_text', 220))
    und = canvas(CELL_W, CELL_H)
    fill(und, 0, 10, CELL_W - 1, 11, tok('term_text'))
    bar = canvas(CELL_W, CELL_H)
    fill(bar, 0, 1, 0, 10, tok('term_text'))
    return {'term_cursor_block': (blk, spec('Curseur bloc (cellule 6x12)', (0, 0, 0, 0), (CELL_W, CELL_H), (0, 0, 0, 0),
                                            resize='none', note="Clignotement par le code (opacite 0/1, 0,5 s).")),
            'term_cursor_underline': (und, spec('Curseur souligne', (0, 0, 0, 0), (CELL_W, CELL_H), (0, 0, 0, 0), resize='none')),
            'term_cursor_bar': (bar, spec('Curseur barre (saisie)', (0, 0, 0, 0), (CELL_W, CELL_H), (0, 0, 0, 0), resize='none'))}


def term_input():
    states = {}
    for st in ('normal', 'focus', 'disabled'):
        img = canvas(24, 16)
        rfill(img, 0, 0, 23, 15, tok('term_bg_2') if st != 'disabled' else tok('navy_0'), 1)
        border = {'normal': tok('navy_2'), 'focus': tok('term_dim'), 'disabled': tok('navy_0')}[st]
        rrect(img, 0, 0, 23, 15, border, 1)
        if st == 'focus':
            hline(img, 1, 22, 14, tok('term_text'))
        glyph(img, 3, 5, ['#..', '.#.', '..#', '.#.', '#..'], tok('term_dim'))
        states[st] = img
    return states['normal'], spec_r('horizontal', 'Champ de commande', (9, 3, 3, 3), (24, 16), (10, 2, 4, 2), states=states,
                                    tokens={'text': 'term_text', 'placeholder': 'term_dim'},
                                    note="Chevron d'invite dans la marge gauche ; texte saisi rendu en police mono.")


MARKS = {
    'prompt': ('Invite', 'term_text', ['.....', '#....', '.#...', '..#..', '.#...', '#....']),
    'ok': ('Succès', 'term_ok', ['.....', '....#', '...##', '#.##.', '###..', '.#...']),
    'error': ('Erreur', 'term_err', ['.....', '#...#', '.#.#.', '..#..', '.#.#.', '#...#']),
    'warning': ('Avertissement', 'term_warn', ['..#..', '..#..', '..#..', '..#..', '.....', '..#..']),
    'info': ('Information', 'term_info', ['..#..', '.....', '.##..', '..#..', '..#..', '.###.']),
    'running': ('En cours', 'term_text', ['.....', '.###.', '#..##', '#.###', '##..#', '.###.']),
}


def term_marks():
    out = {}
    for k, (name, col, rows) in MARKS.items():
        img = canvas(CELL_W, CELL_H)
        glyph(img, 0, 4, rows, tok(col))
        out[f'term_mark_{k}'] = (img, spec(f'Marqueur de ligne : {name}', (0, 0, 0, 0), (CELL_W, CELL_H), (0, 0, 0, 0),
                                           resize='none', tokens={'color': col},
                                           note="Occupe une cellule mono ; aligne sur la ligne de base du texte."))
    return out


def term_progress():
    out = {}
    full = canvas(CELL_W, CELL_H)
    fill(full, 0, 3, CELL_W - 2, 9, tok('term_text'))
    half = canvas(CELL_W, CELL_H)
    fill(half, 0, 3, 2, 9, tok('term_text'))
    empty = canvas(CELL_W, CELL_H)
    for y in range(3, 10):
        for x in range(0, CELL_W - 1):
            if (x + y) % 2 == 0:
                put(empty, x, y, tok('term_dim'))
    capl = canvas(CELL_W, CELL_H)
    glyph(capl, 1, 2, ['###', '#..', '#..', '#..', '#..', '#..', '#..', '###'], tok('term_dim'))
    capr = capl[:, ::-1].copy()
    for k, im, n in (('full', full, 'plein'), ('half', half, 'moitie'), ('empty', empty, 'vide'),
                     ('cap_left', capl, 'crochet gauche'), ('cap_right', capr, 'crochet droit')):
        out[f'term_progress_{k}'] = (im, spec(f'Segment de progression ({n})', (0, 0, 0, 0), (CELL_W, CELL_H), (0, 0, 0, 0),
                                              resize='none', note="Barre [####--] composee de cellules mono."))
    return out


def term_scroll():
    tr = canvas(6, 16)
    fill(tr, 0, 0, 5, 15, tok('navy_0'))
    th = canvas(6, 16)
    rfill(th, 0, 0, 5, 15, tok('navy_2'), 1)
    vline(th, 1, 2, 13, tok('navy_3'))
    rrect(th, 0, 0, 5, 15, INK, 1)
    sel = canvas(CELL_W, CELL_H)
    fill(sel, 0, 0, CELL_W - 1, CELL_H - 1, tok('navy_2', 200))
    return {'term_scroll_track': (tr, spec_r('vertical', 'Défilement sombre (piste)', (0, 2, 0, 2), (6, 8), (0, 0, 0, 0))),
            'term_scroll_thumb': (th, spec_r('vertical', 'Défilement sombre (curseur)', (0, 3, 0, 3), (6, 8), (0, 0, 0, 0))),
            'term_selection': (sel, spec('Surlignage de ligne (cellule)', (0, 0, 0, 0), (CELL_W, CELL_H), (0, 0, 0, 0),
                                         stretch='tile', resize='horizontal'))}


def build_all():
    items = {'term_frame': term_frame(), 'term_titlebar': term_titlebar(), 'term_surface': term_surface(),
             'term_input': term_input()}
    for g in (term_cursors(), term_marks(), term_progress(), term_scroll()):
        items.update(g)
    return items
