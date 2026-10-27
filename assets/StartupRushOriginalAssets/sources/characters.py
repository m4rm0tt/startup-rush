"""Personnages assis StartupRush : bases chauves + tetes + coiffures en couches.

Repere du gabarit : toile 40 x 72 px, point d'assise (ancre) = (20, 50).
Deux vues dessinees : 'front' (regarde vers le sud-est, face camera) et
'back' (regarde vers le nord-ouest, dos camera). Les orientations sw et ne sont
les miroirs de se et nw ; l'ombrage est RECALCULE apres miroir (lumiere
toujours a gauche de l'ecran).

Toutes les bases partagent le meme crane par vue : les 6 coiffures s'alignent
donc sur les 12 bases sans decalage (offsets = 0, verifies par le build).
"""
import numpy as np

from srgen.palette import M, FIXED, RAMPS
RAMPS_BRASS3 = RAMPS['brass'][3]
from srgen.iso import Fix
from srgen.raster2d import Painter

CW, CH = 40, 72
SEAT = (20, 50)
VIEWS = ('front', 'back')
ORIENT_VIEW = {'se': ('front', False), 'sw': ('front', True),
               'nw': ('back', False), 'ne': ('back', True)}

SKIN = M('skin', 'skin_light')
INK = Fix('ink')

# ---------------------------------------------------------------------------
# Crane commun
# ---------------------------------------------------------------------------

def skull_mask(view):
    p = Painter(CW, CH)
    r = p.region('skull', SKIN)
    if view == 'front':
        p.ellipse(r, 20.0, 13.6, 10.6, 9.7)
        p.ellipse(r, 20.8, 17.6, 8.6, 6.6)
    else:
        p.ellipse(r, 20.0, 13.6, 10.6, 9.7)
        p.ellipse(r, 20.0, 17.0, 8.8, 5.6)
    return p.lab > 0


SKULL = {v: skull_mask(v) for v in VIEWS}


# ---------------------------------------------------------------------------
# Visages (vue de face uniquement ; la vue de dos montre crane, oreilles, nuque)
# ---------------------------------------------------------------------------
FACES = {
    # yeux, sourcils, bouche, nez, extras
    'p_m':  dict(eyes='round', brows='flat', mouth='smile', nose='dot', extras=[]),
    'p_f':  dict(eyes='lash', brows='arch', mouth='smile', nose='dot', extras=['blush']),
    'm01':  dict(eyes='dot', brows='flat', mouth='neutral', nose='line', extras=['glasses_rect']),
    'm02':  dict(eyes='round', brows='thick', mouth='grin', nose='button', extras=['beard']),
    'm03':  dict(eyes='calm', brows='flat', mouth='smile', nose='line', extras=['mustache', 'wrinkles']),
    'm04':  dict(eyes='dot', brows='thick', mouth='smirk', nose='dot', extras=['stubble']),
    'm05':  dict(eyes='round', brows='flat', mouth='smile', nose='button', extras=['glasses_round']),
    'f01':  dict(eyes='lash', brows='arch', mouth='neutral', nose='dot', extras=['glasses_rect']),
    'f02':  dict(eyes='round', brows='thin', mouth='grin', nose='dot', extras=['freckles']),
    'f03':  dict(eyes='lash', brows='arch', mouth='smirk', nose='line', extras=['earrings']),
    'f04':  dict(eyes='round', brows='thick', mouth='smile', nose='button', extras=['glasses_round', 'blush']),
    'f05':  dict(eyes='calm', brows='thin', mouth='smile', nose='dot', extras=['earrings', 'mole']),
}


