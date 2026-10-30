"""
Met à jour docs/asset_usage.csv après la finition (suite du lot 7, voir docs/SUIVI_FINITION.md).

Trois états sont écrits ici :
- « validé » : consommateur réel, fichier de définition et un test qui vérifie une conséquence (effet de jeu, ou action et
  état d'un contrôle d'interface dans Avalonia.Headless : UiTests/) ;
- « partiel » : affiché par le vrai jeu et vu dans les captures, sans test qui vérifie l'élément lui-même ;
- « non applicable » : variante sans usage possible dans le jeu tel qu'il est, avec la raison.
Une ligne déjà « validée » n'est jamais rétrogradée.

    python tools/registry_finition.py
"""
import csv
import re
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTRY = os.path.join(ROOT, "docs", "asset_usage.csv")

updates = {}


def validated(i, consumer, files, indicator, test, unlock="toujours"):
    updates[i] = dict(consommateur=consumer, fichier_definition=files, deblocage=unlock, indicateur_ou_interaction=indicator,
                      etat="validé", scenario_test=test)


def partial(i, consumer, files, indicator, captures, unlock="toujours"):
    updates[i] = dict(consommateur=consumer, fichier_definition=files, deblocage=unlock, indicateur_ou_interaction=indicator,
                      etat="partiel", scenario_test=f"vu dans les captures {captures} du vrai jeu ; pas de test de l'élément lui-même")


def not_applicable(i, reason):
    updates[i] = dict(consommateur="—", fichier_definition="—", deblocage="—", indicateur_ou_interaction="—",
                      etat="non applicable", scenario_test=reason)


# ---------------------------------------------------------------- Modules de pièce, cartons, sols
MODULES = "Core/space/Pack.cs;Core/campaign/Campaign.Finishes.cs;UI/Hud/Panels/BuildPanel.cs"
for m in ["module_kitchenette", "module_lounge", "module_meeting_room", "module_pod_4", "module_reception", "module_restroom"]:
    validated(m, "Aménagement › Modules prêts à poser (Campaign.InstallModule)", MODULES,
              "objets et sol du module posés et payés au prix du catalogue ; effets habituels des meubles (confort, postes, coordination)",
              "Tests/campaign/FeaturesTest.cs : EveryRoomModuleOfThePackCanBeInstalledWithItsObjectsAndFloor ; ModulesBringTheRealEffectsOfTheirFurniture")
validated("moving_box", "Siège : cartons à l'entrée tant que des objets attendent d'être déballés ; clic → contenu réel et aménagement",
          "UI/Iso/RoomRenderer.cs;UI/Hud/SelectionInspector.cs", "nombre de cartons selon les objets rangés ; clic",
          "UiTests/RendererTest.cs : PackedObjectsWaitInMovingBoxesAtTheEntranceAndCanBeClicked")
validated("floor_marking", "Hachures des cases d'accès des machines (atelier, usine, laboratoires)", "UI/Iso/RoomRenderer.cs",
          "case à garder libre devant une station", "UiTests/RendererTest.cs : StationsGetFloorMarkingsAndModulesGetThresholds")
validated("floor_threshold", "Seuils à la porte et entre deux sols différents (plan, modules, finitions)", "UI/Iso/RoomRenderer.cs",
          "changement de sol lisible", "UiTests/RendererTest.cs : StationsGetFloorMarkingsAndModulesGetThresholds")

# ---------------------------------------------------------------- Composants de projet et graphe de dépendances
COMP = "Core/products/ProductCatalog.cs;UI/Hud/ComponentArt.cs;UI/Hud/Panels/ProjectPanel.cs"
COMP_TEST = ("Tests/campaign/FeaturesTest.cs : TheApiStorageAndGameMenuComponentsWaitForTheirDependencyAndCoverTheirNeeds ; "
             "UiTests/PanelsTest.cs : TheNewScreensShowWhatTheyPromise")
