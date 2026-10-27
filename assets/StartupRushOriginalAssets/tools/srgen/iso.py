"""Moteur isometrique deterministe de StartupRush.

Principe : chaque objet est decrit par des volumes simples (boites, polyedres
convexes, cylindres, ellipsoides) dans son repere local, puis rendu pixel par
pixel par lancer de rayons orthographique. Le resultat est du pixel art net :
aretes 2:1 exactes, aucune interpolation, aucun anti-aliasing.

Contrat de projection (voir STYLE_GUIDE.md) :
  case = losange 64x32 px, 1 case = 32 sous-unites (su) sur u et v
  screen_x = (u - v)            (u, v en su)   = (U - V) * 32 en cases
  screen_y = (u + v) / 2 - z    (z en px)      = (U + V) * 16 - z
  U vers le bas-droite de l'ecran, V vers le bas-gauche, z vers le haut.
  Le rayon de vue a pour direction (1, 1, 1) : plus t (= z) est grand, plus le
  point est proche de la camera.

Repere local d'un objet : x = droite de l'observateur place face a l'objet,
y = profondeur (de la face avant vers l'arriere), z = haut. La face avant est y=0.
Orientation (facing) = direction vers laquelle regarde la face avant :
  se : avant -> +U (face avant visible a droite)   sw : avant -> +V (visible a gauche)
  nw : avant -> -U (dos visible)                   ne : avant -> -V (dos visible)
"""
import math
import numpy as np

from .palette import RAMPS, FIXED, CHANNELS, Mat, M

TILE = 32
WALL_H = 112
FACINGS = ('se', 'sw', 'nw', 'ne')

_L = np.array([-0.25, 0.45, 1.0])
LIGHT = _L / np.linalg.norm(_L)          # direction vers la lumiere
SHADOW_DIR = (-0.25, 0.45)               # deplacement (u,v) vers la lumiere par px de hauteur
SHADOW_K = 0.55                          # ombres stylisees courtes
DEPTH_GAP = 2.5


# ---------------------------------------------------------------------------
# Transformations local -> monde
# ---------------------------------------------------------------------------
def facing_xform(facing, W, D):
    """Retourne (R, T, (span_u, span_v)) avec (u, v) = R @ (x, y) + T."""
    if facing == 'se':
        R = np.array([[0, -1], [-1, 0]]); T = np.array([D, W]); span = (D, W)
    elif facing == 'sw':
        R = np.array([[1, 0], [0, -1]]); T = np.array([0, D]); span = (W, D)
    elif facing == 'nw':
        R = np.array([[0, 1], [1, 0]]); T = np.array([0, 0]); span = (D, W)
    elif facing == 'ne':
        R = np.array([[-1, 0], [0, 1]]); T = np.array([W, 0]); span = (W, D)
    elif facing == 'world':
        # coordonnees deja exprimees en monde (u, v) : architecture
        R = np.array([[1, 0], [0, 1]]); T = np.array([0, 0]); span = (W, D)
    else:
        raise ValueError(facing)
    return R.astype(float), T.astype(float), span


def to_world(facing, W, D, x, y, z=0.0):
    R, T, _ = facing_xform(facing, W, D)
    u, v = R @ np.array([x, y], float) + T
    return float(u), float(v), float(z)


def screen_of(u, v, z=0.0):
    return (u - v, (u + v) / 2.0 - z)


# ---------------------------------------------------------------------------
# Peinture des decors de faces
# ---------------------------------------------------------------------------
class Delta:
    """Meme materiau que la face, indice de rampe decale."""
    def __init__(self, d):
        self.d = d


class Use:
    """Autre materiau. idx absolu, ou None = indice de la face + rel."""
    def __init__(self, mat, idx=None, rel=0):
        self.mat = mat; self.idx = idx; self.rel = rel


class Fix:
    """Couleur fixe non recolorable."""
    def __init__(self, color):
        if isinstance(color, str):
            if color not in FIXED:
                raise ValueError(f"couleur fixe inconnue : {color!r} (voir palette.FIXED)")
            color = FIXED[color]
        self.color = color


# formes dans le repere de face (s vers la droite de l'observateur, t vers le haut)
def rect(s0, t0, s1, t1):
    return ('rect', s0, t0, s1, t1)


def hline(s0, s1, t, w=1):
    return ('rect', s0, t, s1, t + w)


def vline(s, t0, t1, w=1):
    return ('rect', s, t0, s + w, t1)


def frame(s0, t0, s1, t1, w=1, ws=None):
    return ('frame', s0, t0, s1, t1, w, ws if ws is not None else w)


def disc(cs, ct, rs, rt=None):
    return ('disc', cs, ct, rs, rt if rt is not None else rs)


