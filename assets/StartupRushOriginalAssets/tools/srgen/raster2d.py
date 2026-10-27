"""Peintre 2D par regions etiquetees (personnages, details organiques, icones).

On dessine des regions (materiau + volume + profondeur), puis le rendu calcule :
  - l'ombrage directionnel (lumiere haut-gauche, identique au moteur iso) ;
  - les lignes internes (la region situee derriere recoit un trait sombre) ;
  - le contour exterieur a l'encre.
L'ombrage etant calcule APRES un eventuel miroir, un personnage retourne garde
une lumiere venant de la gauche de l'ecran.
"""
import numpy as np

from .palette import RAMPS, FIXED, Mat
from .iso import Fix


class Region:
    __slots__ = ('rid', 'name', 'mat', 'base', 'vol', 'z', 'shade', 'line', 'band', 'hl')

    def __init__(self, rid, name, mat, base, vol, z, shade, line, band, hl):
        self.rid, self.name, self.mat, self.base = rid, name, mat, base
        self.vol, self.z, self.shade, self.line, self.band, self.hl = vol, z, shade, line, band, hl


class Painter:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.lab = np.zeros((h, w), np.int32)
        self.regions = {}
        self._n = 0
        self.forced = np.zeros((h, w), np.int8)   # decalage d'indice force (plis, reflets)

    # -- regions -------------------------------------------------------------
    def region(self, name, mat, base=3, vol=None, z=0, shade=True, line=True, band=1, hl=True):
        self._n += 1
        r = Region(self._n, name, mat, base, vol if vol is not None else name, z, shade, line, band, hl)
        self.regions[self._n] = r
        return self._n

    def rid(self, name):
        for k, r in self.regions.items():
            if r.name == name:
                return k
        raise KeyError(name)

    # -- dessin ---------------------------------------------------------------
    def _set(self, rid, m):
        self.lab[m] = rid

    def rect(self, rid, x0, y0, x1, y1):
        x0, y0 = max(0, x0), max(0, y0)
        x1, y1 = min(self.w, x1), min(self.h, y1)
        if x1 > x0 and y1 > y0:
            self.lab[y0:y1, x0:x1] = rid

    def ellipse(self, rid, cx, cy, rx, ry):
        yy, xx = np.mgrid[0:self.h, 0:self.w]
        m = ((xx + 0.5 - cx) / rx) ** 2 + ((yy + 0.5 - cy) / ry) ** 2 <= 1.0
        self._set(rid, m)

    def poly(self, rid, pts):
        """Polygone (sommets en coordonnees pixel, centres echantillonnes)."""
        yy, xx = np.mgrid[0:self.h, 0:self.w]
        px = xx + 0.5; py = yy + 0.5
        inside = np.zeros((self.h, self.w), bool)
        n = len(pts)
        for i in range(n):
            x1, y1 = pts[i]; x2, y2 = pts[(i + 1) % n]
            cond = ((y1 > py) != (y2 > py))
            xint = (x2 - x1) * (py - y1) / ((y2 - y1) + 1e-12) + x1
            inside ^= cond & (px < xint)
        self._set(rid, inside)

    def plot(self, rid, pts):
        for (x, y) in pts:
            if 0 <= x < self.w and 0 <= y < self.h:
                self.lab[y, x] = rid

    def hline(self, rid, x0, x1, y):
        self.rect(rid, x0, y, x1, y + 1)

    def vline(self, rid, x, y0, y1):
        self.rect(rid, x, y0, x + 1, y1)

    def stamp(self, rows, x, y, legend, skip='.'):
        """Tampon ASCII : legend = {caractere: rid ou 0 pour effacer}."""
        for j, row in enumerate(rows):
            for i, c in enumerate(row):
                if c == skip or c == ' ':
                    continue
                if c not in legend:
                    raise KeyError(f"caractere '{c}' absent de la legende")
                X, Y = x + i, y + j
                if 0 <= X < self.w and 0 <= Y < self.h:
                    self.lab[Y, X] = legend[c]

    def force(self, pts, delta):
        for (x, y) in pts:
            if 0 <= x < self.w and 0 <= y < self.h:
                self.forced[y, x] = delta

    def erase_rect(self, x0, y0, x1, y1):
        self.rect(0, x0, y0, x1, y1)

    def mirrored(self):
        p = Painter(self.w, self.h)
        p.lab = self.lab[:, ::-1].copy()
        p.forced = self.forced[:, ::-1].copy()
        p.regions = dict(self.regions)
        p._n = self._n
        return p

    def silhouette(self):
        return self.lab > 0

    # -- rendu ----------------------------------------------------------------
    def render(self, outline=True, under=None, soft_under=True):
        """Retourne (rgba, mask). `under` : silhouette des couches situees dessous
        (ex. crane sous les cheveux) : sur cette zone le contour n'est pas encre,
        le bord de la region est assombri a la place (pas de cheveux flottants)."""
        h, w = self.h, self.w
        lab = self.lab
        occ = lab > 0
        ids = np.unique(lab[occ])
        vol = np.zeros((h, w), np.int32)
        zz = np.full((h, w), -999, np.int32)
        base = np.zeros((h, w), np.int16)
        vol_ids = {}
        for k in ids:
            r = self.regions[k]
            m = lab == k
            vol[m] = vol_ids.setdefault(r.vol, len(vol_ids) + 1)
            zz[m] = r.z
            base[m] = r.base

        def shifted(arr, dy, dx, fill):
            out = np.full_like(arr, fill)
            ys0, ys1 = max(0, -dy), h - max(0, dy)
            xs0, xs1 = max(0, -dx), w - max(0, dx)
            out[ys0:ys1, xs0:xs1] = arr[ys0 + dy:ys1 + dy, xs0 + dx:xs1 + dx]
            return out

        idx = base.astype(np.int16).copy()
        shade_m = np.zeros((h, w), bool)
        hl_m = np.zeros((h, w), bool)
        band2 = np.zeros((h, w), bool)
        for k in ids:
            r = self.regions[k]
            m = lab == k
            if r.shade:
                shade_m |= m
            if r.hl:
                hl_m |= m
            if r.band >= 2:
                band2 |= m
        under_m = under if under is not None else np.zeros((h, w), bool)
        delta = np.zeros((h, w), np.int16)
        lit = np.zeros((h, w), bool); dark = np.zeros((h, w), bool)
        for (dy, dx, side) in ((-1, 0, 'L'), (0, -1, 'L'), (1, 0, 'D'), (0, 1, 'D')):
            nv = shifted(vol, dy, dx, 0)
            nz = shifted(zz, dy, dx, -999)
            nunder = shifted(under_m, dy, dx, False)
            diff = occ & (nv != vol)
            sil = diff & ((nv == 0) | (nz < zz)) & ~nunder
            occl = diff & (nv != 0) & (nz > zz)
            if side == 'L':
                lit |= sil
                dark |= occl
            else:
                dark |= sil | (diff & nunder & (nv == 0))
        # bande d'ombre de 2 px a droite pour les volumes ronds
        nv2 = shifted(vol, 0, 2, 0)
        dark |= band2 & occ & (nv2 != vol) & (shifted(zz, 0, 2, -999) <= zz)
        delta[lit & hl_m] += 1
        delta[dark] -= 1
        delta = np.clip(delta, -1, 1)
        idx = np.where(shade_m, idx + delta, idx)
        idx = idx + self.forced
        # lignes internes : la region situee derriere recoit un trait sombre
        line = np.zeros((h, w), bool)
        for (dy, dx) in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            nl = shifted(lab, dy, dx, 0)
            nv = shifted(vol, dy, dx, 0)
            nz = shifted(zz, dy, dx, -999)
            front_line = np.zeros((h, w), bool)
            for k in ids:
                if self.regions[k].line:
                    front_line |= nl == k
            line |= occ & (nv != 0) & (nv != vol) & (nz > zz) & front_line
        idx = np.where(line, idx - 2, idx)
        idx = np.clip(idx, 0, 5)
        if soft_under and under is not None:
            # bord de la region pose sur la couche du dessous : teinte sombre interne
            edge_on_under = np.zeros((h, w), bool)
            for (dy, dx) in ((0, 1), (0, -1), (1, 0), (-1, 0)):
                nocc = shifted(occ, dy, dx, False)
                nunder = shifted(under_m, dy, dx, False)
                edge_on_under |= occ & ~nocc & nunder
            idx = np.where(edge_on_under, np.minimum(idx, 1), idx)
        rgba = np.zeros((h, w, 4), np.uint8)
        mask = np.zeros((h, w, 4), np.uint8)
        for k in ids:
            r = self.regions[k]
            m = lab == k
            if isinstance(r.mat, Fix):
                rgba[m] = r.mat.color
            elif isinstance(r.mat, tuple):
                rgba[m] = r.mat
            else:
                pal = np.array(RAMPS[r.mat.ramp], np.uint8)
                rgba[m] = pal[idx[m]]
                if r.mat.ch > 0:
                    mask[m, 0] = r.mat.ch
                    mask[m, 1] = idx[m]
                    mask[m, 3] = 255
        if outline:
            ring = np.zeros((h, w), bool)
            for (dy, dx) in ((0, 1), (0, -1), (1, 0), (-1, 0)):
                ring |= shifted(occ, dy, dx, False)
            ring &= ~occ
            if under is not None:
                ring &= ~under_m
            rgba[ring] = FIXED['ink']
        return rgba, mask


def trim_box(rgba):
    a = rgba[..., 3] > 0
    if not a.any():
        return (0, 0, 0, 0)
    ys, xs = np.nonzero(a)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