validated("comp_api", "Composant « API publique » (applications, places de marché, IA)", COMP, "productivité et conversion ; demande les comptes", COMP_TEST)
validated("icon_comp_api", "Pictogramme du composant « API publique »", COMP, "carte du composant", COMP_TEST)
validated("comp_storage", "Composant « Stockage de fichiers »", COMP, "productivité et confiance ; charge les serveurs ; demande les comptes", COMP_TEST)
validated("comp_game_menu", "Composant de jeu « Menus et options »", COMP, "confiance et engagement ; demande le gameplay", COMP_TEST)
GRAPH = "UI/Hud/DependencyGraph.cs;UI/Hud/Panels/ProjectPanel.cs"
for i in ["link_h", "link_v", "link_dashed_h", "link_dashed_v", "link_corner_ne", "link_corner_nw", "link_corner_se", "link_corner_sw",
          "link_tee_down", "link_tee_up", "link_arrow_right", "port_out_right", "port_in_left"]:
    partial(i, "Atelier de composants › Dépendances entre composants (graphe orienté de gauche à droite)", GRAPH,
            "lien acquis / en file / possible plus tard (pointillés) / cassé ; port connecté, libre ou invalide", "09b")
for i in ["link_arrow_down", "link_arrow_left", "link_arrow_up", "port_in_bottom", "port_in_right", "port_in_top",
          "port_out_bottom", "port_out_left", "port_out_top"]:
    not_applicable(i, "le graphe des dépendances va toujours de gauche (composant demandé) à droite (composant qui le demande) : "
                      "un lien n'entre jamais par le haut, le bas ou la droite d'une carte")

# ---------------------------------------------------------------- Murs, curseurs, poignée
not_applicable("wall_corner_outer", "aucun plan du pack n'a d'angle saillant : toutes les pièces sont rectangulaires et seuls les deux murs "
                                    "du fond sont dessinés (vérifié dans rooms/*.json) ; pas de pièce en L dans le jeu")
not_applicable("wall_opening", "aucun plan du pack n'a de passage sans porte ; un passage dans un mur du fond donnerait sur l'extérieur "
                               "(le jeu ne simule pas de pièce voisine) ; les sites sont des locaux séparés")
for i in ["cursor_resize_v", "cursor_resize_d1", "cursor_resize_d2"]:
    not_applicable(i, "seul le panneau latéral se redimensionne, à l'horizontale (cursor_resize_h) ; la fenêtre garde le cadre de Windows, "
                      "qui fournit ses propres curseurs de redimensionnement")
not_applicable("icon_drag_grip", "aucune liste ne se réordonne par glisser-déposer (l'ordre des projets se change par ◀ ▶ ou le menu) ; "
                                 "la poignée du panneau latéral utilise hud_drag_handle")
validated("cursor_busy", "Chargement d'une partie et sauvegarde manuelle (MainWindow.Busy)", "UI/MainWindow.axaml.cs;UI/Screens/MenuScreens.cs;UI/Screens/GameScreen.cs",
          "curseur d'attente pendant l'opération, puis curseur normal", "UiTests/PanelsTest.cs : ASlowOperationShowsTheBusyCursorThenRestoresIt")

# ---------------------------------------------------------------- Kit HUD branché dans les panneaux
KIT = "UI/Hud/HudKit.cs"
KIT_TEST = "UiTests/HudKitTest.cs : "
WIRED = "UiTests/PanelsTest.cs : TheHudKitIsWiredIntoTheRealPanels"
validated("hud_dropdown", "Aménagement › Catalogue (rubrique) ; Production › Transférer vers (site)", KIT + ";UI/Hud/Panels/BuildPanel.cs;UI/Hud/Panels/IndustryPanel.cs",
          "choix courant ; normal, survol, ouvert, désactivé", KIT_TEST + "TheDropdownShowsTheChoiceOpensThePackListAndReturnsThePick ; " + WIRED)