def fn(func):
    return ('fn', func)


def _shape_mask(shape, s, t):
    k = shape[0]
    if k == 'rect':
        _, s0, t0, s1, t1 = shape
        return (s >= s0) & (s < s1) & (t >= t0) & (t < t1)
    if k == 'frame':
        _, s0, t0, s1, t1, w, ws = shape
        outer = (s >= s0) & (s < s1) & (t >= t0) & (t < t1)
        inner = (s >= s0 + ws) & (s < s1 - ws) & (t >= t0 + w) & (t < t1 - w)
        return outer & ~inner
    if k == 'disc':
        _, cs, ct, rs, rt = shape
        return ((s - cs) / rs) ** 2 + ((t - ct) / rt) ** 2 <= 1.0
    if k == 'fn':
        return shape[1](s, t)
    raise ValueError(k)


# ---------------------------------------------------------------------------
# Primitives (repere local)
# ---------------------------------------------------------------------------
class Prim:
    kind = 'prim'

    def __init__(self, mat, layer='base', shadow=True, outline=True, edges=True,
                 name=None, flags=0, alpha=255, flag_faces=None):
        self.flag_faces = flag_faces
        self.mat = mat
        self.decals = []
        self.layer = layer
        self.shadow = shadow
        self.outline = outline
        self.edges = edges
        self.name = name
        self.flags = flags          # drapeaux de masque (motif mural...)
        self.alpha = alpha

    def mat_for(self, face):
        if isinstance(self.mat, dict):
            return self.mat.get(face, self.mat.get('default'))
        return self.mat

    def decal(self, face, shape, paint):
        self.decals.append((face, shape, paint))
        return self

    # aides de decor
    def d(self, face, shape, paint):
        return self.decal(face, shape, paint)


class Poly(Prim):
    """Polyedre convexe : liste de plans (nx, ny, nz, d, tag) avec n.p <= d."""
    kind = 'poly'

    def __init__(self, planes, bbox, mat, **kw):
        super().__init__(mat, **kw)
        self.planes = planes
        self.bbox = bbox  # (x0, y0, z0, x1, y1, z1) pour les coordonnees de face


class Box(Poly):
    def __init__(self, x0, y0, z0, x1, y1, z1, mat, **kw):
        assert x1 > x0 and y1 > y0 and z1 > z0, (x0, y0, z0, x1, y1, z1)
        planes = [(1, 0, 0, x1, 'right'), (-1, 0, 0, -x0, 'left'),
                  (0, 1, 0, y1, 'back'), (0, -1, 0, -y0, 'front'),
                  (0, 0, 1, z1, 'top'), (0, 0, -1, -z0, 'bottom')]
        super().__init__(planes, (x0, y0, z0, x1, y1, z1), mat, **kw)


class Cyl(Prim):
    """Cylindre vertical (section elliptique alignee sur x/y)."""
    kind = 'cyl'

    def __init__(self, cx, cy, rx, z0, z1, mat, ry=None, **kw):
        super().__init__(mat, **kw)
        self.cx, self.cy, self.rx, self.ry = cx, cy, rx, (ry if ry is not None else rx)
        self.z0, self.z1 = z0, z1
        self.bbox = (cx - self.rx, cy - self.ry, z0, cx + self.rx, cy + self.ry, z1)


class Ell(Prim):
    """Ellipsoide, eventuellement coupe par z in [zc0, zc1]."""
    kind = 'ell'

    def __init__(self, cx, cy, cz, rx, ry, rz, mat, zclip=None, **kw):
        super().__init__(mat, **kw)
        self.cx, self.cy, self.cz = cx, cy, cz
        self.rx, self.ry, self.rz = rx, ry, rz
        self.zclip = zclip if zclip else (cz - rz - 1, cz + rz + 1)
        self.bbox = (cx - rx, cy - ry, cz - rz, cx + rx, cy + ry, cz + rz)


class HCyl(Prim):
    """Cylindre horizontal d'axe x ou y local."""
    kind = 'hcyl'

    def __init__(self, axis, c, cz, r, a0, a1, mat, rz=None, **kw):
        super().__init__(mat, **kw)
        assert axis in ('x', 'y')
        self.axis, self.c, self.cz, self.r = axis, c, cz, r
        self.rz = rz if rz is not None else r
        self.a0, self.a1 = a0, a1
        if axis == 'x':
            self.bbox = (a0, c - r, cz - self.rz, a1, c + r, cz + self.rz)
        else:
            self.bbox = (c - r, a0, cz - self.rz, c + r, a1, cz + self.rz)


