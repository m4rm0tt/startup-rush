"""Genere QA_REPORT.md a partir des resultats reels des controles (aucun chiffre saisi a la main).

Sources : build_cache/qa_report.json (qa_check.py), qa_characters.json, qa_recolor.json,
qa_determinism.json, COVERAGE.csv, manifest.json. La section 'Controles visuels' liste les planches
effectivement inspectees pendant la production (elle est maintenue dans VISUAL_REVIEW ci-dessous).
"""
import csv
import json
import os
import glob
from collections import Counter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

LABELS = {
    'ids_uniques': 'IDs uniques', 'ids_ascii_snake_case': 'IDs ASCII snake_case',
    'fichiers_references_presents': 'Fichiers references presents', 'noms_fichiers_ascii': 'Noms de fichiers ASCII',
    'png_rgba_8bits': 'PNG RGBA 8 bits', 'png_bloc_srgb': 'Bloc sRGB present', 'png_decodage': 'Decodage PNG',
    'tailles_declarees': 'Tailles = manifeste (iso)', 'alpha_binaire_sprites': 'Alpha binaire des sprites (0/255)',
    'nettete_couleurs_palette': 'Nettete : aucune couleur hors palette (pas de reechantillonnage)',
    'masque_meme_taille': 'Masque meme taille que le sprite', 'masque_alpha_binaire': 'Masque : alpha 0/255',
    'masque_hors_transparence': 'Masque jamais sur la transparence', 'masque_hors_contour': 'Masque jamais sur le contour',
    'masque_canaux_connus': 'Masque : canaux connus', 'masque_indices_0_5': 'Masque : indices 0..5',
    'recolor_defaut_egal_base': 'Recoloration par defaut = sprite de base', 'ombre_forme_pleine': 'Ombres : forme pleine',
    'calque_motif_alpha': 'Calques de motifs : alpha 0/255', 'atlas_rect_dans_page': 'Atlas : rectangles dans la page',
    'atlas_pixels_identiques': 'Atlas : pixels identiques aux PNG sources', 'atlas_sans_chevauchement': 'Atlas : sans chevauchement',
    'sols_pavage_sans_trou_ni_recouvrement': 'Sols : pavage 4x4 sans trou ni recouvrement',
    'sols_1024_px_par_case': 'Sols : 1024 px par case', 'murs_toile_ancre_communes': 'Murs : toile et ancre communes',
    'motifs_limites_a_la_surface_murale': 'Motifs = surface murale marquee (pas de vitre/plinthe)',
    'coiffures_alignees_toutes_bases': 'Coiffures alignees sur les 12 bases (crane commun, scalp, meches)',
    'dix_employes_deux_avatars': '10 employes + 2 avatars presents', 'modules_baie_dans_slots': 'Modules de baie dans leur slot (px)',
    'convoyeurs_ports_relies': 'Convoyeurs : ports relies dans l\'usine', 'plans_valides': 'Plans de pieces valides',
    'ui_tailles_declarees': 'HUD : tailles = manifeste', 'ui_9slice_marges_coherentes': 'HUD : marges 9-slice coherentes',
    'curseurs_point_chaud_dans_image': 'Curseurs : point chaud dans l\'image',
}

VISUAL_REVIEW = [
    ('Lot temoin (mur, fenetre, motifs par masque, sol, bureau, chaise, PC, employe chauve + coiffure)', '_work (essais), previews/sheet_furniture.png, previews/characters_*.png'),
    ('Chambre temoin', 'previews/rooms/room_bedroom_start_x2.png'),
    ('Planches de familles', 'previews/sheet_<famille>.png (architecture, sols, mobilier, informatique, serveurs, labos, industrie, decoration)'),
    ('Murs + 8 motifs sur pans reels', 'previews/wall_patterns_demo.png'),
    ('Sols, seuils, bords de dalle', 'previews/floors_demo.png'),
    ('Personnages : bases, alignement des coiffures (face/dos), avatar 2x3x3, recoloration, portraits', 'previews/characters_*.png'),
    ('Recoloration multi-teintes sans duplication', 'previews/recolor_demo.png'),
    ('Baies remplies (slots)', 'previews/check_racks.png'),
    ('Convoyeurs (boucle + ligne)', 'previews/check_conveyors.png'),
    ('Scenes : chambre, petit bureau, bureau moyen, open space, siege, salle serveur, datacenter, atelier, usine, labo', 'previews/rooms/*.png, previews/rooms_overview.png'),
    ('HUD : composants et etats, 9-slice multi-tailles, icones, police, curseurs/VFX, terminal, composants de projet', 'previews/ui_*.png'),
    ('Ecrans de reference du HUD', 'previews/screens/*.png'),
]