validated("hud_dropdown_list", "Liste ouverte des listes déroulantes", KIT, "un choix ferme la liste et rend la valeur",
          KIT_TEST + "TheDropdownShowsTheChoiceOpensThePackListAndReturnsThePick")
for i in ["hud_field_numeric", "hud_stepper_up", "hud_stepper_down"]:
    validated(i, "Production › Quantité d'un ordre de fabrication (1 à 500, pas de 5)", KIT + ";UI/Hud/Panels/IndustryPanel.cs",
              "valeur bornée ; flèche désactivée à la borne", KIT_TEST + "TheNumericFieldStepsWithinItsBoundsWithThePackArrows")
validated("hud_field_multiline", "Fiche projet › Nom, notes… : notes libres du projet (sauvegardées, 1 000 caractères)",
          KIT + ";UI/Hud/Panels/ProjectPanel.cs;Core/campaign/Campaign.Products.cs", "texte sur plusieurs lignes ; normal, survol, focus",
          KIT_TEST + "TheMultilineFieldAcceptsSeveralLinesAndReportsTheText ; " + WIRED + " ; Tests/persistence/SaveTest.cs : ProjectNotesAreBoundedAndSurviveAReload")
validated("hud_field_search", "Aménagement › Catalogue : recherche par nom dans tout le catalogue (2 lettres)", "UI/Hud/Ui.cs;UI/Hud/Panels/BuildPanel.cs",
          "objets filtrés sans perdre le focus ; « Aucun objet ne correspond »", WIRED)
validated("icon_search", "Loupe du champ de recherche", "UI/Hud/Ui.cs", "champ de recherche", WIRED)
for i in ["hud_menu_panel", "hud_menu_item", "hud_menu_separator"]:
    validated(i, "Portefeuille : menu d'un projet (clic droit sur la carte ou bouton « … ») : ouvrir, terminal, composants, avis, priorité",
              KIT + ";UI/Hud/Panels/ProjectsPanel.cs", "action lancée une fois ; entrée impossible grisée ; séparateur",
              KIT_TEST + "TheMenuRunsAnEntryOnceSkipsDisabledOnesAndShowsSeparators ; ARightClickOpensTheContextMenuOfAControl")
partial("icon_more", "Portefeuille : bouton « … » de chaque carte de projet", KIT + ";UI/Hud/Panels/ProjectsPanel.cs", "ouvre le menu du projet", "13")
for i in ["hud_gauge_base", "hud_gauge_fill", "hud_gauge_segment_map"]:
    validated(i, "Sites : jauges électricité et froid d'un site équipé (un segment par dixième)", KIT + ";UI/Hud/Panels/SitesPanel.cs",
              "part de l'alimentation et du froid prise", KIT_TEST + "TheGaugeLightsOneSegmentPerTenth ; " + WIRED)
FRAMES = KIT_TEST + "BannerChipSeparatorsSecondaryPanelCardStripedBarAndBarsUseTheirPackFrames"
validated("hud_banner", "Finances : bandeau quand des factures sont en retard", KIT + ";UI/Hud/Panels/FinancePanel.cs", "alerte lisible en clair", FRAMES)
validated("hud_chip_dark", "Portefeuille : type du projet (puce sombre)", KIT + ";UI/Hud/Panels/ProjectsPanel.cs", "type du projet", FRAMES + " ; " + WIRED)
validated("hud_separator_h", "Finances › Résultat par site (entre les mois), axe des barres", KIT + ";UI/Hud/Panels/FinancePanel.cs", "séparation", FRAMES)
validated("hud_separator_v", "Production › transfert (entre le site d'arrivée et les tarifs)", KIT + ";UI/Hud/Panels/IndustryPanel.cs", "séparation", FRAMES)
validated("hud_panel_secondary", "Contenu des sections dépliables (Ui.Details) : contenu de second niveau", KIT + ";UI/Hud/Ui.cs", "second niveau", FRAMES)
validated("hud_card", "Aménagement › Modules prêts à poser (carte d'un module, désactivée s'il est verrouillé)", KIT + ";UI/Hud/Panels/BuildPanel.cs",
          "normal, survol, désactivée", FRAMES)