def paint_head(view, face_key, hair_ramp='hair_brown', skin='skin_light'):
    """Couche tete : crane + oreilles + traits du visage. Retourne Painter."""
    p = Painter(CW, CH)
    f = FACES[face_key]
    SKIN = M('skin', skin)
    ear = p.region('ear', SKIN, base=3, vol='ear', z=-1)
    sk = p.region('skin', SKIN, base=3, vol='head', z=0)
    if view == 'front':
        p.ellipse(ear, 9.6, 15.6, 2.2, 2.7)
        p.ellipse(ear, 30.9, 15.8, 1.5, 2.4)
    else:
        p.ellipse(ear, 9.4, 15.0, 2.0, 2.7)
        p.ellipse(ear, 30.6, 15.0, 2.0, 2.7)
    p.lab[SKULL[view]] = sk
    if view == 'back':
        # nuque legerement plus sombre (forcee)
        p.force([(x, 22) for x in range(15, 26)], -1)
        if 'earrings' in f['extras']:
            gold = p.region('earring', Fix(RAMPS_BRASS3), vol='head', shade=False, line=False)
            p.plot(gold, [(9, 18), (31, 18)])
        if 'glasses_rect' in f['extras'] or 'glasses_round' in f['extras']:
            gl = p.region('glasses', Fix(FIXED['ink']), vol='head', shade=False, line=False)
            p.plot(gl, [(9, 13), (10, 13), (30, 13), (31, 13)])
        if 'beard' in f['extras']:
            br = p.region('beard', M('hair', hair_ramp), base=2, vol='head', shade=True, line=False)
            p.plot(br, [(11, 19), (12, 20), (29, 19), (28, 20)])
        return p
    eye = p.region('eye', INK, vol='head', shade=False, line=False)
    eyew = p.region('eyew', Fix(FIXED['eye_white']), vol='head', shade=False, line=False)
    brow = p.region('brow', M('hair', hair_ramp), base=1, vol='head', shade=False, line=False)
    mouth = p.region('mouth', Fix(FIXED['mouth']), vol='head', shade=False, line=False)
    # positions des yeux (visage tourne de 1 px vers la droite de l'ecran)
    lx, rx, ey = 16, 23, 15
    e = f['eyes']
    if e == 'dot':
        p.plot(eye, [(lx, ey), (lx, ey + 1), (rx, ey), (rx, ey + 1)])
    elif e == 'round':
        p.plot(eye, [(lx, ey), (lx + 1, ey), (lx, ey + 1), (lx + 1, ey + 1),
                     (rx, ey), (rx + 1, ey), (rx, ey + 1), (rx + 1, ey + 1)])
        p.plot(eyew, [(lx, ey), (rx, ey)])
    elif e == 'lash':
        p.plot(eye, [(lx, ey), (lx + 1, ey), (lx, ey + 1), (lx + 1, ey + 1),
                     (rx, ey), (rx + 1, ey), (rx, ey + 1), (rx + 1, ey + 1),
                     (lx - 1, ey - 1), (rx + 2, ey - 1)])
        p.plot(eyew, [(lx, ey), (rx, ey)])
    elif e == 'calm':
        p.plot(eye, [(lx, ey + 1), (lx + 1, ey + 1), (rx, ey + 1), (rx + 1, ey + 1)])
    b = f['brows']
    by = 12
    if b == 'flat':
        p.plot(brow, [(lx - 1, by), (lx, by), (lx + 1, by), (rx, by), (rx + 1, by), (rx + 2, by)])
    elif b == 'thick':
        p.plot(brow, [(lx - 1, by), (lx, by), (lx + 1, by), (rx, by), (rx + 1, by), (rx + 2, by),
                      (lx, by - 1), (lx + 1, by - 1), (rx, by - 1), (rx + 1, by - 1)])
    elif b == 'arch':
        p.plot(brow, [(lx - 1, by), (lx, by - 1), (lx + 1, by - 1), (rx, by - 1), (rx + 1, by - 1), (rx + 2, by)])
    elif b == 'thin':
        p.plot(brow, [(lx, by), (lx + 1, by), (rx, by), (rx + 1, by)])
    n = f['nose']
    if n == 'dot':
        p.force([(21, 18)], -1)
    elif n == 'line':
        p.force([(21, 17), (21, 18)], -1)
    elif n == 'button':
        p.force([(20, 18), (21, 18)], -1)
    mo = f['mouth']
    my = 20
    if mo == 'smile':
        p.plot(mouth, [(18, my), (19, my + 1), (20, my + 1), (21, my + 1), (22, my)])
    elif mo == 'grin':
        p.plot(mouth, [(18, my), (19, my), (20, my), (21, my), (22, my), (19, my + 1), (20, my + 1), (21, my + 1)])
        p.plot(eyew, [(19, my), (20, my), (21, my)])
    elif mo == 'neutral':
        p.plot(mouth, [(19, my + 1), (20, my + 1), (21, my + 1)])
    elif mo == 'smirk':
        p.plot(mouth, [(19, my + 1), (20, my + 1), (21, my + 1), (22, my)])
    ex = f['extras']
    if 'blush' in ex:
        bl = p.region('blush', Fix(FIXED['blush']), vol='head', shade=False, line=False)
        p.plot(bl, [(14, 18), (15, 18), (25, 18), (26, 18)])
    if 'freckles' in ex:
        p.force([(14, 18), (16, 19), (24, 18), (26, 19), (15, 17)], -1)
    if 'wrinkles' in ex:
        p.force([(13, 15), (13, 16), (27, 15), (27, 16)], -1)
    if 'mole' in ex:
        mo_ = p.region('mole', Fix(FIXED['ink_soft']), vol='head', shade=False, line=False)
        p.plot(mo_, [(25, 19)])
    if 'earrings' in ex:
        gold = p.region('earring', Fix(RAMPS_BRASS3), vol='head', shade=False, line=False)
        p.plot(gold, [(9, 19), (31, 19)])
    if 'stubble' in ex:
        st = p.region('stubble', M('hair', hair_ramp), base=3, vol='head', shade=False, line=False)
        p.plot(st, [(14, 21), (16, 22), (18, 23), (20, 23), (22, 23), (24, 22), (26, 21), (15, 22), (25, 22)])
    if 'beard' in ex:
        br = p.region('beard', M('hair', hair_ramp), base=2, vol='head', shade=True, line=False)
        pts = []
        for x in range(13, 29):
            for y in range(19, 25):
                if SKULL['front'][y, x] and (y >= 22 or x <= 15 or x >= 26):
                    pts.append((x, y))
        p.plot(br, pts)
        p.plot(mouth, [(19, 21), (20, 21), (21, 21)])
    if 'mustache' in ex:
        mu = p.region('mustache', M('hair', hair_ramp), base=3, vol='head', shade=False, line=False)
        p.plot(mu, [(18, 19), (19, 19), (20, 19), (21, 19), (22, 19), (23, 19)])
    if 'glasses_rect' in ex or 'glasses_round' in ex:
        gl = p.region('glasses', INK, vol='head', shade=False, line=False)
        lens = p.region('lens', Fix('lens'), vol='head', shade=False, line=False)
        if 'glasses_rect' in ex:
            for (cx0) in (lx - 2, rx - 1):
                pts = [(cx0 + i, 14) for i in range(5)] + [(cx0 + i, 17) for i in range(5)] + \
                      [(cx0, 15), (cx0, 16), (cx0 + 4, 15), (cx0 + 4, 16)]
                p.plot(gl, pts)
            p.plot(gl, [(19, 15), (20, 15), (21, 15), (10, 15), (11, 15)])
            p.plot(lens, [(lx - 1, 15), (rx, 15)])
        else:
            for cx0 in (lx - 2, rx - 1):
                pts = [(cx0 + 1, 14), (cx0 + 2, 14), (cx0 + 3, 14), (cx0, 15), (cx0, 16), (cx0 + 4, 15), (cx0 + 4, 16),
                       (cx0 + 1, 17), (cx0 + 2, 17), (cx0 + 3, 17)]
                p.plot(gl, pts)
            p.plot(gl, [(19, 15), (20, 15), (21, 15), (10, 15), (11, 15)])
            p.plot(lens, [(lx - 1, 15), (rx, 15)])
        # les yeux restent visibles derriere les verres
        if e in ('round', 'lash'):
            p.plot(eye, [(lx, ey), (lx + 1, ey), (lx, ey + 1), (lx + 1, ey + 1),
                         (rx, ey), (rx + 1, ey), (rx, ey + 1), (rx + 1, ey + 1)])
        else:
            p.plot(eye, [(lx, ey + 1), (rx, ey + 1), (lx, ey), (rx, ey)])
    return p


