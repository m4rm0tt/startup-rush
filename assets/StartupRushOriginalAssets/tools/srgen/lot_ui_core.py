"""Outils 2D de l'interface (lot UI) : toile RGBA, 9-slice, teinte d'icones,
rendu de texte pixel exact avec la police originale (sources/ui/font_glyphs.py).
"""
import os
import sys
import numpy as np

from .palette import RAMPS, FIXED, hx

_HERE = os.path.dirname(os.path.abspath(__file__))
_ROOT = os.path.dirname(os.path.dirname(_HERE))
if os.path.join(_ROOT, 'sources') not in sys.path:
    sys.path.insert(0, os.path.join(_ROOT, 'sources'))

from ui import font_glyphs as FG  # noqa: E402


# ---------------------------------------------------------------------------
# Jetons de couleur du theme « Papier » (tous issus de palette.py)
# ---------------------------------------------------------------------------
def R(name, i):
    return RAMPS[name][i]


TOK = {
    'ink': FIXED['ink'], 'ink_soft': FIXED['ink_soft'],
    'paper': R('cream', 5), 'paper_2': R('cream', 4), 'paper_3': R('cream', 3), 'paper_4': R('cream', 2),
    'paper_hi': R('plastic', 5), 'text': FIXED['ink'], 'text_muted': R('cream', 0),
    'text_disabled': R('cream', 1), 'text_on_dark': R('cream', 5), 'text_on_accent': R('cream', 5),
    'teal': R('teal', 2), 'teal_hi': R('teal', 3), 'teal_lo': R('teal', 1), 'teal_light': R('teal', 5),
    'coral': R('coral', 3), 'coral_hi': R('coral', 4), 'coral_lo': R('coral', 2), 'coral_deep': R('coral', 1),
    'sun': R('sun', 3), 'sun_hi': R('sun', 4), 'sun_lo': R('sun', 2), 'sun_deep': R('sun', 1),
    'success': R('leaf', 3), 'success_hi': R('leaf', 4), 'success_lo': R('leaf', 2),
    'warning': R('sun', 3), 'warning_lo': R('sun', 1),
    'danger': R('red', 3), 'danger_hi': R('red', 4), 'danger_lo': R('red', 2), 'danger_deep': R('red', 1),
    'info': R('sky', 3), 'info_hi': R('sky', 4), 'info_lo': R('sky', 2),
    'navy': R('navy', 1), 'navy_2': R('navy', 2), 'navy_3': R('navy', 3), 'navy_0': R('navy', 0), 'navy_hi': R('navy', 4),
    'term_bg': R('display', 0), 'term_bg_2': R('display', 1), 'term_text': R('screen', 4), 'term_dim': R('screen', 2),
    'term_ok': FIXED['led_green'], 'term_err': FIXED['led_red'], 'term_warn': FIXED['led_amber'], 'term_info': FIXED['led_blue'],
    'disabled_bg': R('cream', 3), 'disabled_line': R('cream', 1),
    'shadow': (43, 33, 40, 70), 'scrim': (43, 33, 40, 150), 'white': (255, 255, 255, 255),
}


def tok(name, a=None):
    c = TOK[name]
    if a is not None:
        return (c[0], c[1], c[2], a)
    return c


# ---------------------------------------------------------------------------
# Toile
# ---------------------------------------------------------------------------
def canvas(w, h, color=(0, 0, 0, 0)):
    c = np.zeros((h, w, 4), np.uint8)
    c[...] = color
    return c


def put(c, x, y, color):
    if 0 <= y < c.shape[0] and 0 <= x < c.shape[1]:
        c[y, x] = color


def fill(c, x0, y0, x1, y1, color):
    """Rectangle plein, bornes INCLUSIVES."""
    x0, x1 = max(0, x0), min(c.shape[1] - 1, x1)
    y0, y1 = max(0, y0), min(c.shape[0] - 1, y1)
    if x1 >= x0 and y1 >= y0:
        c[y0:y1 + 1, x0:x1 + 1] = color


def hline(c, x0, x1, y, color):
    fill(c, x0, y, x1, y, color)


def vline(c, x, y0, y1, color):
    fill(c, x, y0, x, y1, color)


def rect(c, x0, y0, x1, y1, color):
    hline(c, x0, x1, y0, color); hline(c, x0, x1, y1, color)
    vline(c, x0, y0, y1, color); vline(c, x1, y0, y1, color)


def rrect(c, x0, y0, x1, y1, color, r=1):
    """Contour arrondi : r pixels de coin retires (r=1 : coin coupe)."""
    hline(c, x0 + r, x1 - r, y0, color); hline(c, x0 + r, x1 - r, y1, color)
    vline(c, x0, y0 + r, y1 - r, color); vline(c, x1, y0 + r, y1 - r, color)
    if r == 2:
        for (x, y) in ((x0 + 1, y0 + 1), (x1 - 1, y0 + 1), (x0 + 1, y1 - 1), (x1 - 1, y1 - 1)):
            put(c, x, y, color)


def rfill(c, x0, y0, x1, y1, color, r=1):
    """Rectangle plein aux coins arrondis."""
    fill(c, x0, y0 + r, x1, y1 - r, color)
    fill(c, x0 + r, y0, x1 - r, y1, color)
    if r == 2:
        fill(c, x0 + 1, y0 + 1, x1 - 1, y1 - 1, color)


