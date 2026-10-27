"""Construction des polices TTF originales depuis les glyphes pixel (fontTools),
atlas bitmap de secours et verification (rechargement + comparaison pixel a pixel
du rendu FreeType/Pillow avec le rendu de reference).

1 px de police = 100 unites ; unitsPerEm = 1200 (12 px) ; ascendante 1000, descendante 200.
Rendu net : taille de police 12 px (x1), 24 px (x2), 36 px (x3)...
"""
import json
import os
import numpy as np

from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont

from .lot_ui_core import FG, PixelFont

PX = 100
UPM = FG.LINE_HEIGHT * PX


def _pixels(rows, top, off=0):
    pts = set()
    for j, row in enumerate(rows):
        for i, c in enumerate(row):
            if c == '#':
                pts.add((i + off, top + j))
    return pts


def trace(pixels):
    """Contours orientes (sens horaire, y vers le haut) de l'union des pixels.
    Retourne une liste de contours [(x, y), ...] en px (ligne de base y=0)."""
    asc = FG.ASCENT
    edges = {}

    def add(a, b):
        edges.setdefault(a, []).append(b)
    for (c, r) in pixels:
        x0, x1 = c, c + 1
        y0, y1 = asc - 1 - r, asc - r
        if (c - 1, r) not in pixels:
            add((x0, y0), (x0, y1))
        if (c, r - 1) not in pixels:
            add((x0, y1), (x1, y1))
        if (c + 1, r) not in pixels:
            add((x1, y1), (x1, y0))
        if (c, r + 1) not in pixels:
            add((x1, y0), (x0, y0))
    contours = []
    used = set()

    def turn_rank(d_in, d_out):
        # Aux sommets ambigus (pixels en contact par un coin), on prefere le virage
        # a GAUCHE : les pixels diagonaux forment un seul contour auto-tangent.
        # Verifie : FreeType (Pillow) rend alors tous les glyphes au pixel pres,
        # alors que des carres separes etaient ecartes par l'auto-hinter.
        right = (d_in[1], -d_in[0])
        left = (-d_in[1], d_in[0])
        if d_out == left:
            return 0
        if d_out == d_in:
            return 1
        if d_out == right:
            return 2
        return 3
    for start in sorted(edges):
        for end in list(edges[start]):
            if (start, end) in used:
                continue
            loop = [start]
            a, b = start, end
            used.add((a, b))
            while True:
                loop.append(b)
                if b == start:
                    break
                d_in = (b[0] - a[0], b[1] - a[1])
                cands = [n for n in edges.get(b, []) if (b, n) not in used]
                if not cands:
                    break
                n = min(cands, key=lambda q: turn_rank(d_in, (q[0] - b[0], q[1] - b[1])))
                used.add((b, n))
                a, b = b, n
            loop = loop[:-1]
            # suppression des points colineaires
            simp = []
            L = len(loop)
            for k in range(L):
                p0, p1, p2 = loop[k - 1], loop[k], loop[(k + 1) % L]
                if (p1[0] - p0[0]) * (p2[1] - p1[1]) - (p1[1] - p0[1]) * (p2[0] - p1[0]) != 0:
                    simp.append(p1)
            if len(simp) >= 3:
                contours.append(simp)
    return contours


def _glyph_obj(contours, scale=PX):
    pen = TTGlyphPen(None)
    for cont in contours:
        pen.moveTo((cont[0][0] * scale, cont[0][1] * scale))
        for p in cont[1:]:
            pen.lineTo((p[0] * scale, p[1] * scale))
        pen.closePath()
    return pen.glyph()


def gname(ch):
    if ch == ' ':
        return 'space'
    return 'uni%04X' % ord(ch)


