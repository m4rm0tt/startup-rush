"""
Fabrique les ressources du jeu (UI/Assets) à partir des packs originaux du dossier assets/.

Les originaux ne sont jamais modifiés : on copie ce qui sert tel quel et on écrit les dérivés
(recolorations, frames ajoutées) dans des fichiers séparés. Relancer ce script reproduit
exactement les mêmes fichiers.

    python tools/build_assets.py
"""
import os
import shutil

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "assets")
OUT = os.path.join(ROOT, "UI", "Assets")

# Palette Endesga 32, celle de tout le jeu
ENDESGA = [
    "be4a2f", "d77643", "ead4aa", "e4a672", "b86f50", "733e39", "3e2731", "a22633",
    "e43b44", "f77622", "feae34", "fee761", "63c74d", "3e8948", "265c42", "193c3e",
    "124e89", "0099db", "2ce8f5", "ffffff", "c0cbdc", "8b9bb4", "5a6988", "3a4466",
    "262b44", "181425", "ff0044", "68386c", "b55088", "f6757a", "e8b796", "c28569",
]
PALETTE = [tuple(int(c[i:i + 2], 16) for i in (0, 2, 4)) for c in ENDESGA]


def src(*parts):
    return os.path.join(SRC, *parts)


def out(*parts):
    path = os.path.join(OUT, *parts)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    return path


def nearest(rgb):
    # Distance pondérée par la sensibilité de l'œil (comme Palette.Nearest côté jeu)
    r, g, b = rgb
    return min(PALETTE, key=lambda p: 2 * (p[0] - r) ** 2 + 4 * (p[1] - g) ** 2 + 3 * (p[2] - b) ** 2)


# Pixel Office est dessiné en Sweetie 16. La couleur la plus proche convient pour la plupart des teintes,
# mais la peau et le vert clair partiraient vers le jaune : on les fixe à la main.
OVERRIDES = {
    (255, 205, 117): "e8b796",  # peau claire
    (239, 125, 87): "d77643",   # ombre de peau, meubles orange
    (167, 240, 112): "63c74d",  # vert clair des plantes
    (56, 183, 100): "3e8948",   # vert foncé
}


def to_endesga(image):
    image = image.convert("RGBA")
    pixels = image.load()
    cache = {}
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            if (r, g, b) not in cache:
                fixed = OVERRIDES.get((r, g, b))
                cache[(r, g, b)] = hex_rgba(fixed)[:3] if fixed else nearest((r, g, b))
            pixels[x, y] = cache[(r, g, b)] + (255,)
    return image


# ---------------------------------------------------------------- Sprites (2dPig Pixel Office, CC0)

def build_office():
    office = to_endesga(Image.open(src("2dpig", "pixel-office", "PixelOfficeAssets.png")))

    # Deuxième frame du chat : la queue se relève d'un pixel (rangées 1 à 8, colonnes 11 à 15)
    cat_x, cat_y, cat_w, cat_h = 65, 129, 16, 13
    frame = office.crop((cat_x, cat_y, cat_x + cat_w, cat_y + cat_h))
    lifted = frame.copy()
    for y in range(cat_h):
        for x in range(11, cat_w):
            lifted.putpixel((x, y), (0, 0, 0, 0))
    for y in range(1, 9):
        for x in range(11, cat_w):
            lifted.putpixel((x, y - 1), frame.getpixel((x, y)))
    lifted.putpixel((12, 8), frame.getpixel((12, 9)))

    # Zone libre de la planche juste à droite du chat
    spare = (82, 129, 98, 142)
    assert all(office.getpixel((x, y))[3] == 0 for x in range(spare[0], spare[2]) for y in range(spare[1], spare[3]))
    office.paste(lifted, spare[:2])

    # Les nuages servent de calque devant n'importe quel ciel : on retire le bleu du fond
    sky = office.getpixel((0, 0))
    for y in range(36):
        for x in range(256):
            if office.getpixel((x, y)) == sky:
                office.putpixel((x, y), (0, 0, 0, 0))
    office.save(out("Sprites", "office.png"))


# ---------------------------------------------------------------- Cadres 9-slice (Kenney Pixel UI Pack, CC0)

# Les cadres « Colored » de Kenney ont quatre teintes : ombre, contour, fond, reflet.
# On garde le dessin (biseau, ombre portée du bouton) et on remplace les teintes par la palette sombre du jeu.
SKINS = {
    #  nom               fichier source     ombre      contour    fond       reflet
    "panel":           ("grey",          "0b0913", "5a6988", "262b44", "3a4466"),
    "inset":           ("grey_pressed",  None,     "3a4466", "181425", "262b44"),
    "button":          ("grey",          "0b0913", "181425", "3a4466", "5a6988"),
    "button_hover":    ("grey",          "0b0913", "181425", "5a6988", "8b9bb4"),
    "button_pressed":  ("grey_pressed",  None,     "181425", "262b44", "3a4466"),
    "button_disabled": ("grey_pressed",  None,     "262b44", "262b44", "262b44"),
    "gold":            ("yellow",        "733e39", "be4a2f", "feae34", "fee761"),
    "gold_hover":      ("yellow",        "733e39", "be4a2f", "fee761", "ffffff"),
    "gold_pressed":    ("yellow_pressed", None,    "be4a2f", "f77622", "feae34"),
    "green":           ("green",         "193c3e", "265c42", "3e8948", "63c74d"),
    "green_hover":     ("green",         "193c3e", "265c42", "63c74d", "c0cbdc"),
    "green_pressed":   ("green_pressed", None,     "265c42", "265c42", "3e8948"),
    "red":             ("red",           "3e2731", "3e2731", "a22633", "e43b44"),
    "red_hover":       ("red",           "3e2731", "3e2731", "e43b44", "f6757a"),
    "red_pressed":     ("red_pressed",   None,     "3e2731", "3e2731", "a22633"),
}


