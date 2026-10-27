"""Reconstruction complete et ordonnee de la bibliotheque.

  python tools/rebuild_all.py                 # tout regenerer depuis sources/
  python tools/rebuild_all.py --determinism   # + seconde construction et comparaison SHA-256 de tous les fichiers

Etapes : nettoyage -> build_all (toutes les familles) -> planches -> plans de pieces -> planches d'ensemble ->
recoloration (demo + verification) -> atlas -> manifeste -> ecrans HUD -> QA -> verification C# -> couverture ->
rapport QA -> README. L'archive se fait ensuite avec tools/make_archive.py.
"""
import glob
import hashlib
import json
import os
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PY = sys.executable
T = os.path.join(ROOT, 'tools')


def run(*args):
    print('>>', ' '.join(os.path.relpath(a, ROOT) if os.path.isabs(a) else a for a in args), flush=True)
    r = subprocess.run([PY] + list(args), cwd=ROOT)
    if r.returncode != 0:
        raise SystemExit(f'echec : {args}')


def snapshot():
    out = {}
    for pat in ('assets/**/*.png', 'masks/**/*.png', 'assets/**/*.ttf', 'assets/**/*.json', 'rooms/**/*.json'):
        for f in glob.glob(os.path.join(ROOT, pat), recursive=True):
            out[os.path.relpath(f, ROOT).replace('\\', '/')] = hashlib.sha256(open(f, 'rb').read()).hexdigest()
    return out


def build_assets():
    run(os.path.join(T, 'build_all.py'), '--quiet')
    run(os.path.join(ROOT, 'sources', 'rooms', 'room_plans.py'))


def main():
    determinism = '--determinism' in sys.argv
    for d in ('assets', 'masks', 'atlases', 'build_cache', 'rooms'):
        p = os.path.join(ROOT, d)
        if os.path.isdir(p):
            keep = [f for f in os.listdir(p) if f.startswith('STATUS_')] if d == 'build_cache' else []
            saved = {f: open(os.path.join(p, f), 'rb').read() for f in keep}
            shutil.rmtree(p)
            os.makedirs(p)
            for f, data in saved.items():
                open(os.path.join(p, f), 'wb').write(data)
    build_assets()
    if determinism:
        a = snapshot()
        build_assets()
        b = snapshot()
        diff = sorted(f for f in a if a[f] != b.get(f))
        res = {'scope': 'assets, masks, polices, json d\'assets, plans de pieces', 'files_compared': len(a),
               'identical': len(a) - len(diff), 'different': len(diff), 'examples': diff[:10],
               'method': 'deux constructions completes successives, comparaison SHA-256'}
        json.dump(res, open(os.path.join(ROOT, 'build_cache', 'qa_determinism.json'), 'w', encoding='utf-8'), indent=1)
        print('determinisme :', {k: v for k, v in res.items() if k != 'examples'})
    run(os.path.join(T, 'make_previews.py'))
    run(os.path.join(T, 'make_overview.py'))
    run(os.path.join(T, 'make_arch_demos.py'))
    run(os.path.join(T, 'recolor.py'), '--demo')
    run(os.path.join(T, 'recolor.py'), '--verify')
    run(os.path.join(T, 'pack_atlas.py'))
    run(os.path.join(T, 'make_manifest.py'))
    if os.path.exists(os.path.join(T, 'ui_screens.py')):
        run(os.path.join(T, 'ui_screens.py'))
    run(os.path.join(T, 'qa_check.py'))
    run(os.path.join(T, 'csharp_check.py'))
    run(os.path.join(T, 'make_coverage.py'))
    run(os.path.join(T, 'make_qa_report.py'))
    run(os.path.join(T, 'make_readme.py'))


if __name__ == '__main__':
    main()