# ---------------------------------------------------------------------------
# Modele
# ---------------------------------------------------------------------------
class Model:
    """Ensemble de primitives. W, D = emprise locale en su (32 su = 1 case).

    centered=True : l'origine locale (0,0,0) est le point d'ancrage (objets
    poses sur un support, ex. ecran sur un bureau). Sinon l'ancrage est le coin
    nord (U min, V min) de l'emprise au sol.
    """

    def __init__(self, W=TILE, D=TILE, centered=False, outline='full',
                 shadow=True, shadow_k=SHADOW_K, edge_hl=True):
        self.W, self.D = (0, 0) if centered else (W, D)
        self.fW, self.fD = W, D
        self.centered = centered
        self.prims = []
        self.outline = outline
        self.shadow = shadow
        self.shadow_k = shadow_k
        self.edge_hl = edge_hl
        self.clip = None   # (u0, u1, v0, v1) decoupe en tranches (murs)
        self.extra_bounds = []

    def add(self, p):
        self.prims.append(p)
        return p

    def box(self, x0, y0, z0, x1, y1, z1, mat, **kw):
        return self.add(Box(x0, y0, z0, x1, y1, z1, mat, **kw))

    def cyl(self, cx, cy, r, z0, z1, mat, **kw):
        return self.add(Cyl(cx, cy, r, z0, z1, mat, **kw))

    def ell(self, cx, cy, cz, rx, ry, rz, mat, **kw):
        return self.add(Ell(cx, cy, cz, rx, ry, rz, mat, **kw))

    def hcyl(self, axis, c, cz, r, a0, a1, mat, **kw):
        return self.add(HCyl(axis, c, cz, r, a0, a1, mat, **kw))

    def poly(self, planes, bbox, mat, **kw):
        return self.add(Poly(planes, bbox, mat, **kw))

    # -- formes derivees ----------------------------------------------------
    def wedge_y(self, x0, y0, z0, x1, y1, zf, zb, mat, **kw):
        """Boite dont le dessus est incline : hauteur zf a l'avant (y0), zb a l'arriere."""
        dz = zb - zf; dy = y1 - y0
        # plan z <= zf + (y - y0) * dz/dy  ->  -dz/dy * y + z <= zf - y0*dz/dy
        k = dz / dy
        planes = [(1, 0, 0, x1, 'right'), (-1, 0, 0, -x0, 'left'),
                  (0, 1, 0, y1, 'back'), (0, -1, 0, -y0, 'front'),
                  (0, -k, 1, zf - y0 * k, 'top'), (0, 0, -1, -z0, 'bottom')]
        return self.add(Poly(planes, (x0, y0, z0, x1, y1, max(zf, zb)), mat, **kw))

    def slab_tilt(self, x0, x1, y_bot, z_bot, y_top, z_top, thick, mat, **kw):
        """Plaque inclinee (dossier, ecran de portable) entre (y_bot,z_bot) et (y_top,z_top).
        thick : epaisseur vers +y (arriere)."""
        dy = y_top - y_bot; dz = z_top - z_bot
        ln = math.hypot(dy, dz)
        # normale avant (vers -y, legerement +z si incline vers l'arriere)
        ny, nz = -dz / ln, dy / ln
        d_front = ny * y_bot + nz * z_bot
        planes = [(1, 0, 0, x1, 'right'), (-1, 0, 0, -x0, 'left'),
                  (0, ny, nz, d_front, 'front'),
                  (0, -ny, -nz, -(d_front - thick), 'back'),
                  (0, dy / ln, dz / ln, (dy * y_top + dz * z_top) / ln, 'top'),
                  (0, -dy / ln, -dz / ln, -(dy * y_bot + dz * z_bot) / ln, 'bottom')]
        ys = [y_bot, y_top, y_bot + thick, y_top + thick, y_bot - thick, y_top - thick]
        zs = [z_bot, z_top, z_bot - thick, z_top + thick]
        return self.add(Poly(planes, (x0, min(ys) - 2, min(zs) - 2, x1, max(ys) + 2, max(zs) + 2),
                             mat, **kw))

    def bounds_hint(self, x0, y0, z0, x1, y1, z1):
        self.extra_bounds.append((x0, y0, z0, x1, y1, z1))


# ---------------------------------------------------------------------------
# Rendu
# ---------------------------------------------------------------------------
class _WPrim:
    """Primitive transformee en espace monde."""
    pass


