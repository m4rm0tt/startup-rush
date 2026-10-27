"""
Fabrique le dossier de présentation du jeu (docs/Startup_Rush_Dossier.pdf) à partir des captures réelles
de docs/captures/ et des règles du moteur (valeurs relevées dans Core/, voir DESIGN_SIMULATION.md).

    python tools/build_dossier.py

Les captures se refont avec `StartupRush.exe --capture docs/captures` ; relancer ce script reproduit le PDF.
"""
import os

from PIL import Image as PilImage
from reportlab.graphics.shapes import Drawing, Line, Polygon, Rect, String
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (BaseDocTemplate, CondPageBreak, Frame, Image, KeepTogether, NextPageTemplate,
                                PageBreak, PageTemplate, Paragraph, Spacer, Table, TableStyle)
from reportlab.platypus.tableofcontents import TableOfContents

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CAPTURES = os.path.join(ROOT, "docs", "captures")
OUTPUT = os.path.join(ROOT, "docs", "Startup_Rush_Dossier.pdf")
WINFONTS = os.path.join(os.environ.get("WINDIR", r"C:\Windows"), "Fonts")

# Polices : Press Start 2P (celle du jeu) pour les titres, Segoe UI pour le texte, Segoe UI Symbol pour les symboles
pdfmetrics.registerFont(TTFont("Pixel", os.path.join(ROOT, "UI", "Assets", "Fonts", "PressStart2P-Regular.ttf")))
pdfmetrics.registerFont(TTFont("Body", os.path.join(WINFONTS, "segoeui.ttf")))
pdfmetrics.registerFont(TTFont("Body-Bold", os.path.join(WINFONTS, "segoeuib.ttf")))
pdfmetrics.registerFont(TTFont("Body-Italic", os.path.join(WINFONTS, "segoeuii.ttf")))
pdfmetrics.registerFont(TTFont("Symbol", os.path.join(WINFONTS, "seguisym.ttf")))
pdfmetrics.registerFontFamily("Body", normal="Body", bold="Body-Bold", italic="Body-Italic", boldItalic="Body-Bold")

# Couleurs Endesga 32, comme le jeu
INK = colors.HexColor("#262b44")
NIGHT = colors.HexColor("#181425")
SLATE = colors.HexColor("#3a4466")
FOG = colors.HexColor("#5a6988")
MIST = colors.HexColor("#8b9bb4")
PAPER = colors.HexColor("#eef1f6")
GOLD = colors.HexColor("#feae34")
ORANGE = colors.HexColor("#f77622")
BLUE = colors.HexColor("#0099db")
GREEN = colors.HexColor("#3e8948")
LIME = colors.HexColor("#63c74d")
RED = colors.HexColor("#e43b44")
WINE = colors.HexColor("#a22633")
CREAM = colors.HexColor("#fff6e0")

PAGE_W, PAGE_H = A4
MARGIN_X = 46
WIDTH = PAGE_W - 2 * MARGIN_X

# ------------------------------------------------------------------ Styles

# Les titres et chapeaux restent toujours avec ce qui les suit : jamais de titre seul en bas de page
kicker = ParagraphStyle("kicker", fontName="Body-Bold", fontSize=8.5, leading=11, textColor=BLUE, spaceBefore=16, spaceAfter=4,
                        keepWithNext=1)
h1 = ParagraphStyle("h1", fontName="Pixel", fontSize=14, leading=20, textColor=INK, spaceAfter=10, keepWithNext=1)
h1_plain = ParagraphStyle("h1_plain", parent=h1)
h2 = ParagraphStyle("h2", fontName="Body-Bold", fontSize=12.5, leading=16, textColor=INK, spaceBefore=10, spaceAfter=4,
                    keepWithNext=1)
body = ParagraphStyle("body", fontName="Body", fontSize=10, leading=14.2, textColor=INK, spaceAfter=6)
lead = ParagraphStyle("lead", parent=body, fontSize=11.5, leading=16.5, textColor=SLATE, spaceAfter=10, keepWithNext=1)
bullet = ParagraphStyle("bullet", parent=body, leftIndent=13, bulletIndent=2, spaceAfter=3)
caption = ParagraphStyle("caption", fontName="Body-Italic", fontSize=8.5, leading=11.5, textColor=FOG, spaceBefore=4, spaceAfter=10)
cell = ParagraphStyle("cell", fontName="Body", fontSize=8.8, leading=11.6, textColor=INK)
cell_head = ParagraphStyle("cell_head", parent=cell, fontName="Body-Bold", textColor=colors.white)
note = ParagraphStyle("note", parent=body, fontSize=9.5, leading=13.5, textColor=WINE)
toc_1 = ParagraphStyle("toc1", fontName="Body", fontSize=11, leading=19, textColor=INK, leftIndent=0)


def p(text, style=body):
    return Paragraph(text, style)


def bullets(*items):
    return [Paragraph(item, bullet, bulletText="•") for item in items]


def sym(char):
    """Un symbole absent de Segoe UI (étoile, flèches pleines...), pris dans Segoe UI Symbol."""
    return f'<font name="Symbol">{char}</font>'


# ------------------------------------------------------------------ Éléments

def capture(name):
    return os.path.join(CAPTURES, name)


def figure(name, text, width=WIDTH, keep=True):
    w, h = PilImage.open(capture(name)).size
    img = Image(capture(name), width=width, height=width * h / w)
    frame = Table([[img]], colWidths=[width])
    frame.setStyle(TableStyle([
        ("BOX", (0, 0), (-1, -1), 0.8, SLATE),
        ("LEFTPADDING", (0, 0), (-1, -1), 0), ("RIGHTPADDING", (0, 0), (-1, -1), 0),
        ("TOPPADDING", (0, 0), (-1, -1), 0), ("BOTTOMPADDING", (0, 0), (-1, -1), 0),
    ]))
    parts = [frame, Paragraph(text, caption)]
    # Dans une cellule de tableau, la liste suffit : KeepTogether n'y a pas de hauteur mesurable
    return KeepTogether(parts) if keep else parts


def figures(*items):
    """Deux ou trois captures côte à côte, chacune avec sa légende."""
    gap = 10
    width = (WIDTH - gap * (len(items) - 1)) / len(items)
    row = [figure(name, text, width, keep=False) for name, text in items]
    table = Table([row], colWidths=[width + (gap if i < len(items) - 1 else 0) for i in range(len(items))])
    table.setStyle(TableStyle([
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 0), ("RIGHTPADDING", (0, 0), (-1, -1), gap),
        ("RIGHTPADDING", (-1, 0), (-1, -1), 0),
        ("TOPPADDING", (0, 0), (-1, -1), 0), ("BOTTOMPADDING", (0, 0), (-1, -1), 0),
    ]))
    return KeepTogether([table, Spacer(1, 8)])


def table(rows, widths, head_color=SLATE):
    """Tableau avec en-tête sombre et lignes alternées. Les largeurs sont des fractions de la page."""
    data = [[Paragraph(str(c), cell_head) for c in rows[0]]]
    data += [[Paragraph(str(c), cell) for c in row] for row in rows[1:]]
    t = Table(data, colWidths=[w * WIDTH for w in widths], repeatRows=1)
    style = [
        ("BACKGROUND", (0, 0), (-1, 0), head_color),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("TOPPADDING", (0, 0), (-1, -1), 4), ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("LEFTPADDING", (0, 0), (-1, -1), 6), ("RIGHTPADDING", (0, 0), (-1, -1), 6),
        ("LINEBELOW", (0, -1), (-1, -1), 0.6, SLATE),
    ]
    for i in range(1, len(rows)):
        if i % 2 == 0:
            style.append(("BACKGROUND", (0, i), (-1, i), PAPER))
    t.setStyle(TableStyle(style))
    return KeepTogether([t, Spacer(1, 10)])


