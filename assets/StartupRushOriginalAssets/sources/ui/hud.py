"""Boite a outils HUD « Papier » (sources procedurales, pixels natifs).

Chaque fonction retourne (image RGBA, spec). Spec :
  nine_slice [g, h, d, b] (px natifs), min_size, content_padding [g, h, d, b],
  stretch ('stretch' | 'tile'), states {etat: image}, tokens (couleurs de texte...).
Aucun texte n'est cuit : le jeu dessine textes et nombres avec la police livree.
Bordure encre 1 px, coins coupes (rayon 1) ou arrondis (rayon 2), biseau clair en
haut/gauche, ombre portee de 2 px vers le bas (meme direction de lumiere que la scene).
"""
import numpy as np

from srgen.lot_ui_core import (canvas, fill, hline, vline, rect, rrect, rfill, put, blit, tok, nine, tint)

INK = tok('ink')
SH = tok('shadow')


# ---------------------------------------------------------------------------
# Briques
# ---------------------------------------------------------------------------
def box(w, h, fill_c, border=INK, hi=None, lo=None, r=1, shadow=0, depth=0, depth_c=None, ring=0, ring_c=None):
    """Boite generique. ring = marge transparente (anneau de focus) autour."""
    W, H = w + 2 * ring, h + 2 * ring + shadow
    img = canvas(W, H)
    x0, y0 = ring, ring
    x1, y1 = ring + w - 1, ring + h - 1
    if shadow:
        rfill(img, x0 + 1, y0 + shadow, x1, y1 + shadow, SH, r)
    rfill(img, x0, y0, x1, y1, fill_c, r)
    if depth and depth_c is not None:
        fill(img, x0 + 1, y1 - depth, x1 - 1, y1 - 1, depth_c)
    if hi is not None:
        hline(img, x0 + 1 + (r > 1), x1 - 1 - (r > 1), y0 + 1, hi)
        vline(img, x0 + 1, y0 + 1 + (r > 1), y1 - 1 - depth - (r > 1), hi)
    if lo is not None:
        vline(img, x1 - 1, y0 + 2, y1 - 1 - depth, lo)
        if not depth:
            hline(img, x0 + 2, x1 - 1, y1 - 1, lo)
    if border is not None:
        rrect(img, x0, y0, x1, y1, border, r)
    if ring and ring_c is not None:
        rrect(img, x0 - 1, y0 - 1, x1 + 1, y1 + 1, ring_c, min(2, r + 1))
    return img


def spec(name, nine_slice, min_size, padding, stretch='stretch', resize='both', **kw):
    d = {'name_fr': name, 'nine_slice': list(nine_slice), 'min_size': list(min_size),
         'content_padding': list(padding), 'stretch': stretch, 'resize': resize}
    d.update(kw)
    return d


def spec_r(resize, name, *args, **kw):
    """spec() avec l'axe de redimensionnement en premier argument."""
    return spec(name, *args, resize=resize, **kw)


def glyph(img, x, y, rows, color):
    for j, r in enumerate(rows):
        for i, c in enumerate(r):
            if c == '#':
                put(img, x + i, y + j, color)


CHECK = ['.....#', '....##', '#..##.', '####..', '.##...']
CARET = ['#####', '.###.', '..#..']
CARET_UP = ['..#..', '.###.', '#####']
CARET_RIGHT = ['#..', '##.', '###', '##.', '#..']
CARET_LEFT = ['..#', '.##', '###', '.##', '..#']
MINUS = ['######']
PLUS5 = ['..#..', '..#..', '#####', '..#..', '..#..']
SEARCH = ['.###..', '#...#.', '#...#.', '#...#.', '.###..', '....##', '.....#']
GRIP = ['....#', '.....', '..#.#', '.....', '#.#.#']
COIN = ['.###.', '#YYs#', '#YsS#', '#YSs#', '.###.']


# ---------------------------------------------------------------------------
# Panneaux et conteneurs
# ---------------------------------------------------------------------------
def panel_main():
    img = box(24, 24, tok('paper'), hi=tok('paper_hi'), lo=tok('paper_3'), r=2, shadow=2)
    return img, spec('Panneau principal', (4, 4, 4, 6), (12, 14), (6, 6, 6, 8), tokens={'text': 'ink'})


def panel_secondary():
    img = box(16, 16, tok('paper_2'), border=tok('paper_4'), hi=tok('paper'), lo=tok('paper_3'), r=1)
    return img, spec('Panneau secondaire', (3, 3, 3, 3), (8, 8), (4, 4, 4, 4), tokens={'text': 'ink'})


def panel_inset():
    img = canvas(16, 16)
    rfill(img, 0, 0, 15, 15, tok('paper_3'), 1)
    hline(img, 1, 14, 1, tok('paper_4'))
    vline(img, 1, 1, 14, tok('paper_4'))
    rrect(img, 0, 0, 15, 15, tok('paper_4'), 1)
    return img, spec('Zone en creux (liste, graphique)', (3, 3, 3, 3), (8, 8), (4, 4, 4, 4))


def window_frame(modal=False):
    w, h, th = 40, 44, 16
    sh = 4 if modal else 2
    img = box(w, h, tok('paper'), hi=tok('paper_hi'), lo=tok('paper_3'), r=2, shadow=sh)
    # barre de titre
    rfill(img, 1, 1, w - 2, th, tok('teal'), 1)
    hline(img, 2, w - 3, 1, tok('teal_hi'))
    hline(img, 1, w - 2, th, tok('teal_lo'))
    hline(img, 1, w - 2, th + 1, INK)
    rrect(img, 0, 0, w - 1, h - 1, INK, 2)
    name = 'Fenêtre modale' if modal else 'Fenêtre (cadre + barre de titre)'
    return img, spec(name, (4, th + 3, 4, 4 + sh), (20, th + 8 + sh), (6, th + 5, 6, 6 + sh),
                     tokens={'title_text': 'text_on_accent', 'text': 'ink'},
                     title_bar={'height': th, 'text_padding': [6, 2], 'close_button_slot': [w - 15, 2, 12, 12]},
                     **({'scrim': {'color': 'scrim', 'rgba': list(tok('scrim'))}} if modal else {}))


def titlebar():
    img = canvas(16, 16)
    rfill(img, 0, 0, 15, 15, tok('teal'), 1)
    hline(img, 1, 14, 1, tok('teal_hi'))
    hline(img, 1, 14, 14, tok('teal_lo'))
    rrect(img, 0, 0, 15, 15, INK, 1)
    return img, spec_r('horizontal', 'Barre de titre', (3, 3, 3, 3), (8, 16), (5, 2, 5, 2), tokens={'text': 'text_on_accent'})