def _world_prims(model, facing):
    R, T, span = facing_xform(facing, model.W, model.D)
    swap = abs(R[0, 0]) < 0.5   # les axes x/y sont echanges
    out = []
    for p in model.prims:
        w = _WPrim(); w.src = p; w.kind = p.kind; w.R = R; w.T = T
        if p.kind == 'poly':
            N = []; Dd = []; tags = []
            for (nx, ny, nz, d, tag) in p.planes:
                nu, nv = R @ np.array([nx, ny], float)
                N.append((nu, nv, nz)); Dd.append(d + nu * T[0] + nv * T[1]); tags.append(tag)
            w.N = np.array(N, float); w.Dd = np.array(Dd, float); w.tags = tags
            # boite englobante monde (sommets de la bbox locale)
            x0, y0, z0, x1, y1, z1 = p.bbox
            pts = [R @ np.array([x, y], float) + T for x in (x0, x1) for y in (y0, y1)]
            us = [q[0] for q in pts]; vs = [q[1] for q in pts]
            w.aabb = (min(us), max(us), min(vs), max(vs), z0, z1)
        elif p.kind == 'cyl':
            cu, cv = R @ np.array([p.cx, p.cy], float) + T
            ru, rv = (p.ry, p.rx) if swap else (p.rx, p.ry)
            w.cu, w.cv, w.ru, w.rv, w.z0, w.z1 = cu, cv, ru, rv, p.z0, p.z1
            w.aabb = (cu - ru, cu + ru, cv - rv, cv + rv, p.z0, p.z1)
            w.tags = ['top', 'side']
        elif p.kind == 'ell':
            cu, cv = R @ np.array([p.cx, p.cy], float) + T
            ru, rv = (p.ry, p.rx) if swap else (p.rx, p.ry)
            w.cu, w.cv, w.cz, w.ru, w.rv, w.rz = cu, cv, p.cz, ru, rv, p.rz
            w.zc0, w.zc1 = p.zclip
            w.aabb = (cu - ru, cu + ru, cv - rv, cv + rv, max(p.cz - p.rz, w.zc0), min(p.cz + p.rz, w.zc1))
            w.tags = ['surface', 'cap']
        elif p.kind == 'hcyl':
            if p.axis == 'x':
                c_pt = R @ np.array([0.0, p.c], float) + T
                ax_vec = R @ np.array([1.0, 0.0])
            else:
                c_pt = R @ np.array([p.c, 0.0], float) + T
                ax_vec = R @ np.array([0.0, 1.0])
            if abs(ax_vec[0]) > 0.5:   # axe monde = u
                w.waxis = 'u'; w.c = c_pt[1]
                sgn = ax_vec[0]
                e0 = (R @ np.array([p.a0, 0.0]) + T)[0] if p.axis == 'x' else (R @ np.array([0.0, p.a0]) + T)[0]
                e1 = (R @ np.array([p.a1, 0.0]) + T)[0] if p.axis == 'x' else (R @ np.array([0.0, p.a1]) + T)[0]
            else:
                w.waxis = 'v'; w.c = c_pt[0]
                e0 = (R @ np.array([p.a0, 0.0]) + T)[1] if p.axis == 'x' else (R @ np.array([0.0, p.a0]) + T)[1]
                e1 = (R @ np.array([p.a1, 0.0]) + T)[1] if p.axis == 'x' else (R @ np.array([0.0, p.a1]) + T)[1]
            w.e0, w.e1 = min(e0, e1), max(e0, e1)
            w.cz, w.r, w.rz = p.cz, p.r, p.rz
            if w.waxis == 'u':
                w.aabb = (w.e0, w.e1, w.c - p.r, w.c + p.r, p.cz - p.rz, p.cz + p.rz)
            else:
                w.aabb = (w.c - p.r, w.c + p.r, w.e0, w.e1, p.cz - p.rz, p.cz + p.rz)
            w.tags = ['side', 'end']
        out.append(w)
    return out, R, T, span