# ---------------------------------------------------------------------------
# Corps (couche body) : cou, buste, bras, jambes, chaussures
# ---------------------------------------------------------------------------
BUILDS = {
    # demi-largeur d'epaules, demi-largeur de taille, decalage de bras
    'slim':    dict(sh=8.5, wa=7.5, arm=0),
    'regular': dict(sh=10.0, wa=9.0, arm=1),
    'broad':   dict(sh=11.5, wa=10.5, arm=2),
    'slim_f':  dict(sh=8.0, wa=7.0, arm=0, hip=1.0),
    'reg_f':   dict(sh=9.0, wa=8.0, arm=1, hip=1.5),
    'broad_f': dict(sh=10.5, wa=10.0, arm=2, hip=1.5),
}


def paint_body(view, base, skin=None):
    b = BASES[base]
    SKIN = M('skin', skin or b['skin_default'])
    bd = BUILDS[b['build']]
    col = b['colors']
    p = Painter(CW, CH)
    top = M('primary', col['primary'])
    sec = M('secondary', col['secondary'])
    acc = M('accent', col['accent'])
    shoe = M('fixed', 'graphite') if False else None
    sh, wa, arm = bd['sh'], bd['wa'], bd['arm']
    hip = bd.get('hip', 0.0)
    cx = 20.0
    neck = p.region('neck', SKIN, base=2, vol='neck', z=-2)
    pants = p.region('pants', sec, base=3, vol='legs', z=-1)
    shoes = p.region('shoes', Fix(FIXED['ink_soft']), base=2, vol='shoes', z=-1, shade=False)
    torso = p.region('torso', top, base=3, vol='torso', z=0)
    arm_l = p.region('arm_l', top, base=3, vol='arm_l', z=1)
    arm_r = p.region('arm_r', top, base=3, vol='arm_r', z=1)
    hand = p.region('hand', SKIN, base=3, vol='hand', z=2)
    outfit = b['outfit']
    if view == 'front':
        p.rect(neck, 17, 21, 24, 28)
        # jambes (masquees par le bureau en vue de face, mais completes)
        # vue 3/4 tournee vers la droite de l'ecran : genoux vers l'avant-droite (sous le bureau)
        thigh = p.region('thigh', sec, base=4, vol='thigh', z=0)
        K = 5
        p.poly(thigh, [(cx - wa - hip, 44), (cx + wa + hip, 44), (cx + wa + hip + K, 52.5), (cx - wa - hip + K, 52.5)])
        p.rect(pants, 12 + K, 53, 19 + K, 64)
        p.rect(pants, 22 + K, 53, 29 + K, 64)
        p.force([(x, 52) for x in range(int(cx - wa - hip + K), int(cx + wa + hip + K + 1))], -1)
        p.force([(21 + (y - 45) * K // 8, y) for y in range(45, 52)], -1)
        p.rect(shoes, 11 + K, 63, 19 + K, 67)
        p.rect(shoes, 22 + K, 63, 30 + K, 67)
        # buste
        p.poly(torso, [(cx - sh + 2, 26), (cx + sh - 2, 26), (cx + sh + 0.5, 28.5), (cx + sh + 0.5, 32),
                       (cx + wa + hip, 46), (cx - wa - hip, 46), (cx - sh - 0.5, 32), (cx - sh - 0.5, 28.5)])
        # bras : epaule -> coude -> avant-bras vers la camera
        L = cx - sh - 0.5
        p.poly(arm_l, [(L - 2.5, 29), (L + 1.5, 27.5), (L + 2.5, 30), (L + 2.0, 39), (L - 1, 40.5), (L - 3, 38)])
        p.poly(arm_l, [(L - 2.5, 37.5), (L + 2, 37), (L + 7.5, 42.5), (L + 6, 46), (L + 0.5, 45)])
        p.ellipse(hand, L + 6.5, 45.2, 2.4, 1.9)
        R_ = cx + sh + 0.5
        p.poly(arm_r, [(R_ + 2.5, 29), (R_ - 1.5, 27.5), (R_ - 2.5, 30), (R_ - 2.0, 39), (R_ + 1, 40.5), (R_ + 3, 38)])
        p.poly(arm_r, [(R_ + 2.5, 37.5), (R_ - 2, 37), (R_ - 7.5, 42.5), (R_ - 6, 46), (R_ - 0.5, 45)])
        p.ellipse(hand, R_ - 6.5, 45.2, 2.4, 1.9)
        _outfit_front(p, outfit, top, sec, acc, cx, sh, wa, hip, torso)
    else:
        p.rect(neck, 17, 19, 24, 28)
        p.poly(pants, [(cx - wa - hip, 44), (cx + wa + hip, 44), (cx + wa + hip, 51), (cx - wa - hip, 51)])
        p.poly(torso, [(cx - sh + 2, 26), (cx + sh - 2, 26), (cx + sh + 0.5, 28.5), (cx + sh + 0.5, 32),
                       (cx + wa + hip, 46), (cx - wa - hip, 46), (cx - sh - 0.5, 32), (cx - sh - 0.5, 28.5)])
        L = cx - sh - 0.5
        p.poly(arm_l, [(L - 2.5, 29), (L + 1.5, 27.5), (L + 2.5, 30), (L + 2.5, 39), (L, 42), (L - 3, 40)])
        R_ = cx + sh + 0.5
        p.poly(arm_r, [(R_ + 2.5, 29), (R_ - 1.5, 27.5), (R_ - 2.5, 30), (R_ - 2.5, 39), (R_, 42), (R_ + 3, 40)])
        _outfit_back(p, outfit, top, sec, acc, cx, sh, wa, hip, torso)
    return p


def _outfit_front(p, outfit, top, sec, acc, cx, sh, wa, hip, torso):
    c = int(cx)
    if outfit == 'hoodie':
        hood = p.region('hood', top, base=2, vol='hood', z=-1)
        p.ellipse(hood, cx, 26.5, 7.5, 3.2)
        string = p.region('string', M('fixed', 'cream'), base=4, vol='torso', shade=False, line=False)
        p.plot(string, [(c - 2, 29), (c - 2, 30), (c - 2, 31), (c + 2, 29), (c + 2, 30), (c + 2, 31)])
        pocket = [(x, 40) for x in range(c - 5, c + 6)]
        p.force(pocket, -1)
        p.force([(c, y) for y in range(30, 46)], -1)
    elif outfit == 'shirt_tie':
        collar = p.region('collar', M('fixed', 'plastic'), base=4, vol='torso', z=0, line=False)
        p.poly(collar, [(c - 4, 26), (c + 4, 26), (c + 1.5, 30), (c - 1.5, 30)])
        tie = p.region('tie', acc, base=3, vol='tie', z=1)
        p.poly(tie, [(c - 1, 28), (c + 1, 28), (c + 1.5, 37), (c, 39), (c - 1.5, 37)])
        p.force([(c - 4, y) for y in range(33, 46, 4)], 0)
    elif outfit == 'tee':
        p.force([(x, 27) for x in range(c - 3, c + 4)], -1)
        pr = p.region('print', acc, base=3, vol='torso', shade=False, line=False)
        p.plot(pr, [(c - 1, 33), (c, 32), (c + 1, 33), (c - 2, 34), (c + 2, 34), (c, 34)])
    elif outfit == 'cardigan':
        inner = p.region('inner', M('fixed', 'cream'), base=4, vol='torso', line=False)
        p.poly(inner, [(c - 3, 26), (c + 3, 26), (c + 1, 46), (c - 1, 46)])
        btn = p.region('btn', Fix(FIXED['ink_soft']), vol='torso', shade=False, line=False)
        p.plot(btn, [(c + 2, 33), (c + 2, 37), (c + 2, 41)])
    elif outfit == 'flannel':
        for y in range(29, 46, 4):
            p.force([(x, y) for x in range(c - 9, c + 10)], -1)
        for x in range(c - 8, c + 9, 4):
            p.force([(x, y) for y in range(27, 46)], -1)
        inner = p.region('tee_in', acc, base=3, vol='torso', line=False)
        p.poly(inner, [(c - 2, 26), (c + 2, 26), (c + 1, 32), (c - 1, 32)])
    elif outfit == 'blazer':
        shirt = p.region('shirt', M('fixed', 'plastic'), base=4, vol='torso', line=False)
        p.poly(shirt, [(c - 3, 26), (c + 3, 26), (c, 36)])
        lap = p.region('lapel', top, base=2, vol='torso', shade=False, line=False)
        p.plot(lap, [(c - 3, 27), (c - 2, 29), (c - 1, 31), (c + 3, 27), (c + 2, 29), (c + 1, 31)])
        btn = p.region('btn', Fix(FIXED['ink_soft']), vol='torso', shade=False, line=False)
        p.plot(btn, [(c, 39), (c, 42)])
    elif outfit == 'blouse':
        collar = p.region('collar', acc, base=4, vol='torso', line=False)
        p.plot(collar, [(c - 3, 26), (c - 2, 27), (c - 1, 28), (c + 3, 26), (c + 2, 27), (c + 1, 28)])
        p.force([(c, y) for y in range(28, 46)], -1)
    elif outfit == 'turtleneck':
        tn = p.region('turtle', top, base=4, vol='turtle', z=1)
        p.rect(tn, c - 4, 24, c + 4, 28)
        for y in range(25, 28):
            pass
    elif outfit == 'jacket_tee':
        inner = p.region('tee_in', acc, base=4, vol='torso', line=False)
        p.poly(inner, [(c - 3, 26), (c + 3, 26), (c + 2, 46), (c - 2, 46)])
        zip_ = p.region('zip', Fix(FIXED['ink_soft']), vol='torso', shade=False, line=False)
        p.plot(zip_, [(c - 3, y) for y in range(33, 46, 3)])
    elif outfit == 'denim':
        p.force([(x, 33) for x in range(c - 9, c + 10)], -1)
        pk = p.region('pocket', top, base=2, vol='torso', shade=False, line=False)
        p.plot(pk, [(c - 6, 35), (c - 5, 35), (c - 4, 35), (c + 4, 35), (c + 5, 35), (c + 6, 35)])
        inner = p.region('tee_in', acc, base=4, vol='torso', line=False)
        p.poly(inner, [(c - 2, 26), (c + 2, 26), (c + 1, 33), (c - 1, 33)])
    elif outfit == 'polo':
        collar = p.region('collar', top, base=4, vol='collar', z=1)
        p.poly(collar, [(c - 4, 25.5), (c + 4, 25.5), (c + 3, 28), (c, 27), (c - 3, 28)])
        p.force([(c, 28), (c, 29), (c, 30)], -1)
        btn = p.region('btn', Fix(FIXED['paper']), vol='torso', shade=False, line=False)
        p.plot(btn, [(c, 29)])


def _outfit_back(p, outfit, top, sec, acc, cx, sh, wa, hip, torso):
    c = int(cx)
    if outfit == 'hoodie':
        hood = p.region('hood', top, base=3, vol='hood', z=1)
        p.poly(hood, [(c - 7, 26), (c + 7, 26), (c + 5, 33), (c, 35), (c - 5, 33)])
        p.force([(c, y) for y in range(28, 34)], -1)
    elif outfit in ('shirt_tie', 'blouse', 'polo'):
        p.force([(x, 34) for x in range(c - 7, c + 8)], -1) if outfit == 'shirt_tie' else None
        collar = p.region('collar', top if outfit == 'polo' else M('fixed', 'plastic') if outfit == 'shirt_tie' else acc,
                          base=4, vol='collar', z=1)
        p.poly(collar, [(c - 4, 25), (c + 4, 25), (c + 4, 27), (c - 4, 27)])
    elif outfit == 'turtleneck':
        tn = p.region('turtle', top, base=4, vol='turtle', z=1)
        p.rect(tn, c - 4, 23, c + 4, 28)
    elif outfit == 'cardigan':
        p.force([(x, 44) for x in range(c - 8, c + 9)], -1)
    elif outfit == 'flannel':
        for y in range(29, 46, 4):
            p.force([(x, y) for x in range(c - 9, c + 10)], -1)
        for x in range(c - 8, c + 9, 4):
            p.force([(x, y) for y in range(27, 46)], -1)
    elif outfit in ('blazer', 'jacket_tee', 'denim'):
        p.force([(c, y) for y in range(36, 46)], -1)
        if outfit == 'denim':
            p.force([(x, 31) for x in range(c - 8, c + 9)], -1)
    elif outfit == 'tee':
        pass


# ---------------------------------------------------------------------------
# Bases : 2 avatars joueur + 10 employes (5 par presentation)
# ---------------------------------------------------------------------------
BASES = {
    'player_m':      dict(pres='m', build='regular', outfit='hoodie',     face='p_m', skin_default='skin_light',
                          colors=dict(primary='teal', secondary='navy', accent='sun')),
    'player_f':      dict(pres='f', build='slim_f', outfit='jacket_tee',  face='p_f', skin_default='skin_medium',
                          colors=dict(primary='coral', secondary='navy', accent='cream')),
    'employee_m_01': dict(pres='m', build='regular', outfit='shirt_tie',  face='m01', skin_default='skin_light',
                          colors=dict(primary='sky', secondary='graphite', accent='coral')),
    'employee_m_02': dict(pres='m', build='broad', outfit='tee',          face='m02', skin_default='skin_deep',
                          colors=dict(primary='sun', secondary='navy', accent='teal')),
    'employee_m_03': dict(pres='m', build='slim', outfit='cardigan',      face='m03', skin_default='skin_medium',
                          colors=dict(primary='sage', secondary='walnut', accent='cream')),
    'employee_m_04': dict(pres='m', build='regular', outfit='flannel',    face='m04', skin_default='skin_light',
                          colors=dict(primary='red', secondary='steel', accent='graphite')),
    'employee_m_05': dict(pres='m', build='broad', outfit='polo',         face='m05', skin_default='skin_medium',
                          colors=dict(primary='navy', secondary='sand', accent='sun')),
    'employee_f_01': dict(pres='f', build='slim_f', outfit='blouse',      face='f01', skin_default='skin_light',
                          colors=dict(primary='cream', secondary='navy', accent='teal')),
    'employee_f_02': dict(pres='f', build='reg_f', outfit='turtleneck',   face='f02', skin_default='skin_light',
                          colors=dict(primary='sage', secondary='graphite', accent='coral')),
    'employee_f_03': dict(pres='f', build='reg_f', outfit='blazer',       face='f03', skin_default='skin_deep',
                          colors=dict(primary='graphite', secondary='graphite', accent='sun')),
    'employee_f_04': dict(pres='f', build='broad_f', outfit='denim',      face='f04', skin_default='skin_medium',
                          colors=dict(primary='sky', secondary='navy', accent='rose')),
    'employee_f_05': dict(pres='f', build='slim_f', outfit='tee',         face='f05', skin_default='skin_deep',
                          colors=dict(primary='rose', secondary='walnut', accent='sun')),
}


# ---------------------------------------------------------------------------
# Coiffures (couches hair_back / hair_front), definies sur le crane commun
# ---------------------------------------------------------------------------
HAIRSTYLES = ['short_crop', 'side_part', 'curly_top', 'long_straight', 'bun', 'bob']
HAIR_PRES = {'short_crop': 'm', 'side_part': 'm', 'curly_top': 'm',
             'long_straight': 'f', 'bun': 'f', 'bob': 'f'}


def _band(mask, y0, y1):
    out = np.zeros_like(mask)
    out[y0:y1] = mask[y0:y1]
    return out


def paint_hair(style, view, ramp='hair_brown'):
    """Retourne (painter_front, painter_back_or_None)."""
    H = M('hair', ramp)
    pf = Painter(CW, CH)
    hf = pf.region('hair', H, base=3, vol='hair', z=5, band=2)
    pb = None
    sk = SKULL[view]
    yy, xx = np.mgrid[0:CH, 0:CW]

    def cap(top_extra=0.0, hairline=9, sides_to=14, back=False):
        """Calotte epousant le crane jusqu'a la ligne de cheveux."""
        m = np.zeros((CH, CW), bool)
        # crane dilate (volume)
        dil = ((xx + 0.5 - 20.0) / (10.6 + top_extra * 0.6)) ** 2 + ((yy + 0.5 - 13.6) / (9.7 + top_extra)) ** 2 <= 1.0
        dil &= yy < 14
        m |= dil
        m |= sk & (yy < hairline)
        return m

    if view == 'front':
        if style == 'short_crop':
            m = cap(0.6)
            m &= ~((yy >= 9) & (xx >= 12) & (xx <= 28))
            m |= sk & (yy < 9)
            m |= sk & (yy < 14) & ((xx <= 11) | (xx >= 29))
            pf.lab[m] = hf
            pf.force([(x, 8) for x in range(14, 27, 3)], -1)
        elif style == 'side_part':
            m = cap(1.4)
            m &= ~((yy >= 10) & (xx >= 12) & (xx <= 28))
            m |= sk & (yy < 10) & (xx <= 26)
            m |= sk & (yy < 9)
            m |= sk & (yy < 15) & ((xx <= 11) | (xx >= 29))
            # meche balayee
            pf.lab[m] = hf
            fr = pf.region('fringe', H, base=4, vol='fringe', z=6)
            pf.poly(fr, [(12, 6.5), (25, 4.5), (29.5, 8), (27, 11.5), (21, 10.5), (15, 11), (11.5, 10)])
            pf.force([(x, 7 + (x - 12) // 4) for x in range(13, 27, 2)], -1)
            pf.force([(16, 3), (16, 4), (16, 5)], -1)
        elif style == 'curly_top':
            m = cap(2.2)
            m &= ~((yy >= 9) & (xx >= 12) & (xx <= 28))
            m |= sk & (yy < 9)
            m |= sk & (yy < 13) & ((xx <= 11) | (xx >= 29))
            # bosses de boucles sur la silhouette
            for (cx_, cy_) in ((12, 5), (16, 2.6), (20.5, 2), (25, 2.6), (28.5, 5), (10.5, 9), (30, 9)):
                m |= ((xx + 0.5 - cx_) ** 2 + (yy + 0.5 - cy_) ** 2) <= 6.0
            m &= ~((yy >= 10) & (xx >= 12) & (xx <= 28))
            pf.lab[m] = hf
            curls = [(14, 5), (18, 4), (22, 4), (26, 5), (16, 7), (20, 7), (24, 7), (12, 8), (28, 8)]
            pf.force(curls, 1)
            pf.force([(x + 1, y + 1) for (x, y) in curls], -1)
        elif style == 'long_straight':
            m = cap(1.0)
            m &= ~((yy >= 10) & (xx >= 13) & (xx <= 27))
            m |= sk & (yy < 10)
            # meches laterales qui encadrent le visage
            m |= (xx >= 8) & (xx <= 12) & (yy >= 9) & (yy <= 27)
            m |= (xx >= 28) & (xx <= 32) & (yy >= 9) & (yy <= 26)
            pf.lab[m] = hf
            pf.force([(20, y) for y in range(3, 9)], -1)
            pf.force([(10, y) for y in range(14, 26)], -1)
            pf.force([(30, y) for y in range(14, 25)], -1)
            # partie arriere (derriere les epaules)
            pb = Painter(CW, CH)
            hb = pb.region('hair_b', H, base=2, vol='hair_b', z=-5)
            pb.poly(hb, [(8, 12), (32, 12), (33, 33), (28, 35), (12, 35), (7, 33)])
        elif style == 'bun':
            m = cap(0.8)
            m &= ~((yy >= 9) & (xx >= 12) & (xx <= 28))
            m |= sk & (yy < 9)
            m |= sk & (yy < 14) & ((xx <= 11) | (xx >= 29))
            pf.lab[m] = hf
            pf.force([(x, 4 + abs(x - 20) // 4) for x in range(13, 28, 2)], -1)
            pb = Painter(CW, CH)
            hb = pb.region('bun', H, base=3, vol='bun', z=-5, band=2)
            pb.ellipse(hb, 20.5, 2.4, 5.4, 3.9)
            pb.force([(18, 1), (19, 0), (22, 2), (23, 3)], 1)
        elif style == 'bob':
            m = cap(1.2)
            # frange droite
            m |= sk & (yy < 12) & (xx >= 12) & (xx <= 28)
            m |= (xx >= 8) & (xx <= 12) & (yy >= 9) & (yy <= 22)
            m |= (xx >= 28) & (xx <= 32) & (yy >= 9) & (yy <= 22)
            m &= ~((yy >= 12) & (xx >= 13) & (xx <= 27))
            pf.lab[m] = hf
            pf.force([(x, 11) for x in range(14, 27, 2)], -1)
            pf.force([(10, y) for y in range(12, 22)], -1)
            pf.force([(30, y) for y in range(12, 22)], -1)
    else:  # dos
        if style == 'short_crop':
            m = cap(0.6) | (sk & (yy < 19))
            pf.lab[m] = hf
            pf.force([(x, 18) for x in range(13, 28)], -1)
        elif style == 'side_part':
            m = cap(1.4) | (sk & (yy < 19))
            pf.lab[m] = hf
            pf.force([(24, y) for y in range(3, 10)], -1)
            pf.force([(x, 18) for x in range(13, 28)], -1)
        elif style == 'curly_top':
            m = cap(2.2) | (sk & (yy < 18))
            for (cx_, cy_) in ((12, 5), (16, 2.6), (20.5, 2), (25, 2.6), (28.5, 5), (10.5, 9), (30, 9), (11, 13), (29, 13)):
                m |= ((xx + 0.5 - cx_) ** 2 + (yy + 0.5 - cy_) ** 2) <= 6.0
            pf.lab[m] = hf
            curls = [(14, 5), (18, 4), (22, 4), (26, 5), (16, 8), (20, 8), (24, 8), (13, 11), (18, 12), (23, 12), (27, 11),
                     (15, 15), (20, 15), (25, 15)]
            pf.force(curls, 1)
            pf.force([(x + 1, y + 1) for (x, y) in curls], -1)
        elif style == 'long_straight':
            m = cap(1.0) | (sk & (yy < 23))
            m |= (xx >= 9) & (xx <= 31) & (yy >= 14) & (yy <= 37) & (np.abs(xx + 0.5 - 20) <= 11.5 - np.maximum(0, yy - 31) * 1.2)
            pf.lab[m] = hf
            pf.force([(20, y) for y in range(3, 12)], -1)
            for x in (14, 18, 22, 26):
                pf.force([(x, y) for y in range(20, 35)], -1)
        elif style == 'bun':
            m = cap(0.8) | (sk & (yy < 19))
            pf.lab[m] = hf
            bun = pf.region('bun', H, base=3, vol='bun', z=6, band=2)
            pf.ellipse(bun, 20.0, 6.5, 5.0, 4.2)
            pf.force([(x, 15 + abs(x - 20) // 3) for x in range(13, 28, 2)], -1)
            pf.force([(18, 5), (19, 4), (21, 8), (22, 7)], 1)
        elif style == 'bob':
            m = cap(1.2) | (sk & (yy < 23))
            m |= (xx >= 9) & (xx <= 31) & (yy >= 12) & (yy <= 23)
            pf.lab[m] = hf
            pf.force([(x, 22) for x in range(10, 31, 2)], -1)
            pf.force([(20, y) for y in range(3, 10)], -1)
    return pf, pb


# ---------------------------------------------------------------------------
# Rendu d'une couche pour une orientation
# ---------------------------------------------------------------------------

def render_layers(base, orient, hairstyle=None, hair_ramp='hair_brown', skin=None):
    """Retourne dict couche -> (rgba, mask) pour une base et une orientation.
    skin=None : teinte par defaut de la base (le masque permet les 3 teintes)."""
    view, mirror = ORIENT_VIEW[orient]
    out = {}
    skin = skin or BASES[base]['skin_default']
    body = paint_body(view, base, skin)
    head = paint_head(view, BASES[base]['face'], hair_ramp, skin)
    if mirror:
        body = body.mirrored(); head = head.mirrored()
    out['body'] = body.render()
    out['head'] = head.render()
    if hairstyle:
        pf, pb = paint_hair(hairstyle, view, hair_ramp)
        if mirror:
            pf = pf.mirrored()
            pb = pb.mirrored() if pb else None
        sk = SKULL[view][:, ::-1] if mirror else SKULL[view]
        out['hair_front'] = pf.render(under=sk)
        if pb is not None:
            out['hair_back'] = pb.render()
    return out
