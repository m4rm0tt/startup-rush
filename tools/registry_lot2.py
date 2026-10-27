"""
Met à jour docs/asset_usage.csv avec ce que le lot 2 a réellement branché (voir docs/V1_ETAT.md).

« validé » : un consommateur réel dans le jeu ET un test qui vérifie une conséquence. « partiel » : branché, mais sans test
d'effet (ou vérifié seulement sur les captures du vrai jeu). Les décors des plans affichés sans fonction restent
« non implémenté », avec la mention de leur affichage.

    python tools/registry_lot2.py
"""
import csv
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTRY = os.path.join(ROOT, "docs", "asset_usage.csv")
PACK = os.path.join(ROOT, "assets", "StartupRushOriginalAssets")

CATALOG = "Core/space/Furniture.cs;Core/space/Layout.cs"
RENDER = "UI/Iso/RoomRenderer.cs"


def row(consumer, files, unlock, indicator, state, test):
    return dict(consommateur=consumer, fichier_definition=files, deblocage=unlock,
                indicateur_ou_interaction=indicator, etat=state, scenario_test=test)


updates = {}

# Postes de travail et équipements testés
for ids, indicator, test in [
    (["desk_simple", "chair_budget", "tower_office", "monitor_basic", "keyboard_mouse"],
     "poste complet (chaise, tour, écran, clavier)", "Tests/space/LayoutTest.cs : ADeskNeedsAChairAComputerAndScreensToBeAWorkstation"),
    (["laptop_standard", "chair_office"], "poste complet par portable ; exclusion écran/clavier",
     "Tests/space/LayoutTest.cs : ALaptopReplacesTheScreenAndKeyboard"),
    (["desk_compact"], "emprise, case d'accès réservée, passage", "Tests/space/LayoutTest.cs : FurnitureCannotOverlapLeaveTheRoomOrBlockAccess"),
    (["plant_floor_medium"], "moral d'ambiance plafonné (6)", "Tests/space/LayoutTest.cs : AmbianceHasDiminishingReturns"),
    (["bed_single"], "récupération nocturne du fondateur +10", "Tests/space/LayoutTest.cs : TheFounderBedHelpsRecoveryOvernight"),
    (["tower_used", "workstation_dev"], "production du poste (×0,85 / ×1,10, qualité)", "Tests/campaign/WorkTest.cs : BetterComputersHelpWithDiminishingReturns"),
    (["monitor_arm_second", "monitor_large"], "production +3 % par écran, plafond 6 % par poste", "Tests/campaign/WorkTest.cs : ExtraScreensHelpAPostUpToACap"),
]:
    for i in ids:
        updates[i] = row("Aménagement (achat, pose, sockets), simulation du travail", CATALOG, "dès le départ (catalogue)", indicator, "validé", test)

# Meubles jouables sans test d'effet dédié
partial = {
    "desk_tech": "moral du poste +3", "desk_corner": "moral du poste +2", "desk_long": "3 postes (portables)",
    "chair_ergonomic": "fatigue −20 %", "laptop_old": "production ×0,85", "laptop_pro": "production ×1,05, qualité +0,02",
    "plant_floor_tall": "ambiance +1 (plafonnée)", "plant_succulent": "ambiance +0,5 (plafonnée)", "lamp_floor": "ambiance +0,5",
    "lamp_desk": "ambiance +0,5", "rug_small": "ambiance +0,5", "trash_bin": "ambiance +0,25", "dresser": "ambiance +0,5",
    "nightstand": "ambiance +0,25", "mug": "moral du poste +0,5 (plafonné à 1)", "side_project": "moral du poste +0,5",
    "rubber_duck_debug": "moral du poste +0,5", "trophy_project": "ambiance +0,5", "coffee_machine": "pauses +12 d'énergie, 1 pour 8",
    "kitchen_counter": "socket pour machine à café / micro-ondes", "sofa": "pauses +8, moral +2", "armchair": "pauses +5",
    "whiteboard_mobile": "coordination +1 personne (2 au plus)", "bookcase": "formations +15 % (2 au plus)",
}
for i, indicator in partial.items():
    updates[i] = row("Aménagement (achat, pose, sockets), simulation", CATALOG, "dès le départ (catalogue)", indicator, "partiel",
                     "effet codé ; pas de test d'effet dédié (placement couvert par AssetManifestTest)")

# Personnages : bases et coiffures, tirées et sauvegardées
manifest = json.load(open(os.path.join(PACK, "manifest.json"), encoding="utf-8"))
for a in manifest["assets"]:
    if a["category"] == "characters":
        updates[a["id"]] = row("Apparence des personnes : scène, portraits, nouvelle campagne", "Core/team/Person.cs;UI/Iso/RoomRenderer.cs;UI/Iso/Portraits.cs",
                               "dès le départ", "présentation seulement, aucun effet statistique", "validé",
                               "Tests/campaign/ProgressAndTeamTest.cs : RecruitsAndFoundersUseOnlyPackLayersAndKeepTheirLook")

