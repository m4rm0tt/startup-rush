"""Registre des assets isometriques et export (PNG, masques, ombres, manifeste).

Chaque fichier sources/defs_*.py declare ses assets avec le decorateur
@iso_asset. Le build rend chaque orientation, ecrit les fichiers et produit une
entree de manifeste complete (ancre, emprise, collision, sockets, ecrans...).
"""
import os
import math
import numpy as np

from .iso import Model, render, facing_xform, screen_of, TILE, FACINGS
from .imgio import save_png
from .palette import CHANNEL_NAMES, Mat

PACK_VERSION = '1.0.0'
REGISTRY = []
ALL4 = ('se', 'sw', 'nw', 'ne')
FRONT2 = ('se', 'sw')
ONE = ('se',)


class Socket:
    """Point d'accroche local (x, y en su ; z en px) acceptant certains types."""
    def __init__(self, name, accepts, x, y, z, note='', facing_rel='same', group=None):
        self.name, self.accepts, self.x, self.y, self.z, self.note = name, accepts, x, y, z, note
        self.facing_rel = facing_rel   # 'same' : l'enfant prend l'orientation du parent ; 'opposite'
        self.group = group             # ordre de dessin relatif au parent (voir INTEGRATION_GUIDE)


class Access:
    """Case d'acces/occupation locale (cx, cy en cases, peut etre hors emprise)."""
    def __init__(self, kind, cx, cy, facing_rel='back', note=''):
        # facing_rel : 'back' = l'utilisateur regarde l'objet depuis l'avant
        self.kind, self.cx, self.cy, self.facing_rel, self.note = kind, cx, cy, facing_rel, note


class AssetDef:
    def __init__(self, id, family, category, name_fr, builder, footprint=(1, 1), facings=FRONT2,
                 usage='current', kind='floor', centered=False, size_su=None, height=None,
                 sockets=(), provides=(), access=(), occupant=None, collision=None,
                 walkable=False, screens=(), notes='', tags=(), slots=None, symmetric=False,
                 occludes=None, wall_mount=False, ports=None, layer='objects', silhouette=None,
                 model_kw=None, extra=None):
        self.id = id; self.family = family; self.category = category; self.name_fr = name_fr
        self.builder = builder; self.footprint = footprint; self.facings = facings
        self.usage = usage; self.kind = kind; self.centered = centered; self.size_su = size_su
        self.height = height; self.sockets = list(sockets); self.provides = list(provides)
        self.access = list(access); self.occupant = occupant; self.collision = collision
        self.walkable = walkable; self.screens = list(screens); self.notes = notes
        self.tags = list(tags); self.slots = slots; self.symmetric = symmetric
        self.occludes = occludes; self.wall_mount = wall_mount; self.ports = ports
        self.layer = layer; self.silhouette = silhouette or id
        self.model_kw = model_kw or {}
        self.extra = extra or {}

    def make_model(self):
        if self.centered:
            w, d = self.size_su or (16, 16)
            m = Model(w, d, centered=True, **self.model_kw)
        else:
            fw, fd = self.footprint
            m = Model(fw * TILE, fd * TILE, **self.model_kw)
        if self.kind in ('wall', 'ceiling') or self.wall_mount:
            m.shadow = False   # pas d'ombre au sol pour les objets muraux / suspendus
        self.builder(m)
        self.fixed_parts = normalize_channels(m)
        return m


def _prim_weight(p):
    try:
        x0, y0, z0, x1, y1, z1 = p.bbox
        return max(0.5, abs(x1 - x0) * abs(y1 - y0) + abs(x1 - x0) * abs(z1 - z0) + abs(y1 - y0) * abs(z1 - z0))
    except Exception:
        return 1.0