def _hit(w, a, b):
    """Retourne (t, face_idx, nu, nv, nz) ; t = -inf si pas d'intersection."""
    shp = a.shape
    NEG = -np.inf
    if w.kind == 'poly':
        N = w.N; c = N[:, 0] + N[:, 1] + N[:, 2]
        t_lo = np.full(shp, NEG); t_hi = np.full(shp, np.inf)
        fidx = np.full(shp, -1, np.int32)
        ok = np.ones(shp, bool)
        for k in range(len(c)):
            rhs = w.Dd[k] - N[k, 0] * a - N[k, 1] * b
            if c[k] > 1e-9:
                bound = rhs / c[k]
                upd = bound < t_hi
                t_hi = np.where(upd, bound, t_hi)
                fidx = np.where(upd, k, fidx)
            elif c[k] < -1e-9:
                bound = rhs / c[k]
                t_lo = np.maximum(t_lo, bound)
            else:
                ok &= rhs > 0
        hit = ok & (t_lo < t_hi) & (fidx >= 0)
        t = np.where(hit, t_hi, NEG)
        nrm = np.zeros((3,) + shp)
        for k in range(len(c)):
            m = fidx == k
            n = N[k] / (np.linalg.norm(N[k]) + 1e-12)
            for j in range(3):
                nrm[j][m] = n[j]
        return t, fidx, nrm
    if w.kind == 'cyl':
        p = a - w.cu; q = b - w.cv
        iu = 1.0 / w.ru ** 2; iv = 1.0 / w.rv ** 2
        A = iu + iv; B = 2 * (p * iu + q * iv); C = p * p * iu + q * q * iv - 1
        disc = B * B - 4 * A * C
        ok = disc >= 0
        sq = np.sqrt(np.where(ok, disc, 0))
        t1 = (-B - sq) / (2 * A); t2 = (-B + sq) / (2 * A)
        lo = np.maximum(t1, w.z0); hi = np.minimum(t2, w.z1)
        hit = ok & (lo < hi)
        t = np.where(hit, hi, NEG)
        top = hit & (w.z1 <= t2)
        fidx = np.where(top, 0, 1).astype(np.int32)
        du = (p + t) * iu; dv = (q + t) * iv
        ln = np.sqrt(du * du + dv * dv) + 1e-12
        nrm = np.zeros((3,) + shp)
        nrm[0] = np.where(top, 0, du / ln); nrm[1] = np.where(top, 0, dv / ln); nrm[2] = np.where(top, 1, 0)
        return t, fidx, nrm
    if w.kind == 'ell':
        p = a - w.cu; q = b - w.cv
        iu = 1.0 / w.ru ** 2; iv = 1.0 / w.rv ** 2; iz = 1.0 / w.rz ** 2
        A = iu + iv + iz
        B = 2 * (p * iu + q * iv - w.cz * iz)
        C = p * p * iu + q * q * iv + w.cz * w.cz * iz - 1
        disc = B * B - 4 * A * C
        ok = disc >= 0
        sq = np.sqrt(np.where(ok, disc, 0))
        t1 = (-B - sq) / (2 * A); t2 = (-B + sq) / (2 * A)
        lo = np.maximum(t1, w.zc0); hi = np.minimum(t2, w.zc1)
        hit = ok & (lo < hi)
        t = np.where(hit, hi, NEG)
        cap = hit & (w.zc1 <= t2)
        fidx = np.where(cap, 1, 0).astype(np.int32)
        du = (p + t) * iu; dv = (q + t) * iv; dz = (t - w.cz) * iz
        ln = np.sqrt(du * du + dv * dv + dz * dz) + 1e-12
        nrm = np.zeros((3,) + shp)
        nrm[0] = np.where(cap, 0, du / ln); nrm[1] = np.where(cap, 0, dv / ln); nrm[2] = np.where(cap, 1, dz / ln)
        return t, fidx, nrm
    if w.kind == 'hcyl':
        ir = 1.0 / w.r ** 2; iz = 1.0 / w.rz ** 2
        if w.waxis == 'u':
            q = b - w.c; along = a
        else:
            q = a - w.c; along = b
        A = ir + iz; B = 2 * (q * ir - w.cz * iz); C = q * q * ir + w.cz * w.cz * iz - 1
        disc = B * B - 4 * A * C
        ok = disc >= 0
        sq = np.sqrt(np.where(ok, disc, 0))
        t1 = (-B - sq) / (2 * A); t2 = (-B + sq) / (2 * A)
        lo = np.maximum(t1, w.e0 - along); hi = np.minimum(t2, w.e1 - along)
        hit = ok & (lo < hi)
        t = np.where(hit, hi, NEG)
        end = hit & ((w.e1 - along) <= t2)
        fidx = np.where(end, 1, 0).astype(np.int32)
        dq = (q + t) * ir; dz = (t - w.cz) * iz
        ln = np.sqrt(dq * dq + dz * dz) + 1e-12
        nrm = np.zeros((3,) + shp)
        if w.waxis == 'u':
            nrm[0] = np.where(end, 1, 0); nrm[1] = np.where(end, 0, dq / ln)
        else:
            nrm[1] = np.where(end, 1, 0); nrm[0] = np.where(end, 0, dq / ln)
        nrm[2] = np.where(end, 0, dz / ln)
        return t, fidx, nrm
    raise ValueError(w.kind)