def stats(items, columns=4):
    """Rangée de chiffres clés : grand nombre, puis ce qu'il veut dire."""
    number = ParagraphStyle("stat_n", fontName="Pixel", fontSize=12, leading=17, textColor=INK, alignment=TA_CENTER)
    label = ParagraphStyle("stat_l", fontName="Body", fontSize=8.5, leading=11, textColor=FOG, alignment=TA_CENTER)
    cells = [[Paragraph(n, number), Paragraph(t, label)] for n, t in items]
    rows = [cells[i:i + columns] for i in range(0, len(cells), columns)]
    gap = 6
    w = (WIDTH - gap * (columns - 1)) / columns
    out = []
    for row in rows:
        inner = [Table([[c[0]], [c[1]]], colWidths=[w]) for c in row]
        for t in inner:
            t.setStyle(TableStyle([
                ("BACKGROUND", (0, 0), (-1, -1), PAPER),
                ("LINEABOVE", (0, 0), (-1, 0), 2.5, GOLD),
                ("TOPPADDING", (0, 0), (-1, -1), 6), ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
            ]))
        outer = Table([inner], colWidths=[w + gap] * (len(inner) - 1) + [w])
        outer.setStyle(TableStyle([
            ("LEFTPADDING", (0, 0), (-1, -1), 0), ("RIGHTPADDING", (0, 0), (-1, -1), gap),
            ("RIGHTPADDING", (-1, 0), (-1, -1), 0), ("TOPPADDING", (0, 0), (-1, -1), 0),
            ("BOTTOMPADDING", (0, 0), (-1, -1), gap),
        ]))
        out.append(outer)
    out.append(Spacer(1, 6))
    return out


def callout(text, color=GOLD):
    t = Table([[Paragraph(text, body)]], colWidths=[WIDTH])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, -1), CREAM),
        ("LINEBEFORE", (0, 0), (0, -1), 3, color),
        ("LEFTPADDING", (0, 0), (-1, -1), 10), ("RIGHTPADDING", (0, 0), (-1, -1), 10),
        ("TOPPADDING", (0, 0), (-1, -1), 7), ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
    ]))
    return KeepTogether([t, Spacer(1, 10)])


# ------------------------------------------------------------------ Schémas

def box(d, x, y, w, h, lines, fill, text_color=colors.white, size=8.5, bold_first=True):
    d.add(Rect(x, y, w, h, rx=5, ry=5, fillColor=fill, strokeColor=None))
    leading = size + 2.6
    top = y + h / 2 + (len(lines) - 1) * leading / 2 - size / 3
    for i, line in enumerate(lines):
        font = "Body-Bold" if bold_first and i == 0 else "Body"
        d.add(String(x + w / 2, top - i * leading, line, fontName=font, fontSize=size if i == 0 else size - 0.8,
                     fillColor=text_color, textAnchor="middle"))


def arrow(d, x1, y1, x2, y2, color=MIST, width=1.6):
    d.add(Line(x1, y1, x2, y2, strokeColor=color, strokeWidth=width))
    import math
    angle = math.atan2(y2 - y1, x2 - x1)
    size = 6
    left = (x2 - size * math.cos(angle - 0.45), y2 - size * math.sin(angle - 0.45))
    right = (x2 - size * math.cos(angle + 0.45), y2 - size * math.sin(angle + 0.45))
    d.add(Polygon([x2, y2, left[0], left[1], right[0], right[1]], fillColor=color, strokeColor=None))


def loop_diagram():
    """La boucle de jeu : huit étapes, quatre à l'aller, quatre au retour."""
    d = Drawing(WIDTH, 190)
    w, h, gap = 104, 50, (WIDTH - 4 * 104) / 3
    top, bottom = 122, 22
    steps = [
        (["1. Créer une idée", "un type, un public"], BLUE),
        (["2. Choisir", "ses composants"], BLUE),
        (["3. L'équipe", "développe seule"], SLATE),
        (["4. Lancer", "dès 2 composants"], ORANGE),
        (["5. Utilisateurs", "et revenus"], GREEN),
        (["6. Lire les avis", "tirés de la simulation"], GREEN),
        (["7. Améliorer", "ou créer autre chose"], BLUE),
        (["8. Agrandir", "équipe, locaux, arbre"], WINE),
    ]
    xs = [i * (w + gap) for i in range(4)]
    for i in range(4):
        box(d, xs[i], top, w, h, *steps[i])
        box(d, xs[3 - i], bottom, w, h, *steps[4 + i])
    for i in range(3):
        arrow(d, xs[i] + w + 2, top + h / 2, xs[i + 1] - 3, top + h / 2)
        arrow(d, xs[3 - i] - 2, bottom + h / 2, xs[2 - i] + w + 3, bottom + h / 2)
    arrow(d, xs[3] + w / 2, top - 2, xs[3] + w / 2, bottom + h + 4)
    arrow(d, xs[0] + w / 2, bottom + h + 2, xs[0] + w / 2, top - 4)
    d.add(String(WIDTH / 2, 92, "la boucle recommence avec plus de moyens", fontName="Body-Italic", fontSize=8.5,
                 fillColor=FOG, textAnchor="middle"))
    return d


def market_diagram():
    """Le chemin d'un utilisateur dans le modèle de marché, un pas par jour."""
    d = Drawing(WIDTH, 150)
    w, h = 112, 58
    gap = (WIDTH - 4 * w) / 3
    xs = [i * (w + gap) for i in range(4)]
    y = 80
    steps = [
        (["Public visé", "taille du segment", "(30 000 à 400 000)"], SLATE),
        (["Curieux", "bouche-à-oreille,", "marketing, organique"], BLUE),
        (["Utilisateurs", "attrait × concurrence", "× prix"], GREEN),
        (["Payants", "selon le modèle", "de revenus"], ORANGE),
    ]
    for x, (lines, color) in zip(xs, steps):
        box(d, x, y, w, h, lines, color, size=8.8)
    for i in range(3):
        arrow(d, xs[i] + w + 2, y + h / 2, xs[i + 1] - 3, y + h / 2)
    box(d, xs[2], 4, w, 46, ["Départs", "selon la satisfaction"], WINE, size=8.8)
    arrow(d, xs[2] + w / 2, y - 2, xs[2] + w / 2, 54)
    d.add(String(xs[0], 26, "Attrait = couverture des besoins", fontName="Body", fontSize=8.5, fillColor=INK))
    d.add(String(xs[0], 14, "× (1 − gêne des bugs) × performances", fontName="Body", fontSize=8.5, fillColor=INK))
    return d


def solvency_diagram():
    d = Drawing(WIDTH, 92)
    w, h = 110, 62
    gap = (WIDTH - 4 * w) / 3
    xs = [i * (w + gap) for i in range(4)]
    steps = [
        (["Facture échue", "pas assez d'argent", "à l'échéance"], colors.HexColor("#b86f50")),
        (["Impayés", "payée dès que", "l'argent revient"], ORANGE),
        (["Mise en demeure", "après 20 jours et", "500 CHF au moins"], RED),
        (["Faillite", "30 jours plus tard", "sans régularisation"], NIGHT),
    ]
    for x, (lines, color) in zip(xs, steps):
        box(d, x, 22, w, h, lines, color, size=8.8)
    for i in range(3):
        arrow(d, xs[i] + w + 2, 22 + h / 2, xs[i + 1] - 3, 22 + h / 2)
    d.add(String(WIDTH / 2, 4, "Tout payer, à n'importe quelle étape avant la faillite, ramène à la normale.",
                 fontName="Body-Italic", fontSize=8.5, fillColor=FOG, textAnchor="middle"))
    return d


