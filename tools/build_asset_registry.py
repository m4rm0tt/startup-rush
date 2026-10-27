"""
Tient à jour le registre d'usage des assets (docs/asset_usage.csv) : une ligne par ID du manifeste du pack
(assets/StartupRushOriginalAssets/manifest.json) et par plan ou module de pièce.

Le rôle prévu vient de l'annexe de la spécification (docs/specs/PROMPT_CLAUDE_STARTUPRUSH_V1_TOUS_LES_ASSETS.md).
Les colonnes de réalisation (consommateur, fichier, déblocage, indicateur, état, scénario de test) se remplissent
à la main au fur et à mesure que les systèmes existent : ce script ne les écrase jamais. Il ajoute les IDs nouveaux
et signale les IDs disparus du pack, sans rien inventer.

    python tools/build_asset_registry.py
"""
import csv
import json
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MANIFEST = os.path.join(ROOT, "assets", "StartupRushOriginalAssets", "manifest.json")
SPEC = os.path.join(ROOT, "docs", "specs", "PROMPT_CLAUDE_STARTUPRUSH_V1_TOUS_LES_ASSETS.md")
REGISTRY = os.path.join(ROOT, "docs", "asset_usage.csv")

COLUMNS = ["id", "categorie", "nom", "role_prevu", "consommateur", "fichier_definition", "deblocage",
           "indicateur_ou_interaction", "etat", "scenario_test"]

# États possibles : rien n'est « validé » sans consommateur réel, fichier de définition et scénario de test
NOT_DONE = "non implémenté"


def spec_roles():
    """Rôle prévu de chaque ID d'après l'annexe (tableaux « | `id` | ... | rôle | »)."""
    text = open(SPEC, encoding="utf-8").read()
    roles = {}
    for match in re.finditer(r"^\| `([^`]+)` \|(.*)\|\s*$", text, re.M):
        cells = [c.strip() for c in match.group(2).split("|")]
        roles[match.group(1)] = cells[-1]
    return roles


def manifest_rows():
    manifest = json.load(open(MANIFEST, encoding="utf-8"))
    for asset in manifest["assets"]:
        yield asset["id"], asset["category"], asset.get("name_fr", "")
    for key, category in (("rooms", "room"), ("room_modules", "room_module")):
        entries = manifest[key]
        for entry in entries if isinstance(entries, list) else entries.values():
            yield entry["id"], category, entry.get("name_fr", entry.get("name", ""))


def main():
    roles = spec_roles()
    existing = {}
    if os.path.exists(REGISTRY):
        with open(REGISTRY, encoding="utf-8", newline="") as f:
            existing = {row["id"]: row for row in csv.DictReader(f)}

    rows, seen = [], set()
    for asset_id, category, name in manifest_rows():
        seen.add(asset_id)
        row = existing.get(asset_id) or {c: "" for c in COLUMNS}
        row.update(id=asset_id, categorie=category, nom=name)
        row["role_prevu"] = roles.get(asset_id, row.get("role_prevu", ""))
        row["etat"] = row.get("etat") or NOT_DONE
        rows.append(row)

    gone = sorted(set(existing) - seen)
    missing_role = sorted(r["id"] for r in rows if not r["role_prevu"])

    os.makedirs(os.path.dirname(REGISTRY), exist_ok=True)
    with open(REGISTRY, "w", encoding="utf-8", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=COLUMNS)
        writer.writeheader()
        writer.writerows(rows)

    states = {}
    for r in rows:
        states[r["etat"]] = states.get(r["etat"], 0) + 1
    print(f"{len(rows)} lignes ({', '.join(f'{v} {k}' for k, v in sorted(states.items()))})")
    if gone:
        print("IDs retirés du pack (lignes supprimées) :", ", ".join(gone))
    if missing_role:
        print("IDs sans rôle dans la spécification :", ", ".join(missing_role))


if __name__ == "__main__":
    main()