def _inside(w, u, v, z):
    if w.kind == 'poly':
        ok = np.ones(u.shape, bool)
        for k in range(len(w.Dd)):
            ok &= (w.N[k, 0] * u + w.N[k, 1] * v + w.N[k, 2] * z) <= w.Dd[k]
        return ok
    if w.kind == 'cyl':
        return (((u - w.cu) / w.ru) ** 2 + ((v - w.cv) / w.rv) ** 2 <= 1) & (z >= w.z0) & (z <= w.z1)
    if w.kind == 'ell':
        return ((((u - w.cu) / w.ru) ** 2 + ((v - w.cv) / w.rv) ** 2 + ((z - w.cz) / w.rz) ** 2) <= 1) \
            & (z >= w.zc0) & (z <= w.zc1)
    if w.kind == 'hcyl':
        if w.waxis == 'u':
            q = v - w.c; al = u
        else:
            q = u - w.c; al = v
        return ((q / w.r) ** 2 + ((z - w.cz) / w.rz) ** 2 <= 1) & (al >= w.e0) & (al <= w.e1)
    raise ValueError


def shade_index(nu, nv, nz):
    d = nu * LIGHT[0] + nv * LIGHT[1] + nz * LIGHT[2]
    idx = np.full(d.shape, 1, np.int16)
    idx[d >= -0.30] = 2
    idx[d >= 0.15] = 3
    idx[d >= 0.43] = 4
    return idx


def _face_coords(src, tag, xl, yl, zl):
    """Coordonnees (s, t) dans le repere de face (decals)."""
    x0, y0, z0, x1, y1, z1 = src.bbox
    if tag == 'front':
        return xl - x0, zl - z0
    if tag == 'back':
        return x1 - xl, zl - z0
    if tag == 'right':
        return yl - y0, zl - z0
    if tag == 'left':
        return y1 - yl, zl - z0
    if tag in ('top', 'cap', 'bottom'):
        return xl - x0, yl - y0
    if tag in ('side', 'surface', 'end'):
        if src.kind == 'cyl':
            ang = np.degrees(np.arctan2(xl - src.cx, -(yl - src.cy))) % 360
            return ang, zl - z0
        return xl - x0, zl - z0
    return xl - x0, zl - z0


class Layer:
    def __init__(self, h, w):
        self.rgba = np.zeros((h, w, 4), np.uint8)
        self.mask = np.zeros((h, w, 4), np.uint8)
        self.pid = np.full((h, w), -1, np.int32)