# Plans de location
for room, test in [("room_bedroom_start", "Tests/space/AssetManifestTest.cs : TheStartingBedroomGivesTheFounderACompleteWorkstation"),
                   ("room_startup_small", "Tests/space/LayoutTest.cs : MovingKeepsOwnedEquipmentLeavesWhatCameWithThePlaceAndChargesTheDepositOnce"),
                   ("room_office_medium", "Tests/space/AssetManifestTest.cs : EveryRentalPlanBecomesAValidPremises"),
                   ("room_open_space", "Tests/space/AssetManifestTest.cs : EveryRentalPlanBecomesAValidPremises")]:
    updates[room] = row("Locaux à louer (un site à la fois au lot 2)", "Core/space/RealEstate.cs;Core/space/Pack.cs", "location",
                        "taille, entrée, objets livrés, postes", "validé", test)

# Structure et décor des plans utilisés : rendus, vérifiés sur les captures
used_rooms = ["room_bedroom_start", "room_startup_small", "room_office_medium", "room_open_space"]
structure, decor = set(["wall_corner_inner", "wall_end", "floor_edge", "floor_edge_corner"]), set()
for room in used_rooms:
    plan = json.load(open(os.path.join(PACK, "rooms", room + ".json"), encoding="utf-8"))
    for f in plan["floor"]:
        structure.add(f["material"])
    for f in plan.get("floor_overlays", []):
        structure.add(f["asset"])
    for side in ("left", "right"):
        w = plan["walls"][side]
        if w.get("pattern"):
            structure.add("pattern_" + w["pattern"])
        for seg in w["segments"]:
            if seg:
                structure.add(seg if seg.startswith("wall_") else "wall_" + seg)
    for w in plan.get("wall_items", []):
        decor.add(w["asset"])
    for o in plan["objects"]:
        decor.add(o["asset"])
        for c in (o.get("children") or {}).values():
            decor.add(c if isinstance(c, str) else c["asset"])
for i in structure:
    updates.setdefault(i, row("Rendu isométrique des locaux", RENDER, "avec le local", "structure de la pièce (sol, murs, motifs)", "partiel",
                              "rendu vérifié sur les captures 02, 05, 12 du vrai jeu ; pas de test automatisé"))
for i in decor:
    updates.setdefault(i, row("Affiché dans un local loué (décor du plan)", RENDER, "avec le local", "aucun effet pour l'instant", "non implémenté",
                              "affichage seulement : une fonction reste à coder"))

# HUD « Papier », terminal, polices, icônes réellement employés par l'interface
hud = {
    "hud_panel_main": "cadre des menus", "hud_panel_inset": "encarts", "hud_button_primary": "boutons d'action principale",
    "hud_button_secondary": "boutons", "hud_button_destructive": "boutons de revente et de licenciement",
    "term_frame": "cadre du terminal", "term_progress_full": "mots validés de la session", "term_progress_empty": "mots restants",
    "term_mark_prompt": "étape en cours", "term_mark_info": "statistiques de la session",
    "font_startuprush_pixel": "police de l'interface", "font_startuprush_mono": "police du terminal et des chiffres",
}
icons = ["icon_invoice", "icon_energy", "icon_project", "icon_office", "icon_calendar", "icon_furnish", "icon_chart_line", "icon_success",
         "icon_time", "icon_cash", "icon_pc", "icon_trophy", "icon_comp_game_menu", "icon_risk", "icon_quality", "icon_assignment",
         "icon_retention", "icon_home", "icon_idea", "icon_link", "icon_lock", "icon_mail", "icon_marketing", "icon_users",
         "icon_subscription", "icon_plus", "icon_deploy", "icon_monitor", "icon_security", "icon_error", "icon_star", "icon_user",
         "icon_price", "icon_warning", "icon_network", "icon_terminal", "icon_support", "icon_maintenance", "icon_design", "icon_code",
         "icon_test"]
for i, role in hud.items():
    updates[i] = row("Interface", "UI/Pixel/PixelPanel.cs;UI/Hud/TerminalView.cs;UI/App.axaml", "dès le départ", role, "partiel",
                     "vérifié sur les captures du vrai jeu (01 à 25) ; pas de test automatisé d'interface")
for i in icons:
    updates[i] = row("Interface (icônes des menus et bulles de la scène)", "UI/Pixel/Icons.cs;UI/Scene/SceneCanvas.cs", "dès le départ",
                     "pictogramme d'une action ou d'une activité", "partiel", "vérifié sur les captures du vrai jeu ; pas de test automatisé")

with open(REGISTRY, encoding="utf-8", newline="") as f:
    reader = csv.DictReader(f)
    columns = reader.fieldnames
    rows = list(reader)
known = {r["id"] for r in rows}
missing = sorted(set(updates) - known)
assert not missing, f"IDs absents du registre : {missing}"
for r in rows:
    if r["id"] in updates:
        r.update(updates[r["id"]])
with open(REGISTRY, "w", encoding="utf-8", newline="") as f:
    writer = csv.DictWriter(f, fieldnames=columns)
    writer.writeheader()
    writer.writerows(rows)
states = {}
for r in rows:
    states[r["etat"]] = states.get(r["etat"], 0) + 1
print(states)
