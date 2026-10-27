"""Regenere toute la bibliotheque StartupRush depuis les sources.

Usage :
  python tools/build_all.py                 # tout
  python tools/build_all.py --only furniture,computers
  python tools/build_all.py --list          # familles disponibles

Ecrit assets/, masks/, manifest.json (entrees de chaque famille dans
build_cache/<famille>.json puis fusion). Deterministe : meme source -> memes PNG.
"""
import argparse
import importlib
import json
import os
import sys
import time
import glob

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'tools'))
sys.path.insert(0, os.path.join(ROOT, 'sources'))

from srgen import registry  # noqa: E402


def _jdefault(o):
    if hasattr(o, 'item'):
        return o.item()
    if hasattr(o, 'tolist'):
        return o.tolist()
    return str(o)


def load_sources(modules=None):
    """Importe sources/defs_*.py. Un module en erreur est signale et ignore
    (les autres familles restent constructibles)."""
    import traceback
    mods = []
    for path in sorted(glob.glob(os.path.join(ROOT, 'sources', 'defs_*.py'))):
        name = os.path.splitext(os.path.basename(path))[0]
        if modules and name not in modules:
            continue
        try:
            mods.append(importlib.import_module(name))
        except Exception:
            print(f"!! module {name} ignore (erreur d'import) :")
            traceback.print_exc()
    return mods


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--only', default='')
    ap.add_argument('--list', action='store_true')
    ap.add_argument('--quiet', action='store_true')
    ap.add_argument('--modules', default='', help='ex. defs_servers,defs_laboratories')
    ap.add_argument('--clean', action='store_true', help='ne pas fusionner avec le cache existant')
    args = ap.parse_args()
    load_sources(set(x for x in args.modules.split(',') if x) or None)
    fams = sorted(set([a.family for a in registry.REGISTRY] + [n for _, n, _ in registry.CUSTOM]))
    if args.list:
        print('\n'.join(fams))
        return
    only = set(x for x in args.only.split(',') if x)
    cache = os.path.join(ROOT, 'build_cache')
    os.makedirs(cache, exist_ok=True)
    log = (lambda s: None) if args.quiet else print
    t0 = time.time()
    for order, name, fn in sorted(registry.CUSTOM, key=lambda x: (x[0], x[1])):
        if only and name not in only:
            continue
        t = time.time()
        entries = fn(ROOT, log)
        json.dump(entries, open(os.path.join(cache, f'custom_{name}.json'), 'w', encoding='utf-8'),
                  ensure_ascii=False, indent=1, default=_jdefault)
        print(f"[{name}] {len(entries)} entrees ({time.time() - t:.1f}s)")
    by_fam = {}
    for a in registry.REGISTRY:
        by_fam.setdefault(a.family, []).append(a)
    for fam, assets in sorted(by_fam.items()):
        if only and fam not in only:
            continue
        t = time.time()
        entries = [registry.build_iso_asset(a, ROOT, None) for a in assets]
        path = os.path.join(cache, f'iso_{fam}.json')
        if args.modules and os.path.exists(path) and not args.clean:
            # construction partielle : fusion avec les entrees existantes des autres modules de la famille
            try:
                old = json.load(open(path, encoding='utf-8'))
            except Exception:
                old = []
            ids = {e['id'] for e in entries}
            entries = [e for e in old if e.get('id') not in ids] + entries
        json.dump(entries, open(path, 'w', encoding='utf-8'),
                  ensure_ascii=False, indent=1, default=_jdefault)
        print(f"[{fam}] {len(entries)} assets ({time.time() - t:.1f}s)")
    print(f"termine en {time.time() - t0:.1f}s")


if __name__ == '__main__':
    main()
