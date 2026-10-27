"""Inventaire (taille + SHA-256) et archive ZIP de livraison, puis verification de l'archive.

Usage : python tools/make_archive.py  ->  ../StartupRushOriginalAssets.zip + INVENTORY.csv
Exclut les caches de travail (build_cache/*.json restent inclus : ils sont la trace du build).
"""
import csv
import hashlib
import os
import sys
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXCLUDE_DIRS = {'__pycache__', '.git', 'bin', 'obj', 'csharp_bin', 'csharp_renders'}
EXCLUDE_EXT = {'.pyc'}


def sha256(path):
    h = hashlib.sha256()
    with open(path, 'rb') as f:
        for chunk in iter(lambda: f.read(1 << 20), b''):
            h.update(chunk)
    return h.hexdigest()


def files():
    out = []
    for dp, dn, fn in os.walk(ROOT):
        dn[:] = sorted(d for d in dn if d not in EXCLUDE_DIRS)
        for f in sorted(fn):
            if os.path.splitext(f)[1] in EXCLUDE_EXT:
                continue
            p = os.path.join(dp, f)
            rel = os.path.relpath(p, ROOT).replace('\\', '/')
            if rel == 'INVENTORY.csv':
                continue
            out.append((rel, p))
    return out


def main():
    lst = files()
    inv = os.path.join(ROOT, 'INVENTORY.csv')
    total = 0
    with open(inv, 'w', newline='', encoding='utf-8') as f:
        w = csv.writer(f)
        w.writerow(['path', 'bytes', 'sha256'])
        for rel, p in lst:
            sz = os.path.getsize(p)
            total += sz
            w.writerow([rel, sz, sha256(p)])
    zpath = os.path.join(os.path.dirname(ROOT), 'StartupRushOriginalAssets.zip')
    with zipfile.ZipFile(zpath, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for rel, p in lst + [('INVENTORY.csv', inv)]:
            z.write(p, 'StartupRushOriginalAssets/' + rel)
    # verification : relecture complete et CRC de chaque membre
    with zipfile.ZipFile(zpath) as z:
        bad = z.testzip()
        n = len(z.namelist())
    print(f'{len(lst) + 1} fichiers, {total / 1e6:.1f} Mo non compresses ; archive {os.path.getsize(zpath) / 1e6:.1f} Mo, '
          f'{n} membres, test CRC : {"OK" if bad is None else "ECHEC " + bad}')
    print(zpath)
    print('sha256 archive :', sha256(zpath))


if __name__ == '__main__':
    main()
