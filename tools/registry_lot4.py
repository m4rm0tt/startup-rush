"""
Met à jour docs/asset_usage.csv avec l'infrastructure et les sites branchés au lot 4 (voir docs/V1_ETAT.md).

« validé » : un consommateur réel dans le jeu ET un test qui vérifie une conséquence (Tests/space/InfrastructureTest.cs,
Tests/space/SitesTest.cs). « partiel » : effet codé avec la même règle que des objets testés, sans test sur cet identifiant.

    python tools/registry_lot4.py
"""
import csv
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTRY = os.path.join(ROOT, "docs", "asset_usage.csv")

INFRA = "Core/space/Furniture.cs;Core/space/Infrastructure.cs;Core/campaign/Campaign.Infra.cs"
CONSUMER = "Infrastructure du site (capacités, électricité, froid, réseau), hébergement interne, offres cloud"
TEST = "Tests/space/InfrastructureTest.cs : "


def row(indicator, state, test, files=INFRA, unlock="arbre « Salle serveur » (catalogue)", consumer=CONSUMER):
    return dict(consommateur=consumer, fichier_definition=files, deblocage=unlock,
                indicateur_ou_interaction=indicator, etat=state, scenario_test=test)


updates = {}
validated = [
    (["rack_compact", "rack_standard", "module_server_1u", "module_storage_2u", "module_storage_jbod_4u", "module_network_switch_1u",
      "module_patch_panel_1u", "module_cable_manager_1u"],
     "emplacements en U, switch obligatoire, contrôleur de disques, organisation du câblage (−10 % sans)", "ModulesFillRackUnitsAndNeedASwitchAndAController"),
    (["server_tower_classic", "crac_unit", "fiber_box"], "capacité limitée par le froid, consommation du climatiseur, débit réseau du site",
     "PowerCoolingAndNetworkLimitWhatASiteDelivers"),
    (["rack_high_density", "module_gpu_4u", "module_blank_1u"], "chaleur des modules −20 % (porte refroidie), obturateurs −3 % (9 % au plus), GPU",
     "HighDensityRacksAndBlanksRunCooler"),
    (["ups_floor", "supervision_console"], "probabilité d'incident : couverture des onduleurs, supervision ÷2", "UpsAndSupervisionMakeIncidentsRarer"),
]
for ids, indicator, test in validated:
    for i in ids:
        updates[i] = row(indicator, "validé", TEST + test, unlock="dès le départ (catalogue)" if i in ("server_tower_classic", "fiber_box") else
                         "arbre « Haute densité »" if i in ("rack_high_density", "module_gpu_4u") else "arbre « Salle serveur » (catalogue)")
updates["cable_tray_straight"] = row("organisation du câblage +0,5 ; partage la case (en hauteur)", "validé",
                                     TEST + "ModulesFillRackUnitsAndNeedASwitchAndAController (retirés : −10 %)")

partial = {
    "module_compute_2u": "calcul 40 + hébergement 8", "module_compute_node_2u": "calcul parallèle 64 (sans hébergement)",
    "module_ups_2u": "onduleur de baie, 2 kW", "nas_desktop": "stockage 16 To sur un meuble", "rack_cooler_inrow": "froid +12 kW",
    "power_distribution_cabinet": "puissance du site +30 kW (livrée avec la salle serveur)", "router": "débit réseau +100",
    "network_switch_desk": "débit réseau +50", "access_point": "débit réseau +50 (au mur)", "network_cabinet": "organisation +2",
    "cable_tray_corner": "organisation +0,5", "cable_conduit": "organisation +0,5 (au sol)",
    "security_cage_panel": "sécurité +1 : confiance des offres cloud (+25 % de demande au plus)", "security_cage_door": "sécurité +2",
    "workstation_gpu": "poste de travail ×1,05 et 2 unités GPU", "workstation_compute": "poste de travail ×1,05 et 12 unités de calcul",
}
for i, indicator in partial.items():
    updates[i] = row(indicator, "partiel", "même règle que les objets testés dans " + TEST.split(" :")[0] + " ; pas de test sur cet identifiant")

SITES = "Core/space/RealEstate.cs;Core/campaign/Campaign.Sites.cs"
updates["room_server_compact"] = row("site technique : climatisation, armoire électrique, fibre, chemins de câbles livrés ; pas de bureaux en déménagement",
                                     "validé", "Tests/space/SitesTest.cs : TechnicalRoomsAreUnlockedInTheTreeAndHostNoDesks", SITES,
                                     "arbre « Salle serveur », location en plus", "Sites (louer, déménager, fermer, afficher)")
updates["room_datacenter"] = row("site technique à grande échelle (deux climatiseurs, armoires, cage livrés)", "partiel",
                                 "même code que la salle serveur ; pas de test dédié", SITES, "arbre « Datacenter »", "Sites")
updates["room_hq_floor"] = row("siège : jusqu'à 60 postes, accueil, cafétéria, sanitaires, réunions livrés", "partiel",
                               "même code que les autres bureaux ; pas de test dédié", SITES, "arbre « Plusieurs sites »", "Sites")

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