def architecture_diagram():
    d = Drawing(WIDTH, 176)
    side_w, core_w = 128, 210
    core_x = (WIDTH - core_w) / 2
    box(d, 0, 58, side_w, 66, ["UI (Avalonia 12)", "affiche l'état", "et appelle les commandes"], BLUE, size=8.8)
    box(d, WIDTH - side_w, 58, side_w, 66, ["Tests (NUnit)", "65 tests", "+ bots d'équilibrage"], GREEN, size=8.8)
    d.add(Rect(core_x, 8, core_w, 164, rx=6, ry=6, fillColor=INK, strokeColor=None))
    d.add(String(core_x + core_w / 2, 156, "Core : le moteur, sans interface", fontName="Body-Bold", fontSize=9.2,
                 fillColor=GOLD, textAnchor="middle"))
    modules = [
        ("campaign", "état, temps, nuit, commandes"),
        ("products", "catalogue, marché, avis"),
        ("team", "personnes, recrutement"),
        ("space", "locaux, meubles, chemins"),
        ("finance", "comptes, factures, faillite"),
        ("progress", "arbre de développement"),
        ("persistence", "sauvegardes, migration"),
        ("bots", "stratégies jouées"),
    ]
    for i, (name, what) in enumerate(modules):
        yy = 136 - i * 16
        d.add(String(core_x + 14, yy, name, fontName="Body-Bold", fontSize=8.3, fillColor=colors.white))
        d.add(String(core_x + 84, yy, what, fontName="Body", fontSize=8.3, fillColor=MIST))
    arrow(d, side_w + 3, 91, core_x - 4, 91, color=SLATE)
    arrow(d, WIDTH - side_w - 3, 91, core_x + core_w + 4, 91, color=SLATE)
    return d


# ------------------------------------------------------------------ Pages

class Dossier(BaseDocTemplate):
    def __init__(self, path):
        super().__init__(path, pagesize=A4, title="Startup Rush : dossier de présentation", author="m4rm0tt",
                         subject="Présentation du jeu Startup Rush", leftMargin=MARGIN_X, rightMargin=MARGIN_X)
        cover = Frame(MARGIN_X, 40, WIDTH, PAGE_H - 80, id="cover", showBoundary=0)
        content = Frame(MARGIN_X, 58, WIDTH, PAGE_H - 58 - 46, id="content", showBoundary=0)
        self.addPageTemplates([
            PageTemplate(id="cover", frames=[cover], onPage=self.draw_cover),
            PageTemplate(id="content", frames=[content], onPage=self.draw_page),
        ])

    @staticmethod
    def draw_cover(canvas, doc):
        canvas.saveState()
        canvas.setFillColor(NIGHT)
        canvas.rect(0, 0, PAGE_W, PAGE_H, stroke=0, fill=1)
        canvas.restoreState()

    @staticmethod
    def draw_page(canvas, doc):
        canvas.saveState()
        canvas.setFillColor(INK)
        canvas.rect(0, 0, PAGE_W, 34, stroke=0, fill=1)
        canvas.setFillColor(GOLD)
        canvas.setFont("Pixel", 7)
        canvas.drawString(MARGIN_X, 13, "STARTUP RUSH")
        canvas.setFillColor(MIST)
        canvas.setFont("Body", 8.5)
        canvas.drawRightString(PAGE_W - MARGIN_X, 13, f"Dossier de présentation · {doc.page}")
        canvas.restoreState()

    def afterFlowable(self, flowable):
        if isinstance(flowable, Paragraph) and flowable.style.name == "h1":
            text = flowable.getPlainText()
            key = f"s{self.seq.nextf('section')}"
            self.canv.bookmarkPage(key)
            self.canv.addOutlineEntry(text, key, level=0)
            self.notify("TOCEntry", (0, text, self.page, key))


def section(label, title, intro=None, need=300):
    """Un chapitre. « need » : la place qu'il faut sur la page pour le commencer (plus quand il s'ouvre sur une grande capture)."""
    out = [CondPageBreak(need), Paragraph(label.upper(), kicker), Paragraph(title, h1)]
    if intro:
        out.append(Paragraph(intro, lead))
    return out


