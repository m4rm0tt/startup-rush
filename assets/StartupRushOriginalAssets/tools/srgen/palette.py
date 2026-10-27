"""Palette originale StartupRush « Sunrise Desk » (version de pack 1.0).

Chaque rampe contient 6 teintes rangees du plus sombre (indice 0) au plus clair
(indice 5). Les sprites ne stockent pas de couleur libre pour les surfaces
recolorables : ils stockent (canal, indice de rampe). Changer la rampe d'un
canal conserve donc exactement le relief (faces claires / sombres).

Indices utilises par le moteur :
  0 = creux profonds, lignes internes      3 = face gauche (+V), surface de base
  1 = ombre portee interne, face tres sombre 4 = dessus des volumes
  2 = face droite (+U), cote a l'ombre      5 = arete eclairee, reflet
"""

def hx(s, a=255):
    s = s.lstrip('#')
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), a)


def _r(*hexes):
    assert len(hexes) == 6, hexes
    return [hx(h) for h in hexes]


# --------------------------------------------------------------------------
# Rampes (sombre -> clair)
# --------------------------------------------------------------------------
RAMPS = {
    # neutres chauds
    'cream':      _r('#6f5f5d', '#9c8878', '#c2ae94', '#dccdb1', '#efe4cc', '#fbf5e6'),
    'sand':       _r('#563f36', '#7d5e45', '#a67f56', '#c9a06c', '#e2c08a', '#f3dcae'),
    'kraft':      _r('#4f3a2f', '#735239', '#986d45', '#b98a55', '#d4a86e', '#e8c890'),
    # bois
    'oak':        _r('#4c2d24', '#74452b', '#9e6237', '#c4844a', '#dfa765', '#f2c98b'),
    'walnut':     _r('#321e1e', '#4b2d28', '#673f33', '#865643', '#a8735a', '#c9977a'),
    'birch':      _r('#5f4a3d', '#8f7152', '#bd9a6a', '#dcbb87', '#f0d7a8', '#fbeccc'),
    # gris informatiques
    'steel':      _r('#3d404c', '#5b606d', '#7c828e', '#9fa5af', '#c3c8cf', '#e3e6ea'),
    'graphite':   _r('#1f2028', '#2c2d36', '#393b46', '#4a4d59', '#5f6370', '#7c808c'),
    'plastic':    _r('#66636a', '#918f90', '#b8b7b3', '#d6d5cf', '#ecebe5', '#fbfbf6'),
    'beige_pc':   _r('#5e5548', '#857a68', '#aa9f88', '#c8bea4', '#ded6bf', '#efe9d6'),
    # accents mesures
    'teal':       _r('#1d3c4a', '#275a66', '#357c82', '#4b9f9c', '#72c1b4', '#a6e0cf'),
    'coral':      _r('#5f2a30', '#8f3d39', '#c05843', '#e47a55', '#f5a173', '#fcd0a4'),
    'sun':        _r('#654624', '#976a27', '#c9922e', '#ecb841', '#fbd769', '#fff0a8'),
    'leaf':       _r('#213f31', '#305d3a', '#437d40', '#62a04b', '#8fc062', '#c3e08a'),
    'sky':        _r('#253655', '#34507d', '#4670a6', '#5d90c9', '#85b7e3', '#b5dcf5'),
    'sage':       _r('#394336', '#536447', '#72875a', '#96aa75', '#bccb9a', '#dfe6c4'),
    'rose':       _r('#4f2c3b', '#7b4151', '#a85c6b', '#d07f88', '#eaa9ab', '#f8d3d0'),
    'navy':       _r('#1a2333', '#26334a', '#354562', '#485a7d', '#61759a', '#8597b8'),
    'red':        _r('#4a1d22', '#74292b', '#a23a34', '#c8453d', '#e46c5a', '#f59f86'),
    # matieres techniques
    'screen':     _r('#1b4652', '#276f78', '#3a9a98', '#5cc4b4', '#8fe3cc', '#c8f7e4'),
    'display':    _r('#162235', '#1f2f47', '#2b405d', '#3a5576', '#4f6e92', '#6f8fb0'),
    'glass':      _r('#43677a', '#5f8c9c', '#82b2bd', '#a8d2d6', '#cfeaea', '#f2fbf8'),
    'copper':     _r('#45251f', '#6a3826', '#96512f', '#c06f3f', '#e0955c', '#f5c08a'),
    'brass':      _r('#533c22', '#7f5c28', '#b08332', '#d9ad45', '#f2d372', '#fff1b0'),
    'brick':      _r('#452626', '#67372f', '#8c4a39', '#ad6248', '#c9805f', '#e0a888'),
    'concrete':   _r('#4e4a49', '#6c6762', '#8b857c', '#a8a297', '#c3bdb3', '#dcd7cf'),
    'rubber':     _r('#17171c', '#22222a', '#2e2e37', '#3c3c46', '#4f4f5b', '#686875'),
    'hazard':     _r('#5a3c12', '#8a5e16', '#c08a1c', '#e9b320', '#f8d24a', '#fff0a0'),
    # peaux (3 teintes demandees)
    'skin_light':  _r('#6f4447', '#a0655a', '#cc8a6e', '#e8ab8a', '#f6cdae', '#fde7d2'),
    'skin_medium': _r('#4b2e2a', '#734635', '#9a6143', '#bb8056', '#d9a577', '#f0c79c'),
    'skin_deep':   _r('#2c1b1b', '#452a23', '#5f3b2c', '#7c4f37', '#9c6a4a', '#c08a64'),
    # cheveux (teintes par defaut, le masque permet d'autres teintes)
    'hair_black':  _r('#15131b', '#1e1c26', '#2a2733', '#383443', '#4c4757', '#6a6475'),
    'hair_brown':  _r('#271a18', '#3a251f', '#503226', '#69432f', '#85573a', '#a7724b'),
    'hair_auburn': _r('#3d1f1c', '#5c2d22', '#7f3f27', '#a2552f', '#c5733f', '#e39a62'),
    'hair_blond':  _r('#57412a', '#846436', '#b08944', '#d3ae5c', '#ecd082', '#fbeab0'),
    'hair_gray':   _r('#47454b', '#69666a', '#8f8b85', '#b4b0a8', '#d6d3cb', '#f2f0ea'),
}

