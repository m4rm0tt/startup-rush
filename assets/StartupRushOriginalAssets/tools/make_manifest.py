"""Fusionne build_cache/*.json + atlas + palette + pieces en un manifest.json unique.

Le manifeste est la seule source dont l'integrateur a besoin : chaque entree donne fichiers,
tailles, ancres, emprises, collisions, sockets, masques, etats UI... (voir INTEGRATION_GUIDE.md).
"""
import json
import os
import sys
import glob

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'tools'))

from srgen.registry import PACK_VERSION  # noqa: E402
from srgen.palette import RAMPS, FIXED, CHANNELS, SWATCHES, MASK_FLAG_PATTERN  # noqa: E402
from srgen.iso import TILE, WALL_H, LIGHT  # noqa: E402


def hexc(c):
    return '#%02x%02x%02x' % tuple(c[:3]) + ('' if c[3] == 255 else '%02x' % c[3])


def load_entries():
    entries = []
    for f in sorted(glob.glob(os.path.join(ROOT, 'build_cache', '*.json'))):
        name = os.path.basename(f)
        if not name.startswith(('custom_', 'iso_')):
            continue
        data = json.load(open(f, encoding='utf-8'))
        if isinstance(data, list):
            entries += [d for d in data if isinstance(d, dict) and 'id' in d]
    return entries


def inline_atlas(node, items):
    if isinstance(node, dict):
        f = node.get('file')
        if isinstance(f, str) and f in items and 'atlas' not in node:
            node['atlas'] = items[f]
            m = node.get('mask')
            if isinstance(m, str) and m in items:
                node['mask_atlas'] = items[m]
        for k, v in node.items():
            if k not in ('atlas', 'mask_atlas'):
                inline_atlas(v, items)
    elif isinstance(node, list):
        for v in node:
            inline_atlas(v, items)


def main():
    entries = load_entries()
    ids = [e['id'] for e in entries]
    dup = sorted(set(i for i in ids if ids.count(i) > 1))
    if dup:
        raise SystemExit(f'IDs en double : {dup}')
    atlas = {}
    ap = os.path.join(ROOT, 'atlases', 'atlas_index.json')
    if os.path.exists(ap):
        atlas = json.load(open(ap, encoding='utf-8'))
        for e in entries:
            inline_atlas(e, atlas.get('items', {}))
    rooms = []
    for f in sorted(glob.glob(os.path.join(ROOT, 'rooms', 'room_*.json'))):
        r = json.load(open(f, encoding='utf-8'))
        rooms.append({'id': r['id'], 'name_fr': r['name_fr'], 'file': f"rooms/{os.path.basename(f)}",
                      'size_tiles': r['size_tiles'], 'usage': r.get('usage', 'current'), 'tier': r.get('tier'),
                      'preview': f"previews/rooms/{r['id']}.png", 'valid': r.get('validation', {}).get('ok')})
    modules = [{'id': os.path.splitext(os.path.basename(f))[0], 'file': f"rooms/modules/{os.path.basename(f)}"}
               for f in sorted(glob.glob(os.path.join(ROOT, 'rooms', 'modules', '*.json')))]
    by_cat = {}
    for e in entries:
        by_cat.setdefault(e.get('category', 'misc'), []).append(e['id'])
    manifest = {
        'pack': 'StartupRushOriginalAssets', 'pack_version': PACK_VERSION, 'format_version': 1,
        'conventions': {
            'units': {'tile_px': [64, 32], 'subunits_per_tile': TILE, 'z': 'px', 'wall_height_px': WALL_H},
            'projection': 'screen_x = (U - V) * 32 ; screen_y = (U + V) * 16 - z_px (U bas-droite, V bas-gauche)',
            'orientations': {'se': 'face avant vers +U (visible)', 'sw': 'face avant vers +V (visible)',
                             'nw': 'face avant vers -U (dos visible)', 'ne': 'face avant vers -V (dos visible)',
                             'all': 'piece unique sans orientation'},
            'anchor': 'point du PNG (px) a placer sur la position ecran de reference (voir INTEGRATION_GUIDE)',
            'layers_order': ['floor', 'floor_edge/floor_overlay/floor_decor', 'shadow_layer (opacite 0.30, max)',
                             'walls', 'wall_items', 'objects (tri topologique)', 'ceiling'],
            'object_layers': {'base': 'sprite', 'glass': 'verre translucide, apres base', 'front': 'occultant',
                              'shadow': 'ombre pleine, couche commune'},
            'light_to_screen': 'haut-gauche', 'light_vector_world': [round(float(x), 4) for x in LIGHT],
            'shadow_layer_opacity': 0.30, 'shadow_color': hexc(FIXED['shadow']),
            'sampling': 'plus proche voisin, positions entieres', 'png': 'RGBA 8 bits, sRGB, alpha reel',
        },
        'palette': {'ramps': {k: [hexc(c) for c in v] for k, v in RAMPS.items()},
                    'fixed': {k: hexc(v) for k, v in FIXED.items()}},
        'channels': {'ids': CHANNELS, 'swatches': SWATCHES,
                     'mask_encoding': {'R': 'canal', 'G': 'indice de rampe 0..5',
                                       'B': f'drapeaux (bit {MASK_FLAG_PATTERN - 1} = surface murale a motif)',
                                       'A': '255 recolorable / 0 garder la couleur de base'}},
        'atlas': {k: v for k, v in atlas.items() if k != 'items'} if atlas else None,
        'atlas_items': atlas.get('items', {}) if atlas else {},
        'categories': {k: sorted(v) for k, v in sorted(by_cat.items())},
        'rooms': rooms, 'room_modules': modules,
        'assets': sorted(entries, key=lambda e: (e.get('category', ''), e['id'])),
    }
    json.dump(manifest, open(os.path.join(ROOT, 'manifest.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print(f"manifest.json : {len(entries)} entrees, {len(rooms)} pieces, {len(modules)} modules, "
          f"{len(atlas.get('items', {})) if atlas else 0} references d'atlas")


if __name__ == '__main__':
    main()
