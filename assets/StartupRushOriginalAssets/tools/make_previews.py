"""Genere les planches de controle par famille (previews/sheet_<famille>.png).
Usage : python tools/make_previews.py [famille,...]"""
import os, sys
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'tools'))
from srgen.scene import Library
from srgen.preview import family_sheet

def main():
    lib = Library(ROOT)
    only = set(sys.argv[1].split(',')) if len(sys.argv) > 1 else None
    fams = {}
    for aid, e in lib.index.items():
        fams.setdefault(e['category'], []).append(aid)
    for fam, ids in sorted(fams.items()):
        if only and fam not in only:
            continue
        if fam in ('hud', 'icons', 'fonts', 'cursors', 'vfx', 'terminal', 'project_components', 'characters'):
            continue
        out = os.path.join(ROOT, 'previews', f'sheet_{fam}.png')
        shp = family_sheet(lib, ids, out, title=f'STARTUPRUSH - {fam} - {len(ids)} IDS')
        print(fam, len(ids), shp)

if __name__ == '__main__':
    main()
