"""Export des personnages : 12 bases chauves (body + head) x 4 orientations,
6 coiffures (hair_front / hair_back) x 4 orientations, masques, controles
automatiques d'alignement et planches de demonstration.

Toutes les couches partagent la toile 40x72 et l'ancre d'assise (20, 50) :
aucun decalage par base ni par orientation n'est necessaire (offset = 0, 0).
Ordre : hair_back -> body -> head -> hair_front.
"""
import os
import json
import numpy as np

import characters as C
from srgen.registry import custom_builder, PACK_VERSION
from srgen.imgio import save_png, over, upscale, flatten_on
from srgen.compose import recolor
from srgen.preview import draw_text
from srgen.palette import CHANNEL_NAMES

ORIENTS = ('se', 'sw', 'nw', 'ne')
LAYER_ORDER = ['hair_back', 'body', 'head', 'hair_front']
PORTRAIT_CROP = [4, 0, 32, 32]   # x, y, w, h sur la composition 'se' (ou 'sw')
DEFAULT_HAIR = 'hair_brown'


def _chans(mask):
    c = mask[..., 0][mask[..., 3] > 0]
    return [CHANNEL_NAMES[int(x)] for x in sorted(set(np.unique(c).tolist()))] if c.size else []


def _skull(orient):
    view, mirror = C.ORIENT_VIEW[orient]
    sk = C.SKULL[view]
    return sk[:, ::-1] if mirror else sk


def _components(mask):
    """Composantes 4-connexes (liste de masques booleens)."""
    h, w = mask.shape
    lab = np.zeros((h, w), np.int32)
    n = 0
    comps = []
    for y in range(h):
        for x in range(w):
            if mask[y, x] and lab[y, x] == 0:
                n += 1
                stack = [(y, x)]
                lab[y, x] = n
                pts = []
                while stack:
                    cy, cx = stack.pop()
                    pts.append((cy, cx))
                    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        ny, nx = cy + dy, cx + dx
                        if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and lab[ny, nx] == 0:
                            lab[ny, nx] = n
                            stack.append((ny, nx))
                comps.append(pts)
    return comps


def _dilate(m, r):
    out = m.copy()
    for _ in range(r):
        o = out.copy()
        o[1:] |= out[:-1]; o[:-1] |= out[1:]; o[:, 1:] |= out[:, :-1]; o[:, :-1] |= out[:, 1:]
        out = o
    return out


def hair_qa(style, orient, layers):
    """Controle : sommet du crane couvert (pas de scalp accidentel) et aucune meche detachee."""
    sk = _skull(orient)
    view, _ = C.ORIENT_VIEW[orient]
    hf = layers['hair_front'][0][..., 3] > 0
    hb = layers['hair_back'][0][..., 3] > 0 if 'hair_back' in layers else np.zeros_like(hf)
    allh = hf | hb
    top_rows = 7 if view == 'front' else 15
    need = sk.copy(); need[top_rows:] = False
    uncovered = int((need & ~hf).sum())
    near = _dilate(sk, 2)
    floating = 0
    for comp in _components(allh):
        ys = np.array([p[0] for p in comp]); xs = np.array([p[1] for p in comp])
        if not near[ys, xs].any():
            floating += 1
    return {'uncovered_scalp_px': uncovered, 'floating_components': floating,
            'ok': uncovered == 0 and floating == 0}


def compose(layers, keys=LAYER_ORDER):
    img = np.zeros((C.CH, C.CW, 4), np.uint8)
    for k in keys:
        if k in layers:
            over(img, layers[k][0], 0, 0)
    return img


def compose_recolored(layers, swatches):
    img = np.zeros((C.CH, C.CW, 4), np.uint8)
    for k in LAYER_ORDER:
        if k in layers:
            rgba, mask = layers[k]
            over(img, recolor(rgba, mask, swatches), 0, 0)
    return img


