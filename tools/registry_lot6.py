"""
Met à jour docs/asset_usage.csv avec le studio, la zone XR, le laboratoire avancé, le quantique et les icônes branchés au lot 6
(voir docs/V1_ETAT.md).

« validé » : un consommateur réel dans le jeu ET un test qui vérifie une conséquence (Tests/lab/Lot6Test.cs).
« partiel » : branché, mais sans test d'effet propre à cet identifiant, ou vu seulement dans l'interface.

    python tools/registry_lot6.py
"""
import csv
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REGISTRY = os.path.join(ROOT, "docs", "asset_usage.csv")

FILES = "Core/space/Furniture.cs;Core/campaign/Campaign.Lab.cs"
TEST = "Tests/lab/Lot6Test.cs : "


def row(consumer, indicator, state, test, files, unlock):
    return dict(consommateur=consumer, fichier_definition=files, deblocage=unlock,
                indicateur_ou_interaction=indicator, etat=state, scenario_test=test)


updates = {}
for ids, indicator, test in [
    (["camera_tripod", "greenscreen_backdrop", "softbox_light", "mic_stand", "studio_monitors"],
     "score vidéo/audio du studio ; contenu +30 % au plus, marketing +15 % au plus",
     "AStudioScoresItsPartsWithCapsAndNeedsSomeoneOnSite, TheStudioMakesContentBetterAndMarketingReachFurtherWithinTheCap"),
]:
    for i in ids:
        updates[i] = row("Studio audiovisuel (qualité du contenu et des campagnes)", indicator, "validé", TEST + test, FILES, "arbre « Studio audiovisuel »")
for i in ["xr_headset_stand", "xr_base_station", "xr_play_mat"]:
    updates[i] = row("Zone de test XR (défauts des applications et kits XR)", "capacité de la zone : casque 40 %, balises, tapis ; moins de défauts",
                     "validé", TEST + "AHeadsetAloneServesAPrototypeAndTheFullZoneNeedsEverythingAndSomeone, TheXrZoneRemovesDefectsOfXrProducts", FILES, "arbre « Réalité étendue »")
for i in ["quantum_cryostat_closed", "quantum_control_rack", "cryo_support_unit"]:
    updates[i] = row("Installation quantique stabilisée (mise en route, calcul quantique vendu)", "unités quantiques prises en charge, 14 jours de mise en route, service vendu",
                     "validé", TEST + "ACryostatNeedsItsControlRackAndCryogenicSupport, AQuantumInstallationNeedsAWarmupBeforeItProvidesAnyCompute, QuantumComputeIsSoldAsAServiceOnlyOnceInstalledAndNotPromisedTwice",
                     FILES + ";Core/space/Infrastructure.cs", "arbre « Recherches avancées », recherche « Calibration cryogénique »")
updates["quantum_cryostat_open"] = row("Recherche quantique (équipement requis)", "équipement des recherches de calibration cryogénique", "partiel",
                                       "posé et acheté dans AdvancedLabRents… ; pas de test d'effet propre sur la recherche", FILES + ";Core/campaign/Campaign.Research.cs", "arbre « Recherches avancées »")
updates["compute_cabinet"] = row("Calcul avancé : 120 unités de calcul, équipement de « Grands modèles »", "calcul pour l'entraînement et la recherche", "partiel",
                                 "posé dans TheAdvancedLabRents… ; capacité comptée par la même règle que les serveurs testés", FILES, "arbre « Recherches avancées »")
updates["supervision_terminal"] = row("Supervision d'un site (incidents deux fois moins fréquents)", "même règle que la console de supervision", "partiel",
                                      "posé dans TheAdvancedLabRents… ; effet testé sur la console de supervision, pas sur cet identifiant", FILES, "arbre « Salle serveur »")
updates["room_advanced_lab"] = row("Sites : laboratoire avancé (recherche avancée, quantique)", "location ; machines du plan = aperçu", "validé",
                                   TEST + "TheAdvancedLabRentsPlacesAllItsNewEquipmentAndSellsNothingForFree", "Core/space/RealEstate.cs;Core/campaign/Campaign.Sites.cs", "arbre « Recherches avancées »")
for i, where in {"icon_ai": "arbre, projets d'IA, panneau IA", "icon_quantum": "arbre, produit quantique, panneau studio/XR/quantique", "icon_gpu": "réservé",
                 "icon_server": "réservé", "icon_test_station": "arbre (réalité étendue)", "icon_training": "réservé"}.items():
    if where == "réservé":
        continue
    updates[i] = row("Interface : " + where, "icône d'un nœud ou d'un type de projet", "partiel", "vu dans l'interface ; pas de test automatisé d'interface", "UI/Pixel/Icons.cs", "avec l'arbre")

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