def banner():
    img = box(24, 18, tok('sun'), hi=tok('sun_hi'), r=1, shadow=2, depth=2, depth_c=tok('sun_lo'))
    left = canvas(8, 22)
    # extremite en ruban (repliee)
    fill(left, 2, 4, 7, 17, tok('sun_lo'))
    for y in range(4, 18):
        put(left, 1, y, INK)
    hline(left, 1, 7, 3, INK); hline(left, 1, 7, 18, INK)
    glyph(left, 0, 8, ['#.', '##', '#.'], INK)
    right = left[:, ::-1].copy()
    return img, spec('Bandeau', (4, 4, 4, 6), (12, 16), (6, 3, 6, 6), tokens={'text': 'ink'},
                     extras={'banner_end_left': left, 'banner_end_right': right})


def popover():
    img = box(20, 20, tok('paper'), hi=tok('paper_hi'), lo=tok('paper_3'), r=2, shadow=2)
    arrows = {}
    up = canvas(9, 5)
    for k in range(5):
        hline(up, 4 - k, 4 + k, k, tok('paper'))
        put(up, 4 - k, k, INK); put(up, 4 + k, k, INK)
    arrows['popover_arrow_up'] = up
    arrows['popover_arrow_down'] = up[::-1].copy()
    lf = np.transpose(up, (1, 0, 2)).copy()
    arrows['popover_arrow_left'] = lf
    arrows['popover_arrow_right'] = lf[:, ::-1].copy()
    return img, spec('Popover', (4, 4, 4, 6), (12, 12), (6, 5, 6, 7), tokens={'text': 'ink'},
                     extras=arrows, note="La pointe recouvre la bordure du panneau (chevauchement de 1 px).")


def tooltip():
    img = canvas(12, 12)
    rfill(img, 0, 0, 11, 11, tok('navy'), 1)
    hline(img, 1, 10, 1, tok('navy_2'))
    rrect(img, 0, 0, 11, 11, INK, 1)
    ptr = canvas(7, 4)
    for k in range(4):
        hline(ptr, k, 6 - k, k, tok('navy'))
        put(ptr, k, k, INK); put(ptr, 6 - k, k, INK)
    return img, spec('Infobulle', (3, 3, 3, 3), (8, 8), (4, 2, 4, 2), tokens={'text': 'text_on_dark'},
                     extras={'tooltip_pointer_down': ptr, 'tooltip_pointer_up': ptr[::-1].copy()})


def notification():
    states = {}
    for st, c in (('info', 'info'), ('success', 'success'), ('warning', 'warning'), ('danger', 'danger')):
        img = box(32, 24, tok('paper'), hi=tok('paper_hi'), lo=tok('paper_3'), r=2, shadow=2)
        fill(img, 1, 2, 4, 21, tok(c))
        vline(img, 5, 2, 21, INK)
        states[st] = img
    return states['info'], spec('Notification', (8, 4, 4, 6), (16, 14), (10, 4, 6, 7), states=states,
                                tokens={'text': 'ink', 'icon_slot': [8, 4, 16, 16]},
                                note="Bande de couleur semantique a gauche ; icone (16 px) et texte dessines par le jeu.")


def toast():
    img = box(24, 16, tok('navy'), border=INK, hi=tok('navy_2'), r=2, shadow=2)
    return img, spec_r('horizontal', 'Toast', (5, 4, 5, 6), (12, 14), (7, 2, 7, 4), tokens={'text': 'text_on_dark'})


def card():
    states = {}
    for st in ('normal', 'hover', 'selected', 'disabled'):
        fillc = tok('paper') if st != 'disabled' else tok('paper_3')
        img = box(40, 48, fillc, hi=tok('paper_hi'), lo=tok('paper_3'), r=2, shadow=2)
        hc = {'normal': 'teal', 'hover': 'teal_hi', 'selected': 'teal', 'disabled': 'paper_4'}[st]
        rfill(img, 1, 1, 38, 13, tok(hc), 1)
        hline(img, 1, 38, 14, INK)
        if st == 'selected':
            rrect(img, 0, 0, 39, 47, tok('sun'), 2)
            rrect(img, 1, 1, 38, 46, tok('sun'), 1)
            hline(img, 2, 37, 14, INK)
        else:
            rrect(img, 0, 0, 39, 47, INK, 2)
        states[st] = img
    return states['normal'], spec('Carte produit / projet', (4, 16, 4, 6), (24, 28), (5, 17, 5, 7), states=states,
                                  tokens={'title_text': 'text_on_accent', 'text': 'ink'},
                                  header={'height': 14, 'title_padding': [5, 1]})


def list_row():
    states = {}
    base = canvas(16, 14)
    for st in ('normal', 'hover', 'selected', 'disabled'):
        img = base.copy()
        if st == 'hover':
            fill(img, 0, 0, 15, 12, tok('paper_2'))
        if st == 'selected':
            fill(img, 0, 0, 15, 12, tok('teal_light'))
            fill(img, 0, 0, 1, 12, tok('teal'))
        hline(img, 0, 15, 13, tok('paper_3'))
        states[st] = img
    return states['normal'], spec('Ligne de liste / tableau', (3, 1, 3, 2), (8, 14), (5, 1, 4, 1), states=states,
                                  tokens={'text': 'ink', 'text_disabled': 'text_disabled'})


def separators():
    h = canvas(8, 3)
    hline(h, 0, 7, 0, tok('paper_4')); hline(h, 0, 7, 1, tok('paper_hi'))
    v = canvas(3, 8)
    vline(v, 0, 0, 7, tok('paper_4')); vline(v, 1, 0, 7, tok('paper_hi'))
    return (h, spec_r('horizontal', 'Séparateur horizontal', (0, 0, 0, 0), (1, 2), (0, 0, 0, 0), stretch='tile')), \
           (v, spec_r('vertical', 'Séparateur vertical', (0, 0, 0, 0), (2, 1), (0, 0, 0, 0), stretch='tile'))


def drag_handle():
    img = canvas(8, 14)
    for y in (2, 6, 10):
        for x in (1, 5):
            fill(img, x, y, x + 1, y + 1, tok('paper_4'))
    return img, spec('Poignée de déplacement', (0, 0, 0, 0), (8, 14), (0, 0, 0, 0))


