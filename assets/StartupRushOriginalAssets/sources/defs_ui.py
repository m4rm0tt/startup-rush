"""Lot UI : polices, icones, curseurs, effets, HUD, terminal, composants de projet.

Construit par :  python tools/build_all.py --modules defs_ui
Ecrit assets/{fonts,hud,cursors,vfx,terminal,project_components}/ et des planches
previews/ui_*.png. Les ecrans de reference sont produits par tools/ui_screens.py.
Toutes les sources sont dans sources/ui/ (glyphes, icones, curseurs, peintres HUD).
"""
import json
import os

import numpy as np

from srgen.registry import custom_builder, PACK_VERSION
from srgen.imgio import save_png
from srgen.lot_ui_core import canvas, put, tok, scale, fonts
from srgen import lot_ui_font as LF
from srgen.lot_ui_icons import load_icon_sets, validate, icon_rgba, sheet as icon_sheet
from srgen.lot_ui_atlas import pack
from srgen.lot_ui_preview import hud_sheet, nine_test

from ui import hud, terminal_kit, project_components as PC
from ui.cursors_vfx import CURSORS, VFX, COL

ATLAS_MAX = 1024


def _entry(iid, category, sub, name, kind, **kw):
    e = {'id': iid, 'category': category, 'subcategory': sub, 'name_fr': name, 'pack_version': PACK_VERSION,
         'usage': 'shared', 'kind': kind}
    e.update(kw)
    return e


def _ascii_rgba(rows):
    h, w = len(rows), max(len(r) for r in rows)
    img = np.zeros((h, w, 4), np.uint8)
    for j, r in enumerate(rows):
        for i, c in enumerate(r):
            if c in COL:
                img[j, i] = COL[c]
    return img


def _diamond(fill_c=None, line_c=None, line_w=1, dashed=False, hatch=None):
    """Losange 64x32 (meme regle de pixels que les cases de sol), ancre (32, 0)."""
    w, h = 64, 32
    xs = np.arange(w) + 0.5 - 32
    ys = np.arange(h) + 0.5
    SX, SY = np.meshgrid(xs, ys)
    u = SX / 2 + SY; v = SY - SX / 2
    inside = (u >= 0) & (u < 32) & (v >= 0) & (v < 32)
    img = np.zeros((h, w, 4), np.uint8)
    if fill_c is not None:
        img[inside] = fill_c
    if hatch is not None:
        m = inside & ((np.floor(u + v).astype(int) % 6) < 2)
        img[m] = hatch
    if line_c is not None:
        edge = inside & ((u < line_w) | (u >= 32 - line_w) | (v < line_w) | (v >= 32 - line_w))
        if dashed:
            edge &= (np.floor(np.where((u < line_w) | (u >= 32 - line_w), v, u)).astype(int) % 6) < 3
        img[edge] = line_c
    return img


def _ring(w, h, color, thick=1):
    """Ellipse de 1 px continue : pixels du disque ayant un voisin (4-connexe) hors du disque."""
    yy, xx = np.mgrid[0:h, 0:w]
    cx, cy = (w - 1) / 2.0, (h - 1) / 2.0
    inside = (((xx - cx) / (w / 2.0)) ** 2 + ((yy - cy) / (h / 2.0)) ** 2) <= 1.0
    pad = np.pad(inside, 1)
    allin = pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:]
    img = np.zeros((h, w, 4), np.uint8)
    img[inside & ~allin] = color
    return img


