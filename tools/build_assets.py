"""
Fabrique les ressources du jeu (UI/Assets) à partir des packs originaux du dossier assets/.

Les originaux ne sont jamais modifiés : on copie ce qui sert tel quel et on écrit les dérivés
(recolorations, atlas assemblés) dans des fichiers séparés. Relancer ce script reproduit
exactement les mêmes fichiers.

    python tools/build_assets.py
"""
import json
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


def hex_rgba(value):
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4)) + (255,)


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


# ---------------------------------------------------------------- Personnages (2dPig Pixel Office, CC0)

def build_office():
    # La planche entière, recolorée : les personnages et quelques objets y sont découpés
    office = to_endesga(Image.open(src("2dpig", "pixel-office", "PixelOfficeAssets.png")))
    office.save(out("Sprites", "office.png"))
    return office


# ---------------------------------------------------------------- Mobilier vu de dessus (atlas + manifeste)

KENNEY_INDOOR = ("kenney", "roguelike-indoors", "Tilesheets", "roguelikeIndoor_transparent.png")
TILE, MARGIN = 16, 1  # d'après tilesheetInfo.txt du pack


def kenney_tile(sheet, column, row):
    x, y = column * (TILE + MARGIN), row * (TILE + MARGIN)
    return sheet.crop((x, y, x + TILE, y + TILE))


def grid(sheet, cells):
    """Assemble des tuiles Kenney : cells est une liste de rangées de (colonne, rangée)."""
    width = max(len(row) for row in cells) * TILE
    image = Image.new("RGBA", (width, len(cells) * TILE), (0, 0, 0, 0))
    for j, row in enumerate(cells):
        for i, cell in enumerate(row):
            if cell is not None:
                image.alpha_composite(kenney_tile(sheet, *cell), (i * TILE, j * TILE))
    return image


def office_crop(office, x, y, w, h):
    return office.crop((x, y, x + w, y + h))


def stack(base, top, dx, dy):
    """Pose « top » sur « base » à (dx, dy) depuis le coin haut-gauche de base, en agrandissant vers le haut si besoin."""
    extra = max(0, -dy)
    width = max(base.width, dx + top.width)
    image = Image.new("RGBA", (width, base.height + extra), (0, 0, 0, 0))
    image.alpha_composite(base, (0, extra))
    image.alpha_composite(top, (dx, dy + extra))
    return image