def section_header():
    img = canvas(16, 14)
    fill(img, 0, 3, 2, 8, tok('coral'))
    fill(img, 0, 12, 15, 13, tok('teal'))
    return img, spec_r('horizontal', "En-tête de section", (5, 2, 2, 3), (8, 14), (6, 0, 2, 3), tokens={'text': 'ink'})


# ---------------------------------------------------------------------------
# Boutons
# ---------------------------------------------------------------------------
BTN = {
    'primary':     dict(fill='coral', hi='coral_hi', lo='coral_lo', depth='coral_deep', text='text_on_accent'),
    'secondary':   dict(fill='paper', hi='paper_hi', lo='paper_3', depth='paper_4', text='ink'),
    'destructive': dict(fill='danger', hi='danger_hi', lo='danger_lo', depth='danger_deep', text='text_on_accent'),
}


def button(kind, w=24, h=18):
    s = BTN[kind]
    states = {}
    states['normal'] = box(w, h, tok(s['fill']), hi=tok(s['hi']), r=1, depth=2, depth_c=tok(s['depth']), ring=1)
    states['hover'] = box(w, h, tok(s['hi']), hi=tok('paper_hi') if kind != 'secondary' else tok('paper_hi'), r=1,
                          depth=2, depth_c=tok(s['depth']), ring=1)
    pressed = box(w, h, tok(s['lo']), r=1, ring=1)
    hline(pressed, 2, w - 1, 2, tok(s['depth']))   # ombre interne en haut
    states['pressed'] = pressed
    states['disabled'] = box(w, h, tok('disabled_bg'), border=tok('disabled_line'), r=1, ring=1)
    states['focus'] = box(w, h, tok(s['fill']), hi=tok(s['hi']), r=1, depth=2, depth_c=tok(s['depth']), ring=1,
                          ring_c=tok('sun'))
    names = {'primary': 'Bouton principal', 'secondary': 'Bouton secondaire', 'destructive': 'Bouton destructif'}
    return states['normal'], spec(names[kind], (4, 4, 4, 5), (12, 20), (7, 4, 7, 6), states=states,
                                  tokens={'text': s['text'], 'text_disabled': 'text_disabled'},
                                  pressed_content_offset=[0, 1],
                                  note="Toile = bouton 24x18 + anneau de focus de 1 px (transparent hors focus).")


def button_text():
    w, h = 24, 18
    states = {}
    states['normal'] = canvas(w + 2, h + 2)
    states['hover'] = box(w, h, tok('paper_2'), border=None, r=1, ring=1)
    states['pressed'] = box(w, h, tok('paper_3'), border=None, r=1, ring=1)
    states['disabled'] = canvas(w + 2, h + 2)
    f = canvas(w + 2, h + 2)
    rrect(f, 0, 0, w + 1, h + 1, tok('sun'), 2)
    states['focus'] = f
    return states['normal'], spec('Bouton texte', (4, 4, 4, 4), (12, 20), (7, 4, 7, 4), states=states,
                                  tokens={'text': 'teal', 'text_hover': 'teal_lo', 'text_disabled': 'text_disabled'})


def button_icon(size=18, small=False):
    states = {}
    if small:
        st = {'normal': canvas(size + 2, size + 2),
              'hover': box(size, size, tok('teal_hi'), border=None, r=1, ring=1),
              'pressed': box(size, size, tok('teal_lo'), border=None, r=1, ring=1),
              'disabled': canvas(size + 2, size + 2)}
        f = canvas(size + 2, size + 2)
        rrect(f, 0, 0, size + 1, size + 1, tok('sun'), 2)
        st['focus'] = f
        return st['normal'], spec('Petit bouton icone (barre de titre)', (3, 3, 3, 3), (size + 2, size + 2),
                                  (1, 1, 1, 1), states=st, tokens={'icon': 'text_on_accent'},
                                  icon_size=12 if size <= 12 else 16)
    s = BTN['secondary']
    states['normal'] = box(size, size, tok(s['fill']), hi=tok(s['hi']), r=1, depth=2, depth_c=tok(s['depth']), ring=1)
    states['hover'] = box(size, size, tok('paper_hi'), hi=tok('paper_hi'), r=1, depth=2, depth_c=tok(s['depth']), ring=1)
    p = box(size, size, tok(s['lo']), r=1, ring=1)
    hline(p, 2, size - 1, 2, tok(s['depth']))
    states['pressed'] = p
    states['disabled'] = box(size, size, tok('disabled_bg'), border=tok('disabled_line'), r=1, ring=1)
    states['focus'] = box(size, size, tok(s['fill']), hi=tok(s['hi']), r=1, depth=2, depth_c=tok(s['depth']), ring=1,
                          ring_c=tok('sun'))
    states['toggled'] = box(size, size, tok('teal'), hi=tok('teal_hi'), r=1, depth=2, depth_c=tok('teal_lo'), ring=1)
    return states['normal'], spec('Bouton icone', (4, 4, 4, 5), (size + 2, size + 2), (2, 2, 2, 3), states=states,
                                  tokens={'icon': 'ink', 'icon_toggled': 'text_on_accent', 'icon_disabled': 'text_disabled'},
                                  icon_size=16, pressed_content_offset=[0, 1])


# ---------------------------------------------------------------------------
# Navigation
# ---------------------------------------------------------------------------
def tabs():
    w, h = 24, 18
    states = {}
    for st, c in (('inactive', 'paper_3'), ('hover', 'paper_2'), ('active', 'paper')):
        img = canvas(w, h + 2)
        y1 = h + 1 if st == 'active' else h
        rfill(img, 0, 2 if st != 'active' else 0, w - 1, y1, tok(c), 1)
        top = 2 if st != 'active' else 0
        hline(img, 1, w - 2, top, INK)
        vline(img, 0, top + 1, y1, INK); vline(img, w - 1, top + 1, y1, INK)
        if st == 'active':
            hline(img, 1, w - 2, 1, tok('paper_hi'))
            fill(img, 2, 2, w - 3, 3, tok('coral'))
        else:
            hline(img, 0, w - 1, h, INK)
        states[st] = img
    return states['inactive'], spec_r('horizontal', 'Onglet', (4, 4, 4, 3), (12, 20), (6, 5, 6, 3), states=states,
                                    tokens={'text': 'ink', 'text_inactive': 'text_muted'},
                                    note="L'onglet actif deborde de 2 px vers le bas pour recouvrir la bordure du panneau.")