def build_ttf(path, mono=False):
    font = PixelFont(mono)
    chars = [' '] + sorted(ch for ch in font.glyphs.keys() if ch != ' ')
    order = ['.notdef'] + [gname(c) for c in chars]
    glyphs = {}
    hmtx = {}
    cmap = {}
    # .notdef : boite creuse
    nd = set()
    for y in range(3, 10):
        for x in range(0, 4):
            if y in (3, 9) or x in (0, 3):
                nd.add((x, y))
    glyphs['.notdef'] = _glyph_obj(trace(nd))
    hmtx['.notdef'] = (5 * PX if not mono else FG.MONO_ADVANCE * PX, 0)
    for ch in chars:
        top, rows, w, adv, off = font.glyph(ch)
        pix = _pixels(rows, top, off)
        cont = trace(pix) if pix else []
        glyphs[gname(ch)] = _glyph_obj(cont)
        lsb = min((p[0] for c in cont for p in c), default=0) * PX
        hmtx[gname(ch)] = (adv * PX, lsb)
        cmap[ord(ch)] = gname(ch)
    fam = 'StartupRush Mono' if mono else 'StartupRush Pixel'
    ps = 'StartupRushMono-Regular' if mono else 'StartupRushPixel-Regular'
    fb = FontBuilder(UPM, isTTF=True)
    fb.setupGlyphOrder(order)
    fb.setupCharacterMap(cmap)
    fb.setupGlyf(glyphs)
    fb.setupHorizontalMetrics(hmtx)
    fb.setupHorizontalHeader(ascent=FG.ASCENT * PX, descent=-FG.DESCENT * PX, lineGap=0)
    fb.setupNameTable({
        'familyName': fam,
        'styleName': 'Regular',
        'uniqueFontIdentifier': f'{ps};1.000;StartupRush',
        'fullName': f'{fam} Regular',
        'psName': ps,
        'version': 'Version 1.000',
        'copyright': 'Police pixel originale creee pour le projet StartupRush (pack 1.0).',
        'manufacturer': 'StartupRush asset pack',
        'description': 'Grille 12 px : capitales 7 px, hauteur d x 5 px, jambages 2 px. Rendu net a 12, 24, 36 px.',
    })
    fb.setupOS2(sTypoAscender=FG.ASCENT * PX, sTypoDescender=-FG.DESCENT * PX, sTypoLineGap=0,
                usWinAscent=FG.ASCENT * PX, usWinDescent=FG.DESCENT * PX,
                sxHeight=FG.X_HEIGHT * PX, sCapHeight=FG.CAP_HEIGHT * PX,
                achVendID='SRUP', fsType=0, usWeightClass=400, fsSelection=0x40)
    fb.setupPost(isFixedPitch=1 if mono else 0, underlinePosition=-PX, underlineThickness=PX)
    f = fb.font
    from fontTools.ttLib.tables.O_S_2f_2 import Panose
    pan = Panose()
    for k in ('bFamilyType', 'bSerifStyle', 'bWeight', 'bProportion', 'bContrast', 'bStrokeVariation',
              'bArmStyle', 'bLetterForm', 'bMidline', 'bXHeight'):
        setattr(pan, k, 0)
    pan.bFamilyType = 2
    pan.bProportion = 9 if mono else 0
    f['OS/2'].panose = pan
    f['OS/2'].recalcUnicodeRanges(f)
    f['OS/2'].recalcAvgCharWidth(f)
    f['head'].macStyle = 0
    # Programme TrueType minimal (controle de dropout) : la police est vue comme
    # « hintee nativement », ce qui desactive l'auto-hinter (qui deplacait des
    # pixels isoles d'accents). Les contours, deja sur la grille, restent exacts.
    from fontTools.ttLib import newTable
    from fontTools.ttLib.tables import ttProgram
    prep = newTable('prep')
    prep.program = ttProgram.Program()
    prep.program.fromAssembly(['PUSHW[ ]', '511', 'SCANCTRL[ ]', 'PUSHB[ ]', '4', 'SCANTYPE[ ]'])
    f['prep'] = prep
    mx = f['maxp']
    mx.maxZones = 1; mx.maxTwilightPoints = 0; mx.maxStorage = 0; mx.maxFunctionDefs = 0
    mx.maxInstructionDefs = 0; mx.maxStackElements = 16; mx.maxSizeOfInstructions = 0
    gasp = newTable('gasp')
    gasp.version = 1
    gasp.gaspRange = {0xFFFF: 0x0001 | 0x0004}
    f['gasp'] = gasp
    # dates fixes et pas de recalcul a l'enregistrement : memes octets a chaque construction
    from fontTools.misc.timeTools import timestampFromString
    ts = timestampFromString('Tue Oct 27 00:00:00 2026')
    f['head'].created = ts
    f['head'].modified = ts
    f.recalcTimestamp = False
    os.makedirs(os.path.dirname(path), exist_ok=True)
    fb.save(path)
    return {'family': fam, 'ps_name': ps, 'glyph_count': len(order), 'chars': len(chars)}


