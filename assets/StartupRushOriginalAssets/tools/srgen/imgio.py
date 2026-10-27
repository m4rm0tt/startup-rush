"""Entrees/sorties PNG et composition (alpha 'over', ombres en couche unique)."""
import os
import numpy as np
from PIL import Image, PngImagePlugin

from .palette import FIXED

SHADOW_LAYER_OPACITY = 0.30


def save_png(path, arr):
    """Ecrit un PNG RGBA 8 bits avec bloc sRGB (intention perceptuelle)."""
    os.makedirs(os.path.dirname(path) or '.', exist_ok=True)
    arr = np.ascontiguousarray(arr.astype(np.uint8))
    if arr.ndim == 2:
        arr = np.stack([arr] * 3 + [np.full(arr.shape, 255, np.uint8)], -1)
    if arr.shape[2] == 3:
        arr = np.concatenate([arr, np.full(arr.shape[:2] + (1,), 255, np.uint8)], 2)
    im = Image.fromarray(arr, 'RGBA')
    info = PngImagePlugin.PngInfo()
    info.add(b'sRGB', b'\x00')
    im.save(path, 'PNG', pnginfo=info, optimize=True)


def load_png(path):
    im = Image.open(path)
    im.load()
    return np.array(im.convert('RGBA'))


def over(dst, src, x, y, opacity=1.0):
    """Compose src (RGBA uint8) sur dst (RGBA uint8) en (x, y), pixels entiers."""
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
    out = np.concatenate([rgb, oa * 255.0], -1)
    dst[y0:y1, x0:x1] = np.clip(np.round(out), 0, 255).astype(np.uint8)
    return dst


def stamp_max(dst_alpha, src, x, y):
    """Accumule une ombre dans un tampon (max) : aucune accumulation aux recouvrements."""
    H, W = dst_alpha.shape
    h, w = src.shape[:2]
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(W, x + w), min(H, y + h)
    if x1 <= x0 or y1 <= y0:
        return
    dst_alpha[y0:y1, x0:x1] = np.maximum(dst_alpha[y0:y1, x0:x1], src[y0 - y:y1 - y, x0 - x:x1 - x, 3])


def shadow_layer_to_rgba(alpha_buf, opacity=SHADOW_LAYER_OPACITY):
    h, w = alpha_buf.shape
    out = np.zeros((h, w, 4), np.uint8)
    c = FIXED['shadow']
    out[..., 0], out[..., 1], out[..., 2] = c[0], c[1], c[2]
    out[..., 3] = (alpha_buf.astype(np.float32) / 255.0 * opacity * 255).round().astype(np.uint8)
    return out


def upscale(arr, k):
    return np.repeat(np.repeat(arr, k, 0), k, 1)


def flatten_on(arr, bg=(46, 40, 48)):
    h, w = arr.shape[:2]
    base = np.zeros((h, w, 4), np.uint8)
    base[..., :3] = bg
    base[..., 3] = 255
    over(base, arr, 0, 0)
    return base


def checker(h, w, a=(214, 206, 196), b=(200, 192, 182), cell=8):
    yy, xx = np.mgrid[0:h, 0:w]
    m = ((yy // cell + xx // cell) % 2) == 0
    out = np.zeros((h, w, 4), np.uint8)
    out[m, :3] = a
    out[~m, :3] = b
    out[..., 3] = 255
    return out