def hex_rgba(value):
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4)) + (255,)


def build_skins():
    for name, (source, shadow, outline, fill, light) in SKINS.items():
        image = Image.open(src("kenney", "pixel-ui-pack", "9-Slice", "Colored", source + ".png")).convert("RGBA")
        # Teintes classées de la plus sombre à la plus claire
        shades = sorted({p for p in image.get_flattened_data() if p[3] > 0}, key=lambda p: sum(p[:3]))
        targets = [shadow, outline, fill, light] if len(shades) == 4 else [outline, fill, light]
        mapping = dict(zip(shades, [hex_rgba(t) for t in targets]))
        pixels = image.load()
        for y in range(image.height):
            for x in range(image.width):
                if pixels[x, y][3] > 0:
                    pixels[x, y] = mapping[pixels[x, y]]
        image.save(out("Ui", "skins", name + ".png"))


# ---------------------------------------------------------------- Glyphes de touches (Kenney Input Prompts Pixel, CC0)

def build_prompts():
    shutil.copyfile(src("kenney", "input-prompts-pixel", "Tilemap", "tilemap_packed.png"), out("Ui", "input_prompts.png"))


# ---------------------------------------------------------------- Sons et musiques

RETRO = ("opengameart", "512-sound-effects-8-bit-style", "files",
         "The Essential Retro Video Game Sound Effects Collection [512 sounds] By Juhani Junkala")

SOUNDS = {
    "work.wav": RETRO + ("General Sounds", "Menu Sounds", "sfx_menu_move1.wav"),
    "hover.wav": RETRO + ("General Sounds", "Menu Sounds", "sfx_menu_move2.wav"),
    "select.wav": RETRO + ("General Sounds", "Menu Sounds", "sfx_menu_select1.wav"),
    "offer.wav": RETRO + ("General Sounds", "Simple Bleeps", "sfx_sounds_Blip7.wav"),
    "coin.wav": RETRO + ("General Sounds", "Coins", "sfx_coin_single1.wav"),
    "cash.wav": RETRO + ("General Sounds", "Coins", "sfx_coin_cluster3.wav"),
    "buy.wav": RETRO + ("General Sounds", "Coins", "sfx_coin_double4.wav"),
    "bill.wav": RETRO + ("General Sounds", "Coins", "sfx_coin_double6.wav"),
    "error.wav": RETRO + ("General Sounds", "Negative Sounds", "sfx_sounds_error3.wav"),
    "crit.wav": RETRO + ("General Sounds", "Positive Sounds", "sfx_sounds_powerup15.wav"),
    "levelup.wav": RETRO + ("General Sounds", "Fanfares", "sfx_sounds_fanfare1.wav"),
    "victory.wav": RETRO + ("General Sounds", "Fanfares", "sfx_sounds_fanfare3.wav"),
    "alarm.wav": RETRO + ("General Sounds", "Alarms", "Alarms", "sfx_alarm_loop1.wav"),
    "unpaid.wav": RETRO + ("General Sounds", "Negative Sounds", "sfx_sounds_negative2.wav"),
    "defeat.wav": RETRO + ("Movement", "Falling Sounds", "sfx_sounds_falling2.wav"),
    "event.wav": RETRO + ("General Sounds", "Neutral Sounds", "sfx_sound_neutral6.wav"),
    "pause_in.wav": RETRO + ("General Sounds", "Pause Sounds", "sfx_sounds_pause1_in.wav"),
    "pause_out.wav": RETRO + ("General Sounds", "Pause Sounds", "sfx_sounds_pause1_out.wav"),
    "card_fan.ogg": ("kenney", "casino-audio", "Audio", "card-fan-1.ogg"),
    "card_place.ogg": ("kenney", "casino-audio", "Audio", "card-place-1.ogg"),
    "card_shuffle.ogg": ("kenney", "casino-audio", "Audio", "card-shuffle.ogg"),
}

ADVENTURE = ("opengameart", "4-chiptunes-adventure", "files")
MUSIC = {
    "menu.ogg": ADVENTURE + ("Juhani Junkala [Chiptune Adventures] 4. Stage Select.ogg",),
    "run1.ogg": ADVENTURE + ("Juhani Junkala [Chiptune Adventures] 1. Stage 1.ogg",),
    "run2.ogg": ADVENTURE + ("Juhani Junkala [Chiptune Adventures] 2. Stage 2.ogg",),
    "crisis.ogg": ADVENTURE + ("Juhani Junkala [Chiptune Adventures] 3. Boss Fight.ogg",),
}


def build_audio():
    for name, parts in SOUNDS.items():
        shutil.copyfile(src(*parts), out("Audio", "Sfx", name))
    for name, parts in MUSIC.items():
        shutil.copyfile(src(*parts), out("Audio", "Music", name))


# ---------------------------------------------------------------- Police de titre (OFL)

def build_fonts():
    shutil.copyfile(src("fonts", "pressstart2p", "PressStart2P-Regular.ttf"), out("Fonts", "PressStart2P-Regular.ttf"))
    shutil.copyfile(src("fonts", "pressstart2p", "OFL.txt"), out("Fonts", "OFL-PressStart2P.txt"))


if __name__ == "__main__":
    build_office()
    build_skins()
    build_prompts()
    build_audio()
    build_fonts()
    print("ressources écrites dans", OUT)