validated("hud_chart_bar", "Finances › Résultat par site : une barre par site, hauteur = résultat réel du mois", KIT + ";UI/Hud/Panels/FinancePanel.cs",
          "positif vers le haut (vert), négatif vers le bas (rouge)", FRAMES + " ; " + WIRED)
validated("hud_progress_fill_striped", "Recherche : barre d'une recherche qui avance aujourd'hui", KIT + ";UI/Hud/Panels/ResearchPanel.cs",
          "travail en cours", FRAMES)
validated("hud_scroll_track_h", "Finances › Mois par mois : le tableau de tous les mois défile à l'horizontale", "UI/App.axaml;" + KIT,
          "défilement horizontal", KIT_TEST + "AWideTableScrollsHorizontallyWithThePackScrollbar ; " + WIRED)
for i in ["hud_scroll_thumb_h", "hud_scroll_grip_h", "hud_scroll_arrow_left", "hud_scroll_arrow_right"]:
    partial(i, "Barre de défilement horizontale (Finances › Mois par mois)", "UI/App.axaml;UI/Pixel/PackSkins.cs", "défilement ; survol et appui", "16")
validated("hud_theme_papier", "Jetons de couleur du thème « Papier » (Palette)", "UI/Pixel/Palette.cs", "couleurs de toute l'interface",
          KIT_TEST + "ThePaletteIsThePapierThemeOfThePack")
partial("hud_titlebar", "Inspecteur de sélection : nom de l'objet dans la barre de titre du pack", "UI/Hud/SelectionInspector.cs", "objet sélectionné", "12b")
partial("hud_chart_grid_v", "Courbes (projet, finances) : grille verticale", "UI/Hud/LineChart.cs", "repères de temps", "11b")

# ---------------------------------------------------------------- Icônes
SECTIONS = "UI/Hud/Ui.cs (SectionIcons)"
for i, where in {"icon_assembly": "Lignes du site", "icon_conveyor": "Ordres de fabrication", "icon_rack": "Stock et achats",
                 "icon_budget": "Financement", "icon_client": "D'où il vient", "icon_receivable": "À payer et à recevoir",
                 "icon_occupancy": "Résultat par site", "icon_cloud": "Services cloud", "icon_external": "Serveurs",
                 "icon_contract": "Locaux à louer", "icon_access": "Tes sites", "icon_components": "Modules prêts à poser (et menu d'un projet)",
                 "icon_filter": "Catalogue", "icon_memory": "Capacités de l'entreprise", "icon_competition": "Tendances"}.items():
    partial(i, f"Titre de section « {where} »", SECTIONS, "repère de la rubrique", "16, 27, 31")
partial("icon_cooling", "Sites : jauge du froid ; Sites › Capacités", "UI/Hud/Panels/SitesPanel.cs", "froid du site", "27")
partial("icon_caret_down", "Chevron des listes déroulantes", KIT, "liste à ouvrir", "18, 31")
partial("icon_sort", "Portefeuille : ordre de priorité des projets", "UI/Hud/Panels/ProjectsPanel.cs", "priorité", "13")
partial("icon_unavailable", "Aménagement › Catalogue : objet pas encore débloqué", "UI/Hud/Panels/BuildPanel.cs", "objet verrouillé", "18")
partial("icon_bug", "Avis : cause « bugs » ou « défauts »", "UI/Hud/Panels/ProjectPanel.cs", "cause de l'avis", "08")
partial("icon_conversion", "Fiche projet : besoin « conversion » du public", "UI/Hud/Panels/ProjectPanel.cs", "besoin du public", "11")
partial("icon_dependency", "Atelier de composants : en-tête des dépendances", "UI/Hud/Panels/ProjectPanel.cs", "graphe", "09b")
partial("icon_product", "Titre de section « Fabrication et ventes »", SECTIONS, "repère de la rubrique", "33")

