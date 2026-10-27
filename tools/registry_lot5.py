"""
Met à jour docs/asset_usage.csv avec la recherche, les laboratoires et la production branchés au lot 5 (voir docs/V1_ETAT.md).

« validé » : un consommateur réel dans le jeu ET un test qui vérifie une conséquence (Tests/industry/IndustryTest.cs).
« partiel » : effet codé avec la même règle que des objets testés, ou vu seulement sur les captures du vrai jeu.

    python tools/registry_lot5.py
"""
import csv
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTRY = os.path.join(ROOT, "docs", "asset_usage.csv")

PROD = "Core/space/Furniture.cs;Core/industry/Industry.cs;Core/campaign/Campaign.Industry.cs"
LAB = "Core/space/Furniture.cs;Core/campaign/Campaign.Research.cs"
TEST = "Tests/industry/IndustryTest.cs : "


def row(consumer, indicator, state, test, files, unlock):
    return dict(consommateur=consumer, fichier_definition=files, deblocage=unlock,
                indicateur_ou_interaction=indicator, etat=state, scenario_test=test)


LINE = "Lignes de production (ports, convoyeurs, stations, diagnostics)"
updates = {}
for ids, indicator, test, unlock in [
    (["assembly_station_manual", "flash_station", "packing_station", "conveyor_straight", "conveyor_end"],
     "étapes assemblage, logiciel, emballage ; transport ; entrée au stock", "AnAssemblyLineTurnsMaterialsIntoBoxedProductsWithoutLosingAnything, ABlockedLineHoldsABoundedBufferAndResumes, StationsInTheWrongOrderAreDiagnosedNotTeleported", "arbre « Matériel »"),
    (["pick_and_place_machine", "reflow_oven", "aoi_inspection", "pcb_tester", "conveyor_transfer_table", "assembly_cell_robotic"],
     "ligne de cartes ; une station inutile laisse passer", "TheSuggestedFactoryLineMakesBoardsAndPassesThroughUnneededStations", "arbre « Industrialisation »"),
    (["chip_fab_tool", "precision_station", "cleanroom_partition", "cleanroom_airlock", "dust_extraction", "industrial_chiller"],
     "puces en salle propre (20 par plaquette, la moitié sans contrôle de précision)", "ChipsComeFromALoadedToolInACleanRoomAndPrecisionSavesHalf", "arbre « Salle propre »"),
]:
    for i in ids:
        updates[i] = row(LINE, indicator, "validé", TEST + test, PROD, unlock)
for i in ["workbench_electronics", "oscilloscope"]:
    updates[i] = row("Recherche (équipement requis sur le site du chercheur, poste de recherche)", "la recherche n'avance qu'avec son équipement",
                     "validé", TEST + "ResearchNeedsItsEquipmentKeepsProgressWhenPausedAndUnlocksAFamily", LAB, "arbre « Matériel »")

partial_lab = {
    "research_station": "poste de chercheur, +10 % de recherche (2 au plus)", "test_bench_pc": "poste et équipement (PC, serveurs, GPU)",
    "test_bench_embedded": "poste et équipement (portables)", "calibration_bench": "équipement (imprimantes 3D)",
    "instrument_rack": "équipement (serveurs, contrôle qualité, cartes en série, GPU)", "printer_3d": "équipement (imprimantes 3D, gabarits)",
    "laser_cutter": "équipement (gabarits d'assemblage)", "cobot_cell": "équipement (cobotique)", "soldering_station": "équipement (cartes)",
    "lab_power_supply": "outil d'établi (prototypes)", "microscope": "équipement (contrôle qualité, cartes en série, puces)",
    "cnc_mill_desktop": "équipement (imprimantes 3D, gabarits)",
}
for i, indicator in partial_lab.items():
    updates[i] = row("Recherche (équipement requis sur le site du chercheur)", indicator, "partiel",
                     "même règle que l'établi et l'oscilloscope (" + TEST + "ResearchNeeds…) ; pas de test sur cet identifiant", LAB, "arbre « Matériel »")

partial_prod = {
    "conveyor_curve": "transport à angle droit", "conveyor_junction_t": "réunit deux convoyeurs", "storage_rack_industrial": "stock du site +150",
    "pallet": "stock du site +40", "tote_bin": "stock du site +10 ; illustration des composants", "stress_test_rack": "−40 % de défauts sur 30 unités par jour",
    "control_cabinet": "supervise 4 automates sans opérateur",
}
for i, indicator in partial_prod.items():
    updates[i] = row(LINE, indicator, "partiel", "même code que les objets testés ; pas de test sur cet identifiant", PROD, "arbre « Matériel » ou « Industrialisation »")
for i, what in {"product_case": "boîtier", "product_pcb": "circuit nu et carte électronique", "product_cpu": "processeur", "product_ram": "module mémoire",
                "product_wafer": "plaquette", "product_gpu": "carte graphique (matière et produit fini)", "product_box": "emballage et produit fini emballé",
                "product_pc_assembled": "produit fini (PC, serveurs, portables)"}.items():
    updates[i] = row("Stock et achats (illustration d'un article)", what, "partiel", "vu sur la capture 31 du vrai jeu ; pas de test automatisé d'interface",
                     "Core/industry/Industry.cs;UI/Hud/Panels/IndustryPanel.cs", "avec le matériel")

SITES = "Core/space/RealEstate.cs;Core/campaign/Campaign.Sites.cs"
updates["room_hw_workshop"] = row("Sites : atelier matériel (recherche, petites séries)", "location ; machines du plan = aperçu", "validé",
                                  TEST + "AnAssemblyLineTurnsMaterialsIntoBoxedProductsWithoutLosingAnything", SITES, "arbre « Matériel »")
updates["room_factory_hall"] = row("Sites : usine (salle propre, racks livrés ; ligne du plan = disposition conseillée)", "location", "validé",
                                   TEST + "TheSuggestedFactoryLineMakesBoardsAndPassesThroughUnneededStations", SITES, "arbre « Industrialisation »")

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