def segmented():
    out = {}
    for piece in ('left', 'mid', 'right'):
        states = {}
        for st, c in (('normal', 'paper'), ('hover', 'paper_hi'), ('selected', 'teal'), ('disabled', 'disabled_bg')):
            img = canvas(16, 18)
            fill(img, 0, 0, 15, 17, tok(c))
            hline(img, 0, 15, 0, INK); hline(img, 0, 15, 17, INK)
            if st == 'selected':
                hline(img, 1, 14, 1, tok('teal_lo'))
            elif st != 'disabled':
                hline(img, 0, 15, 16, tok('paper_3'))
            if piece == 'left':
                vline(img, 0, 0, 17, INK); put(img, 0, 0, (0, 0, 0, 0)); put(img, 0, 17, (0, 0, 0, 0))
            if piece == 'right':
                vline(img, 15, 0, 17, INK); put(img, 15, 0, (0, 0, 0, 0)); put(img, 15, 17, (0, 0, 0, 0))
            if piece != 'right':
                vline(img, 15, 1, 16, tok('paper_4') if st != 'selected' else INK)
            states[st] = img
        out[piece] = (states['normal'], spec_r('horizontal', f'Contrôle segmenté ({piece})',
                                             (3 if piece == 'left' else 0, 3, 3 if piece == 'right' else 1, 3),
                                             (8, 18), (5, 3, 5, 3), states=states,
                                             tokens={'text': 'ink', 'text_selected': 'text_on_accent'}))
    return out


def badges():
    out = {}
    for st, c, tc in (('neutral', 'paper_3', 'ink'), ('info', 'info', 'text_on_accent'),
                      ('success', 'success', 'text_on_accent'), ('warning', 'warning', 'ink'),
                      ('danger', 'danger', 'text_on_accent'), ('accent', 'coral', 'text_on_accent')):
        img = canvas(12, 12)
        rfill(img, 0, 0, 11, 11, tok(c), 2)
        hline(img, 2, 9, 1, tok('paper_hi') if st == 'neutral' else tok(c))
        rrect(img, 0, 0, 11, 11, INK, 2)
        out[st] = (img, spec_r('horizontal', f'Badge {st}', (3, 3, 3, 3), (8, 12), (4, 0, 4, 0), tokens={'text': tc}))
    cnt = canvas(12, 12)
    from srgen.lot_ui_core import fonts
    yy, xx = np.mgrid[0:12, 0:12]
    d = (xx - 5.5) ** 2 + (yy - 5.5) ** 2
    cnt[d <= 30] = tok('coral')
    ring = (d <= 30) & ~((((xx - 5.5) ** 2 + (yy - 4.5) ** 2) <= 30) & (((xx - 5.5) ** 2 + (yy - 6.5) ** 2) <= 30) &
                         (((xx - 4.5) ** 2 + (yy - 5.5) ** 2) <= 30) & (((xx - 6.5) ** 2 + (yy - 5.5) ** 2) <= 30))
    cnt[ring] = INK
    out['count'] = (cnt, spec_r('horizontal', 'Pastille compteur', (5, 5, 5, 5), (12, 12), (3, 0, 3, 0), tokens={'text': 'text_on_accent'},
                              note="S'etire horizontalement pour 2-3 chiffres."))
    return out