def blit(dst, src, x, y, opacity=1.0):
    """Composition alpha 'over' (pixels entiers)."""
    H, W = dst.shape[:2]
    h, w = src.shape[:2]
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(W, x + w), min(H, y + h)
    if x1 <= x0 or y1 <= y0:
        return dst
    s = src[y0 - y:y1 - y, x0 - x:x1 - x].astype(np.float32)
    d = dst[y0:y1, x0:x1].astype(np.float32)
    sa = s[..., 3:4] / 255.0 * opacity
    da = d[..., 3:4] / 255.0
    oa = sa + da * (1 - sa)
    rgb = np.where(oa > 0, (s[..., :3] * sa + d[..., :3] * da * (1 - sa)) / np.maximum(oa, 1e-6), 0)
    dst[y0:y1, x0:x1] = np.clip(np.round(np.concatenate([rgb, oa * 255.0], -1)), 0, 255).astype(np.uint8)
    return dst


def tint(mask_img, color):
    """Teinte une icone blanche (alpha) : couleur * alpha."""
    out = np.zeros_like(mask_img)
    out[..., 0], out[..., 1], out[..., 2] = color[0], color[1], color[2]
    a = color[3] if len(color) > 3 else 255
    out[..., 3] = (mask_img[..., 3].astype(np.uint16) * a // 255).astype(np.uint8)
    return out


def scale(img, k):
    return np.repeat(np.repeat(img, k, 0), k, 1)


def nine(src, W, H, margins, mode='stretch'):
    """Rendu 9-slice au plus proche voisin. margins = (l, t, r, b).
    mode : 'stretch' (centre et bords etires) ou 'tile' (repetes)."""
    h, w = src.shape[:2]
    l, t, r, b = margins
    W = max(W, l + r + 1)
    H = max(H, t + b + 1)

    def idx(n, total, a, z, md):
        out = np.empty(n, int)
        mid_src = total - a - z
        mid_dst = n - a - z
        for i in range(n):
            if i < a:
                out[i] = i
            elif i >= n - z:
                out[i] = i - (n - total)
            else:
                k = i - a
                if md == 'tile' or mid_src <= 0:
                    out[i] = a + (k % max(1, mid_src))
                else:
                    out[i] = a + min(mid_src - 1, (k * mid_src) // max(1, mid_dst))
        return out
    xi = idx(W, w, l, r, mode)
    yi = idx(H, h, t, b, mode)
    return src[yi][:, xi]


# ---------------------------------------------------------------------------
# Texte (police originale, rendu pixel exact)
# ---------------------------------------------------------------------------
class PixelFont:
    def __init__(self, mono=False):
        self.mono = mono
        self.glyphs = FG.all_glyphs(mono)
        self.line_height = FG.LINE_HEIGHT
        self.ascent = FG.ASCENT

    def glyph(self, ch):
        if ch not in self.glyphs:
            ch = '?'
        top, rows = self.glyphs[ch]
        w = max((len(r) for r in rows), default=0)
        adv = FG.advance_of(ch, rows, self.mono)
        off = 0
        if self.mono:
            off = max(0, (FG.MONO_ADVANCE - w) // 2) if w < 5 else 0
        elif ch.isdigit():
            off = (4 - w) // 2 if w < 4 else 0
        return top, rows, w, adv, off

    def measure(self, s):
        if not s:
            return 0
        x = 0
        for ch in s:
            x += self.glyph(ch)[3]
        return x - 1  # pas d'espacement apres le dernier glyphe

    def draw(self, img, x, y, s, color, k=1, shadow=None):
        """Dessine `s` ; (x, y) = coin haut-gauche de la cellule de ligne (12 px)."""
        if shadow is not None:
            self.draw(img, x + k, y + k, s, shadow, k)
        cx = x
        H, W = img.shape[:2]
        for ch in s:
            top, rows, w, adv, off = self.glyph(ch)
            for j, row in enumerate(rows):
                for i, c in enumerate(row):
                    if c == '#':
                        px = cx + (off + i) * k
                        py = y + (top + j) * k
                        if px + k > 0 and py + k > 0 and px < W and py < H:
                            img[max(0, py):max(0, py) + k, max(0, px):max(0, px) + k] = color
            cx += adv * k
        return cx - x

    def draw_box(self, img, x, y, w, s, color, align='left', k=1, shadow=None):
        tw = self.measure(s) * k
        if align == 'center':
            x = x + (w - tw) // 2
        elif align == 'right':
            x = x + w - tw
        self.draw(img, x, y, s, color, k, shadow)
        return tw

    def wrap(self, s, maxw):
        words = s.split(' ')
        lines = []; cur = ''
        for wd in words:
            t = (cur + ' ' + wd) if cur else wd
            if self.measure(t) <= maxw or not cur:
                cur = t
            else:
                lines.append(cur); cur = wd
        if cur:
            lines.append(cur)
        return lines


FONT = None
MONO = None


def fonts():
    global FONT, MONO
    if FONT is None:
        FONT = PixelFont(False)
        MONO = PixelFont(True)
    return FONT, MONO
