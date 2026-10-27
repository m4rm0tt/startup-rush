"""Compositeur de reference (a porter dans le jeu) : sols, murs, objets,
enfants poses dans des sockets, postes de travail avec personnage assis,
couche d'ombre unique. Utilise uniquement le manifeste et les PNG livres.

Ordre de rendu (voir INTEGRATION_GUIDE.md) :
  1. sols   2. bords de dalle + marquages   3. couche d'ombre (opacite 0.30)
  4. murs (angle, segments du fond vers l'avant, terminaisons) + objets muraux
  5. objets au sol tries par profondeur (tri topologique des emprises)
     dans chaque groupe : enfants 'before_parent' -> parent -> enfants tries par
     profondeur monde ; poste de travail : chaise(base) -> personnage -> chaise(front)
"""
import os
import json
import numpy as np

from .imgio import load_png, over, stamp_max, shadow_layer_to_rgba
from .compose import recolor, apply_wall_pattern

OPP = {'se': 'nw', 'nw': 'se', 'sw': 'ne', 'ne': 'sw'}


class Library:
    def __init__(self, root, entries=None):
        self.root = root
        self.index = {}
        self._cache = {}
        if entries is None:
            entries = []
            cache = os.path.join(root, 'build_cache')
            for fn in sorted(os.listdir(cache)):
                if fn.endswith('.json') and fn.startswith(('custom_', 'iso_')):
                    try:
                        data = json.load(open(os.path.join(cache, fn), encoding='utf-8'))
                    except Exception as ex:  # fichier en cours d'ecriture par un autre build
                        print(f"(ignore {fn} : {ex})")
                        continue
                    if isinstance(data, list):
                        entries += [d for d in data if isinstance(d, dict) and 'id' in d]
        for e in entries:
            self.index[e['id']] = e

    def png(self, rel):
        if rel not in self._cache:
            self._cache[rel] = load_png(os.path.join(self.root, rel))
        return self._cache[rel]

    def get(self, aid):
        return self.index[aid]


class Node:
    def __init__(self, aid, facing, sx, sy, depth, swatches=None):
        self.aid, self.facing, self.sx, self.sy, self.depth = aid, facing, sx, sy, depth
        self.swatches = swatches or {}
        self.before, self.after = [], []
        self.chairs = []
        self.character = None


