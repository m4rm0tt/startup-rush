"""Genere README_FR.md depuis sources/docs/README_FR.template.md avec les chiffres reels du manifeste."""
import json, os, glob
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
NAMES = {'architecture': 'Architecture (murs, angles, terminaisons)', 'floors': 'Sols, bords, seuils, marquages',
         'wall_patterns': 'Motifs muraux (calques)', 'furniture': 'Mobilier et vie des locaux',
         'computers': 'Postes informatiques et accessoires', 'servers': 'Datacenter (baies, modules, infrastructure)',
         'laboratories': 'Laboratoires et métiers tech', 'industry_future': 'Industrie informatique (future)',
         'decoration': 'Décoration et personnalité', 'characters': 'Personnages (bases + coiffures)',
         'hud': 'HUD : composants, icônes, thème', 'fonts': 'Polices originales', 'cursors': 'Curseurs',
         'vfx': 'Effets (primitives statiques)', 'terminal': 'Kit terminal', 'project_components': 'Composants de projet'}
LIMITS = [
    "**Intégration Avalonia non exécutée** (le jeu n'était pas disponible) : composition validée avec le compositeur de référence.",
    "Personnages en trois quarts stylisés : buste presque frontal, diagonales sw/ne obtenues par miroir (ombrage recalculé). Aucune animation (exigence de la commande).",
    "Le luminaire suspendu est livré mais absent des aperçus : en vue en coupe, son câble se lit comme un pied de lampadaire.",
    "Verre translucide : couche `glass` composée après le contenu ; deux vitres superposées dans un même objet ne montrent que la plus proche.",
    "Les 10 plans livrés sont rectangulaires ; les pièces en L sont possibles (angle saillant fourni) mais aucun plan en L n'est livré.",
    "Le parquet (vannerie fine) et la courbe de convoyeur sont lisibles mais simplifiés à l'échelle native.",
    "Aucun décor extérieur (exigence) : les fenêtres montrent un ciel stylisé.",
    "Polices vérifiées avec fontTools et Pillow ; le rendu Avalonia (anticrénelage du texte) reste à régler : l'atlas bitmap garantit un rendu au pixel.",
    "Les écrans de référence du HUD sont des compositions de test (complétude, lisibilité), pas une interface fonctionnelle.",
    "Couvrir toutes les inventions futures est impossible : le système d'extension (sources + générateur + critères §7G) est documenté.",
]


def main():
    man = json.load(open(os.path.join(ROOT, 'manifest.json'), encoding='utf-8'))
    tpl = open(os.path.join(ROOT, 'sources', 'docs', 'README_FR.template.md'), encoding='utf-8').read()
    stats = defaultdict(lambda: [0, 0, 0])
    for a in man['assets']:
        c = a['category']
        stats[c][0] += 1
        stats[c][1] += len(a.get('orientations') or a.get('variants') or a.get('states') or {}) or 1
    for c in stats:
        stats[c][2] = len(glob.glob(os.path.join(ROOT, 'assets', c, '**', '*.png'), recursive=True))
    rows = ['| Famille | IDs | Orientations / variantes / états | PNG |', '|---|---|---|---|']
    tot = [0, 0, 0]
    for c in NAMES:
        if c in stats:
            s = stats[c]
            rows.append(f'| {NAMES[c]} (`{c}`) | {s[0]} | {s[1]} | {s[2]} |')
            tot = [tot[i] + s[i] for i in range(3)]
    nmask = len(glob.glob(os.path.join(ROOT, 'masks', '**', '*.png'), recursive=True))
    rows.append(f'| **Total** | **{tot[0]}** | **{tot[1]}** | **{tot[2]}** (+ {nmask} masques) |')
    counts = '\n'.join(rows) + ('\n\nCes nombres comptent des IDs distincts. Les variantes de couleur ne sont pas '
                                'des fichiers : elles passent par les masques (voir COLOR_SYSTEM.md).')
    rr = ['| Pièce | Taille (cases) | Usage | Postes occupés | Cases libres à aménager |', '|---|---|---|---|---|']
    for r in sorted(man['rooms'], key=lambda r: r.get('tier') or 0):
        p = json.load(open(os.path.join(ROOT, r['file']), encoding='utf-8'))
        c = p.get('computed', {})
        rr.append(f"| {r['name_fr']} (`{r['id']}`) | {r['size_tiles'][0]} x {r['size_tiles'][1]} | {r['usage']} | "
                  f"{c.get('workstations', 0)} | {c.get('buildable_count', 0)} / {c.get('cell_count', 0)} |")
    out = tpl.replace('__COUNTS__', counts).replace('__ROOMS__', '\n'.join(rr)).replace('__LIMITS__',
                                                                                         '\n'.join('- ' + l for l in LIMITS))
    open(os.path.join(ROOT, 'README_FR.md'), 'w', encoding='utf-8').write(out)
    print('README_FR.md ecrit', tot)


if __name__ == '__main__':
    main()