def build():
    story = []

    # --- Couverture
    title = ParagraphStyle("cover_t", fontName="Pixel", fontSize=34, leading=44, textColor=GOLD, alignment=TA_CENTER)
    sub = ParagraphStyle("cover_s", fontName="Body", fontSize=14, leading=20, textColor=colors.white, alignment=TA_CENTER)
    small = ParagraphStyle("cover_m", fontName="Body", fontSize=9.5, leading=14, textColor=MIST, alignment=TA_CENTER)
    story += [
        Spacer(1, 70),
        Paragraph("STARTUP RUSH", title),
        Spacer(1, 14),
        Paragraph("Dossier de présentation du jeu", sub),
        Spacer(1, 4),
        Paragraph("Un tycoon de la tech en pixel art : de ton appartement de Prilly à tes propres bureaux", small),
        Spacer(1, 34),
        figure("12_campagne_avancee.png", "", WIDTH),
        Spacer(1, 26),
        Paragraph("Windows 10/11 64 bits · C# / .NET 9 / Avalonia · octobre 2026", small),
        Paragraph("Toutes les images sont des captures du jeu réel, prises par son mode <i>--capture</i>.", small),
        NextPageTemplate("content"),
        PageBreak(),
    ]

    # --- Sommaire
    toc = TableOfContents()
    toc.levelStyles = [toc_1]
    toc.dotsMinLevel = 0
    story += [Paragraph("SOMMAIRE", kicker), Paragraph("Ce que contient ce dossier", h1_plain), Spacer(1, 6), toc, PageBreak()]

    # --- 1. Le jeu en bref
    story += section("Le jeu en bref", "Ton entreprise tech",
                     "Tu démarres seul dans ton appartement avec tes économies et un vieux portable. Tu inventes tes propres "
                     "applications et tes propres jeux, tu choisis leurs composants, ton équipe les développe toute seule, "
                     "tu lis ce qu'en disent les utilisateurs et tu fais grandir l'entreprise. La partie est longue, "
                     "sauvegardée, et continue tant que l'argent suit.")
    story += stats([
        ("18 000", "CHF au départ, plus 2 000 de découvert"),
        ("2 160", "CHF de dépenses fixes par mois"),
        ("≈ 8", "mois d'autonomie sans aucun revenu"),
        ("≈ 3", "semaines pour sortir le premier projet"),
        ("4", "types de projets (2 à débloquer)"),
        ("26", "composants, chacun sur 3 niveaux"),
        ("17", "nœuds dans l'arbre de développement"),
        ("4", "locaux, de l'appartement à l'étage complet"),
    ])
    story += [p("La boucle de jeu", h2), loop_diagram(), Spacer(1, 8)]
    story.append(table([
        ["Le jeu le fait tout seul", "Tu décides"],
        ["Enchaîner design, développement et tests, composant par composant", "Quels projets créer, et dans quel ordre l'équipe s'en occupe"],
        ["Répondre aux utilisateurs et corriger les bugs (part d'entretien fixe)", "Quels composants ajouter ou améliorer"],
        ["Choisir l'offre de serveurs qui tient la charge, sous ton plafond", "Prix, budget marketing, niveau d'entretien, plafond des serveurs"],
        ["Payer salaires, loyer et factures à l'échéance", "Recruter, former, accorder des congés ou une augmentation"],
        ["Signaler les problèmes et les regrouper", "Aménager, déménager, dépenser les points de l'arbre"],
    ], [0.5, 0.5]))
    story.append(callout(
        "<b>Ce que le jeu ne fait pas :</b> il ne recrute pas, ne loue pas, n'emprunte pas et n'augmente aucun budget à ta place. "
        "Il n'y a pas de clients ni de contrats : tout l'argent vient de tes propres projets. Il n'y a pas non plus de tutoriel : "
        "une phrase d'aide apparaît à la première ouverture de chaque outil."))

    # --- 2. Commencer
    story += section("Commencer", "Lancer une partie",
                     "Le menu propose Continuer, Charger, Nouvelle partie, Réglages et Quitter. Une nouvelle campagne "
                     "a son propre dossier de sauvegardes : elle n'écrase jamais une autre partie.")
    story.append(figures(("01_menu_principal.png", "Le menu principal."),
                         ("24_nouvelle_partie.png", "Nouvelle campagne : ton nom, l'entreprise, ton profil, une graine facultative.")))
    story.append(p("Trois profils de fondateur", h2))
    story.append(p("Le profil fixe tes compétences de départ (sur 100). Il change ta vitesse dans chaque discipline, "
                   "pas ce que tu as le droit de faire : tout le monde peut tout faire, plus lentement hors de sa spécialité."))
    story.append(table([
        ["Profil", "Design", "Développement", "Tests", "Support", "En pratique"],
        ["Développeur·se", "30", "62", "40", "35", "Le code avance vite, le design plus lentement."],
        ["Designer", "64", "38", "34", "40", "Interfaces et graphismes de meilleure qualité, code plus lent."],
        ["Polyvalent·e", "46", "48", "42", "46", "Aucun point faible marqué."],
    ], [0.17, 0.09, 0.17, 0.08, 0.1, 0.39]))
    story.append(p("<b>La graine</b> : laisser vide tire une partie au hasard. Avec la même graine, la même partie se rejoue "
                   "exactement : candidats, concurrents, bugs. Tout le hasard du jeu sort d'un seul générateur dont l'état est sauvegardé."))

    # --- 3. L'écran
    story += section("L'écran de jeu", "La scène et les menus",
                     "Ton local occupe tout l'écran, vu de trois quarts dessus. Chacun est assis à son poste ; l'icône "
                     "au-dessus de sa tête dit ce qu'il fait et la petite barre montre l'avancement de sa tâche.", need=460)
    story.append(figure("06_scene_travail.png",
                        "Lundi 4 janvier 2027, 13 h 41 : le fondateur travaille seul dans l'appartement, sur son vieux portable. "
                        "Le losange bleu signale du design. En haut : 18 000 CHF et −2 160 CHF par mois."))
    story += bullets(
        "<b>En haut à gauche</b> : l'entreprise, le local, la date et l'heure de jeu.",
        "<b>Au centre</b> : pause et quatre vitesses. Une étiquette jaune indique quand le temps est arrêté par un menu.",
        "<b>En haut à droite</b> : l'argent, le solde du mois, et le bouton <b>Journal</b>, qui devient <b>À traiter</b> en rouge quand un problème t'attend.",
        "<b>En bas</b> : les cinq menus. Projets (P), Équipe (T), Aménagement (B), Développement (D, avec les points à dépenser) et Finances (F).",
        "<b>Icônes de travail</b> : écran = développement, losange = design, coche = tests, enveloppe = support, éclair = maintenance, sablier = attend.",
    )
    story.append(p("Le temps", h2))
    story.append(p("Le jeu avance minute de jeu par minute de jeu, au rythme du temps réel mesuré, jamais au rythme des images. "
                   "Une journée de travail va de 9 h à 17 h, avec une pause à midi ; on ne travaille pas le week-end."))
    story.append(table([
        ["Vitesse", "Touche", "Minutes de jeu par seconde", "Une journée de 9 h à 17 h dure"],
        ["Pause", "Espace", "0", "—"],
        ["1", "1", "5", "1 min 36 s"],
        ["2", "2", "20", "24 s"],
        ["3", "3", "60", "8 s"],
        ["4", "4", "180", "2,7 s"],
    ], [0.18, 0.16, 0.33, 0.33]))
    story.append(p("Quand personne n'est au bureau (nuits, week-ends), le temps passe 8 fois plus vite. Les grands menus "
                   "(Projets, Finances…) mettent le jeu en pause par défaut. Les deux comportements se règlent dans Réglages."))
    story.append(p("Contrôles", h2))
    story.append(table([
        ["Touche ou geste", "Action"],
        ["Clic gauche", "Sélectionner une personne ou un objet ; poser un meuble en aménagement"],
        ["Clic droit", "Annuler la pose ; maintenu : déplacer la vue"],
        ["Molette, + / −", "Zoom (entier, pour garder les pixels nets)"],
        ["Flèches ou WASD", "Déplacer la vue (Origine : recentrer)"],
        ["Espace · 1 à 4", "Pause · vitesse"],
        ["P, T, B, D, F · N", "Les cinq menus · le journal"],
        ["Échap", "Ferme ce qui est ouvert, sinon ouvre la pause. Ne quitte jamais la partie."],
        ["F11", "Plein écran"],
    ], [0.3, 0.7]))

    # --- 4. Créer un projet
    story += section("Projets", "Créer un projet",
                     "Créer un projet, c'est choisir deux choses : un type et un public. Le nom, le prix et les composants "
                     "de départ ont des valeurs par défaut qui fonctionnent ; tu changeras tout plus tard.")
    story.append(figures(("03_portefeuille_vide.png", "Portefeuille vide : une grande carte invite à créer le premier projet."),
                         ("04_nouveau_projet.png", "Nouveau projet : quoi, pour qui, les composants proposés, le nom.")))
    story.append(p("Quatre types de projets", h2))
    story.append(table([
        ["Type", "Disponible", "Revenus", "Particularités"],
        ["Application", "dès le départ", "abonnement, ou gratuit + option payante", "Les utilisateurs partent lentement mais posent beaucoup de questions au support."],
        ["Jeu", "dès le départ", "vente à l'unité", "Une liste de souhaits se remplit avant la sortie et se convertit en ventes le jour du lancement. Ensuite, chaque nouveau joueur achète une fois. Une mise à jour fait revenir du monde."],
        ["App communautaire", "arbre (2 points)", "publicité, ou gratuit + option", "Bouche-à-oreille très fort, départs rapides, beaucoup de charge serveur."],
        ["Place de marché", "arbre (3 points)", "commission sur les ventes", "Il faut des vendeurs pour retenir les acheteurs : sans assez d'offres, ils repartent."],
    ], [0.18, 0.14, 0.22, 0.46]))
    story.append(p("Neuf publics", h2))
    story.append(p("Chaque public a sa taille (combien de personnes peuvent découvrir le projet), son prix habituel et "
                   "sa sensibilité au prix : plus elle est forte, plus un prix au-dessus de l'habitude fait fuir."))
    story.append(table([
        ["Type", "Public", "Taille", "Prix habituel", "Sensibilité au prix", "En une phrase"],
        ["Application", "Indépendants", "80 000", "12 CHF/mois", "1,3", "Petit prix, besoin de gagner du temps."],
        ["Application", "PME", "30 000", "39 CHF/mois", "0,8", "Paient plus, veulent du solide."],
        ["Application", "Grand public", "300 000", "4 CHF/mois", "1,8", "Nombreux, mais paient peu."],
        ["Jeu", "Joueurs occasionnels", "400 000", "6 CHF", "1,6", "Beaucoup de monde, petits prix."],
        ["Jeu", "Passionnés", "150 000", "20 CHF", "1,0", "Exigeants, prêts à payer un bon jeu."],
        ["App communautaire", "Étudiants", "400 000", "3 CHF", "2,0", "Nombreux et volatils."],
        ["App communautaire", "Créateurs", "200 000", "6 CHF", "1,3", "Veulent de l'audience."],
        ["Place de marché", "Artisans locaux", "150 000", "8 CHF", "1,0", "Peu de vendeurs au début."],
        ["Place de marché", "Occasion", "300 000", "5 CHF", "1,4", "Beaucoup de demande, faibles marges."],
    ], [0.17, 0.17, 0.1, 0.13, 0.12, 0.31]))
    story.append(p("Chaque public a un ou deux concurrents. Ils progressent lentement, et annoncent leurs nouvelles versions 10 à 20 jours à l'avance."))

    # --- 5. Composants et production
    story += section("L'atelier", "Composants et développement",
                     "Un projet est fait de composants : comptes, tableau de bord, gameplay, graphismes, paiement… "
                     "Chacun répond à certains besoins du public. L'atelier montre ce que ton public attend et ce qui y répond.", need=460)
    story.append(figure("09_atelier_composants.png",
                        "L'atelier de Factura, une application pour indépendants. En haut, les besoins du public et leur couverture. "
                        "« Demandé dans les avis » marque la Messagerie, réclamée par les utilisateurs."))
    story.append(p("Les besoins", h2))
    story.append(p("Sept besoins existent : <b>utilité au travail</b>, <b>confiance et finition</b>, <b>envie de payer</b>, "
                   "<b>envie de revenir</b>, <b>contenu et découverte</b>, <b>plaisir de jeu</b> et <b>offre des vendeurs</b>. "
                   "Chaque type de projet les pondère différemment : une application vit surtout de son utilité (40 %), un jeu de son "
                   "plaisir (35 %). Chaque composant comble une part de ce qui manque encore : le deuxième composant sur un même besoin "
                   "apporte moins que le premier. Ajouter tout ce qui existe n'est donc pas la bonne stratégie, d'autant que chaque "
                   "composant ajoute de la charge serveur, des questions au support et de l'entretien."))
    story.append(table([
        ["Niveau", "Valeur apportée", "Travail demandé", "Comment l'obtenir"],
        ["1", "×1", "×1", "Ajouter le composant"],
        ["2", "×1,5", "×1,4", "Améliorer"],
        ["3", "×1,85", "×2", "Améliorer, après avoir débloqué Composants avancés dans l'arbre"],
    ], [0.12, 0.2, 0.2, 0.48]))
    story.append(p("Certains composants en demandent d'autres (la Messagerie demande les Comptes). Un composant verrouillé "
                   "dit toujours pourquoi : dépendance, niveau 3 ou nœud de l'arbre."))
    story.append(p("Comment le travail avance", h2))
    story += bullets(
        "<b>Ajouter</b> ou <b>Améliorer</b> met le travail en file. Chaque amélioration devient une suite de tâches "
        "<b>design → développement → tests</b>, découpées en morceaux de 10 points au plus.",
        "<b>Un point de travail</b> = une heure d'une personne de niveau 50 sur un PC standard. La vitesse réelle dépend de "
        "la compétence, de l'ordinateur (vieux portable ×0,85, PC ×1, station ×1,10), de l'énergie, du moral, des heures du soir "
        "et de la taille de l'équipe.",
        "<b>Personne n'est jamais bloqué</b> : chacun prend la prochaine tâche prête, de préférence dans sa spécialité.",
        "<b>La qualité compte</b> : elle dépend de qui a fait le travail et dans quel état. Une qualité basse rend le composant "
        "moins utile et laisse passer plus de bugs ; les tests en attrapent une partie.",
    )
    story.append(figure("05_projet_en_developpement.png",
                        "Un projet en développement : la file des composants, l'avancement et le temps restant estimé."))

    # --- 6. Lancer et marché
    story += section("Sortie", "Lancer et faire vivre un projet",
                     "On peut lancer dès que deux composants sont terminés. Quand la file est vide, la carte affiche "
                     "« Prêt à sortir » et un bouton Lancer !.")
    story.append(figures(("07_pret_a_sortir.png", "25 janvier 2027 : Factura est prête, 21 jours après sa création, "
                                                   "avec le fondateur seul et aucune affectation manuelle."),
                         ("11_apercu_projet.png", "50 jours après la sortie : 94 utilisateurs, 43 payants, 374 CHF de revenus "
                                                  "sur 30 jours, note 3,2 sur 5.")))
    story += bullets(
        "<b>Après la sortie</b>, chaque amélioration terminée est une <b>mise à jour</b> : la version augmente, des curieux reviennent.",
        "<b>Sur un projet en ligne</b>, une part fixe du temps de l'équipe part dans l'entretien : répondre aux utilisateurs et corriger les bugs.",
        "<b>Fermer</b> un projet arrête ses coûts (serveurs, marketing) et ses revenus.",
    )
    story.append(p("Le marché, un pas par jour", h2))
    story.append(p("Chaque nuit, à minuit, chaque projet en ligne avance d'un jour. Toutes les étapes sont gardées dans "
                   "l'historique du projet : c'est ce qui permet au jeu d'expliquer une hausse ou une baisse, et d'en tirer les avis."))
    story.append(market_diagram())
    story.append(Spacer(1, 6))
    story += bullets(
        "<b>Face aux concurrents</b>, la part se calcule sur l'attrait au cube : être un peu meilleur rapporte beaucoup plus de monde.",
        "<b>Les serveurs</b> ralentissent au-delà de 80 % de charge et tombent en panne au-delà de 100 %.",
        "<b>Le support</b> : les questions s'accumulent si l'équipe n'y répond pas, et la satisfaction baisse.",
        "<b>Le marketing</b> a un rendement décroissant : doubler le budget ne double pas les visiteurs.",
    )
    story.append(table([
        ["Modèle", "Qui paie", "Ce qui le fait monter"],
        ["Abonnement", "une part des utilisateurs, chaque mois", "l'envie de payer et un prix proche de l'habitude du public"],
        ["Gratuit + option", "une petite part (5 % environ), chaque mois", "l'envie de payer ; plus d'utilisateurs car l'essai est gratuit"],
        ["Publicité", "la régie, facturée en fin de mois", "le nombre d'utilisateurs et leur envie de revenir"],
        ["Commission", "un pourcentage de chaque vente (panier moyen 55 CHF)", "assez de vendeurs pour les acheteurs"],
        ["Vente", "chaque nouveau joueur, une fois", "la liste de souhaits au lancement, puis les mises à jour"],
    ], [0.2, 0.38, 0.42]))

    # --- 7. Avis
    story += section("Avis", "Ce qu'en pensent les utilisateurs",
                     "Les avis ne sont pas du texte au hasard : ils viennent des vrais facteurs de la simulation, calculés "
                     "sur la dernière semaine. Ce ne sont pas des commandes, mais des tendances, avec un lien vers l'endroit où agir.", need=460)
    story.append(figure("08_avis.png",
                        "Les avis de Factura : 40 % disent « Rien ne me donne envie de revenir » (Améliorer mène au bon composant), "
                        "d'autres aiment le tableau de bord."))
    story.append(table([
        ["Ce qu'ils disent", "Quand", "Où agir"],
        ["« C'est lent à charger », « Ça ne marche plus du tout par moments »", "Serveurs chargés au-delà de 80 %, ou pannes", "Réglages → Serveurs"],
        ["« Ça plante trop souvent »", "Les bugs pèsent sur l'attrait", "Réglages → Entretien"],
        ["« Personne ne répond à mes questions »", "Les questions s'accumulent", "Réglages → Entretien"],
        ["« Trop cher pour ce que c'est »", "Prix nettement au-dessus de l'habitude du public", "Réglages → Prix"],
        ["« On s'ennuie vite », « Il manque l'essentiel »…", "Le besoin le plus mal couvert (moins de 35 %)", "Le composant qui y répond le mieux"],
        ["« Pas assez d'offres »", "Place de marché sans assez de vendeurs", "Annonces"],
        ["« La dernière mise à jour est vraiment bien », « J'adore… »", "Mise à jour récente, meilleur composant", "—"],
    ], [0.4, 0.36, 0.24]))
    story.append(p("Au plus trois tendances négatives et deux positives, avec la part des avis concernés. Quelques avis datés "
                   "s'ajoutent chaque lundi. La note sur 5 vient de la satisfaction de la dernière semaine."))

    # --- 8. Réglages
    story += section("Réglages d'un projet", "Prix, marketing, entretien, serveurs",
                     "Quatre réglages, tous avec une valeur par défaut sensée. Ils se changent à tout moment.", need=460)
    story.append(figure("10_reglages_projet.png", "Les réglages de Factura : prix, marketing, entretien et serveurs (automatiques, plafond 400 CHF)."))
    story += bullets(
        "<b>Prix</b> : comparé au prix habituel du public. Monter le prix d'un abonnement en ligne fait partir une partie des abonnés.",
        "<b>Entretien</b> : la part du temps de l'équipe du projet qui va au support et aux corrections. Peu 10 %, Normal 25 %, "
        "Prioritaire 45 %. Quand il n'y a rien à développer, tout le temps y passe.",
        "<b>Serveurs</b> : en automatique, la plus petite offre débloquée qui tient la charge à 75 % au plus, sans dépasser un "
        "plafond mensuel que tu fixes (400 CHF par défaut). Une alerte prévient si le plafond empêche de suivre.",
    )
    story.append(table([
        ["Canal marketing", "Disponible", "Efficace pour"],
        ["Réseaux sociaux", "dès le départ", "jeux et apps communautaires ; peu de professionnels"],
        ["Moteurs de recherche", "arbre (1 point)", "applications et places de marché : des gens qui cherchent déjà"],
        ["Salons et presse", "arbre (2 points)", "applications et jeux : cher, mais vise juste"],
    ], [0.28, 0.2, 0.52]))
    story.append(table([
        ["Offre de serveurs", "Coût mensuel", "Capacité (Mutualisé = 1)", "Disponible"],
        ["Mutualisé", "15 CHF", "1", "dès le départ"],
        ["Cloud S", "90 CHF", "5", "dès le départ"],
        ["Cloud M", "340 CHF", "22,5", "arbre : Cloud M"],
        ["Cloud L", "1 150 CHF", "85", "arbre : Cloud L et cluster"],
        ["Cluster", "3 900 CHF", "325", "arbre : Cloud L et cluster"],
    ], [0.28, 0.2, 0.26, 0.26]))

    # --- 9. Équipe
    story += section("Équipe (T)", "Recruter et garder son équipe",
                     "Au début, l'équipe, c'est toi. Embaucher fait avancer plus vite, mais un salaire coûte de 4 000 à "
                     "9 500 CHF par mois : c'est la première cause de faillite quand les revenus ne suivent pas.")
    story.append(figures(("14_equipe.png", "L'équipe : énergie, moral, et sur quoi chacun travaille (Auto ou un projet)."),
                         ("19_fiche_personne.png", "La fiche de Noah Rossi : compétences, horaire, salaire, rôle.")))
    story += bullets(
        "<b>Recrutement</b> : trois candidats ou plus chaque lundi, visibles une à deux semaines. Spécialité, niveau (Junior, "
        "Confirmé·e, Senior) et salaire, environ 3 800 + 70 × niveau CHF par mois. Certains ont un préavis de quelques jours. "
        "Plus tes projets ont d'utilisateurs, plus les profils sont expérimentés.",
        "<b>Affectation</b> : en <b>Auto</b>, chacun travaille sur le premier projet du portefeuille qui a du travail en file, "
        "sinon sur l'entretien du premier projet en ligne. On peut aussi fixer quelqu'un sur un projet. Une personne ne "
        "travaille que sur un projet à la fois.",
        "<b>Horaire</b> : 9 h–17 h ou 9 h–20 h. Les soirées produisent un peu plus, mais fatiguent et pèsent sur le moral.",
        "<b>Congés</b> : cinq jours ouvrés, salaire maintenu, énergie au maximum et moral en hausse.",
        "<b>Formation</b> : 900 CHF et deux jours d'absence pour gagner quelques points dans une compétence (moins à haut niveau).",
        "<b>Augmentation</b> : +8 % de salaire et +15 de moral (moitié moins pour la deuxième de l'année). Deux par an au plus.",
        "<b>Moral</b> : il tend vers une cible qui dépend du poste, du mobilier, du salaire comparé à la référence et des impayés. "
        "Sous 25 pendant 10 jours, la personne annonce sa démission pour dans 10 jours ouvrés, annulée si le moral remonte. "
        "Un salaire impayé pendant 14 jours la fait partir aussitôt.",
        "<b>Coordination</b> : au-delà de 3 personnes sur un même projet, chaque personne de plus coûte 12 % d'efficacité. "
        "Un responsable (nœud Responsables) porte la limite à 6, Grandes équipes ajoute 2, chaque tableau blanc 1 (2 au plus).",
    )

    # --- 10. Aménagement
    story += section("Aménagement (B)", "Le local et les meubles",
                     "Un poste de travail complet, c'est un bureau, une chaise juste derrière et un ordinateur posé dessus, "
                     "avec un accès libre. Sans poste complet, une recrue attend près de l'entrée et perd le moral.", need=460)
    story.append(figure("18_amenagement.png",
                        "L'aménagement du Petit bureau du Flon : l'état de chaque poste, le catalogue, et la case de pose (verte si possible)."))
    story.append(table([
        ["Meuble", "Prix", "Effet"],
        ["Bureau simple · Bureau large", "180 · 460 CHF", "Plan de travail ; le bureau large donne +3 de moral à la personne qui y travaille."],
        ["Chaise · Chaise ergonomique", "60 · 390 CHF", "L'ergonomique fatigue 20 % moins vite."],
        ["Vieux portable · PC fixe · Station de travail", "0 · 1 250 · 3 100 CHF", "Production ×0,85 · ×1 · ×1,10 et qualité +0,04."],
        ["Plante", "45 CHF", "+1 de moral pour l'équipe (6 plantes au plus)."],
        ["Machine à café · Canapé", "650 · 900 CHF", "Pauses plus reposantes ; une machine suffit pour 8 personnes ; le canapé ajoute +2 de moral."],
        ["Tableau blanc", "220 CHF", "Une personne de plus par équipe sans perte de coordination (2 au plus)."],
        ["Bibliothèque technique", "320 CHF", "Formations 15 % plus efficaces (2 au plus)."],
        ["Tapis", "120 CHF", "Décoration, sans effet sur la simulation."],
    ], [0.36, 0.2, 0.44]))
    story.append(KeepTogether([p("Les locaux", h2), table([
        ["Local", "Loyer mensuel", "Postes", "Notes"],
        ["Ton appartement, Prilly", "compris dans ta rémunération", "3 au plus", "Le salon et la chambre restent privés."],
        ["Petit bureau, Flon", "2 300 CHF", "6 à 7", "60 m² au centre-ville, kitchenette."],
        ["Plateau, Malley", "5 400 CHF", "jusqu'à 15", "130 m² ouverts près de la gare."],
        ["Étage complet, Renens", "12 500 CHF", "jusqu'à 30", "300 m² sur un étage."],
    ], [0.27, 0.24, 0.14, 0.35])]))
    story.append(p("Déménager coûte une garantie de trois mois de loyer (rendue au départ), un forfait de déménageurs et "
                   "35 CHF par meuble. Rien n'est perdu : les meubles suivent dans les cartons, à réinstaller dans le nouveau local. "
                   "Un meuble se revend à la moitié de son prix."))

    # --- 11. Arbre
    story += section("Développement (D)", "L'arbre de développement",
                     "Une seule monnaie : les points de développement. Ils viennent de vrais succès et servent à débloquer de "
                     "nouvelles possibilités. Aucune branche n'en exclut une autre et rien n'est irréversible.", need=460)
    story.append(figure("15_arbre.png", "L'arbre après 16 mois de jeu : 8 nœuds acquis, 6 points à dépenser, les jalons en bas."))
    story.append(table([
        ["Comment on gagne des points (une seule fois chacun)", "Points"],
        ["Un projet atteint 50 utilisateurs actifs (il a « trouvé son public »)", "+2"],
        ["Un projet passe 500, puis 5 000, puis 50 000 utilisateurs", "+1, +2, +3"],
        ["Un jeu se vend à 1 000 exemplaires", "+1"],
        ["Premier mois bénéficiaire", "+2"],
        ["Jalons : première embauche, vrais locaux, 1 000 utilisateurs, 3 projets qui ont trouvé leur public", "+1 chacun"],
        ["Jalons : 10 000 CHF de revenus par mois, 10 personnes, 50 000 utilisateurs au total", "+2 chacun"],
    ], [0.8, 0.2]))
    story.append(p("On démarre avec 1 point. Lancer des projets vides ne rapporte rien, et les mises à jour non plus."))
    story.append(table([
        ["Branche", "Nœuds (coût en points)"],
        ["Création", "Composants avancés, niveau 3 (1) · Apps communautaires (2) · Multijoueur (2, après Composants avancés) · Places de marché (3, après Apps communautaires)"],
        ["Organisation", "Recrutement élargi, +2 candidats par semaine (1) · Responsables (1) · Formation interne, moitié prix et +30 % (2) · Grandes équipes (2)"],
        ["Infrastructure", "Cloud M (1) · Optimisation, −15 % de charge (1) · Cloud L et cluster (2)"],
        ["Commercial", "Moteurs de recherche (1) · Lancements événementiels, +50 % d'intérêt avant la sortie d'un jeu (1) · Salons et presse (2) · Fidélisation, −15 % de départs (2)"],
        ["Innovation", "Laboratoire qualité, +0,05 de qualité (2) · Assistants de code, +10 % de production (3)"],
    ], [0.18, 0.82]))
    story.append(p("Un nœud débloqué fait apparaître sa nouveauté à l'endroit naturel : le type dans « Nouveau projet », "
                   "le composant dans l'atelier, le canal dans Réglages, l'offre dans Serveurs."))

    # --- 12. Argent
    story += section("Finances (F)", "L'argent",
                     "L'écran Finances montre l'essentiel : l'argent disponible, le solde du mois, où va l'argent, d'où il vient "
                     "et combien de jours tu tiens au rythme actuel. Les factures, les tableaux mois par mois et les financements "
                     "sont repliés dans les détails.", need=460)
    story.append(figure("16_finances.png",
                        "Avril 2028 : 12 347 CHF. Les revenus (24 216 CHF par mois, dont 22 096 de Planio) ne couvrent pas les "
                        "dépenses (29 122 CHF, surtout les salaires) : −4 906 CHF par mois, encore 88 jours d'autonomie."))
    story.append(table([
        ["Quand", "Ce qui est facturé", "Échéance"],
        ["Le 1er du mois", "Loyer", "le 5"],
        ["Le 1er du mois", "Internet, électricité, assurances (160 CHF + 4 % du loyer)", "le 10"],
        ["Le 1er du mois", "Hébergement et marketing du mois écoulé (comptés jour après jour)", "le 10"],
        ["Le 1er du mois", "Intérêts du découvert (1 % par mois) et échéances de prêt", "le 10, le 15"],
        ["Le 25 du mois", "Salaires et ta rémunération (2 000 CHF)", "le jour même"],
        ["Chaque jour", "Encaissements des projets (moins 3 % de frais de paiement)", "—"],
    ], [0.2, 0.62, 0.18]))
    story.append(p("Les factures se paient seules à l'échéance s'il y a l'argent (découvert compris), salaires d'abord, "
                   "puis ta rémunération, le loyer, les prêts et le reste."))
    story.append(KeepTogether([
        p("La faillite, une procédure annoncée", h2),
        p("Un solde légèrement négatif ne fait rien. C'est une facture impayée qui ouvre la procédure, avec un compte "
          "à rebours affiché et des alertes regroupées. La sauvegarde n'est jamais effacée."),
        solvency_diagram(),
        Spacer(1, 8),
    ]))
    story.append(table([
        ["Levier", "Conditions", "Ce qu'il apporte"],
        ["Réduire", "aucune", "Baisser le marketing, fermer un projet déficitaire, revendre ou ranger du matériel, réduire l'équipe, déménager plus petit."],
        ["Prêt bancaire", "60 jours d'historique, aucun impayé, un seul à la fois", "Jusqu'à 3 mois de revenus moyens (2 000 CHF au moins), sur 12 mois à 7 % par an."],
        ["Investisseur", "un projet à 1 500 utilisateurs ou 100 payants, en croissance de 20 % sur 30 jours", "50 000 à 400 000 CHF contre 10 à 30 % du capital ; une seule levée ; offre valable 21 jours."],
        ["Plan de redressement", "des impayés, et des revenus qui couvrent la moitié des charges fixes", "Les dettes échues étalées sur 6 mois, +10 % ; un tous les deux ans."],
    ], [0.18, 0.36, 0.46]))

    # --- 13. Journal
    story += section("Journal (N)", "Notifications",
                     "Trois catégories : à traiter, bonnes nouvelles, infos. Un problème qui se répète est regroupé (« ×3 ») au lieu "
                     "de s'empiler ; une fois réglé, il passe en « réglé » et quitte la liste. Un clic ouvre l'endroit où agir.", need=460)
    story.append(figure("17_journal.png", "Le journal : deux problèmes de support regroupés, chacun avec son compteur."))
    story.append(p("Seules les bonnes nouvelles et les nouveaux problèmes apparaissent en bulles en haut à droite ; les infos "
                   "restent dans le journal. Chaque catégorie a son son : alerte, impayé, jalon, lancement, encaissement."))

    # --- 14. Sauvegardes et fin
    story += section("Hors de la partie", "Pause, sauvegardes, fin de partie")
    story.append(figures(("22_pause.png", "Pause (Échap) : reprendre, sauvegarder sous un nom, charger, réglages, menu."),
                         ("25_charger.png", "Charger : les sauvegardes de chaque campagne, abîmées comprises."),
                         ("23_faillite.png", "Faillite : le bilan, puis charger une sauvegarde ou recommencer.")))
    story += bullets(
        "<b>Où</b> : <i>%AppData%\\StartupRush\\campaigns\\</i>, un dossier par campagne.",
        "<b>Quoi</b> : des sauvegardes nommées, trois sauvegardes automatiques tournantes (chaque lundi à minuit de jeu) et une "
        "sauvegarde de sortie, écrite en quittant ou en fermant la fenêtre. « Continuer » reprend la plus récente.",
        "<b>Sûres</b> : l'écriture passe par un fichier temporaire puis un remplacement atomique, et la version précédente reste en "
        "<i>.bak</i>. Un fichier illisible n'est jamais supprimé : il est signalé, copié en <i>.corrupt</i>, et le jeu essaie la copie.",
        "<b>Exactes</b> : recharger redonne exactement le même état et le même futur. Deux copies avancées de 20 jours restent "
        "identiques octet pour octet (testé).",
        "<b>Compatibles</b> : les parties de l'ancienne version à prestations sont converties au chargement, sans rien perdre ni "
        "inventer en argent, et l'original est gardé à côté.",
    )

    # --- 15. Équilibrage
    story += section("Équilibrage", "Ce que disent les mesures",
                     "L'équilibrage est mesuré par des bots qui jouent avec les mêmes commandes que l'interface, une décision par "
                     "jour ouvré, sans aucun avantage. Protocole : 8 façons de jouer × 5 graines, 18 mois de jeu, fondateur polyvalent.")
    story.append(table([
        ["Façon de jouer", "Faillites", "Argent final (médiane)", "1re sortie (jour)", "Revenus / mois (médiane)", "Utilisateurs (médiane)"],
        ["Une application, améliorée d'après les avis", "0/5", "50 036", "23", "24 791", "3 179"],
        ["Plusieurs petits projets à la suite", "0/5", "172 230", "23", "38 135", "1 675"],
        ["Un jeu ambitieux pour passionnés", "0/5", "92 034", "24", "15 326", "7 637"],
        ["Un petit jeu grand public + mises à jour", "0/5", "9 892", "30", "3 016", "3 583"],
        ["Erreur : 6 000 CHF de marketing par mois", "5/5", "−736", "23", "5 527", "897"],
        ["La même erreur, corrigée à temps", "0/5", "50 001", "23", "19 347", "2 479"],
        ["Erreur : trois embauches dès le départ", "5/5", "−916", "11", "974", "229"],
        ["Erreur : cinq projets d'un coup, promus avant la sortie", "5/5", "−502", "29", "947", "499"],
    ], [0.34, 0.1, 0.16, 0.12, 0.15, 0.13]))
    story += bullets(
        "<b>Les façons de jouer raisonnables survivent toutes</b>, avec des résultats très différents : le jeu récompense les choix, il ne les impose pas.",
        "<b>Les trois erreurs typiques mènent à la faillite</b> à chaque fois : trop de marketing, embaucher trop tôt, s'éparpiller.",
        "<b>Une erreur corrigée à temps se rattrape</b> : couper le budget dès que l'argent devient tendu suffit à s'en sortir.",
    )
    story.append(callout("<b>Limite :</b> ces mesures disent si l'économie tient debout, pas si le jeu est amusant. "
                         "Le plaisir de jeu n'a pas encore été validé par des joueurs humains.", ORANGE))
    story.append(p("Bien démarrer", h2))
    story += bullets(
        "Crée une application ou un jeu, laisse le fondateur travailler, lance dès que c'est prêt.",
        "Lis les avis et améliore ce qu'ils demandent : c'est le plus rentable.",
        "N'embauche que quand les revenus couvrent le nouveau salaire.",
        "Garde un œil sur le solde du mois en haut à droite et sur les jours d'autonomie.",
        "Dépense tes points : Composants avancés et Cloud M sont de bons premiers choix.",
    )

    # --- 16. Sous le capot
    story += section("Sous le capot", "Comment le jeu est construit",
                     "Startup Rush est écrit en C# sur .NET 9, environ 11 000 lignes réparties en trois projets. La règle qui "
                     "compte : le moteur ne connaît pas l'interface. Il tourne seul, et c'est ce qui permet de le tester et de le "
                     "faire jouer par des bots.")
    story.append(architecture_diagram())
    story.append(Spacer(1, 8))
    story += bullets(
        "<b>Une seule horloge</b> : la minute de jeu, un entier sauvegardé tel quel. Chaque nuit à minuit, le moteur fait tourner "
        "le marché, les avis, les échéances, l'équipe et les jalons. Chaque événement périodique a une clé (« rent:2027-02 ») "
        "et ne peut jamais se déclencher deux fois, même après un rechargement.",
        "<b>Un seul hasard</b> : un générateur xorshift64* dont l'état tient dans un nombre sauvegardé. Même graine, même partie.",
        "<b>L'argent en decimal</b> : des francs exacts, sans erreur d'arrondi, et des factures qui ne se paient qu'une fois.",
        "<b>Les réglages au même endroit</b> : toutes les valeurs d'équilibrage sont dans <i>Core/campaign/Tuning.cs</i> et les catalogues.",
        "<b>Les tests</b> : 65 tests NUnit (travail, projets, avis, finances, faillite, sauvegardes, aménagement, assets), plus la "
        "campagne d'équilibrage qui réécrit <i>BALANCE_REPORT.md</i>.",
        "<b>Les captures</b> : <i>StartupRush.exe --capture</i> joue un parcours scripté dans le vrai jeu et enregistre les écrans de "
        "ce dossier, avec ses sauvegardes dans un dossier temporaire à part.",
        "<b>Le pixel art</b> : les packs Kenney et 2dPig Pixel Office (CC0) sont découpés et recolorés vers la palette Endesga 32 par "
        "<i>tools/build_assets.py</i>, puis affichés au plus proche voisin avec un zoom entier.",
    )
    story.append(figures(("20_petite_fenetre.png", "960 × 540 : l'interface se réduit sans couper les actions."),
                         ("21_1080p.png", "1920 × 1080 : elle grandit, la scène reste nette.")))

    # --- 17. Histoire et suite
    story += section("Le projet", "D'où vient le jeu, et la suite",
                     "Startup Rush a commencé comme projet d'école et a changé trois fois de forme en trois semaines.")
    story.append(table([
        ["Date", "Étape"],
        ["6 – 9 octobre 2026", "Plan P0 : la solution Core / Tests / UI, l'horloge de jeu, le portefeuille, les charges de l'entreprise."],
        ["26 octobre 2026", "Demandes clients et tâches, machine à états, puis un roguelike : de « Fauché » à « Roi du monde », atouts, crises. Premiers assets intégrés."],
        ["27 octobre 2026", "Refonte en simulation d'entreprise persistante, avec des prestations clients et des sauvegardes."],
        ["27 octobre 2026", "Refonte « tycoon » : plus de clients, des projets à composants, la production automatique, les avis, l'arbre de développement, la vue 3/4 de dessus."],
        ["27 octobre 2026", "Branche <i>new-assets</i> : 31 nouveaux packs (27 sous CC0, 4 familles de polices sous OFL-1.1) ajoutés au dépôt."],
    ], [0.22, 0.78]))
    story.append(p("Limites connues", h2))
    story += bullets(
        "Testé seulement sur Windows 11 x64, avec une mise à l'échelle de 100 %.",
        "Équilibrage mesuré par des bots ; le plaisir de jeu reste à valider avec des joueurs.",
        "Les sons se chargent et se déclenchent (vérifié), mais n'ont pas été validés à l'oreille.",
        "Pas de rotation des meubles. Le matériel et la fabrication ne sont pas simulés.",
        "Les nouveaux packs ne sont pas encore intégrés : le jeu utilise toujours Kenney et Pixel Office.",
    )
    story.append(callout("<b>Pour la suite :</b> la consigne d'intégration des nouveaux assets (<i>PROMPT_CLAUDE_STARTUPRUSH.md</i>) "
                         "et le PDF de concept décrivent encore le roguelike, avec une pièce vue de face et un héros de dos. "
                         "La refonte a choisi une vue 3/4 de dessus. Il faudra trancher entre les deux avant d'intégrer les nouveaux packs.", BLUE))

    doc = Dossier(OUTPUT)
    doc.multiBuild(story)
    print(f"{OUTPUT} : {doc.page} pages, {os.path.getsize(OUTPUT) / 1e6:.1f} Mo")


if __name__ == "__main__":
    build()