@custom_builder('characters', order=30)
def build_characters(root, log=None):
    entries = []
    fam = 'characters'
    qa = {'hair': {}, 'summary': {}}
    # ---- bases ----
    for base, b in C.BASES.items():
        e = {'id': base, 'category': fam, 'subcategory': 'avatar' if base.startswith('player') else 'employee',
             'name_fr': ('Avatar joueur' if base.startswith('player') else 'Employe') +
                        (' (presentation masculine)' if b['pres'] == 'm' else ' (presentation feminine)'),
             'pack_version': PACK_VERSION, 'usage': 'current', 'kind': 'character_base',
             'presentation': b['pres'], 'build': b['build'], 'outfit': b['outfit'], 'face': b['face'],
             'channels': dict(skin=b['skin_default'], hair=DEFAULT_HAIR, **b['colors']),
             'skin_options': ['skin_light', 'skin_medium', 'skin_deep'],
             'compatible_hair': [f'hair_{h}' for h in C.HAIRSTYLES], 'hair_offset_px': [0, 0],
             'canvas': [C.CW, C.CH], 'seat_anchor': list(C.SEAT), 'head_center': [20, 14],
             'portrait_crop': PORTRAIT_CROP, 'layer_order': LAYER_ORDER, 'pose': 'assis, statique',
             'orientations': {}}
        for o in ORIENTS:
            view, mirror = C.ORIENT_VIEW[o]
            L = C.render_layers(base, o)
            od = {'size': [C.CW, C.CH], 'anchor': list(C.SEAT), 'view': view, 'mirrored_from': None if not mirror else
                  ('se' if view == 'front' else 'nw'), 'layers': {}}
            for ln in ('body', 'head'):
                rgba, mask = L[ln]
                png = f"assets/{fam}/bases/{base}__{o}__{ln}.png"
                msk = f"masks/{fam}/bases/{base}__{o}__{ln}__mask.png"
                save_png(os.path.join(root, png), rgba)
                save_png(os.path.join(root, msk), mask)
                od['layers'][ln] = {'file': png, 'mask': msk, 'mask_channels': _chans(mask)}
            e['orientations'][o] = od
        entries.append(e)
    # ---- coiffures ----
    any_base = 'employee_m_01'
    for hs in C.HAIRSTYLES:
        e = {'id': f'hair_{hs}', 'category': fam, 'subcategory': 'hair', 'name_fr': 'Coiffure ' + hs.replace('_', ' '),
             'pack_version': PACK_VERSION, 'usage': 'current', 'kind': 'character_hair',
             'presentation_hint': C.HAIR_PRES[hs], 'channels': {'hair': DEFAULT_HAIR},
             'hair_ramps': ['hair_black', 'hair_brown', 'hair_auburn', 'hair_blond', 'hair_gray'],
             'compatible_bases': list(C.BASES.keys()), 'offset_px': [0, 0], 'canvas': [C.CW, C.CH],
             'anchor': list(C.SEAT), 'orientations': {}}
        for o in ORIENTS:
            L = C.render_layers(any_base, o, hs, DEFAULT_HAIR)
            od = {'size': [C.CW, C.CH], 'anchor': list(C.SEAT), 'layers': {}}
            for ln in ('hair_front', 'hair_back'):
                if ln not in L:
                    continue
                rgba, mask = L[ln]
                png = f"assets/{fam}/hair/hair_{hs}__{o}__{ln.split('_')[1]}.png"
                msk = f"masks/{fam}/hair/hair_{hs}__{o}__{ln.split('_')[1]}__mask.png"
                save_png(os.path.join(root, png), rgba)
                save_png(os.path.join(root, msk), mask)
                od['layers'][ln] = {'file': png, 'mask': msk, 'mask_channels': _chans(mask)}
            res = hair_qa(hs, o, L)
            od['qa'] = res
            qa['hair'][f'{hs}:{o}'] = res
            e['orientations'][o] = od
        entries.append(e)
    # alignement verifie sur les 12 bases (le crane est commun : on le prouve)
    mism = 0
    for base in C.BASES:
        for o in ORIENTS:
            L = C.render_layers(base, o)
            head_a = L['head'][0][..., 3] > 0
            sk = _skull(o)
            if (sk & ~head_a).any():
                mism += 1
    qa['summary'] = {'bases': len(C.BASES), 'hairstyles': len(C.HAIRSTYLES), 'orientations': 4,
                     'hair_checks': len(qa['hair']), 'hair_failures': sum(1 for v in qa['hair'].values() if not v['ok']),
                     'skull_mismatch_layers': mism}
    os.makedirs(os.path.join(root, 'build_cache'), exist_ok=True)
    json.dump(qa, open(os.path.join(root, 'build_cache', 'qa_characters.json'), 'w', encoding='utf-8'), indent=1)
    _previews(root)
    if log:
        log(f"  personnages : {qa['summary']}")
    return entries


# ---------------------------------------------------------------------------
# Planches de demonstration
# ---------------------------------------------------------------------------
BG = (216, 207, 194)


def _sheet(w, h):
    s = np.zeros((h, w, 4), np.uint8)
    s[..., :3] = BG; s[..., 3] = 255
    return s