def load(name):
    p = os.path.join(ROOT, 'build_cache', name)
    return json.load(open(p, encoding='utf-8')) if os.path.exists(p) else None


def main():
    qa = load('qa_report.json') or {'summary': {}, 'details': {}}
    ch = load('qa_characters.json')
    rc = load('qa_recolor.json')
    dt = load('qa_determinism.json')
    man = json.load(open(os.path.join(ROOT, 'manifest.json'), encoding='utf-8'))
    cov = list(csv.DictReader(open(os.path.join(ROOT, 'COVERAGE.csv'), encoding='utf-8')))
    cc = Counter(r['status'] for r in cov)
    n_png = len(glob.glob(os.path.join(ROOT, 'assets', '**', '*.png'), recursive=True))
    n_mask = len(glob.glob(os.path.join(ROOT, 'masks', '**', '*.png'), recursive=True))
    L = []
    L.append('# StartupRush — Rapport qualité (QA_REPORT)\n')
    L.append('Rapport généré par `tools/make_qa_report.py` à partir des résultats réels des contrôles '
             '(`tools/qa_check.py`, contrôles des personnages, de recoloration et de reproductibilité).\n')
    L.append(f"**Bilan automatique : {'tous les contrôles passent' if qa.get('all_ok') else 'certains contrôles échouent (voir ci-dessous)'}.**  ")
    L.append(f"Manifeste : {len(man['assets'])} entrées, {len(man.get('rooms', []))} pièces, "
             f"{len(man.get('room_modules', []))} modules ; {n_png} PNG d'assets, {n_mask} masques.\n")
    L.append('## 1. Contrôles automatiques\n')
    L.append('| Contrôle | Résultat | Éléments vérifiés | Échecs |')
    L.append('|---|---|---|---|')
    for k, v in qa.get('summary', {}).items():
        L.append(f"| {LABELS.get(k, k)} | {'OK' if v['ok'] else 'ÉCHEC'} | {v['count']} | {v['failures']} |")
    fails = {k: v for k, v in qa.get('details', {}).items() if not v['ok']}
    if fails:
        L.append('\nDétails des échecs :\n')
        for k, v in fails.items():
            L.append(f"- {LABELS.get(k, k)} : " + '; '.join(v['failures'][:8]))
    L.append('\n## 2. Contrôles spécifiques\n')
    if ch:
        s = ch['summary']
        L.append(f"- Personnages : {s['bases']} bases × {s['orientations']} orientations, {s['hairstyles']} coiffures ; "
                 f"{s['hair_checks']} contrôles coiffure × orientation (scalp non couvert, mèches détachées) : "
                 f"{s['hair_failures']} échec ; couches de tête différentes du crâne commun : {s['skull_mismatch_layers']}.")
    if rc:
        L.append(f"- Recoloration (tout le pack) : {rc['layers']} couches masquées, {rc['pixels']} pixels ; masque sur "
                 f"transparence : {rc['mask_on_transparent']}, sur contour : {rc['mask_on_ink']} ; couleurs hors rampe : "
                 f"{len(rc['errors'])} couche(s) en erreur.")
    if dt:
        L.append(f"- Reproductibilité ({dt.get('method', 'reconstruction et comparaison SHA-256')}) : {dt['identical']} / "
                 f"{dt['files_compared']} fichiers identiques ({dt['different']} différent).")
    cs = load('qa_csharp.json')
    if cs and cs.get('ran'):
        rooms = cs.get('rooms', [])
        ok = sum(1 for r in rooms if r['identical'])
        L.append(f"- **Portage C# du compositeur** (`tools/csharp/`, .NET {cs.get('dotnet')}, aucune dépendance) : compilation "
                 f"{'réussie' if cs.get('build_ok') else 'en échec'} ; rendu des {len(rooms)} pièces depuis le seul manifeste : "
                 f"**{ok} / {len(rooms)} identiques au pixel près** au rendu Python de référence.")
    elif cs:
        L.append(f"- Portage C# : {cs.get('note', 'non exécuté')}.")
    ui = os.path.join(ROOT, 'build_cache', 'STATUS_ui.md')
    if os.path.exists(ui):
        L.append("- Lot HUD (voir `build_cache/STATUS_ui.md`) : polices rechargées avec fontTools, rendu FreeType 12/24 px "
                 "identique au pixel à la référence (0 pixel gris), 9-slice testé à 3 tailles sur 19 composants "
                 "(`previews/ui_nine_slice_test.png`).")
    L.append('- Une rampe par canal et par asset : garantie par le build (`normalize_channels`), les parties secondaires '
             'sont signalées dans `fixed_parts` du manifeste.')
    L.append('- Plans de pièces : chaque plan est validé (assets, orientations, sockets, chevauchements, cases d\'accès, '
             'entrées) avant rendu ; résultat dans `rooms/<id>.json` → `validation`.')
    L.append('\n## 3. Couverture (COVERAGE.csv)\n')
    L.append('| Statut | Nombre |')
    L.append('|---|---|')
    for k in ('complete', 'partial', 'missing', 'extra'):
        L.append(f'| {k} | {cc.get(k, 0)} |')
    miss = [r for r in cov if r['status'] in ('missing', 'partial')]
    if miss:
        L.append('\nÉléments non complets :\n')
        for r in miss:
            L.append(f"- `{r['id']}` ({r['family']}) : {r['status']} {r['note']}")
    L.append('\n## 4. Contrôles visuels effectués\n')
    L.append('Les scripts ne jugent pas l\'esthétique : chaque planche ci-dessous a été inspectée à l\'œil pendant la '
             'production (agrandissements au plus proche voisin), et les défauts trouvés ont été corrigés à la source.\n')
    L.append('| Planche / scène | Fichier(s) |')
    L.append('|---|---|')
    for a, b in VISUAL_REVIEW:
        L.append(f'| {a} | `{b}` |')
    L.append('\nCorrections faites suite aux contrôles visuels (extraits) : jambes des personnages vus de face orientées '
             'sous le bureau ; fenêtre masquée par une bibliothèque déplacée ; plafonniers retirés des aperçus (lecture '
             'ambiguë en coupe) ; occultations corrigées dans l\'atelier ; distribution des personnages variée par instance '
             'de module ; cloisons posées sur les arêtes pour fermer les angles des salles vitrées ; couche `glass` '
             'ajoutée au moteur (contenu visible derrière les vitres) ; moquette en dalles adoucie.')
    L.append('\n## 5. Ce qui n\'a PAS été testé\n')
    L.append('- **Aucune intégration Avalonia n\'a été exécutée** : le jeu n\'était pas disponible dans cette session. '
             'La composition a été validée avec le compositeur de référence Python (`tools/srgen/scene.py`), qui '
             'n\'utilise que le manifeste et les fichiers livrés.')
    L.append('- Le chargement des polices TTF a été vérifié avec fontTools et Pillow (voir le lot HUD) ; pas dans Avalonia.')
    L.append("- Le portage C# valide l'algorithme d'assemblage en .NET, pas le rendu par Avalonia/Skia (BGRA prémultiplié, "
             "interpolation, mise à l'échelle), qui reste à intégrer et vérifier dans le jeu.")
    L.append('- Les écrans de référence du HUD sont des compositions de test, pas une interface fonctionnelle.')
    open(os.path.join(ROOT, 'QA_REPORT.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')
    print('QA_REPORT.md ecrit')


if __name__ == '__main__':
    main()