def _render_layer(wprims, sel, a, b, model, R, T):
    h, w = a.shape
    tbuf = np.full((h, w), -np.inf)
    pid = np.full((h, w), -1, np.int32)
    fid = np.full((h, w), -1, np.int32)
    nrm = np.zeros((3, h, w))
    for i in sel:
        t, f, n = _hit(wprims[i], a, b)
        if model.clip is not None:
            u0, u1, v0, v1 = model.clip
            uh = a + t; vh = b + t
            t = np.where((uh >= u0) & (uh < u1) & (vh >= v0) & (vh < v1), t, -np.inf)
        upd = t > tbuf
        tbuf = np.where(upd, t, tbuf)
        pid = np.where(upd, i, pid)
        fid = np.where(upd, f, fid)
        for j in range(3):
            nrm[j] = np.where(upd, n[j], nrm[j])
    occ = pid >= 0
    idx = shade_index(nrm[0], nrm[1], nrm[2])
    # canal / rampe / couleur fixe par pixel
    ch = np.zeros((h, w), np.int16)
    ramp_id = np.full((h, w), -1, np.int32)
    fixed = np.zeros((h, w, 4), np.uint8)
    isfixed = np.zeros((h, w), bool)
    flags = np.zeros((h, w), np.uint8)
    alpha = np.full((h, w), 255, np.uint8)
    noedge = np.zeros((h, w), bool)
    ramp_names = list(RAMPS.keys())
    rindex = {n: k for k, n in enumerate(ramp_names)}
    uh = a + tbuf; vh = b + tbuf; zh = tbuf
    # coordonnees locales
    Rt = R.T
    xl = Rt[0, 0] * (uh - T[0]) + Rt[0, 1] * (vh - T[1])
    yl = Rt[1, 0] * (uh - T[0]) + Rt[1, 1] * (vh - T[1])
    for i in sel:
        wp = wprims[i]; src = wp.src
        pm = pid == i
        if not pm.any():
            continue
        if src.flag_faces is None:
            flags[pm] = src.flags
        alpha[pm] = src.alpha
        if not src.edges:
            noedge[pm] = True
        for k, tag in enumerate(wp.tags):
            fm = pm & (fid == k)
            if not fm.any():
                continue
            if src.flag_faces is not None and tag in src.flag_faces:
                flags[fm] = src.flags
            mat = src.mat_for(tag)
            if mat is None:
                mat = src.mat_for('default')
            if isinstance(mat, Fix):
                fixed[fm] = mat.color; isfixed[fm] = True
            else:
                ch[fm] = mat.ch; ramp_id[fm] = rindex[mat.ramp]
            # decals
            decs = [d for d in src.decals if d[0] == tag or (d[0] == 'sides' and tag in ('front', 'back', 'left', 'right'))]
            if not decs:
                continue
            ys_, xs_ = np.nonzero(fm)
            s, tt = _face_coords(src, tag, xl[ys_, xs_], yl[ys_, xs_], zh[ys_, xs_])
            base_idx = idx[ys_, xs_].copy()
            for (_, shape, paint) in decs:
                m = _shape_mask(shape, s, tt)
                if not m.any():
                    continue
                yy, xx = ys_[m], xs_[m]
                if isinstance(paint, Delta):
                    idx[yy, xx] = np.clip(base_idx[m] + paint.d, 0, 5)
                elif isinstance(paint, Use):
                    if isinstance(paint.mat, Fix):
                        fixed[yy, xx] = paint.mat.color; isfixed[yy, xx] = True
                    else:
                        isfixed[yy, xx] = False
                        ch[yy, xx] = paint.mat.ch; ramp_id[yy, xx] = rindex[paint.mat.ramp]
                        if paint.idx is not None:
                            idx[yy, xx] = paint.idx
                        else:
                            idx[yy, xx] = np.clip(base_idx[m] + paint.rel, 0, 5)
                    noedge[yy, xx] = True
                elif isinstance(paint, Fix):
                    fixed[yy, xx] = paint.color; isfixed[yy, xx] = True
                    noedge[yy, xx] = True
    # aretes eclairees (meme primitive)
    if model.edge_hl:
        top_like = occ & (nrm[2] > 0.7)
        below_pid = np.full((h, w), -2, np.int32); below_pid[:-1] = pid[1:]
        below_top = np.zeros((h, w), bool); below_top[:-1] = top_like[1:]
        m = top_like & (below_pid == pid) & ~below_top & ~noedge
        idx[m] = np.minimum(idx[m] + 1, 5)
        left_like = occ & (nrm[1] > 0.7)
        right_pid = np.full((h, w), -2, np.int32); right_pid[:, :-1] = pid[:, 1:]
        right_is_u = np.zeros((h, w), bool); right_is_u[:, :-1] = (nrm[0] > 0.7)[:, 1:]
        m = left_like & (right_pid == pid) & right_is_u & ~noedge
        idx[m] = np.minimum(idx[m] + 1, 5)
    # lignes internes : pixel en retrait d'une autre primitive
    outl = np.array([wprims[i].src.outline if i >= 0 else False for i in range(len(wprims))] + [False])
    line = np.zeros((h, w), bool)
    for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
        npid = np.full((h, w), -1, np.int32); nt = np.full((h, w), -np.inf)
        ys0, ys1 = max(0, -dy), h - max(0, dy)
        xs0, xs1 = max(0, -dx), w - max(0, dx)
        npid[ys0:ys1, xs0:xs1] = pid[ys0 + dy:ys1 + dy, xs0 + dx:xs1 + dx]
        nt[ys0:ys1, xs0:xs1] = tbuf[ys0 + dy:ys1 + dy, xs0 + dx:xs1 + dx]
        front_ok = np.where(npid >= 0, outl[np.clip(npid, 0, None)], False)
        line |= occ & (npid >= 0) & (npid != pid) & (nt - tbuf > DEPTH_GAP) & front_ok
    idx[line & ~isfixed] = np.maximum(idx[line & ~isfixed] - 2, 0)
    # composition des couleurs
    lay = Layer(h, w)
    lay.pid = pid
    rgba = lay.rgba; mask = lay.mask
    for rn, k in rindex.items():
        m = occ & (ramp_id == k) & ~isfixed
        if not m.any():
            continue
        pal = np.array(RAMPS[rn], np.uint8)
        rgba[m] = pal[np.clip(idx[m], 0, 5)]
    rgba[isfixed & occ] = fixed[isfixed & occ]
    rgba[..., 3] = np.where(occ, np.where(isfixed, fixed[..., 3], alpha), 0)
    recol = occ & ~isfixed & (ch > 0)
    mask[recol, 0] = ch[recol]
    mask[recol, 1] = np.clip(idx[recol], 0, 5)
    mask[recol, 2] = flags[recol]
    mask[recol, 3] = 255
    # contour exterieur
    if model.outline == 'full':
        opaque_ol = occ & np.where(pid >= 0, outl[np.clip(pid, 0, None)], False)
        ring = np.zeros((h, w), bool)
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            sh = np.zeros((h, w), bool)
            ys0, ys1 = max(0, -dy), h - max(0, dy)
            xs0, xs1 = max(0, -dx), w - max(0, dx)
            sh[ys0:ys1, xs0:xs1] = opaque_ol[ys0 + dy:ys1 + dy, xs0 + dx:xs1 + dx]
            ring |= sh
        ring &= ~occ
        rgba[ring] = FIXED['ink']
        lay.pid = np.where(ring, -3, pid)
    return lay, tbuf


