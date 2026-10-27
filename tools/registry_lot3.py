"""
Met à jour docs/asset_usage.csv avec le mobilier de confort branché au lot 3 (voir docs/V1_ETAT.md).

« validé » : un consommateur réel dans le jeu ET un test qui vérifie une conséquence (Tests/space/ComfortTest.cs).
« partiel » : effet codé avec la même règle que des objets testés, mais sans test qui vise cet identifiant.

    python tools/registry_lot3.py
"""
import csv
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTRY = os.path.join(ROOT, "docs", "asset_usage.csv")

CATALOG = "Core/space/Furniture.cs;Core/space/Layout.cs"
WALL = "Core/space/Furniture.cs;Core/space/Layout.cs;Core/space/RealEstate.cs;UI/Iso/RoomRenderer.cs"
CONSUMER = "Aménagement (achat, pose, sockets, murs), confort du local (Layout.Comfort), simulation"
TEST = "Tests/space/ComfortTest.cs : "


def row(indicator, state, test, files=CATALOG, unlock="dès le départ (catalogue)", consumer=CONSUMER):
    return dict(consommateur=consumer, fichier_definition=files, deblocage=unlock,
                indicateur_ou_interaction=indicator, etat=state, scenario_test=test)


updates = {}

validated = [
    (["photo_abstract", "clock_wall"], "ambiance +0,5 / +0,25 ; pose au mur plein, jamais sur la porte ni une fenêtre", WALL,
     "WallItemsHangOnPlainWallsBlindsOnWindowsNeverOnTheDoor"),
    (["lamp_ceiling"], "ambiance +0,75 ; au plafond, aucune case au sol", CATALOG, "ACeilingLampTakesNoFloorSpace"),
    (["locker", "toilet", "toilet_stall", "washbasin"], "confort de base (moral, 3 au plus) selon les personnes couvertes", CATALOG,
     "StorageAndSanitariesGiveBaseComfortForThePeopleTheyCover"),
    (["coffee_machine", "vending_drinks", "kitchen_sink", "kitchen_counter"], "énergie par pause, places limitées (15 au plus)", CATALOG,
     "BreakServicesServeALimitedNumberOfPeople"),
    (["sofa", "table_coffee"], "énergie par pause et moral de détente ; la table basse complète un coin assis", CATALOG,
     "ACoffeeTableOnlyCompletesASeatingArea"),
    (["partition_acoustic"], "production +2 % pour 4 personnes (4 % au plus)", CATALOG, "PartitionsHelpFocusUpToACapForThePeopleTheyCover"),
    (["table_meeting_small", "chair_meeting", "notebook", "whiteboard_mobile"],
     "coordination (personnes sans perte, 3 au plus ; petits supports 1 au plus)", CATALOG,
     "MeetingTablesCountWithTheirChairsAndSmallNotesAreCapped"),
    (["bookcase", "presentation_screen", "poster_schematic"], "efficacité des formations (40 % au plus)", WALL, "TrainingSupportsAreCapped"),
    (["reception_desk"], "1 candidat de plus chaque semaine", CATALOG, "AReceptionDeskBringsOneMoreCandidateEachWeek"),
]
for ids, indicator, files, test in validated:
    for i in ids:
        updates[i] = row(indicator, "validé", TEST + test, files)
updates["trophy_project"] = row("récompense du premier lancement (cartons), ambiance +0,5, sans revente", "validé",
                                TEST + "TheFirstLaunchLeavesATrophyToPlace", "Core/space/Furniture.cs;Core/campaign/Campaign.Daily.cs",
                                "jalon « Premier lancement »")

partial = {
    "plant_trailing": "ambiance +0,5 (sur un meuble)", "rug_large": "ambiance +1", "poster_project": "ambiance +0,5 (au mur)",
    "window_blinds": "ambiance +0,25 (sur une fenêtre)", "display_case": "ambiance +1",
    "figurine_robot": "moral du poste +0,5 (1 au plus)", "retro_message": "moral du poste +0,5 (1 au plus)",
    "water_cooler": "pause +4, 10 personnes", "bottle": "pause +2, 1 personne", "vending_snacks": "pause +3, 15 personnes",
    "fridge": "pause +3, 10 personnes", "microwave": "pause +3, 8 personnes (sur un plan de travail)", "snack_bowl": "pause +1,5, 3 personnes",
    "armchair": "pause +2 pour 1 personne, détente +1",
    "dresser": "rangement, 2 personnes", "nightstand": "rangement, 1 personne", "trash_bin": "propreté, 3 personnes",
    "recycling_station": "propreté, 8 personnes", "coat_rack": "rangement, 4 personnes", "filing_cabinet": "rangement, 3 personnes",
    "wardrobe_office": "rangement, 3 personnes", "shelf_metal": "rangement, 4 personnes",
    "sideboard_low": "rangement, 2 personnes ; socket café / micro-ondes", "pedestal_drawer": "rangement, 1 personne",
    "storage_boxes": "rangement, 1 personne (sur un meuble)", "cable_organizer": "rangement du poste, 1 personne",
    "kitchen_wall_cabinet": "rangement, 2 personnes (au mur)",
    "partition_low": "production +1 %, 4 personnes", "partition_glass": "production +1 %, 4 personnes",
    "partition_glass_door": "production +1 %, 4 personnes",
    "planning_board": "coordination +1 (au mur, deux segments)", "table_meeting_large": "coordination +2 avec 4 chaises, formations +5 %",
    "table_collab": "coordination +1", "sticky_notes": "coordination +0,25 (au mur)", "idea_sketch": "coordination +0,25 (au mur)",
    "paper_stack": "coordination +0,25 (sur un meuble)", "books_stack": "formations +5 % (sur un meuble)",
}
for i, indicator in partial.items():
    updates[i] = row(indicator, "partiel", "même règle que les objets testés dans " + TEST.split(" :")[0] + " ; pas de test sur cet identifiant",
                     WALL if i in ("poster_project", "window_blinds", "planning_board", "sticky_notes", "idea_sketch", "kitchen_wall_cabinet") else CATALOG)
updates["award_plaque"] = row("récompense des 10 000 CHF par mois (cartons), ambiance +0,5 au mur", "partiel",
                              "même code que le trophée (TheFirstLaunchLeavesATrophyToPlace) ; jalon non atteint dans les tests",
                              "Core/space/Furniture.cs;Core/campaign/Campaign.Daily.cs", "jalon « 10 000 CHF par mois »")

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
