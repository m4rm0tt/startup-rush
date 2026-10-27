"""COVERAGE.csv : croise ASSET_PLAN.csv (plan etabli avant production) et manifest.json.

Statuts : complete (ID livre avec toutes les orientations prevues), partial (livre, orientations manquantes),
missing (prevu, absent), extra (livre, non prevu : justifie dans les notes du manifeste).
Les lignes de type "groupe" (police, kit HUD...) sont evaluees sur la presence d'entrees de leur famille.
"""
import csv
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

GROUPS = {
    'font_startuprush_pixel': ('fonts', 'pixel'), 'font_startuprush_mono': ('fonts', 'mono'),
    'icons_set': ('hud', 'icon'), 'hud_panels': ('hud', 'panel'), 'hud_buttons': ('hud', 'button'),
    'hud_inputs': ('hud', ('field', 'checkbox', 'slider', 'dropdown', 'input')), 'hud_navigation': ('hud', ('tab', 'segment', 'scroll')),
    'hud_feedback': ('hud', ('toast', 'notification', 'tooltip', 'badge', 'progress')), 'hud_charts': ('hud', 'chart'),
    'cursors_set': ('cursors', ''), 'vfx_set': ('vfx', ''), 'terminal_kit': ('terminal', ''),
    'project_component_cards': ('project_components', ('card', 'component')),
    'project_component_previews': ('project_components', ('preview', 'thumb', 'component')),
    'reference_screens': ('screens', ''),
}


def main():
    man = json.load(open(os.path.join(ROOT, 'manifest.json'), encoding='utf-8'))
    assets = {a['id']: a for a in man['assets']}
    rooms = {r['id']: r for r in man.get('rooms', [])}
    plan = list(csv.DictReader(open(os.path.join(ROOT, 'ASSET_PLAN.csv'), encoding='utf-8')))
    planned = set()
    rows = []
    screens_dir = os.path.join(ROOT, 'previews', 'screens')
    n_screens = len([f for f in os.listdir(screens_dir) if f.endswith('.png')]) if os.path.isdir(screens_dir) else 0
    for p in plan:
        pid = p['id']; planned.add(pid)
        want = [x for x in p['orientations'].replace('(murs)', '').split('/') if x in ('se', 'sw', 'nw', 'ne')]
        status, got, note = 'missing', '', ''
        if pid in assets:
            a = assets[pid]
            keys = list((a.get('orientations') or {}).keys()) or list((a.get('variants') or {}).keys())
            got = '/'.join(keys)
            if a.get('symmetric') or not want or 'all' in keys or a.get('kind') in ('floor_tile', 'floor_overlay', 'character_base',
                                                                                       'character_hair'):
                status = 'complete'
            else:
                miss = [w for w in want if w not in keys]
                status = 'complete' if not miss else 'partial'
                note = '' if not miss else 'orientations absentes : ' + '/'.join(miss)
        elif pid in rooms:
            status = 'complete' if rooms[pid].get('valid') else 'partial'
            got = 'plan + apercu'
        elif pid in GROUPS:
            fam, key = GROUPS[pid]
            keys = key if isinstance(key, tuple) else (key,)
            if fam == 'screens':
                hits = n_screens
            elif pid == 'project_component_previews':
                hits = sum(1 for a in assets.values() if a.get('category') == fam and isinstance(a.get('preview'), dict))
            else:
                hits = sum(1 for a in assets.values() if a.get('category') == fam and
                           any(k in (a['id'] + ' ' + str(a.get('subcategory', ''))) for k in keys))
            status = 'complete' if hits else 'missing'
            got = f'{hits} entree(s)'
        rows.append([pid, p['family'], p['usage'], p['priority'], status, got, note])
    for aid, a in sorted(assets.items()):
        if aid in planned or a.get('category') in ('hud', 'fonts', 'cursors', 'vfx', 'terminal', 'project_components'):
            continue
        if a.get('category') == 'servers' and aid.startswith('module_'):
            continue
        rows.append([aid, a.get('category'), a.get('usage', ''), '-', 'extra',
                     '/'.join((a.get('orientations') or a.get('variants') or {}).keys()), (a.get('notes') or '')[:120]])
    with open(os.path.join(ROOT, 'COVERAGE.csv'), 'w', newline='', encoding='utf-8') as f:
        w = csv.writer(f)
        w.writerow(['id', 'family', 'usage', 'priority', 'status', 'delivered', 'note'])
        w.writerows(rows)
    from collections import Counter
    c = Counter(r[4] for r in rows)
    print('COVERAGE.csv :', dict(c))
    for r in rows:
        if r[4] in ('missing', 'partial'):
            print('  ', r)


if __name__ == '__main__':
    main()