# Couleurs fixes (jamais recolorees)
FIXED = {
    'ink':        hx('#2b2128'),   # contour unique de tous les sprites
    'ink_soft':   hx('#4a3a42'),
    'shadow':     hx('#2b2128'),   # couche d'ombre (opacite de couche 0.30)
    'void':       hx('#1c1720'),   # fond d'une ouverture de porte
    'led_green':  hx('#7ef08a'),
    'led_amber':  hx('#ffbf4a'),
    'led_red':    hx('#ff6157'),
    'led_blue':   hx('#63c7ff'),
    'led_white':  hx('#f8fbff'),
    'paper':      hx('#fbf5e6'),
    'paper_line': hx('#c2ae94'),
    'sky_top':    hx('#9fd0ea'),
    'sky_low':    hx('#d9eff0'),
    'sky_cloud':  hx('#f4fbfa'),
    'white':      hx('#fbfbf6'),
    'eye':        hx('#2b2128'),
    'eye_white':  hx('#fbf5e6'),
    'mouth':      hx('#8f3d39'),
    'blush':      hx('#e48f80'),
    'lens':       hx('#d6ecec'),   # verres de lunettes
    'lamp_glow':  hx('#ffecaa'),   # ampoule / diffuseur allume (decor fixe, pas un etat)
    'void_floor': hx('#342c34'),   # sol entrevu par une ouverture de porte
    'void_edge':  hx('#282129'),
}

# --------------------------------------------------------------------------
# Canaux de recoloration (valeur exacte stockee dans le canal R du masque)
# --------------------------------------------------------------------------
CHANNELS = {
    'fixed': 0,      # non recolorable (contours, LED, details imprimes)
    'primary': 1,
    'secondary': 2,
    'wood': 3,
    'fabric': 4,
    'metal': 5,
    'glass': 6,
    'screen': 7,
    'skin': 8,
    'hair': 9,
    'accent': 10,
    'wall': 11,      # surface murale principale (accepte un motif)
    'floor': 12,
    'trim': 13,      # plinthes, encadrements, chambranles
    'wall2': 14,     # seconde teinte des motifs muraux
    'floor2': 15,    # seconde teinte des sols (joints, mouchetis)
    'plant': 16,
}
CHANNEL_NAMES = {v: k for k, v in CHANNELS.items()}

# Mask : R = canal, G = indice de rampe (0..5), B = drapeaux, A = 255 si recolorable
MASK_FLAG_PATTERN = 1   # bit 0 : pixel de surface murale recevant un motif