def _bounds(wprims, model, facing):
    sxs = []; sys_ = []
    k = model.shadow_k
    for w in wprims:
        u0, u1, v0, v1, z0, z1 = w.aabb
        for u in (u0, u1):
            for v in (v0, v1):
                for z in (z0, z1):
                    sxs.append(u - v); sys_.append((u + v) / 2 - z)
                    if model.shadow and w.src.shadow:
                        us = u - SHADOW_DIR[0] * k * z; vs = v - SHADOW_DIR[1] * k * z
                        sxs.append(us - vs); sys_.append((us + vs) / 2)
    R, T, _ = facing_xform(facing, model.W, model.D)
    for (x0, y0, z0, x1, y1, z1) in model.extra_bounds:
        for x in (x0, x1):
            for y in (y0, y1):
                u, v = R @ np.array([x, y], float) + T
                for z in (z0, z1):
                    sxs.append(u - v); sys_.append((u + v) / 2 - z)
    return min(sxs), max(sxs), min(sys_), max(sys_)


class RenderResult:
    pass


BACK_FACINGS = ('nw', 'ne')
FRONT_IF_BACK = {'nw': 'front', 'ne': 'front', 'default': 'base'}


def layer_of(p, facing):
    """Couche d'une primitive ; un dict permet une couche par orientation
    (ex. dossier de chaise au premier plan quand l'assise tourne le dos a la camera).
    Les primitives translucides (alpha < 255) d'une couche 'base' vont dans la couche 'glass',
    composee par-dessus le contenu opaque (vitrine, enceinte d'imprimante, cloison vitree)."""
    if isinstance(p.layer, dict):
        ln = p.layer.get(facing, p.layer.get('default', 'base'))
    else:
        ln = p.layer
    if ln == 'base' and getattr(p, 'alpha', 255) < 255:
        return 'glass'
    return ln


def render(model, facing, pad=2, layers=('base', 'front')):
    with np.errstate(invalid='ignore', over='ignore'):
        return _render(model, facing, pad)


def _render(model, facing, pad=2):
    wprims, R, T, span = _world_prims(model, facing)
    mnx, mxx, mny, mxy = _bounds(wprims, model, facing)
    X0 = int(math.floor(mnx)) - pad; X1 = int(math.ceil(mxx)) + pad
    Y0 = int(math.floor(mny)) - pad; Y1 = int(math.ceil(mxy)) + pad
    W = X1 - X0; H = Y1 - Y0
    sx = np.arange(W) + 0.5 + X0
    sy = np.arange(H) + 0.5 + Y0
    SX, SY = np.meshgrid(sx, sy)
    a = SX / 2 + SY
    b = SY - SX / 2
    res = RenderResult()
    res.size = (W, H)
    res.anchor = (-X0, -Y0)
    res.facing = facing
    res.span = span
    res.layers = {}
    res.depth = {}
    present = sorted(set(layer_of(p, facing) for p in model.prims), key=lambda n: (n == 'glass', n))
    for ln in present:
        sel = [i for i, w in enumerate(wprims) if layer_of(w.src, facing) == ln]
        lay, tb = _render_layer(wprims, sel, a, b, model, R, T)
        if ln == 'glass' and 'base' in res.depth:
            # le verre n'est visible que la ou il est devant la geometrie opaque
            hidden = (res.depth['base'] >= tb) & (res.layers['base'].pid >= 0)
            lay.rgba[hidden] = 0
            lay.mask[hidden] = 0
        res.layers[ln] = lay
        res.depth[ln] = tb
    # ombre
    sh = np.zeros((H, W, 4), np.uint8)
    if model.shadow:
        casters = [w for w in wprims if w.src.shadow]
        if casters:
            zmax = max(w.aabb[5] for w in casters)
            k = model.shadow_k
            inside = np.zeros((H, W), bool)
            u0 = a.copy(); v0 = b.copy()
            s = 0.5
            while s <= zmax + 0.5:
                uu = u0 + SHADOW_DIR[0] * k * s
                vv = v0 + SHADOW_DIR[1] * k * s
                zz = np.full(a.shape, s)
                for w in casters:
                    inside |= _inside(w, uu, vv, zz)
                s += 1.0
            sh[inside] = FIXED['shadow']
    res.shadow = sh
    res.wprims = wprims
    res.R, res.T = R, T
    res.a, res.b = a, b
    res.origin = (X0, Y0)
    return res


def project_local(model, facing, x, y, z):
    """Position ecran (px, relative a l'ancre) d'un point local."""
    u, v, zz = to_world(facing, model.W, model.D, x, y, z)
    return screen_of(u, v, zz)