# ---------------------------------------------------------------------------
# Champs de saisie
# ---------------------------------------------------------------------------
def field_img(w, h, st, left_icon=False, caret=False, stepper=False):
    img = canvas(w, h)
    bg = tok('paper') if st not in ('disabled',) else tok('disabled_bg')
    rfill(img, 0, 0, w - 1, h - 1, bg, 1)
    if st != 'disabled':
        hline(img, 1, w - 2, 1, tok('paper_3'))
        vline(img, 1, 1, h - 2, tok('paper_3'))
    border = {'normal': INK, 'hover': tok('teal_lo'), 'focus': tok('teal'), 'disabled': tok('disabled_line'),
              'error': tok('danger'), 'open': tok('teal')}[st]
    rrect(img, 0, 0, w - 1, h - 1, border, 1)
    if st in ('focus', 'open'):
        rrect(img, 1, 1, w - 2, h - 2, tok('teal_light'), 1)
    if st == 'error':
        rrect(img, 1, 1, w - 2, h - 2, tok('danger_hi'), 1)
    fg = tok('ink_soft') if st != 'disabled' else tok('disabled_line')
    if left_icon:
        glyph(img, 4, (h - 7) // 2, SEARCH, fg)
    if caret:
        vline(img, w - 13, 2, h - 3, tok('paper_3') if st != 'disabled' else tok('disabled_line'))
        glyph(img, w - 9, (h - 3) // 2, CARET if st != 'open' else CARET_UP, fg)
    if stepper:
        fill(img, w - 11, 1, w - 2, h - 2, tok('paper_2') if st != 'disabled' else tok('disabled_bg'))
        vline(img, w - 12, 1, h - 2, border)
        hline(img, w - 11, w - 2, h // 2, tok('paper_4'))
        glyph(img, w - 9, 3, CARET_UP, fg)
        glyph(img, w - 9, h - 6, CARET, fg)
    return img


def fields():
    out = {}
    sts = ('normal', 'hover', 'focus', 'disabled', 'error')
    out['field_text'] = (None, spec_r('horizontal', 'Champ texte', (3, 3, 3, 3), (12, 16), (4, 2, 4, 2),
                                    states={s: field_img(24, 16, s) for s in sts},
                                    tokens={'text': 'ink', 'placeholder': 'text_muted', 'caret': 'ink'}))
    out['field_numeric'] = (None, spec_r('horizontal', 'Champ numérique (+/-)', (3, 3, 13, 3), (24, 16), (4, 2, 14, 2),
                                       states={s: field_img(32, 16, s, stepper=True) for s in sts},
                                       tokens={'text': 'ink'}, stepper_hit_areas={'up': [-11, 1, 10, 7], 'down': [-11, 8, 10, 7]},
                                       note="Zones des fleches relatives au bord droit. Le texte est aligne a droite."))
    out['field_search'] = (None, spec_r('horizontal', 'Champ de recherche', (13, 3, 3, 3), (24, 16), (14, 2, 4, 2),
                                      states={s: field_img(32, 16, s, left_icon=True) for s in sts},
                                      tokens={'text': 'ink', 'placeholder': 'text_muted'}))
    ta = {}
    for s in sts:
        im = field_img(24, 24, s)
        glyph(im, 18, 18, GRIP, tok('paper_4') if s != 'disabled' else tok('disabled_line'))
        ta[s] = im
    out['field_multiline'] = (None, spec('Zone multiligne', (3, 3, 7, 7), (16, 24), (4, 3, 4, 3), states=ta,
                                         tokens={'text': 'ink'}, line_height=12))
    out['dropdown'] = (None, spec_r('horizontal', 'Liste déroulante (champ)', (3, 3, 14, 3), (28, 16), (4, 2, 15, 2),
                                  states={s: field_img(36, 16, s, caret=True) for s in ('normal', 'hover', 'open', 'disabled')},
                                  tokens={'text': 'ink'}))
    lst = box(16, 16, tok('paper'), hi=tok('paper_hi'), lo=tok('paper_3'), r=1, shadow=2)
    out['dropdown_list'] = (lst, spec('Liste déroulante (volet)', (3, 3, 3, 5), (8, 10), (2, 2, 2, 4)))
    return out


def menu():
    out = {}
    w, h = 16, 14
    states = {}
    for st in ('normal', 'hover', 'disabled', 'checked'):
        img = canvas(w, h)
        if st == 'hover':
            fill(img, 0, 0, w - 1, h - 1, tok('teal'))
        if st == 'checked':
            glyph(img, 3, 4, CHECK, tok('teal'))
        states[st] = img
    out['menu_item'] = (states['normal'], spec_r('horizontal', 'Élément de menu', (12, 1, 2, 1), (16, 14), (13, 1, 4, 1), states=states,
                                               tokens={'text': 'ink', 'text_hover': 'text_on_accent', 'text_disabled': 'text_disabled'},
                                               note="Marge gauche 12 px reservee a la coche ; chevron de sous-menu = icone chevron_right."))
    pn = box(16, 16, tok('paper'), hi=tok('paper_hi'), lo=tok('paper_3'), r=1, shadow=2)
    out['menu_panel'] = (pn, spec('Menu contextuel (panneau)', (3, 3, 3, 5), (16, 10), (2, 2, 2, 4)))
    sp = canvas(12, 5)
    hline(sp, 0, 11, 2, tok('paper_3'))
    out['menu_separator'] = (sp, spec_r('horizontal', 'Séparateur de menu', (0, 0, 0, 0), (4, 5), (0, 0, 0, 0), stretch='tile'))
    return out


# ---------------------------------------------------------------------------
# Cases, radios, interrupteurs, curseurs, defilement
# ---------------------------------------------------------------------------
def checkbox():
    states = {}
    for val in ('off', 'on', 'mixed'):
        for st in ('normal', 'hover', 'disabled'):
            img = canvas(12, 12)
            on = val != 'off'
            dis = st == 'disabled'
            bg = tok('teal') if on and not dis else (tok('disabled_bg') if dis else tok('paper'))
            rfill(img, 1, 1, 10, 10, bg, 1)
            if not on and not dis:
                hline(img, 2, 9, 2, tok('paper_3')); vline(img, 2, 2, 9, tok('paper_3'))
            border = INK if st == 'normal' else (tok('teal_lo') if st == 'hover' else tok('disabled_line'))
            rrect(img, 1, 1, 10, 10, border, 1)
            mark = tok('text_on_accent') if not dis else tok('disabled_line')
            if val == 'on':
                glyph(img, 3, 3, CHECK, mark)
            if val == 'mixed':
                hline(img, 3, 8, 5, mark); hline(img, 3, 8, 6, mark)
            states[f'{val}_{st}'] = img
    return states['off_normal'], spec('Case à cocher', (0, 0, 0, 0), (12, 12), (0, 0, 0, 0), states=states,
                                      label_offset=[15, 0])


def radio():
    states = {}
    yy, xx = np.mgrid[0:12, 0:12]
    d = (xx - 5.5) ** 2 + (yy - 5.5) ** 2
    disc = d <= 22
    inner = d <= 14
    for val in ('off', 'on'):
        for st in ('normal', 'hover', 'disabled'):
            img = canvas(12, 12)
            dis = st == 'disabled'
            img[disc] = tok('paper') if not dis else tok('disabled_bg')
            if val == 'off' and not dis:
                img[disc & (yy < 4)] = tok('paper_3') if False else img[disc & (yy < 4)]
            border = INK if st == 'normal' else (tok('teal_lo') if st == 'hover' else tok('disabled_line'))
            img[disc & ~inner] = border
            if val == 'on':
                img[d <= 5.5] = tok('teal') if not dis else tok('disabled_line')
            states[f'{val}_{st}'] = img
    return states['off_normal'], spec('Bouton radio', (0, 0, 0, 0), (12, 12), (0, 0, 0, 0), states=states,
                                      label_offset=[15, 0])


def switch():
    states = {}
    for val in ('off', 'on'):
        for st in ('normal', 'hover', 'disabled'):
            img = canvas(22, 12)
            dis = st == 'disabled'
            track = (tok('teal') if val == 'on' else tok('paper_3')) if not dis else tok('disabled_bg')
            rfill(img, 0, 1, 21, 10, track, 2)
            rrect(img, 0, 1, 21, 10, INK if not dis else tok('disabled_line'), 2)
            kx = 12 if val == 'on' else 1
            knob = tok('paper') if st != 'hover' else tok('paper_hi')
            rfill(img, kx, 0, kx + 8, 11, knob if not dis else tok('paper_2'), 2)
            rrect(img, kx, 0, kx + 8, 11, INK if not dis else tok('disabled_line'), 2)
            hline(img, kx + 2, kx + 6, 9, tok('paper_3'))
            states[f'{val}_{st}'] = img
    return states['off_normal'], spec('Interrupteur', (0, 0, 0, 0), (22, 12), (0, 0, 0, 0), states=states,
                                      label_offset=[25, 0])


def slider():
    out = {}
    tr = canvas(12, 6)
    rfill(tr, 0, 0, 11, 5, tok('paper_3'), 1)
    hline(tr, 1, 10, 1, tok('paper_4'))
    rrect(tr, 0, 0, 11, 5, INK, 1)
    out['slider_track'] = (tr, spec_r('horizontal', 'Glissière (piste)', (3, 0, 3, 0), (8, 6), (0, 0, 0, 0)))
    fl = canvas(12, 6)
    rfill(fl, 0, 0, 11, 5, tok('teal'), 1)
    hline(fl, 1, 10, 1, tok('teal_hi'))
    rrect(fl, 0, 0, 11, 5, INK, 1)
    out['slider_fill'] = (fl, spec_r('horizontal', 'Glissière (remplissage)', (3, 0, 3, 0), (6, 6), (0, 0, 0, 0)))
    states = {}
    for st, c in (('normal', 'paper'), ('hover', 'paper_hi'), ('pressed', 'paper_3'), ('disabled', 'disabled_bg')):
        img = canvas(10, 14)
        rfill(img, 0, 0, 9, 13, tok(c), 1)
        if st != 'disabled':
            hline(img, 1, 8, 1, tok('paper_hi'))
            hline(img, 1, 8, 12, tok('paper_3') if st != 'pressed' else tok('paper_4'))
        vline(img, 3, 4, 9, tok('paper_4')); vline(img, 6, 4, 9, tok('paper_4'))
        rrect(img, 0, 0, 9, 13, INK if st != 'disabled' else tok('disabled_line'), 1)
        states[st] = img
    out['slider_knob'] = (states['normal'], spec('Glissière (bouton)', (0, 0, 0, 0), (10, 14), (0, 0, 0, 0), states=states,
                                                 anchor=[5, 7], note="Centrer le bouton sur la valeur ; piste centree verticalement."))
    return out


def scrollbars():
    out = {}
    for ori in ('v', 'h'):
        w, h = (10, 16) if ori == 'v' else (16, 10)
        tr = canvas(w, h)
        rfill(tr, 0, 0, w - 1, h - 1, tok('paper_3'), 1)
        if ori == 'v':
            vline(tr, 1, 1, h - 2, tok('paper_4'))
        else:
            hline(tr, 1, w - 2, 1, tok('paper_4'))
        rrect(tr, 0, 0, w - 1, h - 1, tok('paper_4'), 1)
        out[f'scroll_track_{ori}'] = (tr, spec(f'Barre de défilement ({ori}) piste',
                                               (0, 3, 0, 3) if ori == 'v' else (3, 0, 3, 0), (w, h) if ori == 'v' else (8, h),
                                               (0, 0, 0, 0)))
        states = {}
        for st, c in (('normal', 'paper'), ('hover', 'paper_hi'), ('pressed', 'teal_light')):
            img = canvas(w, h)
            rfill(img, 0, 0, w - 1, h - 1, tok(c), 1)
            if ori == 'v':
                vline(img, 1, 1, h - 2, tok('paper_hi')); vline(img, w - 2, 2, h - 2, tok('paper_3'))
            else:
                hline(img, 1, w - 2, 1, tok('paper_hi')); hline(img, 2, w - 2, h - 2, tok('paper_3'))
            rrect(img, 0, 0, w - 1, h - 1, INK, 1)
            states[st] = img
        out[f'scroll_thumb_{ori}'] = (states['normal'], spec(f'Barre de défilement ({ori}) curseur',
                                                             (0, 4, 0, 4) if ori == 'v' else (4, 0, 4, 0),
                                                             (w, 10) if ori == 'v' else (10, h), (0, 0, 0, 0), states=states,
                                                             resize='vertical' if ori == 'v' else 'horizontal',
                                                             note="Dessiner scroll_grip_%s centre sur le curseur." % ori))
        grip = canvas(6, 5) if ori == 'v' else canvas(5, 6)
        for k in (0, 2, 4):
            if ori == 'v':
                hline(grip, 0, 5, k, tok('paper_4'))
            else:
                vline(grip, k, 0, 5, tok('paper_4'))
        out[f'scroll_grip_{ori}'] = (grip, spec(f'Prise du curseur de defilement ({ori})', (0, 0, 0, 0),
                                                list(grip.shape[1::-1]), (0, 0, 0, 0), resize='none',
                                                note='Centree sur le curseur, jamais etiree.'))
    for dname, rows in (('up', CARET_UP), ('down', CARET), ('left', CARET_LEFT), ('right', CARET_RIGHT)):
        states = {}
        for st, c in (('normal', 'paper'), ('hover', 'paper_hi'), ('pressed', 'paper_3'), ('disabled', 'disabled_bg')):
            img = canvas(10, 10)
            rfill(img, 0, 0, 9, 9, tok(c), 1)
            rrect(img, 0, 0, 9, 9, INK if st != 'disabled' else tok('disabled_line'), 1)
            fg = tok('ink') if st != 'disabled' else tok('disabled_line')
            gw, gh = len(rows[0]), len(rows)
            glyph(img, (10 - gw) // 2, (10 - gh) // 2 + (1 if st == 'pressed' else 0), rows, fg)
            states[st] = img
        out[f'scroll_arrow_{dname}'] = (states['normal'], spec(f'Flèche de defilement {dname}', (0, 0, 0, 0), (10, 10),
                                                                (0, 0, 0, 0), states=states))
    return out


def progress():
    out = {}
    tr = canvas(16, 8)
    rfill(tr, 0, 0, 15, 7, tok('paper_3'), 1)
    hline(tr, 1, 14, 1, tok('paper_4'))
    rrect(tr, 0, 0, 15, 7, INK, 1)
    out['progress_track'] = (tr, spec_r('horizontal', 'Barre de progression (piste)', (3, 3, 3, 3), (8, 8), (1, 1, 1, 1),
                                      note="Le remplissage se dessine dans la zone interieure (1 px) a la largeur = valeur."))
    for st, c, hi, lo in (('default', 'teal', 'teal_hi', 'teal_lo'), ('success', 'success', 'success_hi', 'success_lo'),
                          ('warning', 'sun', 'sun_hi', 'sun_lo'), ('danger', 'danger', 'danger_hi', 'danger_lo')):
        f = canvas(8, 6)
        fill(f, 0, 0, 7, 5, tok(c))
        hline(f, 0, 7, 0, tok(hi))
        hline(f, 0, 7, 5, tok(lo))
        out[f'progress_fill_{st}'] = (f, spec_r('horizontal', f'Remplissage {st}', (1, 1, 1, 1), (2, 6), (0, 0, 0, 0)))
    st = canvas(8, 6)
    for x in range(8):
        for y in range(6):
            st[y, x] = tok('teal') if ((x + y) // 2) % 2 == 0 else tok('teal_hi')
    out['progress_fill_striped'] = (st, spec('Remplissage en cours (hachures, a faire defiler par code)', (0, 0, 0, 0),
                                             (2, 6), (0, 0, 0, 0), stretch='tile'))
    return out


def gauge():
    """Jauge semi-circulaire a 10 segments : base vide, remplissage colore, carte d'index."""
    W, H = 34, 20
    base = canvas(W, H); full = canvas(W, H); idx = canvas(W, H)
    cx, cy = 16.5, 17.5
    yy, xx = np.mgrid[0:H, 0:W]
    dx = xx - cx; dy = yy - cy
    d = np.sqrt(dx * dx + dy * dy)
    ang = np.degrees(np.arctan2(-dy, -dx))  # 0 = gauche, 180 = droite
    ring = (d >= 10.5) & (d <= 15.5) & (dy <= 0.5)
    seg = np.clip((ang / 18.0).astype(int), 0, 9)
    gap = (np.abs((ang % 18.0) - 0.0) < 2.4) & (ang > 1) & (ang < 179)
    body = ring & ~gap
    zones = [tok('success')] * 5 + [tok('sun')] * 3 + [tok('danger')] * 2
    for k in range(10):
        m = body & (seg == k)
        base[m] = tok('paper_3')
        full[m] = zones[k]
        idx[m] = (k + 1, 0, 0, 255)
    edge = body & ~(np.roll(body, 1, 0) & np.roll(body, -1, 0) & np.roll(body, 1, 1) & np.roll(body, -1, 1))
    base[edge] = INK; full[edge] = INK
    hub = d <= 2.2
    base[hub] = INK; full[hub] = INK
    return {'gauge_base': (base, spec('Jauge (base vide)', (0, 0, 0, 0), (W, H), (0, 0, 0, 0))),
            'gauge_fill': (full, spec('Jauge (segments pleins)', (0, 0, 0, 0), (W, H), (0, 0, 0, 0))),
            'gauge_segment_map': (idx, spec("Jauge : carte d'index (R = segment 1..10)", (0, 0, 0, 0), (W, H), (0, 0, 0, 0),
                                            note="Afficher gauge_fill la ou R <= N (N = valeur*10), gauge_base ailleurs."))}


def pickers():
    out = {}
    states = {}
    for st in ('normal', 'hover', 'selected'):
        img = canvas(18, 18)
        rrect(img, 2, 2, 15, 15, INK, 1)
        rect(img, 3, 3, 14, 14, tok('paper_hi'))
        if st == 'hover':
            rrect(img, 1, 1, 16, 16, tok('teal_hi'), 1)
        if st == 'selected':
            rrect(img, 1, 1, 16, 16, tok('sun'), 1)
            rrect(img, 0, 0, 17, 17, INK, 2)
        states[st] = img
    out['swatch_cell'] = (states['normal'], spec('Pastille de couleur', (0, 0, 0, 0), (18, 18), (0, 0, 0, 0), states=states,
                                                 fill_rect=[4, 4, 10, 10],
                                                 note="Le jeu peint la couleur dans fill_rect (10x10) sous le cadre."))
    states = {}
    for st in ('normal', 'hover', 'selected'):
        img = canvas(26, 26)
        rrect(img, 2, 2, 23, 23, INK, 1)
        rect(img, 3, 3, 22, 22, tok('paper_hi'))
        if st == 'hover':
            rrect(img, 1, 1, 24, 24, tok('teal_hi'), 1)
        if st == 'selected':
            rrect(img, 1, 1, 24, 24, tok('sun'), 1)
            rrect(img, 0, 0, 25, 25, INK, 2)
        states[st] = img
    out['pattern_cell'] = (states['normal'], spec('Pastille de motif', (0, 0, 0, 0), (26, 26), (0, 0, 0, 0), states=states,
                                                  fill_rect=[4, 4, 18, 18],
                                                  note="Vignette 18x18 du motif mural ou du sol (rendue par le jeu)."))
    return out


def charts():
    out = {}
    fr = canvas(24, 24)
    rfill(fr, 0, 0, 23, 23, tok('paper'), 1)
    rrect(fr, 0, 0, 23, 23, tok('paper_4'), 1)
    out['chart_frame'] = (fr, spec('Cadre de graphique', (3, 3, 3, 3), (12, 12), (8, 4, 4, 10),
                                   note="padding gauche/bas reserve aux graduations dessinees par le jeu."))
    gh = canvas(4, 1)
    gh[0, 0] = tok('paper_3'); gh[0, 1] = tok('paper_3')
    out['chart_grid_h'] = (gh, spec_r('horizontal', 'Ligne de grille horizontale (pointillee)', (0, 0, 0, 0), (4, 1), (0, 0, 0, 0), stretch='tile'))
    gv = canvas(1, 4)
    gv[0, 0] = tok('paper_3'); gv[1, 0] = tok('paper_3')
    out['chart_grid_v'] = (gv, spec_r('vertical', 'Ligne de grille verticale (pointillee)', (0, 0, 0, 0), (1, 4), (0, 0, 0, 0), stretch='tile'))
    ax = canvas(4, 2)
    fill(ax, 0, 0, 3, 0, tok('ink')); fill(ax, 0, 1, 3, 1, tok('paper_4'))
    out['chart_axis_h'] = (ax, spec_r('horizontal', 'Axe horizontal', (0, 0, 0, 0), (4, 2), (0, 0, 0, 0), stretch='tile'))
    bar = canvas(8, 8)
    fill(bar, 0, 0, 7, 7, (255, 255, 255, 255))
    hline(bar, 1, 6, 1, (255, 255, 255, 255))
    rect(bar, 0, 0, 7, 7, INK)
    fill(bar, 1, 6, 6, 6, (200, 200, 200, 255))
    out['chart_bar'] = (bar, spec('Barre de graphique (teinte par multiplication)', (2, 2, 2, 2), (4, 4), (0, 0, 0, 0),
                                  tint='multiply', note="Hauteur = donnee reelle ; couleurs : teal, coral, sun, sky, leaf."))
    pt = canvas(5, 5)
    glyph(pt, 0, 0, ['.###.', '#WWW#', '#WWW#', '#WWW#', '.###.'], INK)
    for (x, y) in [(1, 1), (2, 1), (3, 1), (1, 2), (2, 2), (3, 2), (1, 3), (2, 3), (3, 3)]:
        put(pt, x, y, (255, 255, 255, 255))
    out['chart_point'] = (pt, spec('Point de courbe (teinte par multiplication)', (0, 0, 0, 0), (5, 5), (0, 0, 0, 0),
                                   tint='multiply', anchor=[2, 2]))
    lg = canvas(8, 8)
    fill(lg, 0, 0, 7, 7, (255, 255, 255, 255)); rect(lg, 0, 0, 7, 7, INK)
    out['chart_legend'] = (lg, spec('Pastille de legende (teinte par multiplication)', (0, 0, 0, 0), (8, 8), (0, 0, 0, 0),
                                    tint='multiply'))
    return out


def portrait_frame():
    states = {}
    for st in ('normal', 'selected'):
        img = canvas(38, 38)
        c = INK if st == 'normal' else tok('sun')
        rfill(img, 0, 0, 37, 37, tok('paper_2'), 2)
        rrect(img, 0, 0, 37, 37, c, 2)
        if st == 'selected':
            rrect(img, 1, 1, 36, 36, tok('sun'), 1)
        rect(img, 2, 2, 35, 35, tok('paper_4'))
        fill(img, 3, 3, 34, 34, (0, 0, 0, 0))
        states[st] = img
    return states['normal'], spec('Cadre de portrait', (0, 0, 0, 0), (38, 38), (3, 3, 3, 3), states=states,
                                  window_rect=[3, 3, 32, 32],
                                  note="Fenêtre transparente 32x32 : dessiner le portrait AVANT le cadre (ou dessous).")


def topbar():
    out = {}
    bg = canvas(16, 22)
    fill(bg, 0, 0, 15, 19, tok('navy'))
    hline(bg, 0, 15, 0, tok('navy_2'))
    hline(bg, 0, 15, 19, tok('navy_0'))
    hline(bg, 0, 15, 20, (43, 33, 40, 110)); hline(bg, 0, 15, 21, (43, 33, 40, 50))
    out['topbar_bg'] = (bg, spec_r('horizontal', 'Barre supérieure (fond)', (2, 2, 2, 4), (8, 22), (4, 4, 4, 6), tokens={'text': 'text_on_dark'}))
    dv = canvas(3, 20)
    vline(dv, 0, 3, 16, tok('navy_0')); vline(dv, 1, 3, 16, tok('navy_2'))
    out['topbar_divider'] = (dv, spec_r('vertical', 'Barre supérieure (séparateur)', (0, 3, 0, 3), (3, 8), (0, 0, 0, 0)))
    seg = canvas(16, 16)
    rfill(seg, 0, 0, 15, 15, tok('navy_2'), 2)
    hline(seg, 2, 13, 1, tok('navy_3'))
    rrect(seg, 0, 0, 15, 15, tok('navy_0'), 2)
    out['topbar_segment'] = (seg, spec('Barre supérieure (groupe)', (4, 4, 4, 4), (8, 16), (5, 2, 5, 2), tokens={'text': 'text_on_dark'}))
    chip = canvas(22, 14)
    rfill(chip, 0, 0, 21, 13, tok('sun'), 2)
    hline(chip, 2, 19, 1, tok('sun_hi'))
    hline(chip, 2, 19, 12, tok('sun_lo'))
    rrect(chip, 0, 0, 21, 13, INK, 2)
    cm = {'#': INK, 'Y': tok('sun_hi'), 'S': tok('sun'), 's': tok('sun_lo')}
    for j, r in enumerate(COIN):
        for i, c in enumerate(r):
            if c in cm:
                put(chip, 3 + i, 4 + j, cm[c])
    out['money_chip'] = (chip, spec_r('horizontal', 'Puce trésorerie', (10, 4, 4, 4), (16, 14), (11, 1, 5, 1), tokens={'text': 'ink'},
                                    note="Piece dessinee dans la marge gauche ; montant (ex. 12 450 CHF) rendu par le jeu."))
    dchip = canvas(16, 14)
    rfill(dchip, 0, 0, 15, 13, tok('navy_2'), 2)
    hline(dchip, 2, 13, 1, tok('navy_3'))
    rrect(dchip, 0, 0, 15, 13, tok('navy_0'), 2)
    out['chip_dark'] = (dchip, spec_r('horizontal', 'Puce sombre (date, statut)', (4, 4, 4, 4), (10, 14), (5, 1, 5, 1), tokens={'text': 'text_on_dark'}))
    return out


def steppers():
    out = {}
    for dname, rows in (('up', CARET_UP), ('down', CARET)):
        states = {}
        for st, c in (('normal', 'paper_2'), ('hover', 'paper_hi'), ('pressed', 'paper_3'), ('disabled', 'disabled_bg')):
            img = canvas(10, 7)
            fill(img, 0, 0, 9, 6, tok(c))
            glyph(img, 2, 2, rows, tok('ink') if st != 'disabled' else tok('disabled_line'))
            states[st] = img
        out[f'stepper_{dname}'] = (states['normal'], spec(f'Flèche de champ numerique ({dname})', (0, 0, 0, 0), (10, 7),
                                                          (0, 0, 0, 0), states=states))
    return out


# ---------------------------------------------------------------------------
# Catalogue complet
# ---------------------------------------------------------------------------
def build_all():
    """Retourne {id: (image_base_ou_None, spec)} pour tout le kit HUD."""
    items = {}
    for fid, fn in (('panel_main', panel_main), ('panel_secondary', panel_secondary), ('panel_inset', panel_inset),
                    ('titlebar', titlebar), ('banner', banner), ('popover', popover), ('tooltip', tooltip),
                    ('notification', notification), ('toast', toast), ('card', card), ('list_row', list_row),
                    ('drag_handle', drag_handle), ('section_header', section_header), ('button_text', button_text),
                    ('tabs', tabs), ('checkbox', checkbox), ('radio', radio), ('switch', switch),
                    ('portrait_frame', portrait_frame)):
        items[fid] = fn()
    items['window'] = window_frame(False)
    items['modal'] = window_frame(True)
    for kind in ('primary', 'secondary', 'destructive'):
        items[f'button_{kind}'] = button(kind)
    items['button_icon'] = button_icon(18)
    items['button_icon_small'] = button_icon(12, small=True)
    (sh_img, sh_spec), (sv_img, sv_spec) = separators()
    items['separator_h'] = (sh_img, sh_spec)
    items['separator_v'] = (sv_img, sv_spec)
    for piece, v in segmented().items():
        items[f'segmented_{piece}'] = v
    for k, v in badges().items():
        items[f'badge_{k}'] = v
    for group in (fields(), menu(), slider(), scrollbars(), progress(), gauge(), pickers(), charts(), topbar(), steppers()):
        items.update(group)
    return items