# Rampes proposees par canal (echantillons de validation, pas un produit cartesien)
SWATCHES = {
    'primary':   ['plastic', 'graphite', 'steel', 'teal', 'coral', 'sun', 'sky', 'sage', 'navy', 'rose'],
    'secondary': ['graphite', 'steel', 'plastic', 'navy', 'teal', 'coral'],
    'wood':      ['oak', 'walnut', 'birch'],
    'fabric':    ['teal', 'coral', 'sun', 'sky', 'sage', 'navy', 'rose', 'graphite', 'cream'],
    'metal':     ['steel', 'graphite', 'brass', 'copper'],
    'glass':     ['glass'],
    'screen':    ['screen', 'display', 'sky'],
    'skin':      ['skin_light', 'skin_medium', 'skin_deep'],
    'hair':      ['hair_black', 'hair_brown', 'hair_auburn', 'hair_blond', 'hair_gray'],
    'accent':    ['coral', 'sun', 'teal', 'sky', 'leaf', 'rose', 'red'],
    'wall':      ['cream', 'sand', 'sage', 'sky', 'rose', 'concrete', 'teal', 'plastic'],
    'wall2':     ['teal', 'sage', 'coral', 'navy', 'sand', 'walnut', 'cream'],
    'floor':     ['oak', 'walnut', 'birch', 'concrete', 'steel', 'sage', 'navy', 'teal', 'cream', 'plastic'],
    'floor2':    ['cream', 'steel', 'graphite', 'sand', 'plastic'],
    'trim':      ['plastic', 'oak', 'walnut', 'graphite', 'cream', 'steel'],
    'plant':     ['leaf', 'sage'],
}


class Mat:
    """Materiau = canal de recoloration + rampe par defaut."""
    __slots__ = ('channel', 'ramp')

    def __init__(self, channel, ramp):
        assert channel in CHANNELS, channel
        assert ramp in RAMPS, ramp
        self.channel = channel
        self.ramp = ramp

    @property
    def ch(self):
        return CHANNELS[self.channel]

    def color(self, idx):
        idx = max(0, min(5, int(idx)))
        return RAMPS[self.ramp][idx]

    def __repr__(self):
        return f"Mat({self.channel},{self.ramp})"


def M(channel, ramp):
    return Mat(channel, ramp)


# Materiaux courants (raccourcis)
OAK = M('wood', 'oak')
WALNUT = M('wood', 'walnut')
BIRCH = M('wood', 'birch')
STEEL = M('metal', 'steel')
GRAPHITE_M = M('metal', 'graphite')
GLASS = M('glass', 'glass')


# --------------------------------------------------------------------------
# Generation d'une rampe a partir d'une couleur (selecteur de couleur libre)
# Algorithme documente dans COLOR_SYSTEM.md (OKLCh, decalage de teinte)
# --------------------------------------------------------------------------
def _srgb_to_lin(c):
    c = c / 255.0
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def _lin_to_srgb(c):
    c = max(0.0, min(1.0, c))
    v = 12.92 * c if c <= 0.0031308 else 1.055 * (c ** (1 / 2.4)) - 0.055
    return int(round(max(0.0, min(1.0, v)) * 255))


def rgb_to_oklch(rgb):
    import math
    r, g, b = (_srgb_to_lin(x) for x in rgb[:3])
    l = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b
    m = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b
    s = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b
    l_, m_, s_ = (x ** (1 / 3) if x > 0 else 0.0 for x in (l, m, s))
    L = 0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_
    A = 1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_
    B = 0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_
    C = math.hypot(A, B)
    H = math.degrees(math.atan2(B, A)) % 360
    return L, C, H


def oklch_to_rgb(L, C, H):
    import math
    A = C * math.cos(math.radians(H))
    B = C * math.sin(math.radians(H))
    l_ = L + 0.3963377774 * A + 0.2158037573 * B
    m_ = L - 0.1055613458 * A - 0.0638541728 * B
    s_ = L - 0.0894841775 * A - 1.2914855480 * B
    l, m, s = l_ ** 3, m_ ** 3, s_ ** 3
    r = 4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s
    g = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s
    b = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s
    return (_lin_to_srgb(r), _lin_to_srgb(g), _lin_to_srgb(b), 255)


# (delta L, facteur de chroma, attraction de teinte vers 70deg (chaud) ou 265deg (froid))
RAMP_RECIPE = [
    (-0.30, 0.80, ('cool', 22)),
    (-0.19, 0.92, ('cool', 12)),
    (-0.09, 1.00, ('cool', 5)),
    (0.00, 1.00, None),
    (+0.09, 0.92, ('warm', 6)),
    (+0.17, 0.70, ('warm', 12)),
]


def _toward(h, target, amount):
    d = ((target - h + 540) % 360) - 180
    step = max(-amount, min(amount, d))
    return (h + step) % 360


def ramp_from_color(rgb):
    """Construit une rampe de 6 teintes ou `rgb` devient l'indice 3."""
    L, C, H = rgb_to_oklch(rgb)
    out = []
    for dl, cf, shift in RAMP_RECIPE:
        h = H
        if shift:
            kind, amt = shift
            h = _toward(H, 70 if kind == 'warm' else 265, amt if C > 0.02 else 0)
        out.append(oklch_to_rgb(max(0.05, min(0.98, L + dl)), C * cf, h))
    return out