def normalize_channels(model):
    """Garantit UNE rampe par canal de recoloration et par asset.
    Pour chaque canal utilise avec plusieurs rampes, la rampe dominante (surface des primitives) garde le
    canal ; les autres parties passent en canal 'fixed' (meme couleur, non recolorable). Retourne la liste
    des remappages {canal, rampe_conservee, rampes_fixees}."""
    from .iso import Use
    weights = {}
    for p in model.prims:
        w = _prim_weight(p)
        mats = list(p.mat.values()) if isinstance(p.mat, dict) else [p.mat]
        for mt in mats:
            if isinstance(mt, Mat) and mt.channel != 'fixed':
                weights.setdefault(mt.channel, {}).setdefault(mt.ramp, 0.0)
                weights[mt.channel][mt.ramp] += w
        for (_, _, paint) in p.decals:
            mt = getattr(paint, 'mat', None)
            if isinstance(mt, Mat) and mt.channel != 'fixed':
                weights.setdefault(mt.channel, {}).setdefault(mt.ramp, 0.0)
                weights[mt.channel][mt.ramp] += 0.25
    keep = {}
    report = []
    for ch, rw in weights.items():
        if len(rw) > 1:
            main = max(rw.items(), key=lambda kv: kv[1])[0]
            keep[ch] = main
            report.append({'channel': ch, 'kept': main, 'fixed': sorted(r for r in rw if r != main)})
    if not keep:
        return []
    cache = {}

    def fix(mt):
        if isinstance(mt, Mat) and mt.channel in keep and mt.ramp != keep[mt.channel]:
            key = mt.ramp
            if key not in cache:
                cache[key] = Mat('fixed', mt.ramp)
            return cache[key]
        return mt
    for p in model.prims:
        if isinstance(p.mat, dict):
            p.mat = {k: fix(v) for k, v in p.mat.items()}
        else:
            p.mat = fix(p.mat)
        for (face, shape, paint) in p.decals:
            if isinstance(paint, Use) and isinstance(paint.mat, Mat):
                paint.mat = fix(paint.mat)
    return report


def iso_asset(id, family, category, name_fr, **kw):
    def deco(fn):
        a = AssetDef(id, family, category, name_fr, fn, **kw)
        assert all(x.id != id for x in REGISTRY), f"ID en double : {id}"
        REGISTRY.append(a)
        return fn
    return deco


def register(a):
    assert all(x.id != a.id for x in REGISTRY), f"ID en double : {a.id}"
    REGISTRY.append(a)
    return a


# ---------------------------------------------------------------------------
# Outils de conversion local -> monde (cases) -> ecran
# ---------------------------------------------------------------------------
def cell_world(facing, W, D, cx, cy):
    """Case locale (cx, cy) -> case monde (U, V) relative a l'emprise."""
    R, T, _ = facing_xform(facing, W, D)
    # centre de case local
    x = (cx + 0.5) * TILE; y = (cy + 0.5) * TILE
    u, v = R @ np.array([x, y], float) + T
    return int(math.floor(u / TILE)), int(math.floor(v / TILE))


CYCLE = ('se', 'sw', 'nw', 'ne')   # +U -> +V -> -U -> -V


def rotate_facing(facing, rel):
    """rel : 'same', 'opposite', 'cw' (suivant dans se->sw->nw->ne), 'ccw'."""
    if facing not in CYCLE:
        return facing
    k = CYCLE.index(facing)
    return CYCLE[(k + {'same': 0, 'cw': 1, 'opposite': 2, 'ccw': 3}[rel]) % 4]


def facing_after(facing, rel):
    """Orientation de l'utilisateur d'un objet. rel='back' : il fait face a l'avant de l'objet."""
    opp = {'se': 'nw', 'nw': 'se', 'sw': 'ne', 'ne': 'sw'}
    if rel == 'back':
        return opp[facing]
    if rel == 'same':
        return facing
    return rel


def selection_poly(span, height):
    """Hexagone ecran (px relatifs a l'ancre) du volume englobant u in[0,su] v in[0,sv] z in[0,h]."""
    su, sv = span
    pts = [screen_of(0, 0, height), screen_of(su, 0, height), screen_of(su, 0, 0),
           screen_of(su, sv, 0), screen_of(0, sv, 0), screen_of(0, sv, height)]
    return [[int(round(x)), int(round(y))] for x, y in pts]