# ---------------------------------------------------------------- Objets du catalogue (UiTests/CatalogCoverageTest.cs)
# Chaque objet achetable est posé, dessiné, cliquable (EveryBuyableObjectIsPlacedDrawnAndClickable) ; chaque effet déclaré
# dans Core/space/Furniture.cs est mesuré avec et sans l'objet (EveryDeclaredEffectMovesItsIndicator). Un objet passe à
# « validé » quand TOUS ses effets déclarés sont mesurés par ce test ; un objet dont l'effet est un rendement de travail
# (Output, Quality : ordinateurs, stations de labo) ou qui agit par son identifiant ailleurs reste « partiel ».
with open(os.path.join(ROOT, "Core", "space", "Furniture.cs"), encoding="utf-8") as f:
    furniture = f.read()
DEF = re.compile(r'new\("([a-z0-9_]+)", "[^"]*", FurnitureKind\.(\w+), ([0-9_]+)m, "(?:[^"\\]|\\.)*"\)\s*(\{[^}]*\})?')
MEASURED = {"BreakRecovery", "Coordination", "TrainingBonus", "Recruits", "Power", "Hosting", "Compute", "Gpu", "Storage",
            "Network", "PowerSupply", "Cooling", "Backup", "Organization", "Security"}
CATALOG_TEST = ("UiTests/CatalogCoverageTest.cs : EveryBuyableObjectIsPlacedDrawnAndClickable (affichage, clic) ; "
                "EveryDeclaredEffectMovesItsIndicator ({})")
for m in DEF.finditer(furniture):
    i, kind, price, init = m.group(1), m.group(2), int(m.group(3).replace("_", "")), m.group(4) or ""
    if price <= 0:
        continue
    effects = {k for k, _ in re.findall(r"(\w+) = ([^,}]+)", init)} - {"UnlockNode", "Reward", "EnergyDrain"}
    measured = {k for k in effects if k in MEASURED
                or (k == "Morale" and kind in ("Plant", "Ambiance", "Lounge", "Desk", "Personal"))
                or (k == "Focus" and kind != "Peripheral")
                or (k == "Covers" and ("BreakRecovery" in effects or kind in ("Storage", "Partition", "Sanitary")))}
    if effects and effects == measured:
        updates[i] = dict(consommateur=f"Aménagement : objet « {kind} » posé dans un local", fichier_definition="Core/space/Furniture.cs;Core/space/Layout.cs;Core/space/Infrastructure.cs;UI/Iso/RoomRenderer.cs",
                          deblocage="voir le catalogue", indicateur_ou_interaction=", ".join(sorted(effects)), etat="validé",
                          scenario_test=CATALOG_TEST.format(", ".join(sorted(effects))))

with open(REGISTRY, encoding="utf-8", newline="") as f:
    reader = csv.DictReader(f)
    columns = reader.fieldnames
    rows = list(reader)
# Garder le consommateur et le déblocage déjà écrits par les lots précédents quand ils sont plus précis
previous = {r["id"]: r for r in rows}
for i, u in updates.items():
    p = previous.get(i)
    if p and u["consommateur"].startswith("Aménagement : objet") and p["consommateur"]:
        u["consommateur"], u["deblocage"] = p["consommateur"], p["deblocage"] or u["deblocage"]
known = {r["id"] for r in rows}
missing = sorted(set(updates) - known)
assert not missing, f"IDs absents du registre : {missing}"
for r in rows:
    if r["id"] in updates and r["etat"] != "validé":
        r.update(updates[r["id"]])
with open(REGISTRY, "w", encoding="utf-8", newline="") as f:
    writer = csv.DictWriter(f, fieldnames=columns)
    writer.writeheader()
    writer.writerows(rows)
states = {}
for r in rows:
    states[r["etat"]] = states.get(r["etat"], 0) + 1
print(states)
print("encore non implémentés :", [r["id"] for r in rows if r["etat"] == "non implémenté"])