def _previews(root):
    P = os.path.join(root, 'previews')
    bases = list(C.BASES.keys())
    # 1) bases chauves, 4 orientations
    cw, ch = C.CW + 4, C.CH + 10
    sh = _sheet(14 + cw * len(bases), 16 + ch * 4)
    draw_text(sh, 4, 3, 'BASES CHAUVES (12) X 4 ORIENTATIONS : SE SW NW NE')
    for i, b in enumerate(bases):
        draw_text(sh, 8 + i * cw, 12, b.replace('employee_', 'e_').replace('player_', 'p_'))
        for j, o in enumerate(ORIENTS):
            over(sh, compose(C.render_layers(b, o)), 8 + i * cw, 20 + j * ch)
    save_png(os.path.join(P, 'characters_bases.png'), upscale(sh, 3))
    # 2) grille d'alignement des coiffures (12 bases x 6 coiffures) vue de face et de dos
    for o, label in (('se', 'FACE (SE)'), ('nw', 'DOS (NW)')):
        sh = _sheet(14 + cw * len(bases), 24 + (C.CH + 4) * 6)
        draw_text(sh, 4, 3, f'ALIGNEMENT COIFFURES X BASES - {label} - OFFSET 0,0 POUR TOUTES')
        for i, b in enumerate(bases):
            for j, hs in enumerate(C.HAIRSTYLES):
                ramp = ['hair_brown', 'hair_black', 'hair_auburn', 'hair_blond', 'hair_gray'][(i + j) % 5]
                over(sh, compose(C.render_layers(b, o, hs, ramp)), 8 + i * cw, 14 + j * (C.CH + 4))
        for j, hs in enumerate(C.HAIRSTYLES):
            draw_text(sh, 1, 14 + j * (C.CH + 4) + 2, str(j + 1))
        save_png(os.path.join(P, f'characters_hair_alignment_{o}.png'), upscale(sh, 3))
    # 3) avatar joueur : 2 presentations x 3 coiffures x 3 peaux (recoloration par masque)
    skins = ['skin_light', 'skin_medium', 'skin_deep']
    sh = _sheet(14 + cw * 9, 30 + ch * 2)
    draw_text(sh, 4, 3, 'AVATAR JOUEUR : 2 PRESENTATIONS X 3 COIFFURES X 3 PEAUX (MASQUES)')
    for r, (base, styles) in enumerate((('player_m', ['short_crop', 'side_part', 'curly_top']),
                                         ('player_f', ['long_straight', 'bun', 'bob']))):
        for k, hs in enumerate(styles):
            L = C.render_layers(base, 'se', hs, 'hair_brown')
            for si, sk in enumerate(skins):
                img = compose_recolored(L, {'skin': sk})
                over(sh, img, 8 + (k * 3 + si) * cw, 16 + r * ch)
    save_png(os.path.join(P, 'characters_avatar_matrix.png'), upscale(sh, 3))
    # 4) recoloration : meme geometrie, peau x cheveux, et tenues
    hairs = ['hair_black', 'hair_brown', 'hair_auburn', 'hair_blond', 'hair_gray']
    sh = _sheet(14 + cw * 8, 30 + ch * 3)
    draw_text(sh, 4, 3, 'MEME GEOMETRIE, AUCUNE BASE DUPLIQUEE : PEAU X CHEVEUX + TENUE')
    L = C.render_layers('employee_f_02', 'se', 'bob', 'hair_brown')
    for si, sk in enumerate(skins):
        for hi, hr in enumerate(hairs):
            over(sh, compose_recolored(L, {'skin': sk, 'hair': hr}), 8 + hi * cw, 16 + si * ch)
    L2 = C.render_layers('player_m', 'se', 'side_part', 'hair_brown')
    for k, (pr, sc) in enumerate((('teal', 'navy'), ('coral', 'graphite'), ('sun', 'steel'))):
        over(sh, compose_recolored(L2, {'primary': pr, 'secondary': sc}), 8 + (5 + k) * cw, 16)
    save_png(os.path.join(P, 'characters_recolor.png'), upscale(sh, 3))
    # 5) portraits HUD (recadrage de la composition 'se')
    x0, y0, pw, ph = PORTRAIT_CROP
    sh = _sheet(14 + (pw + 6) * 12, 26 + ph)
    draw_text(sh, 4, 3, 'PORTRAITS HUD (RECADRAGE 32X32 DE LA VUE SE)')
    for i, b in enumerate(bases):
        img = compose(C.render_layers(b, 'se', C.HAIRSTYLES[i % 6], hairs[i % 5]))
        crop = img[y0:y0 + ph, x0:x0 + pw]
        bgc = np.zeros((ph, pw, 4), np.uint8); bgc[..., :3] = (239, 228, 204); bgc[..., 3] = 255
        over(bgc, crop, 0, 0)
        sh[14:14 + ph, 8 + i * (pw + 6):8 + i * (pw + 6) + pw] = bgc
    save_png(os.path.join(P, 'characters_portraits.png'), upscale(sh, 3))