@custom_builder('ui', order=90)
def build_ui(root, log=None):
    say = log or (lambda s: None)
    entries = []
    atlas_items = []          # (cle, image) pour l'atlas d'interface
    file_of = {}              # cle -> chemin relatif

    def write(rel, img, atlas=True):
        save_png(os.path.join(root, rel), img)
        if atlas:
            atlas_items.append((rel, img))
        file_of[rel] = rel
        return rel

    # ------------------------------------------------------------------ polices
    fdir = 'assets/fonts'
    font_info = {}
    for mono in (False, True):
        base = 'StartupRushMono-Regular' if mono else 'StartupRushPixel-Regular'
        ttf = f'{fdir}/{base}.ttf'
        info = LF.build_ttf(os.path.join(root, ttf), mono)
        rep = LF.verify_ttf(os.path.join(root, ttf), mono)
        tag = 'startuprush_mono' if mono else 'startuprush_pixel'
        apng, ajson = f'{fdir}/{tag}_atlas.png', f'{fdir}/{tag}_atlas.json'
        LF.build_atlas(os.path.join(root, apng), os.path.join(root, ajson), mono)
        font_info[tag] = (info, rep)
        mism = sum(c['pixel_mismatch'] for c in rep['checks'])
        gray = sum(c['gray_pixels'] for c in rep['checks'])
        entries.append(_entry(f'font_{tag}', 'fonts', 'font', info['family'], 'font',
                              files={'ttf': ttf, 'bitmap_atlas': apng, 'bitmap_metrics': ajson},
                              family=info['family'], ps_name=info['ps_name'], glyph_count=info['glyph_count'],
                              metrics={'units_per_em': LF.UPM, 'px_units': LF.PX, 'line_height_px': 12, 'ascent_px': 10,
                                       'descent_px': 2, 'cap_height_px': 7, 'x_height_px': 5,
                                       'advance': 'fixe 6 px' if mono else 'proportionnelle (chiffres tabulaires 5 px)'},
                              crisp_sizes_px=[12, 24, 36, 48],
                              verification={'reloaded_with_fonttools': True, 'missing_chars': rep['missing_chars'],
                                            'pixel_mismatch_vs_reference_12_24px': mism, 'gray_pixels': gray,
                                            'renderer': 'Pillow/FreeType'},
                              coverage='ASCII 32-126, accents francais (majuscules et minuscules), oe/OE, ae/AE, euro, livre, '
                                       'degre, guillemets francais et typographiques, points de suspension, tirets, '
                                       'operateurs (x, /, +-, <=, >=, !=), espaces insecables ; CHF = lettres.',
                              avalonia='FontFamily="avares://<Assembly>/Assets/Fonts#' + info['family'] +
                                       '" ; tailles 12/24/36 px ; RenderOptions.TextRenderingMode="Alias" ; positions entieres.',
                              source='sources/ui/font_glyphs.py (glyphes dessines au pixel) ; tools/srgen/lot_ui_font.py'))
        say(f'  police {info["family"]} : ecarts={mism} gris={gray}')

    # ------------------------------------------------------------------ icones
    icons = load_icon_sets()
    errs = validate(icons)
    assert not errs, errs
    idir = 'assets/hud/icons'
    icon_imgs = []
    for iid, (label, rows) in icons.items():
        img = icon_rgba(rows)
        rel = f'{idir}/icon_{iid}.png'
        save_png(os.path.join(root, rel), img)
        icon_imgs.append((iid, img))
    pages, rects = pack([(k, v) for k, v in icon_imgs], max_w=256, pad=1)
    assert len(pages) == 1
    save_png(os.path.join(root, 'assets/hud/icons_atlas.png'), pages[0])
    ij = {'size': 16, 'tint': 'multiplication par la couleur voulue (icones blanches)', 'atlas': 'assets/hud/icons_atlas.png',
          'icons': {k: {'rect': list(r[1:]), 'label_fr': icons[k][0]} for k, r in rects.items()}}
    json.dump(ij, open(os.path.join(root, 'assets/hud/icons_atlas.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    for iid, (label, rows) in icons.items():
        entries.append(_entry(f'icon_{iid}', 'hud', 'icon', label, 'icon',
                              file=f'{idir}/icon_{iid}.png', size=[16, 16],
                              atlas={'page': 'assets/hud/icons_atlas.png', 'rect': list(rects[iid][1:])},
                              tint='multiply', semantic_colors=['ink', 'text_on_dark', 'teal', 'success', 'warning',
                                                                'danger', 'info', 'text_disabled']))
    say(f'  {len(icons)} icones')

    # ------------------------------------------------------------------ curseurs
    cdir = 'assets/cursors'
    for cid, (label, hot, rows) in CURSORS.items():
        img = _ascii_rgba(rows)
        r1 = write(f'{cdir}/cursor_{cid}.png', img)
        r2 = write(f'{cdir}/cursor_{cid}__x2.png', scale(img, 2), atlas=False)
        entries.append(_entry(f'cursor_{cid}', 'cursors', 'cursor', label, 'cursor',
                              file=r1, size=[16, 16], hotspot=list(hot),
                              technical_exports={'x2': {'file': r2, 'size': [32, 32], 'hotspot': [hot[0] * 2, hot[1] * 2],
                                                        'note': 'export technique x2 (meme dessin agrandi) pour curseur systeme'}},
                              source='sources/ui/cursors_vfx.py'))

    # ------------------------------------------------------------------ effets
    vdir = 'assets/vfx'
    anim = {'sparkle_small': 'echelle 0,6->1->0 en 0,5 s, legere rotation interdite (pixel art) : clignoter',
            'sparkle_large': 'apparition echelle x1 puis fondu 0,6 s',
            'idea': 'monter de 10 px en 0,9 s au-dessus de la tete/ecran, fondu les 0,3 dernieres s',
            'work': 'monter de 8 px, fondu ; repetition toutes les 2-4 s pendant le travail',
            'code': 'monter de 10 px depuis l ecran, fondu',
            'alert': 'pulsation d opacite 1/0,6 (0,8 s) jusqu a lecture',
            'success': 'monter de 12 px en 0,8 s, fondu final 0,3 s',
            'coin': 'monter de 14 px, fondu (gain de tresorerie)',
            'heart_morale': 'monter de 10 px, fondu'}
    for vid, (label, anchor, rows) in VFX.items():
        img = _ascii_rgba(rows)
        rel = write(f'{vdir}/vfx_{vid}.png', img)
        h, w = img.shape[:2]
        anc = [w // 2, h // 2] if anchor == 'centre' else [w // 2, h - 1]
        entries.append(_entry(f'vfx_{vid}', 'vfx', 'particle', label, 'vfx_sprite', file=rel, size=[w, h],
                              anchor=anc, animation_hint=anim.get(vid, 'monter + fondu'), static=True))
    tiles = {
        'tile_hover': ('Case survolee', _diamond(None, tok('paper_hi'), 1)),
        'tile_selected': ('Case selectionnee', _diamond(tok('teal', 70), tok('teal'), 2)),
        'tile_valid': ('Placement valide', _diamond(tok('success', 95), tok('success'), 1)),
        'tile_invalid': ('Placement invalide', _diamond(tok('danger', 90), tok('danger'), 1, hatch=tok('danger_lo', 140))),
        'tile_footprint': ('Emprise (pointilles)', _diamond(None, tok('sun'), 1, dashed=True)),
    }
    for vid, (label, img) in tiles.items():
        rel = write(f'{vdir}/vfx_{vid}.png', img)
        entries.append(_entry(f'vfx_{vid}', 'vfx', 'tile_overlay', label, 'vfx_tile', file=rel, size=[64, 32], anchor=[32, 0],
                              placement='ancre = coin nord de la case (comme les sols)',
                              animation_hint='opacite 1 <-> 0,7 (1 s) pour le survol ; fixe pour la selection'))
    ring = _ring(30, 15, tok('sun'), 1)
    rel = write(f'{vdir}/vfx_hover_ring.png', ring)
    entries.append(_entry('vfx_hover_ring', 'vfx', 'ring', 'Anneau de survol / selection', 'vfx_sprite', file=rel,
                          size=[30, 15], anchor=[15, 7], animation_hint='pulsation echelle 1 <-> 1,1 (pixel : alterner 2 tailles)'))
    ring2 = _ring(38, 19, tok('teal'), 1)
    rel = write(f'{vdir}/vfx_select_ring.png', ring2)
    entries.append(_entry('vfx_select_ring', 'vfx', 'ring', 'Anneau de selection (large)', 'vfx_sprite', file=rel,
                          size=[38, 19], anchor=[19, 9]))

    # ------------------------------------------------------------------ HUD
    hdir = 'assets/hud/components'
    hud_items = hud.build_all()
    for hid, (img, sp) in hud_items.items():
        e = _entry(f'hud_{hid}', 'hud', 'component', sp['name_fr'], 'ui_9slice' if sp['nine_slice'] != [0, 0, 0, 0] or
                   sp.get('stretch') == 'tile' else 'ui_sprite')
        for k in ('nine_slice', 'min_size', 'content_padding', 'stretch', 'resize', 'tokens', 'note', 'pressed_content_offset',
                  'icon_size', 'title_bar', 'header', 'scrim', 'stepper_hit_areas', 'line_height', 'label_offset', 'anchor',
                  'fill_rect', 'window_rect', 'tint'):
            if k in sp:
                e[k] = sp[k]
        if sp.get('states'):
            e['states'] = {}
            for st, im in sp['states'].items():
                rel = write(f'{hdir}/hud_{hid}__{st}.png', im)
                e['states'][st] = {'file': rel, 'size': [im.shape[1], im.shape[0]]}
            first = next(iter(sp['states'].values()))
            e['size'] = [first.shape[1], first.shape[0]]
        else:
            rel = write(f'{hdir}/hud_{hid}.png', img)
            e['file'] = rel
            e['size'] = [img.shape[1], img.shape[0]]
        if sp.get('extras'):
            e['extras'] = {}
            for xk, xim in sp['extras'].items():
                rel = write(f'{hdir}/hud_{xk}.png', xim)
                e['extras'][xk] = {'file': rel, 'size': [xim.shape[1], xim.shape[0]]}
        entries.append(e)
    # jetons du theme
    theme = {k: '#%02x%02x%02x' % v[:3] + ('' if len(v) < 4 or v[3] == 255 else '%02x' % v[3]) for k, v in
             __import__('srgen.lot_ui_core', fromlist=['TOK']).TOK.items()}
    json.dump({'theme': 'Papier', 'tokens': theme}, open(os.path.join(root, 'assets/hud/theme_tokens.json'), 'w',
                                                        encoding='utf-8'), ensure_ascii=False, indent=1)
    entries.append(_entry('hud_confirm_dialog', 'hud', 'layout', 'Dialogue de confirmation (composition)', 'ui_layout',
                          min_size=[220, 104], default_size=[260, 120],
                          composition=[
                              {'part': 'scrim', 'token': 'scrim', 'rect': 'ecran entier', 'note': 'sous le dialogue'},
                              {'part': 'hud_modal', 'rect': [0, 0, 'w', 'h'], 'title': 'texte dynamique (barre de titre)'},
                              {'part': 'icon', 'ids': ['icon_warning', 'icon_error', 'icon_help', 'icon_info'], 'rect': [8, 23, 16, 16],
                               'tint': 'warning | danger | info selon la gravite'},
                              {'part': 'message', 'rect': [30, 22, 'w-38', 'h-56'], 'font': 'font_startuprush_pixel',
                               'line_height': 13, 'wrap': True},
                              {'part': 'hud_button_secondary', 'label': 'Annuler', 'rect': ['w-196', 'h-28', 88, 18]},
                              {'part': 'hud_button_primary | hud_button_destructive', 'label': 'action', 'rect': ['w-100', 'h-28', 92, 18]},
                          ],
                          keyboard={'Entree': 'action', 'Echap': 'annuler'},
                          note="Composition de composants livres (pas d'image figee) ; voir previews/screens/13_*."))
    entries.append(_entry('hud_theme_papier', 'hud', 'theme', 'Theme Papier (jetons de couleur)', 'ui_theme',
                          file='assets/hud/theme_tokens.json', tokens=theme))
    say(f'  {len(hud_items)} composants HUD')

    # ------------------------------------------------------------------ terminal
    tdir = 'assets/terminal'
    for tid, (img, sp) in terminal_kit.build_all().items():
        e = _entry(tid, 'terminal', 'terminal', sp['name_fr'], 'ui_9slice' if sp['nine_slice'] != [0, 0, 0, 0] or
                   sp.get('stretch') == 'tile' else 'ui_sprite')
        for k in ('nine_slice', 'min_size', 'content_padding', 'stretch', 'resize', 'tokens', 'note', 'title_bar', 'cell'):
            if k in sp:
                e[k] = sp[k]
        if sp.get('states'):
            e['states'] = {}
            for st, im in sp['states'].items():
                rel = write(f'{tdir}/{tid}__{st}.png', im)
                e['states'][st] = {'file': rel, 'size': [im.shape[1], im.shape[0]]}
            e['size'] = list(next(iter(sp['states'].values())).shape[1::-1])
        else:
            rel = write(f'{tdir}/{tid}.png', img)
            e['file'] = rel
            e['size'] = [img.shape[1], img.shape[0]]
        e['font'] = 'font_startuprush_mono'
        entries.append(e)

    # ------------------------------------------------------------------ composants de projet
    pdir = 'assets/project_components'
    for cid, (name, group, icon) in PC.COMPONENTS.items():
        pv = PC.preview(cid)
        rel = write(f'{pdir}/comp_preview_{cid}.png', pv)
        shared = not icon.startswith('comp_')
        entries.append(_entry(f'comp_{cid}', 'project_components', group, name, 'project_component',
                              icon={'id': f'icon_{icon}', 'file': f'{idir}/icon_{icon}.png', 'shared_with_hud': shared},
                              preview={'file': rel, 'size': [PC.PW, PC.PH]},
                              card='hud_comp_card', dynamic_fields=['nom', 'etat', 'cout', 'qualite', 'exigences', 'dependances'],
                              note='Apercu abstrait (maquette) : aucune page reelle, aucun texte cuit.'))
    img, sp = PC.comp_card()
    e = _entry('comp_card', 'project_components', 'card', sp['name_fr'], 'ui_9slice',
               nine_slice=sp['nine_slice'], min_size=sp['min_size'], content_padding=sp['content_padding'],
               slots=sp['slots'], tokens=sp['tokens'], note=sp['note'], states={},
               state_colors={k: v for k, v in PC.CARD_STATES.items()})
    for st, im in sp['states'].items():
        rel = write(f'{pdir}/comp_card__{st}.png', im)
        e['states'][st] = {'file': rel, 'size': [im.shape[1], im.shape[0]]}
    for xk, xim in sp['extras'].items():
        rel = write(f'{pdir}/{xk}.png', xim)
        e['selection_overlay'] = {'file': rel, 'size': [xim.shape[1], xim.shape[0]], 'offset': [-1, -1],
                                  'nine_slice': sp.get('selection_nine_slice', [4, 4, 4, 4]),
                                  'note': 'cadre de selection 9-slice : taille = carte + 2 px, decalage (-1, -1)'}
    e['size'] = list(next(iter(sp['states'].values())).shape[1::-1])
    entries.append(e)
    for pid, (img, sp) in PC.ports().items():
        e = _entry(pid, 'project_components', 'port', sp['name_fr'], 'ui_sprite', anchor=sp.get('anchor'),
                   note=sp.get('note'), states={})
        for st, im in sp['states'].items():
            rel = write(f'{pdir}/{pid}__{st}.png', im)
            e['states'][st] = {'file': rel, 'size': [8, 8]}
        e['size'] = [8, 8]
        entries.append(e)
    for lid, (img, sp) in PC.links().items():
        rel = write(f'{pdir}/{lid}.png', img)
        e = _entry(lid, 'project_components', 'link', sp['name_fr'], 'ui_9slice' if sp.get('stretch') == 'tile' else 'ui_sprite',
                   file=rel, size=[img.shape[1], img.shape[0]], stretch=sp.get('stretch'), resize=sp.get('resize'),
                   tint='multiply', anchor=sp.get('anchor'), state_colors=PC.LINK_STATES)
        entries.append(e)

    # ------------------------------------------------------------------ atlas d'interface
    pages, rects = pack(atlas_items, max_w=ATLAS_MAX, pad=2)
    page_files = []
    for pi, page in enumerate(pages):
        rel = f'assets/hud/ui_atlas_{pi}.png'
        save_png(os.path.join(root, rel), page)
        page_files.append(rel)
    amap = {k: {'page': page_files[r[0]], 'rect': list(r[1:])} for k, r in rects.items()}
    json.dump({'sampling': 'plus proche voisin', 'padding_px': 2, 'extrusion_px': 1, 'pages': page_files, 'rects': amap},
              open(os.path.join(root, 'assets/hud/ui_atlas.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

    def attach(obj):
        if isinstance(obj, dict):
            if 'file' in obj and obj['file'] in amap and 'atlas' not in obj:
                obj['atlas'] = amap[obj['file']]
            for v in obj.values():
                attach(v)
        elif isinstance(obj, list):
            for v in obj:
                attach(v)
    for e in entries:
        attach(e)
    say(f'  atlas UI : {len(pages)} page(s), {len(rects)} images')

    # ------------------------------------------------------------------ planches de controle
    pv = os.path.join(root, 'previews')
    save_png(os.path.join(pv, 'ui_icons.png'), icon_sheet(icons, 'STARTUPRUSH - ICONES 16 PX (%d) - teintes : encre, sur marine, sarcelle, desactive' % len(icons), cols=10, k=2))
    keys = list(hud_items.keys())
    half = len(keys) // 2
    save_png(os.path.join(pv, 'ui_hud_components_a.png'), hud_sheet({k: hud_items[k] for k in keys[:half]}, 'HUD PAPIER - COMPOSANTS ET ETATS (1/2)', width=820))
    save_png(os.path.join(pv, 'ui_hud_components_b.png'), hud_sheet({k: hud_items[k] for k in keys[half:]}, 'HUD PAPIER - COMPOSANTS ET ETATS (2/2)', width=820))
    term = terminal_kit.build_all()
    save_png(os.path.join(pv, 'ui_terminal_kit.png'), hud_sheet(term, 'KIT TERMINAL (police mono, cellule 6x12)', width=820))
    ninelist = [k for k in ('panel_main', 'window', 'modal', 'card', 'button_primary', 'button_secondary', 'field_text',
                            'field_search', 'dropdown', 'notification', 'tooltip', 'popover', 'progress_track', 'banner',
                            'topbar_bg', 'money_chip', 'list_row', 'tabs', 'badge_success') if k in hud_items]
    save_png(os.path.join(pv, 'ui_nine_slice_test.png'), nine_test(hud_items, ninelist))
    _font_specimen(root)
    _cursor_vfx_sheet(root, entries)
    _components_sheet(root, icons)
    say('  planches ecrites')
    return entries


# ---------------------------------------------------------------------------
# Planches
# ---------------------------------------------------------------------------
def _font_specimen(root):
    from srgen.lot_ui_core import fill
    F, MO = fonts()
    lines = [
        ('StartupRush Pixel — police pixel originale (12 px)', F),
        ('Ça démarre : 12 450 CHF de trésorerie, 3 777 € de revenus, qualité 98 % !', F),
        ('Œuvre, cœur, élève, fenêtre, naïf, Noël, où, août, garçon, æ, Æ', F),
        ('ÉTÉ À Â Ä Ç È É Ê Ë Î Ï Ô Ö Ù Û Ü Ÿ '
         'à â ä ç è é ê ë î ï ô ö ù û ü ÿ', F),
        ('ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz', F),
        ('0123456789  1 111 111  +12,5 % - ± × ÷ ≤ ≥ ≠ ° £ €', F),
        ('« Projet Nébula » — version 1.0… ‘ok’ “prêt” (test) [beta] {api}', F),
        ('StartupRush Mono : > deploy --env local  [OK] 100%', MO),
        ('0123456789 abcdefghij ABCDEFGHIJ éèàçô € CHF', MO),
    ]
    W = 470
    img = canvas(W, 20 + len(lines) * 14 + 46, tok('paper'))
    y = 6
    for s, f in lines:
        f.draw(img, 6, y, s, tok('ink'))
        y += 14
    F.draw(img, 6, y + 2, 'x2 :', tok('text_muted'))
    F.draw(img, 30, y, 'Bureau à l’étage, 4 800 CHF', tok('teal'), k=2)
    save_png(os.path.join(root, 'previews', 'ui_font_specimen.png'), scale(img, 2))


def _cursor_vfx_sheet(root, entries):
    from srgen.lot_ui_core import fill, blit
    from srgen.imgio import load_png
    F, _ = fonts()
    items = [e for e in entries if e['category'] in ('cursors', 'vfx')]
    W = 640
    img = canvas(W, 260, tok('paper_2'))
    F.draw(img, 6, 4, 'CURSEURS (hotspot en rouge) ET EFFETS STATIQUES', tok('ink'))
    x, y = 6, 22
    for e in items:
        im = load_png(os.path.join(root, e['file']))
        h, w = im.shape[:2]
        cw = max(w, F.measure(e['id'][-14:])) + 10
        if x + cw > W - 6:
            x = 6; y += 58
        bg = tok('navy') if e['category'] == 'vfx' and e['subcategory'] != 'tile_overlay' else tok('paper')
        fill(img, x, y, x + cw - 4, y + max(h, 16) + 4, bg if e['category'] == 'vfx' else tok('paper_3'))
        if e['subcategory'] == 'tile_overlay':
            fill(img, x, y, x + cw - 4, y + h + 4, tok('paper_4'))
        blit(img, im, x + 2, y + 2)
        if 'hotspot' in e:
            hx, hy = e['hotspot']
            put(img, x + 2 + hx, y + 2 + hy, tok('danger'))
        F.draw(img, x, y + max(h, 16) + 6, e['id'].replace('cursor_', '').replace('vfx_', '')[:14], tok('ink'))
        x += cw
    save_png(os.path.join(root, 'previews', 'ui_cursors_vfx.png'), scale(img, 2))


def _components_sheet(root, icons):
    from srgen.lot_ui_core import fill, blit, tint
    F, _ = fonts()
    cols = 7
    cw, ch = 88, 66
    n = len(PC.COMPONENTS)
    rows = (n + cols - 1) // cols
    img = canvas(cols * cw + 10, rows * ch + 120, tok('paper_2'))
    F.draw(img, 6, 4, 'COMPOSANTS DE PROJET : icone 16 px + apercu abstrait 48x32 (texte et etat = donnees du jeu)', tok('ink'))
    for k, (cid, (name, group, icon)) in enumerate(PC.COMPONENTS.items()):
        x = 6 + (k % cols) * cw; y = 20 + (k // cols) * ch
        fill(img, x, y, x + cw - 4, y + ch - 4, tok('paper'))
        blit(img, PC.preview(cid), x + 18, y + 3)
        blit(img, tint(icon_rgba(icons[icon][1]), tok('teal')), x + 1, y + 3)
        F.draw(img, x + 2, y + 38, name[:15], tok('ink'))
        F.draw(img, x + 2, y + 50, group, tok('text_muted'))
    # cartes + ports + liens
    y0 = 20 + rows * ch + 4
    img2, sp = PC.comp_card()
    x = 6
    for st, im in sp['states'].items():
        blit(img, im, x, y0)
        F.draw(img, x + 4, y0 + 2, st, tok('ink'))
        x += im.shape[1] + 6
    for pid, (pimg, psp) in list(PC.ports().items())[:2]:
        for st, im in psp['states'].items():
            blit(img, im, x, y0 + 4)
            x += 12
    lk = PC.links()
    for i, (lid, (limg, lsp)) in enumerate(lk.items()):
        blit(img, tint(limg, tok(list(PC.LINK_STATES.values())[i % 4])) if False else limg, x + (i % 6) * 9, y0 + 20 + (i // 6) * 9)
    save_png(os.path.join(root, 'previews', 'ui_project_components.png'), scale(img, 2))