def _layer_files(root, family, aid, facing, layer):
    base = f"{aid}__{facing}" + ('' if layer == 'base' else f"__{layer}")
    png = f"assets/{family}/{base}.png"
    msk = f"masks/{family}/{base}__mask.png"
    return png, msk


def _channels_used(mask):
    ch = mask[..., 0][mask[..., 3] > 0]
    return sorted(set(int(c) for c in np.unique(ch))) if ch.size else []


def build_iso_asset(a, root, log=None):
    """Rend toutes les orientations d'un asset et retourne l'entree de manifeste."""
    entry = {
        'id': a.id, 'category': a.family, 'subcategory': a.category, 'name_fr': a.name_fr,
        'pack_version': PACK_VERSION, 'usage': a.usage, 'kind': a.kind,
        'silhouette_group': a.silhouette, 'render_layer': a.layer,
        'footprint_tiles': list(a.footprint) if not a.centered else None,
        'size_su': list(a.size_su) if a.centered and a.size_su else None,
        'height_px': a.height, 'walkable': a.walkable, 'symmetric': a.symmetric,
        'wall_mount': a.wall_mount, 'tags': a.tags, 'notes': a.notes,
        'provides': a.provides, 'orientations': {}, 'channels': {},
    }
    if a.extra:
        entry.update(a.extra)
    model = a.make_model()
    chan_defaults = {}
    for p in model.prims:
        mats = p.mat.values() if isinstance(p.mat, dict) else [p.mat]
        for mt in mats:
            if hasattr(mt, 'channel') and mt.channel != 'fixed':
                chan_defaults.setdefault(mt.channel, mt.ramp)
        for (_, _, paint) in p.decals:
            mt = getattr(paint, 'mat', None)
            if hasattr(mt, 'channel') and mt.channel != 'fixed':
                chan_defaults.setdefault(mt.channel, mt.ramp)
    for f in a.facings:
        res = render(model, f)
        o = {'size': list(res.size), 'anchor': list(res.anchor), 'layers': {}}
        for ln, lay in res.layers.items():
            png, msk = _layer_files(root, a.family, a.id, f, ln)
            save_png(os.path.join(root, png), lay.rgba)
            L = {'file': png}
            if (lay.mask[..., 3] > 0).any():
                save_png(os.path.join(root, msk), lay.mask)
                L['mask'] = msk
                L['mask_channels'] = [CHANNEL_NAMES[c] for c in _channels_used(lay.mask)]
            o['layers'][ln] = L
        if (res.shadow[..., 3] > 0).any():
            png = f"assets/{a.family}/{a.id}__{f}__shadow.png"
            save_png(os.path.join(root, png), res.shadow)
            o['layers']['shadow'] = {'file': png, 'blend': 'shadow_layer'}
        W, D = model.W, model.D
        if not a.centered:
            su, sv = res.span
            o['footprint_world_tiles'] = [int(su // TILE), int(sv // TILE)]
            fw, fd = a.footprint
            coll = a.collision if a.collision is not None else [(x, y) for x in range(fw) for y in range(fd)]
            o['collision_tiles'] = sorted([list(cell_world(f, W, D, cx, cy)) for (cx, cy) in coll])
            o['access'] = [{'kind': ac.kind, 'tile': list(cell_world(f, W, D, ac.cx, ac.cy)),
                            'user_facing': facing_after(f, ac.facing_rel), 'note': ac.note} for ac in a.access]
            if a.height:
                o['selection_poly'] = selection_poly(res.span, a.height)
        socks = {}
        for s in a.sockets:
            sx, sy = _proj(f, W, D, s.x, s.y, s.z)
            u, v, _ = _world(f, W, D, s.x, s.y)
            socks[s.name] = {'offset_px': [sx, sy], 'world_su': [round(u, 2), round(v, 2), s.z],
                             'accepts': s.accepts, 'child_facing': rotate_facing(f, s.facing_rel),
                             'draw': s.group or 'after_parent', 'note': s.note}
        if socks:
            o['sockets'] = socks
        if a.occupant:
            ox, oy, oz, rel = a.occupant
            sx, sy = _proj(f, W, D, ox, oy, oz)
            o['occupant'] = {'seat_offset_px': [sx, sy], 'character_orientation': facing_after(f, rel),
                             'world_su': list(_world(f, W, D, ox, oy)[:2]) + [oz]}
        scr = []
        for q in a.screens:
            pts = [list(_proj(f, W, D, x, y, z)) for (x, y, z) in q['quad']]
            visible = _quad_visible(pts)
            scr.append({'name': q.get('name', 'screen'), 'quad_px': pts, 'visible': visible,
                        'note': 'coins : haut-gauche, haut-droit, bas-droit, bas-gauche (vus de face)'})
        if scr:
            o['screens'] = scr
        if a.ports:
            o['ports'] = [{'name': pt['name'], 'dir': pt['dir'], 'kind': pt['kind'],
                           'tile': list(cell_world(f, W, D, pt['cx'], pt['cy'])),
                           'world_dir': _dir_world(f, pt['dir'])} for pt in a.ports]
        if a.slots:
            o['slots'] = _slots_for(a, f, W, D)
        if a.occludes:
            o['occlusion'] = {'occludes_behind': True, 'fade_above_px': a.occludes}
        entry['orientations'][f] = o
        if log:
            log(f"  {a.id} {f} {res.size}")
    entry['channels'] = chan_defaults
    if getattr(a, 'fixed_parts', None):
        entry['fixed_parts'] = a.fixed_parts   # parties secondaires rendues non recolorables (1 rampe par canal)
    entry['sockets_def'] = [{'name': s.name, 'accepts': s.accepts, 'local': [s.x, s.y, s.z],
                             'facing_rel': s.facing_rel, 'note': s.note} for s in a.sockets]
    return entry


def _world(f, W, D, x, y):
    R, T, _ = facing_xform(f, W, D)
    u, v = R @ np.array([x, y], float) + T
    return float(u), float(v), 0.0


def _proj(f, W, D, x, y, z):
    u, v, _ = _world(f, W, D, x, y)
    sx, sy = screen_of(u, v, z)
    return int(math.floor(sx + 0.5)), int(math.floor(sy + 0.5))


def _quad_visible(pts):
    # visible si les sommets (vus de face) tournent dans le sens horaire a l'ecran
    a = 0.0
    for i in range(4):
        x1, y1 = pts[i]; x2, y2 = pts[(i + 1) % 4]
        a += x1 * y2 - x2 * y1
    return a > 0


def _dir_world(f, d):
    # d : direction locale 'front','back','left','right' -> direction monde (+U,-U,+V,-V)
    vec = {'front': (0, -1), 'back': (0, 1), 'left': (-1, 0), 'right': (1, 0)}[d]
    R, _, _ = facing_xform(f, 0, 0)
    u, v = R @ np.array(vec, float)
    if abs(u) > 0.5:
        return '+U' if u > 0 else '-U'
    return '+V' if v > 0 else '-V'


def _slots_for(a, f, W, D):
    s = a.slots
    out = {'unit_px': s['unit_px'], 'count': s['count'], 'face': s.get('face', 'front'),
           'accepts': s['accepts'], 'positions': []}
    x0, y_face, z_top = s['x0'], s['y_face'], s['z_top']
    for i in range(s['count']):
        z = z_top - i * s['unit_px']
        sx, sy = _proj(f, W, D, x0, y_face, z)
        out['positions'].append({'index': i, 'offset_px': [sx, sy]})
    out['visible'] = f in ('se', 'sw')
    return out


# ---------------------------------------------------------------------------
# Constructeurs specialises (murs en tranches, sols, personnages, HUD...)
# ---------------------------------------------------------------------------
CUSTOM = []


def custom_builder(name, order=50):
    """Enregistre une fonction build(root, log) -> liste d'entrees de manifeste."""
    def deco(fn):
        CUSTOM.append((order, name, fn))
        return fn
    return deco