class Scene:
    def __init__(self, lib, origin=(0, 0)):
        self.lib = lib
        self.ox, self.oy = origin
        self.floor = []      # (rgba, x, y)
        self.edges = []
        self.walls = []
        self.wall_items = []
        self.groups = []     # (footprint (u0,u1,v0,v1) en cases, Node)

    def tile_screen(self, U, V):
        return self.ox + (U - V) * 32, self.oy + (U + V) * 16

    # -- sols et murs --------------------------------------------------------
    def floor_rect(self, fid, U0, V0, Wt, Dt, swatches=None):
        e = self.lib.get(fid)
        pu, pv = e['period_tiles']
        for U in range(U0, U0 + Wt):
            for V in range(V0, V0 + Dt):
                var = e['variants'][f"u{U % pu}v{V % pv}"]
                img = self.lib.png(var['layers']['base']['file'])
                if swatches:
                    img = recolor(img, self.lib.png(var['layers']['base']['mask']), swatches)
                x, y = self.tile_screen(U, V)
                self.floor.append((img, x - var['anchor'][0], y - var['anchor'][1]))

    def floor_overlay(self, aid, key, U, V, swatches=None):
        e = self.lib.get(aid)
        o = e['orientations'][key]
        img = self.lib.png(o['layers']['base']['file'])
        if swatches and 'mask' in o['layers']['base']:
            img = recolor(img, self.lib.png(o['layers']['base']['mask']), swatches)
        x, y = self.tile_screen(U, V)
        self.edges.append((img, x - o['anchor'][0], y - o['anchor'][1]))

    def slab_edges(self, Wt, Dt, U0=0, V0=0):
        e = self.lib.get('floor_edge')
        for V in range(V0, V0 + Dt - 1):
            self.floor_overlay('floor_edge', 'se', U0 + Wt, V)
        for U in range(U0, U0 + Wt - 1):
            self.floor_overlay('floor_edge', 'sw', U, V0 + Dt)
        self.floor_overlay('floor_edge_corner', 'all', U0 + Wt, V0 + Dt)

    def wall(self, kind, orient, U, V, pattern=None, swatches=None):
        e = self.lib.get(kind if kind.startswith('wall_') else f'wall_{kind}')
        o = e['orientations'][orient]
        rgba = self.lib.png(o['layers']['base']['file'])
        mask = self.lib.png(o['layers']['base']['mask']) if 'mask' in o['layers']['base'] else None
        sw = swatches or {}
        if mask is not None:
            if pattern and orient in ('se', 'sw'):
                po = self.lib.get(f'pattern_{pattern}')['orientations'][orient]
                ov = self.lib.png(po['layers']['overlay']['file'])
                dx = o['anchor'][0] - po['anchor'][0]; dy = o['anchor'][1] - po['anchor'][1]
                step = (-32, 16) if orient == 'se' else (32, 16)
                offs = [(dx + k * step[0], dy + k * step[1]) for k in range(o.get('span_tiles', 1))]
                rgba = apply_wall_pattern(rgba, mask, ov, sw, offs)
            elif sw:
                rgba = recolor(rgba, mask, sw)
        x, y = self.tile_screen(U, V)
        self.walls.append((rgba, x - o['anchor'][0], y - o['anchor'][1]))

    def room_shell(self, Wt, Dt, left, right, floor, pat_left='paint', pat_right='paint',
                   sw_left=None, sw_right=None, floor_sw=None, ends=True, slab=True):
        """left : liste de types de segments du mur gauche (V=0..Dt-1), None = rien (couvert par 2 cases).
        right : idem mur droit (U=0..Wt-1)."""
        self.floor_rect(floor, 0, 0, Wt, Dt, floor_sw)
        if slab:
            self.slab_edges(Wt, Dt)
        self.wall('wall_corner_inner', 'all', 0, 0, swatches=sw_left)
        for V, k in enumerate(left):
            if k:
                self.wall(k, 'se', 0, V, pat_left, sw_left)
        for U, k in enumerate(right):
            if k:
                self.wall(k, 'sw', U, 0, pat_right, sw_right)
        if ends:
            self.wall('wall_end', 'se', 0, Dt, swatches=sw_left)
            self.wall('wall_end', 'sw', Wt, 0, swatches=sw_right)

    def wall_item(self, aid, wall, index, swatches=None, height_offset=0):
        """Objet mural : wall='left' (orientation se, case V=index) ou 'right' (sw, U=index)."""
        e = self.lib.get(aid)
        f = 'se' if wall == 'left' else 'sw'
        o = e['orientations'][f]
        img = self._layer_img(o['layers']['base'], swatches)
        U, V = (0, index) if wall == 'left' else (index, 0)
        x, y = self.tile_screen(U, V)
        self.wall_items.append((img, x - o['anchor'][0], y - o['anchor'][1] - height_offset))

    # -- objets ---------------------------------------------------------------
    def _layer_img(self, L, swatches):
        img = self.lib.png(L['file'])
        if swatches and 'mask' in L:
            img = recolor(img, self.lib.png(L['mask']), swatches)
        return img

    def place(self, aid, U, V, facing, children=None, swatches=None, character=None):
        e = self.lib.get(aid)
        f = facing if facing in e['orientations'] else next(iter(e['orientations']))
        o = e['orientations'][f]
        if e.get('footprint_tiles') is None and e.get('size_su'):
            # objet centre pose au sol : ancre au centre de la case
            x, y = self.tile_screen(U + 0.5, V + 0.5)
            x, y = int(round(x)), int(round(y))
        else:
            x, y = self.tile_screen(U, V)
        node = Node(aid, f, x, y, 0, swatches)
        node.is_chair = bool(character) and 'occupant' in o
        node.character = character
        su, sv = o.get('footprint_world_tiles', [1, 1])
        fp = (U, U + su, V, V + sv)
        for sock, spec in (children or {}).items():
            if sock == 'slots':
                # modules de baie : [{'index': i, 'asset': id}] places aux positions de slot du manifeste
                sl = o.get('slots')
                if not sl:
                    continue
                for it in sorted(spec, key=lambda q: q['index']):
                    pos = sl['positions'][it['index']]['offset_px']
                    child = Node(it['asset'], f, x + pos[0], y + pos[1], 10000 + it['index'], it.get('swatches'))
                    node.after.append(child)
                continue
            self._attach(node, o, sock, spec)
        self.groups.append((fp, node))
        return node

    def _attach(self, node, o, sock, spec):
        if isinstance(spec, str):
            spec = {'asset': spec}
        s = o['sockets'][sock]
        cf = s['child_facing']
        child = Node(spec['asset'], cf, node.sx + s['offset_px'][0], node.sy + s['offset_px'][1],
                     s['world_su'][0] + s['world_su'][1], spec.get('swatches'))
        ce = self.lib.get(spec['asset'])
        co = ce['orientations'].get(cf) or next(iter(ce['orientations'].values()))
        for sub, subspec in (spec.get('children') or {}).items():
            self._attach(child, co, sub, subspec)
        if s.get('draw') == 'before_parent':
            node.before.append(child)
        elif s.get('draw') == 'workstation':
            child.character = spec.get('character')
            child.seat_depth = s['world_su'][0] + s['world_su'][1]
            node.chairs.append(child)
        else:
            node.after.append(child)

    # -- rendu ----------------------------------------------------------------
    def _draw_node(self, img, shadow, node, char_render=None):
        e = self.lib.get(node.aid)
        o = e['orientations'].get(node.facing) or next(iter(e['orientations'].values()))
        ax, ay = o['anchor']
        for c in node.before:
            self._draw_node(img, shadow, c, char_render)
        if 'shadow' in o['layers'] and shadow is not None:
            stamp_max(shadow, self.lib.png(o['layers']['shadow']['file']), node.sx - ax, node.sy - ay)
        over(img, self._layer_img(o['layers']['base'], node.swatches), node.sx - ax, node.sy - ay)
        if 'glass' in o['layers']:
            over(img, self._layer_img(o['layers']['glass'], node.swatches), node.sx - ax, node.sy - ay)
        if 'front' in o['layers']:
            # objet sans occupant (canape vu de dos...) : la couche front suit directement la base
            over(img, self._layer_img(o['layers']['front'], node.swatches), node.sx - ax, node.sy - ay)
        kids = sorted(node.after, key=lambda c: c.depth)
        for c in kids:
            self._draw_node(img, None, c, char_render)

    def _draw_chair(self, img, shadow, chair, char_render):
        ce = self.lib.get(chair.aid)
        co = ce['orientations'].get(chair.facing) or next(iter(ce['orientations'].values()))
        ax, ay = co['anchor']
        if 'shadow' in co['layers'] and shadow is not None:
            stamp_max(shadow, self.lib.png(co['layers']['shadow']['file']), chair.sx - ax, chair.sy - ay)
        over(img, self._layer_img(co['layers']['base'], chair.swatches), chair.sx - ax, chair.sy - ay)
        if 'glass' in co['layers']:
            over(img, self._layer_img(co['layers']['glass'], chair.swatches), chair.sx - ax, chair.sy - ay)
        if chair.character and char_render and 'occupant' in co:
            occ = co['occupant']
            cx, cy = chair.sx + occ['seat_offset_px'][0], chair.sy + occ['seat_offset_px'][1]
            for layer_img, (sx, sy) in char_render(chair.character, occ['character_orientation']):
                over(img, layer_img, cx - sx, cy - sy)
        if 'front' in co['layers']:
            over(img, self._layer_img(co['layers']['front'], chair.swatches), chair.sx - ax, chair.sy - ay)

    def _draw_workstation(self, img, shadow, desk, char_render):
        """Chaises situees derriere le parent (profondeur monde plus faible) : avant lui ; devant : apres."""
        if not desk.chairs:
            self._draw_node(img, shadow, desk, char_render)
            return
        de = self.lib.get(desk.aid)
        do = de['orientations'].get(desk.facing) or next(iter(de['orientations'].values()))
        fw = do.get('footprint_world_tiles', [1, 1])
        center = (fw[0] + fw[1]) * 16.0     # profondeur u+v du centre de l'emprise (su)
        behind = sorted([c for c in desk.chairs if c.seat_depth < center], key=lambda c: c.seat_depth)
        front = sorted([c for c in desk.chairs if c.seat_depth >= center], key=lambda c: c.seat_depth)
        for c in behind:
            self._draw_chair(img, shadow, c, char_render)
        self._draw_node(img, shadow, desk, char_render)
        for c in front:
            self._draw_chair(img, shadow, c, char_render)

    def render(self, size, bg=(58, 52, 60), char_render=None, shadows=True):
        W, H = size
        img = np.zeros((H, W, 4), np.uint8)
        img[..., :3] = bg
        img[..., 3] = 255
        for (s, x, y) in self.floor:
            over(img, s, x, y)
        for (s, x, y) in self.edges:
            over(img, s, x, y)
        shadow = np.zeros((H, W), np.uint8)
        # les ombres sont collectees en dessinant les objets dans un premier passage
        objs = np.zeros_like(img)
        ordered = topo_sort(self.groups)
        for (fp, node) in ordered:
            if getattr(node, 'is_chair', False):
                self._draw_chair(objs, shadow, node, char_render)
            else:
                self._draw_workstation(objs, shadow, node, char_render)
        if shadows:
            over(img, shadow_layer_to_rgba(shadow), 0, 0)
        for (s, x, y) in self.walls:
            over(img, s, x, y)
        for (s, x, y) in self.wall_items:
            over(img, s, x, y)
        over(img, objs, 0, 0)
        return img


def topo_sort(groups):
    """Tri des emprises au sol (u0,u1,v0,v1) en cases : A avant B si A est derriere B.
    A derriere B si A.u1 <= B.u0 (et recouvrement en v) ou A.v1 <= B.v0 (et recouvrement en u)."""
    n = len(groups)
    before = [set() for _ in range(n)]
    for i in range(n):
        a = groups[i][0]
        for j in range(n):
            if i == j:
                continue
            b = groups[j][0]
            behind = (a[1] <= b[0]) or (a[3] <= b[2])
            infront = (b[1] <= a[0]) or (b[3] <= a[2])
            if behind and not infront:
                before[j].add(i)
    out = []; done = set()
    order = sorted(range(n), key=lambda k: (groups[k][0][1] + groups[k][0][3], groups[k][0][0] + groups[k][0][2]))
    def visit(k, stack=()):
        if k in done:
            return
        if k in stack:
            return
        for d in sorted(before[k], key=lambda q: order.index(q)):
            visit(d, stack + (k,))
        done.add(k)
        out.append(groups[k])
    for k in order:
        visit(k)
    return out