def verify_ttf(path, mono=False, samples=None):
    """Recharge la TTF (fontTools) puis compare le rendu Pillow/FreeType a 12 px et 24 px
    avec le rendu de reference pixel. Retourne un rapport."""
    from PIL import Image, ImageDraw, ImageFont
    tt = TTFont(path)
    cmap = tt.getBestCmap()
    font = PixelFont(mono)
    missing = [ch for ch in font.glyphs if ord(ch) not in cmap]
    samples = samples or [
        'Ça démarre : 12 450 CHF, 3 777 € et 98 % de qualité !',
        'Œuvre, cœur, élève, fenêtre, naïf, Noël, où, août, garçon, ÉTÉ, ÀÏÔÛ',
        'Projet « Nébula » — version 1.0… ‘ok’ “prêt” ±2 ≤ 5 ≥ 1 ≠ 0 × ÷',
        'abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789',
        '!"#$%&\'()*+,-./:;<=>?@[\\]^_`{|}~ æÆ £ ° ·',
    ]
    report = {'file': os.path.basename(path), 'glyphs_in_cmap': len(cmap), 'missing_chars': missing,
              'checks': []}
    for k in (1, 2):
        pil = ImageFont.truetype(path, FG.LINE_HEIGHT * k)
        for s in samples:
            w = font.measure(s) * k + 8 * k
            h = FG.LINE_HEIGHT * k + 4 * k
            ref = np.zeros((h, w, 4), np.uint8)
            font.draw(ref, 2 * k, 2 * k, s, (255, 255, 255, 255), k)
            im = Image.new('L', (w, h), 0)
            d = ImageDraw.Draw(im)
            d.text((2 * k, 2 * k + FG.ASCENT * k), s, font=pil, fill=255, anchor='ls')
            got = np.array(im)
            ref_m = ref[..., 3] > 0
            got_m = got > 127
            gray = int(((got > 20) & (got < 235)).sum())
            diff = int((ref_m != got_m).sum())
            report['checks'].append({'scale': k, 'text': s, 'pixel_mismatch': diff, 'gray_pixels': gray,
                                     'ink_pixels': int(ref_m.sum())})
    return report


def build_atlas(path_png, path_json, mono=False):
    """Atlas bitmap (blanc sur transparent) + metriques JSON (repli pixel exact)."""
    from .imgio import save_png
    font = PixelFont(mono)
    chars = sorted(font.glyphs.keys())
    cols = 16
    cw, chh = 8, FG.LINE_HEIGHT
    rows_n = (len(chars) + cols - 1) // cols
    img = np.zeros((rows_n * chh, cols * cw, 4), np.uint8)
    meta = {'font': 'StartupRush Mono' if mono else 'StartupRush Pixel', 'line_height': FG.LINE_HEIGHT,
            'ascent': FG.ASCENT, 'descent': FG.DESCENT, 'cap_height': FG.CAP_HEIGHT, 'x_height': FG.X_HEIGHT,
            'cell': [cw, chh], 'note': 'x,y,w,h = boite d encre dans l atlas ; dx = decalage depuis la plume ; '
                                      'dy = decalage depuis le haut de la ligne ; adv = chasse en px',
            'glyphs': {}}
    for n, ch in enumerate(chars):
        top, rows, w, adv, off = font.glyph(ch)
        gx, gy = (n % cols) * cw, (n // cols) * chh
        ys = [top + j for j, r in enumerate(rows) if '#' in r]
        for j, row in enumerate(rows):
            for i, c in enumerate(row):
                if c == '#':
                    img[gy + top + j, gx + i] = (255, 255, 255, 255)
        h_ink = (max(ys) - min(ys) + 1) if ys else 0
        meta['glyphs'][ch] = {'code': ord(ch), 'x': gx, 'y': gy + (min(ys) if ys else 0), 'w': w, 'h': h_ink,
                              'dx': off, 'dy': min(ys) if ys else 0, 'adv': adv}
    save_png(path_png, img)
    json.dump(meta, open(path_json, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    return meta