def laptop():
    # Aucun pack ne fournit d'ordinateur portable vu de 3/4 : dessin original, palette Endesga
    rows = [
        "..KKKKKKKKKK..",
        "..KbbbbbbbbK..",
        "..KbBBBBBBbK..",
        "..KbBCBBBBbK..",
        "..KbBBBBBBbK..",
        "..KKKKKKKKKK..",
        ".KggggggggggK.",
        "KgcgcgcgcgcgcK",
        "KKKKKKKKKKKKKK",
    ]
    inks = {"K": "181425", "b": "124e89", "B": "0099db", "C": "2ce8f5", "g": "8b9bb4", "c": "c0cbdc"}
    image = Image.new("RGBA", (14, len(rows)), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        for x, ink in enumerate(row):
            if ink in inks:
                image.putpixel((x, y), hex_rgba(inks[ink]))
    return image


# Emprise au sol en cases. Elle doit correspondre à Core/space/Furniture.cs : un test le vérifie.
FOOTPRINTS = {
    "desk": (2, 1), "desk_pro": (2, 1), "chair": (1, 1), "chair_ergo": (1, 1),
    "laptop": (1, 1), "pc": (1, 1), "workstation": (1, 1),
    "plant": (1, 1), "coffee": (1, 1), "sofa": (2, 1), "whiteboard": (2, 1), "bookshelf": (1, 1), "rug": (2, 2),
    "bed": (2, 1), "kitchen": (3, 1), "fridge": (1, 1), "home_sofa": (2, 1), "dining": (2, 2), "kitchenette": (2, 1),
}

# Les personnages de Pixel Office (de face), un par apparence
CHARACTERS = [(2, 105, 15, 23), (19, 104, 19, 24), (40, 107, 13, 21), (3, 132, 17, 23), (22, 132, 17, 23)]


def build_furniture(office):
    sheet = to_endesga(Image.open(src(*KENNEY_INDOOR)))
    monitor = office_crop(office, 231, 104, 19, 23)
    tower = office_crop(office, 233, 135, 9, 17)

    sprites = {
        "desk": (grid(sheet, [[(4, 3), (5, 3)]]), "Kenney Roguelike Indoors, tuiles (4,3) (5,3)"),
        "desk_pro": (grid(sheet, [[(5, 5), (6, 5)]]), "Kenney Roguelike Indoors, tuiles (5,5) (6,5)"),
        "chair": (grid(sheet, [[(0, 2)]]), "Kenney Roguelike Indoors, tuile (0,2)"),
        "chair_ergo": (grid(sheet, [[(0, 8)]]), "Kenney Roguelike Indoors, tuile (0,8)"),
        "laptop": (laptop(), "Dessin original (aucun portable dans les packs)"),
        "pc": (monitor, "2dPig Pixel Office, rectangle 231,104,19,23"),
        "workstation": (stack(monitor, tower, 19, 6), "2dPig Pixel Office, 231,104,19,23 + unité centrale 233,135,9,17"),
        "plant": (grid(sheet, [[(16, 0)]]), "Kenney Roguelike Indoors, tuile (16,0)"),
        "coffee": (stack(grid(sheet, [[(1, 12)]]), office_crop(office, 159, 91, 7, 11), 4, -7),
                   "Kenney (1,12) + machine à café Pixel Office 159,91,7,11"),
        "sofa": (office_crop(office, 120, 121, 33, 16), "2dPig Pixel Office, rectangle 120,121,33,16"),
        "whiteboard": (office_crop(office, 84, 70, 26, 20), "2dPig Pixel Office, rectangle 84,70,26,20"),
        "bookshelf": (grid(sheet, [[(20, 17)]]), "Kenney Roguelike Indoors, tuile (20,17)"),
        "rug": (grid(sheet, [[(16, 8), (18, 8)], [(16, 9), (18, 9)]]), "Kenney Roguelike Indoors, coins du tapis (16..18, 8..9)"),
        "bed": (grid(sheet, [[(13, 1), (12, 1)]]), "Kenney Roguelike Indoors, tuiles (13,1) (12,1)"),
        "kitchen": (grid(sheet, [[(0, 12), (8, 12), (1, 12)]]), "Kenney Roguelike Indoors, tuiles (0,12) (8,12) (1,12)"),
        "fridge": (grid(sheet, [[(11, 15)], [(11, 16)]]), "Kenney Roguelike Indoors, tuiles (11,15) (11,16)"),
        "home_sofa": (office_crop(office, 119, 66, 33, 15), "2dPig Pixel Office, rectangle 119,66,33,15"),
        "dining": (grid(sheet, [[(0, 0), (2, 0)], [(0, 1), (2, 1)]]), "Kenney Roguelike Indoors, tuiles (0..2, 0..1)"),
        "kitchenette": (grid(sheet, [[(8, 12), (0, 12)]]), "Kenney Roguelike Indoors, tuiles (8,12) (0,12)"),
        "window": (office_crop(office, 59, 96, 26, 22), "2dPig Pixel Office, rectangle 59,96,26,22"),
    }
    for i, rect in enumerate(CHARACTERS):
        sprites[f"person{i}"] = (office_crop(office, *rect), "2dPig Pixel Office, rectangle " + ",".join(map(str, rect)))

    # Atlas : un objet par rangée, 1 px d'écart pour qu'aucun voisin ne déborde
    width = max(image.width for image, _ in sprites.values()) + 2
    height = sum(image.height + 1 for image, _ in sprites.values()) + 1
    atlas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    manifest = {}
    y = 1
    for name, (image, source) in sprites.items():
        atlas.alpha_composite(image, (1, y))
        manifest[name] = {"x": 1, "y": y, "w": image.width, "h": image.height,
                          "footprint": list(FOOTPRINTS.get(name, (0, 0))), "source": source}
        y += image.height + 1
    atlas.save(out("Sprites", "furniture.png"))
    with open(out("Sprites", "furniture.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=1)


# ---------------------------------------------------------------- Cadres 9-slice (Kenney Pixel UI Pack, CC0)

# Les cadres « Colored » de Kenney ont quatre teintes : ombre, contour, fond, reflet.
# On garde le dessin (biseau, ombre portée du bouton) et on remplace les teintes par la palette du jeu.
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
    "select.wav": RETRO + ("General Sounds", "Menu Sounds", "sfx_menu_select1.wav"),
    "offer.wav": RETRO + ("General Sounds", "Simple Bleeps", "sfx_sounds_Blip7.wav"),
    "coin.wav": RETRO + ("General Sounds", "Coins", "sfx_coin_single1.wav"),
    "cash.wav": RETRO + ("General Sounds", "Coins", "sfx_coin_cluster3.wav"),
    "buy.wav": RETRO + ("General Sounds", "Coins", "sfx_coin_double4.wav"),
    "bill.wav": RETRO + ("General Sounds", "Coins", "sfx_coin_double6.wav"),
    "error.wav": RETRO + ("General Sounds", "Negative Sounds", "sfx_sounds_error3.wav"),
    "levelup.wav": RETRO + ("General Sounds", "Fanfares", "sfx_sounds_fanfare1.wav"),
    "victory.wav": RETRO + ("General Sounds", "Fanfares", "sfx_sounds_fanfare3.wav"),
    "alarm.wav": RETRO + ("General Sounds", "Alarms", "Alarms", "sfx_alarm_loop1.wav"),
    "unpaid.wav": RETRO + ("General Sounds", "Negative Sounds", "sfx_sounds_negative2.wav"),
    "defeat.wav": RETRO + ("Movement", "Falling Sounds", "sfx_sounds_falling2.wav"),
    "event.wav": RETRO + ("General Sounds", "Neutral Sounds", "sfx_sound_neutral6.wav"),
    "pause_in.wav": RETRO + ("General Sounds", "Pause Sounds", "sfx_sounds_pause1_in.wav"),
    "pause_out.wav": RETRO + ("General Sounds", "Pause Sounds", "sfx_sounds_pause1_out.wav"),
}

ADVENTURE = ("opengameart", "4-chiptunes-adventure", "files")
MUSIC = {
    "menu.ogg": ADVENTURE + ("Juhani Junkala [Chiptune Adventures] 4. Stage Select.ogg",),
    "work1.ogg": ADVENTURE + ("Juhani Junkala [Chiptune Adventures] 1. Stage 1.ogg",),
    "work2.ogg": ADVENTURE + ("Juhani Junkala [Chiptune Adventures] 2. Stage 2.ogg",),
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
    build_furniture(build_office())
    build_skins()
    build_prompts()
    build_audio()
    build_fonts()
    print("ressources écrites dans", OUT)
