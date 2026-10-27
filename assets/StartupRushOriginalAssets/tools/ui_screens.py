"""Ecrans de reference du HUD (tests de completude et de lisibilite).

Usage :  python tools/ui_screens.py           -> previews/screens/*.png (natif 640x360 + x2)

Chaque ecran est compose UNIQUEMENT a partir des fichiers livres : PNG des
composants (assets/hud, assets/terminal, assets/project_components), icones,
police originale (rendu pixel exact), manifeste (build_cache/*.json) et rendus de
pieces via les plans rooms/*.json (tools/srgen/rooms.py). Les textes et nombres
sont dessines a l'execution : rien n'est cuit dans les composants.
Ces captures ne sont PAS des assets a integrer.
"""
import json
import os
import sys

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'tools'))
sys.path.insert(0, os.path.join(ROOT, 'sources'))

from srgen.scene import Library                                     # noqa: E402
from srgen.imgio import save_png, load_png                           # noqa: E402
from srgen.compose import recolor                                    # noqa: E402
from srgen.lot_ui_core import (canvas, fill, hline, vline, rect, rrect, rfill, put, blit, tint, scale,
                               nine, fonts, tok)                     # noqa: E402

W, H = 640, 360
OUT = os.path.join(ROOT, 'previews', 'screens')


class UI:
    def __init__(self):
        self.lib = Library(ROOT)
        self.F, self.M = fonts()
        self._room_cache = {}

    # -- acces aux fichiers livres -------------------------------------------
    def entry(self, iid):
        return self.lib.get(iid)

    def comp(self, hid, state=None):
        e = self.entry(hid if hid.startswith(('hud_', 'term_', 'comp_', 'port_', 'link_')) else 'hud_' + hid)
        if 'states' in e and e['states']:
            st = state if state in e['states'] else next(iter(e['states']))
            return self.lib.png(e['states'][st]['file']), e
        return self.lib.png(e['file']), e

    def draw9(self, img, hid, x, y, w, h, state=None):
        src, e = self.comp(hid, state)
        ns = e.get('nine_slice', [0, 0, 0, 0])
        rz = e.get('resize', 'both')
        if rz == 'horizontal':
            h = src.shape[0]
        if rz == 'vertical':
            w = src.shape[1]
        if rz == 'none' or ns == [0, 0, 0, 0] and e.get('stretch') != 'tile':
            blit(img, src, x, y)
            return src.shape[1], src.shape[0]
        out = nine(src, w, h, ns, e.get('stretch', 'stretch'))
        blit(img, out, x, y)
        return out.shape[1], out.shape[0]

    def extra(self, hid, key):
        e = self.entry('hud_' + hid)
        return self.lib.png(e['extras'][key]['file'])

    def icon(self, img, name, x, y, color='ink', k=1):
        ic = self.lib.png(self.entry('icon_' + name)['file'])
        c = tok(color) if isinstance(color, str) else color
        t = tint(ic, c)
        if k > 1:
            t = scale(t, k)
        blit(img, t, x, y)

    def text(self, img, x, y, s, color='ink', k=1, mono=False, align='left', w=0, shadow=None):
        f = self.M if mono else self.F
        c = tok(color) if isinstance(color, str) else color
        sh = (tok(shadow) if isinstance(shadow, str) else shadow) if shadow else None
        if align != 'left':
            return f.draw_box(img, x, y, w, s, c, align, k, sh)
        return f.draw(img, x, y, s, c, k, sh)

    def measure(self, s, mono=False):
        return (self.M if mono else self.F).measure(s)

    # -- composants composes --------------------------------------------------
    def button(self, img, kind, x, y, w, label='', state='normal', icon=None, h=18):
        self.draw9(img, 'button_' + kind, x - 1, y - 1, w + 2, h + 2, state)
        e = self.entry('hud_button_' + kind)
        tc = e['tokens']['text'] if state != 'disabled' else 'text_disabled'
        dy = 1 if state == 'pressed' else 0
        tw = self.measure(label) + (19 if icon else 0)
        tx = x + (w - tw) // 2
        if icon:
            self.icon(img, icon, tx, y + (h - 16) // 2 - 1 + dy, tc)
            tx += 19
        if label:
            self.text(img, tx, y + (h - 12) // 2 - 1 + dy, label, tc)

    def icon_button(self, img, x, y, icon, state='normal'):
        self.draw9(img, 'button_icon', x - 1, y - 1, 20, 20, state)
        col = 'text_on_accent' if state == 'toggled' else ('text_disabled' if state == 'disabled' else 'ink')
        self.icon(img, icon, x + 1, y + 1 + (1 if state == 'pressed' else 0), col)

    def field(self, img, kind, x, y, w, value='', placeholder='', state='normal'):
        self.draw9(img, kind, x, y, w, 16, state)
        pad = self.entry('hud_' + kind)['content_padding']
        if value:
            self.text(img, x + pad[0], y + 2, value, 'ink')
        elif placeholder:
            self.text(img, x + pad[0], y + 2, placeholder, 'text_muted')
        if state == 'focus':
            cx = x + pad[0] + self.measure(value) + 1
            vline(img, cx, y + 3, y + 12, tok('ink'))

    def progress(self, img, x, y, w, value, kind='default'):
        self.draw9(img, 'progress_track', x, y, w, 8)
        fw = int(round((w - 2) * max(0.0, min(1.0, value))))
        if fw >= 2:
            self.draw9(img, f'progress_fill_{kind}', x + 1, y + 1, fw, 6)

    def badge(self, img, kind, x, y, label):
        e = self.entry(f'hud_badge_{kind}')
        w = self.measure(label) + 8
        self.draw9(img, f'badge_{kind}', x, y, w, 12)
        self.text(img, x + 4, y, label, e['tokens']['text'])
        return w

    def window(self, img, x, y, w, h, title, modal=False, close=True):
        hid = 'modal' if modal else 'window'
        self.draw9(img, hid, x, y, w, h)
        self.text(img, x + 6, y + 2, title, 'text_on_accent')
        if close:
            self.draw9(img, 'button_icon_small', x + w - 16, y + 1, 14, 14, 'normal')
            self.icon(img, 'close', x + w - 17, y, 'text_on_accent')
        return x + 6, y + 21, w - 12, h - 29

    def panel(self, img, x, y, w, h, kind='panel_main'):
        self.draw9(img, kind, x, y, w, h)

    def section(self, img, x, y, w, label):
        self.draw9(img, 'section_header', x, y, w, 14)
        self.text(img, x + 6, y, label, 'ink')

    def tabs(self, img, x, y, labels, active=0, w=None):
        cx = x
        for i, lab in enumerate(labels):
            tw = (w or (self.measure(lab) + 14))
            st = 'active' if i == active else 'inactive'
            self.draw9(img, 'tabs', cx, y, tw, 20, st)
            self.text(img, cx + (tw - self.measure(lab)) // 2, y + 5 if st == 'active' else y + 6, lab,
                      'ink' if st == 'active' else 'text_muted')
            cx += tw - 1
        return cx

    def topbar(self, img, money='48 300 CHF', date='Jour 142', company='Nebula Labs'):
        self.draw9(img, 'topbar_bg', 0, 0, W, 22)
        self.icon(img, 'company', 4, 2, 'text_on_dark')
        self.text(img, 22, 4, company, 'text_on_dark')
        x = 30 + self.measure(company)
        self.draw9(img, 'topbar_divider', x, 1, 3, 20); x += 8
        self.draw9(img, 'money_chip', x, 4, self.measure(money) + 17, 14)
        self.text(img, x + 11, 4, money, 'ink')
        x += self.measure(money) + 23
        self.draw9(img, 'chip_dark', x, 4, self.measure(date) + 24, 14)
        self.icon(img, 'calendar', x + 2, 3, 'text_on_dark')
        self.text(img, x + 19, 4, date, 'text_on_dark')
        return x + self.measure(date) + 30

    def portrait(self, img, x, y, base='employee_f_02', hair='hair_bob', skin=None, hair_ramp='hair_brown', state='normal'):
        from srgen.rooms import char_layers_from_lib
        rend = char_layers_from_lib(self.lib)
        spec = {'base': base, 'hair': hair, 'hair_ramp': hair_ramp}
        if skin:
            spec['skin'] = skin
        ch = canvas(40, 72)
        for lay, (ax, ay) in rend(spec, 'se'):
            blit(ch, lay, 0, 0)
        be = self.entry(base)
        cx, cy, cw, chh = be.get('portrait_crop', [4, 0, 32, 32])
        crop = ch[cy:cy + chh, cx:cx + cw]
        fr, fe = self.comp('portrait_frame', state)
        bg = canvas(32, 32, tok('teal_light'))
        fill(bg, 0, 20, 31, 31, tok('teal_hi'))
        blit(img, bg, x + 3, y + 3)
        blit(img, crop, x + 3, y + 3)
        blit(img, fr, x, y)

    def room(self, rid):
        """Rendu d'un plan de piece (rooms/<rid>.json) avec le moteur de reference."""
        if rid in self._room_cache:
            return self._room_cache[rid]
        img = None
        try:
            from srgen import rooms as RM
            plan = json.load(open(os.path.join(ROOT, 'rooms', f'{rid}.json'), encoding='utf-8'))
            try:
                plan2, _ = RM.validate(plan, self.lib)
            except Exception:
                plan2 = plan
            img = RM.render_plan(plan2, self.lib, None, scale=1, margin=24, bg=(46, 40, 50))
        except Exception as ex:
            p = os.path.join(ROOT, 'previews', 'rooms', f'{rid}.png')
            if os.path.exists(p):
                img = load_png(p)
            else:
                print('  (piece indisponible :', rid, ex, ')')
        self._room_cache[rid] = img
        return img

    def backdrop(self, img, rid, dx=0, dy=0, dim=0):
        fill(img, 0, 0, W - 1, H - 1, (46, 40, 50, 255))
        r = self.room(rid)
        if r is not None:
            blit(img, r, (W - r.shape[1]) // 2 + dx, (H - r.shape[0]) // 2 + dy)
        if dim:
            blit(img, canvas(W, H, (43, 33, 40, dim)), 0, 0)

    def vfx(self, img, name, x, y):
        e = self.entry('vfx_' + name)
        im = self.lib.png(e['file'])
        ax, ay = e.get('anchor', [0, 0])
        blit(img, im, x - ax, y - ay)

    def chart_bars(self, img, x, y, w, h, series, colors, labels):
        self.draw9(img, 'chart_frame', x, y, w, h)
        bar = self.lib.png(self.entry('hud_chart_bar')['file'])
        grid = self.lib.png(self.entry('hud_chart_grid_h')['file'])
        px0, py0 = x + 22, y + 6
        pw, ph = w - 28, h - 22
        vmax = max(max(s) for s in series)
        for k in range(5):
            gy = py0 + ph - int(ph * k / 4)
            blit(img, nine(grid, pw, 1, [0, 0, 0, 0], 'tile'), px0, gy)
            self.text(img, x + 2, gy - 6, f'{int(vmax * k / 4 / 1000)}k', 'text_muted')
        n = len(series[0])
        slot = pw // n
        bw = max(3, (slot - 3) // len(series))
        for i in range(n):
            for s, (vals, col) in enumerate(zip(series, colors)):
                bh = max(2, int(ph * vals[i] / vmax))
                b = nine(bar, bw, bh, [2, 2, 2, 2])
                b = b.astype(np.uint16)
                c = tok(col)
                b[..., 0] = b[..., 0] * c[0] // 255; b[..., 1] = b[..., 1] * c[1] // 255; b[..., 2] = b[..., 2] * c[2] // 255
                blit(img, b.astype(np.uint8), px0 + i * slot + 2 + s * bw, py0 + ph - bh + 1)
            self.text(img, px0 + i * slot + 1, py0 + ph + 3, labels[i], 'text_muted')
        ax = self.lib.png(self.entry('hud_chart_axis_h')['file'])
        blit(img, nine(ax, pw, 2, [0, 0, 0, 0], 'tile'), px0, py0 + ph + 1)


# ---------------------------------------------------------------------------
# Ecrans
# ---------------------------------------------------------------------------
def scr_main_menu(ui):
    img = canvas(W, H)
    ui.backdrop(img, 'room_startup_small', dx=120, dy=6)
    blit(img, canvas(250, H, (38, 51, 74, 215)), 0, 0)
    ui.text(img, 22, 28, 'StartupRush', 'sun', k=3, shadow='navy_0')
    ui.text(img, 24, 66, 'De la chambre au campus tech', 'text_on_dark')
    ui.panel(img, 20, 92, 200, 182)
    labels = [('Continuer', 'primary', 'load'), ('Charger une partie', 'secondary', None),
              ('Nouvelle partie', 'secondary', 'plus'), ('Paramètres', 'secondary', 'settings'), ('Quitter', 'text', None)]
    y = 102
    for i, (lab, kind, ic) in enumerate(labels):
        st = 'hover' if i == 1 else 'normal'
        if kind == 'text':
            ui.draw9(img, 'button_text', 29, y - 1, 182, 20, 'normal')
            ui.text(img, 30, y + 2, lab, 'teal', align='center', w=180)
        else:
            ui.button(img, kind, 30, y, 180, lab, st, icon=ic)
        y += 30
    ui.draw9(img, 'tooltip', 226, 101, 168, 34)
    ui.text(img, 232, 104, 'Nebula Labs · Jour 142', 'text_on_dark')
    ui.text(img, 232, 117, 'Trésorerie : 48 300 CHF', 'sun_hi')
    ui.text(img, 22, 340, 'v0.1 · pack d’assets 1.0', 'text_muted')
    return img


def scr_campaigns(ui):
    img = canvas(W, H)
    ui.backdrop(img, 'room_office_medium', dim=150)
    x, y, w, h = ui.window(img, 70, 24, 500, 314, 'Campagnes sauvegardées', modal=True)
    ui.tabs(img, x, y, ['Toutes', 'Récentes', 'Archivées'], 0)
    ui.field(img, 'field_search', x + w - 150, y + 2, 150, '', 'Rechercher…')
    ui.panel(img, x, y + 20, w, 202, 'panel_inset')
    saves = [('Nebula Labs', 'Jour 142', '48 300 CHF', '7', 'room_startup_small', 'success', 'Bureau moyen'),
             ('Pixel Forge', 'Jour 61', '9 850 CHF', '3', 'room_bedroom_start', 'info', 'Chambre'),
             ('Orbitale Cloud', 'Jour 388', '1 204 000 CHF', '42', 'room_datacenter', 'warning', 'Datacenter')]
    cy = y + 26
    for i, (name, day, money, team, rid, bk, loc) in enumerate(saves):
        st = 'selected' if i == 0 else 'normal'
        ui.draw9(img, 'card', x + 6, cy, w - 22, 60, st)
        ui.text(img, x + 12, cy + 1, name, 'text_on_accent')
        thumb = ui.room(rid)
        if thumb is not None:
            t = thumb[::4, ::4]
            tw, th = min(64, t.shape[1]), min(40, t.shape[0])
            ox, oy = (t.shape[1] - tw) // 2, (t.shape[0] - th) // 2
            fill(img, x + 11, cy + 17, x + 11 + tw + 1, cy + 17 + th + 1, tok('navy'))
            blit(img, t[oy:oy + th, ox:ox + tw], x + 12, cy + 18)
        ui.icon(img, 'calendar', x + 84, cy + 18, 'text_muted')
        ui.text(img, x + 102, cy + 20, day, 'ink')
        ui.icon(img, 'cash', x + 160, cy + 18, 'text_muted')
        ui.text(img, x + 178, cy + 20, money, 'ink')
        ui.icon(img, 'team', x + 270, cy + 18, 'text_muted')
        ui.text(img, x + 288, cy + 20, team + ' employés', 'ink')
        ui.badge(img, bk, x + 84, cy + 38, loc)
        ui.text(img, x + 230, cy + 40, 'Dernière session : il y a %d h' % (2 + i * 19), 'text_muted')
        cy += 66
    ui.draw9(img, 'scroll_track_v', x + w - 12, y + 24, 10, 194)
    ui.draw9(img, 'scroll_thumb_v', x + w - 12, y + 26, 10, 120, 'hover')
    ui.draw9(img, 'scroll_grip_v', x + w - 10, y + 84, 6, 5)
    by = y + h - 18
    ui.button(img, 'secondary', x, by, 140, 'Nouvelle campagne', icon='plus')
    ui.button(img, 'destructive', x + w - 260, by, 110, 'Supprimer', icon='delete')
    ui.button(img, 'primary', x + w - 140, by, 140, 'Charger', icon='load')
    return img


def scr_hud(ui):
    img = canvas(W, H)
    ui.backdrop(img, 'room_office_medium', dy=14)
    x = ui.topbar(img)
    for i, ic in enumerate(('pause', 'speed_1', 'speed_2', 'speed_3')):
        ui.icon_button(img, W - 132 + i * 21, 2, ic, 'toggled' if i == 1 else 'normal')
    ui.icon_button(img, W - 42, 2, 'bell')
    ui.draw9(img, 'badge_count', W - 30, 0, 12, 12)
    ui.text(img, W - 27, 0, '3', 'text_on_accent')
    ui.icon_button(img, W - 21, 2, 'settings')
    tools = ['furnish', 'team', 'project', 'product', 'cash', 'server', 'statistics']
    ui.panel(img, 4, 40, 28, 7 * 22 + 10, 'panel_secondary')
    for i, ic in enumerate(tools):
        ui.icon_button(img, 8, 46 + i * 22, ic, 'toggled' if i == 2 else 'normal')
    ui.panel(img, 448, 30, 186, 112)
    ui.section(img, 456, 36, 170, 'Projets en cours')
    rows = [('Nebula v1.3', 0.72, 'default'), ('Contrat Alpaga Bio', 0.41, 'warning'), ('Correctifs API', 0.93, 'success')]
    for i, (lab, v, k) in enumerate(rows):
        ui.text(img, 458, 54 + i * 26, lab, 'ink')
        ui.text(img, 590, 54 + i * 26, f'{int(v * 100)} %', 'text_muted')
        ui.progress(img, 458, 67 + i * 26, 164, v, k)
    ui.vfx(img, 'idea', 224, 116)
    ui.vfx(img, 'coin', 288, 96)
    ui.vfx(img, 'success', 384, 196)
    ui.vfx(img, 'code', 330, 150)
    ui.vfx(img, 'sparkle_large', 300, 164)
    ui.draw9(img, 'notification', 380, 300, 254, 44, 'success')
    ui.icon(img, 'publish', 390, 306, 'success')
    ui.text(img, 410, 305, 'Nebula v1.2 est en ligne !', 'ink')
    ui.text(img, 410, 318, '+3 200 utilisateurs cette semaine', 'text_muted')
    ui.draw9(img, 'toast', 8, 330, 230, 22)
    ui.icon(img, 'info', 12, 333, 'info_hi')
    ui.text(img, 30, 334, 'Camille a terminé sa formation.', 'text_on_dark')
    return img


def scr_furnish(ui):
    img = canvas(W, H)
    ui.backdrop(img, 'room_startup_small', dx=-90, dy=16)
    ui.topbar(img, money='12 450 CHF', date='Jour 38', company='Nebula Labs')
    x, y, w, h = ui.window(img, 392, 28, 244, 326, 'Aménagement')
    ui.tabs(img, x, y, ['Bureaux', 'Sièges', 'Tech', 'Déco'], 0)
    ui.panel(img, x, y + 20, w, 196, 'panel_inset')
    items = [('desk_simple', 'Simple', '450'), ('desk_compact', 'Compact', '280'), ('desk_long', 'Long', '720'),
             ('desk_corner', 'D’angle', '640'), ('desk_tech', 'Technique', '980'), ('table_collab', 'Collective', '1 150')]
    for i, (aid, name, price) in enumerate(items):
        cx = x + 4 + (i % 3) * 76
        cy = y + 24 + (i // 3) * 96
        st = 'selected' if i == 0 else ('disabled' if i == 5 else 'normal')
        ui.draw9(img, 'card', cx, cy, 72, 90, st)
        ui.text(img, cx + 5, cy + 1, name[:11], 'text_on_accent' if st != 'disabled' else 'text_muted')
        try:
            e = ui.entry(aid)
            o = e['orientations'].get('se') or next(iter(e['orientations'].values()))
            sp = ui.lib.png(o['layers']['base']['file'])
            k = 2 if sp.shape[1] > 66 else 1
            sp2 = sp[::k, ::k]
            blit(img, sp2, cx + (72 - sp2.shape[1]) // 2, cy + 18 + max(0, (50 - sp2.shape[0]) // 2))
        except Exception:
            ui.icon(img, 'furnish', cx + 28, cy + 30, 'ink')
        ui.icon(img, 'cash', cx + 4, cy + 70, 'text_muted')
        ui.text(img, cx + 21, cy + 72, price + ' CHF', 'ink' if st != 'disabled' else 'text_disabled')
        if st == 'disabled':
            ui.icon(img, 'lock', cx + 52, cy + 18, 'text_muted')
    ui.section(img, x, y + 222, w, 'Bureau simple · 2 × 1 cases')
    ui.text(img, x + 2, y + 238, 'Postes : 1   Confort : +2   Prises : 4', 'text_muted')
    for i, (ic, tip) in enumerate((('rotate', 'Tourner'), ('move', 'Deplacer'), ('sell', 'Revendre'))):
        ui.icon_button(img, x + i * 24, y + h - 16, ic)
    ui.button(img, 'primary', x + w - 92, y + h - 16, 92, 'Placer', icon='place')
    gx, gy = 150, 200
    for (du, dv) in ((0, 0), (0, 1)):
        ui.vfx(img, 'tile_valid', gx + (du - dv) * 32, gy + (du + dv) * 16)
    blit(img, ui.lib.png(ui.entry('cursor_place_valid')['file']), gx + 30, gy + 24)
    ui.draw9(img, 'tooltip', gx + 50, gy + 40, 132, 22)
    ui.text(img, gx + 55, gy + 43, 'Emplacement libre', 'text_on_dark')
    return img


def scr_employee(ui):
    img = canvas(W, H)
    ui.backdrop(img, 'room_office_medium', dim=120)
    x, y, w, h = ui.window(img, 100, 24, 440, 314, 'Fiche employé')
    ui.portrait(img, x, y, 'employee_f_02', 'hair_bob', hair_ramp='hair_auburn', state='selected')
    ui.text(img, x + 46, y + 1, 'Camille Roy', 'ink', k=2)
    ui.text(img, x + 46, y + 25, 'Développeuse full-stack · niveau 4', 'text_muted')
    bx = x + 46
    for lab, kind in (('Curieuse', 'info'), ('Rapide', 'success'), ('Café ++', 'accent')):
        bx += ui.badge(img, kind, bx, y + 38, lab) + 4
    ui.tabs(img, x, y + 56, ['Profil', 'Compétences', 'Historique'], 1)
    ui.panel(img, x, y + 76, w, 150, 'panel_secondary')
    skills = [('code', 'Code', 0.82, 'default'), ('design', 'Design', 0.45, 'default'), ('test', 'Tests', 0.60, 'default'),
              ('marketing', 'Marketing', 0.20, 'warning'), ('ai', 'IA', 0.35, 'default'), ('support', 'Support', 0.70, 'success')]
    for i, (ic, lab, v, k) in enumerate(skills):
        cx = x + 8 + (i % 2) * 212
        cy = y + 84 + (i // 2) * 44
        ui.icon(img, ic, cx, cy, 'teal')
        ui.text(img, cx + 20, cy + 2, lab, 'ink')
        ui.text(img, cx + 170, cy + 2, str(int(v * 100)), 'ink', align='right', w=24)
        ui.progress(img, cx + 20, cy + 18, 174, v, k)
    ui.icon(img, 'salary', x, y + 234, 'teal')
    ui.text(img, x + 20, y + 236, 'Salaire : 5 400 CHF / mois', 'ink')
    ui.text(img, x + 230, y + 236, 'Moral', 'ink')
    g0 = ui.lib.png(ui.entry('hud_gauge_base')['file']); g1 = ui.lib.png(ui.entry('hud_gauge_fill')['file'])
    gm = ui.lib.png(ui.entry('hud_gauge_segment_map')['file'])
    gauge = np.where((gm[..., 0:1] > 0) & (gm[..., 0:1] <= 7), g1, g0)
    blit(img, gauge.astype(np.uint8), x + 270, y + 228)
    ui.text(img, x + 310, y + 236, '70 %', 'success')
    by = y + h - 18
    ui.button(img, 'secondary', x, by, 112, 'Former', icon='training')
    ui.button(img, 'primary', x + 120, by, 112, 'Augmenter', icon='income')
    ui.button(img, 'destructive', x + w - 112, by, 112, 'Licencier', state='normal')
    return img


def scr_projects(ui):
    img = canvas(W, H)
    ui.backdrop(img, 'room_open_space', dim=130)
    x, y, w, h = ui.window(img, 24, 24, 592, 318, 'Projets et contrats')
    segs = ['En cours', 'Proposés', 'Terminés']
    sx = x
    for i, s in enumerate(segs):
        piece = 'left' if i == 0 else ('right' if i == 2 else 'mid')
        sw = 90
        st = 'selected' if i == 1 else 'normal'
        ui.draw9(img, f'segmented_{piece}', sx, y, sw, 18, st)
        ui.text(img, sx, y + 2, s, 'text_on_accent' if st == 'selected' else 'ink', align='center', w=sw)
        sx += sw
    ui.field(img, 'dropdown', x + w - 300, y + 1, 150, 'Tous les clients')
    ui.field(img, 'field_search', x + w - 142, y + 1, 142, '', 'Filtrer…')
    ui.panel(img, x, y + 24, 360, 220, 'panel_inset')
    cols = [('Client', 6), ('Projet', 96), ('Délai', 200), ('Prime', 250), ('État', 306)]
    for lab, cx in cols:
        ui.text(img, x + cx, y + 28, lab, 'text_muted')
    rows = [('Alpaga Bio', 'App de livraison', '12 j', '18 k', 'info', 'Nouveau'),
            ('Ville de Sion', 'Portail citoyen', '30 j', '42 k', 'warning', 'Urgent'),
            ('Studio Lumen', 'Jeu mobile', '45 j', '65 k', 'accent', 'Jeu'),
            ('Banque Riva', 'Audit sécurité', '8 j', '22 k', 'danger', 'Risque'),
            ('École Polytech', 'Plateforme IA', '60 j', '120 k', 'success', 'Ideal'),
            ('Atelier Vela', 'Site vitrine', '6 j', '4 k', 'neutral', 'Simple')]
    for i, (cl, pr, dl, rw, bk, bl) in enumerate(rows):
        ry = y + 42 + i * 30
        st = 'selected' if i == 1 else ('hover' if i == 3 else 'normal')
        ui.draw9(img, 'list_row', x + 2, ry, 356, 30, st)
        ui.icon(img, 'client', x + 6, ry + 6, 'teal')
        ui.text(img, x + 24, ry + 8, cl, 'ink')
        ui.text(img, x + 96, ry + 8, pr, 'ink')
        ui.text(img, x + 200, ry + 8, dl, 'ink')
        ui.text(img, x + 250, ry + 8, rw + ' CHF', 'ink')
        ui.badge(img, bk, x + 306, ry + 8, bl)
    dx = x + 372
    ui.draw9(img, 'card', dx, y + 24, w - 372, 254, 'normal')
    ui.text(img, dx + 6, y + 25, 'Portail citoyen', 'text_on_accent')
    ui.icon(img, 'contract', dx + 8, y + 44, 'teal')
    ui.text(img, dx + 28, y + 46, 'Client : Ville de Sion', 'ink')
    lines = ['Formulaires en ligne, comptes', 'citoyens et paiements simulés.', 'Exigences : sécurité 60,',
             'accessibilité, API ouverte.']
    for i, l in enumerate(lines):
        ui.text(img, dx + 8, y + 66 + i * 13, l, 'ink')
    for i, (ic, lab, val) in enumerate((('time', 'Délai', '30 jours'), ('cash', 'Prime', '42 000 CHF'),
                                        ('risk', 'Risque', 'Moyen'), ('quality', 'Qualité min.', '70 %'))):
        ui.icon(img, ic, dx + 8, y + 124 + i * 18, 'text_muted')
        ui.text(img, dx + 28, y + 126 + i * 18, lab, 'text_muted')
        ui.text(img, dx + 110, y + 126 + i * 18, val, 'ink')
    ui.progress(img, dx + 8, y + 202, w - 390, 0.0)
    ui.text(img, dx + 8, y + 212, 'Équipe suggérée : 3 personnes', 'text_muted')
    ui.button(img, 'secondary', dx + 8, y + 232, 90, 'Refuser')
    ui.button(img, 'primary', dx + w - 372 - 102, y + 232, 94, 'Accepter', icon='confirm')
    return img


def scr_product(ui):
    img = canvas(W, H)
    fill(img, 0, 0, W - 1, H - 1, tok('paper_2'))
    for gx in range(0, W, 16):
        vline(img, gx, 22, H - 1, tok('paper_3'))
    for gy in range(22, H, 16):
        hline(img, 0, W - 1, gy, tok('paper_3'))
    ui.topbar(img, company='Nebula Labs · Produit : Nebula (application)')
    from ui import project_components as PC
    cards = [('home', 30, 50, 'done'), ('auth', 30, 150, 'done'), ('profile', 160, 150, 'in_dev'),
             ('catalog', 160, 50, 'done'), ('search', 290, 50, 'in_dev'), ('payment', 290, 150, 'planned'),
             ('notifications', 30, 250, 'bug'), ('analytics', 290, 250, 'locked'), ('api', 160, 250, 'planned')]
    state_lab = {'done': 'Terminé', 'in_dev': 'En cours', 'planned': 'Prévu', 'bug': 'Bogue', 'locked': 'Verrouillé'}
    pos = {c[0]: (c[1], c[2]) for c in cards}
    link_h = ui.lib.png(ui.entry('link_h')['file']); link_v = ui.lib.png(ui.entry('link_v')['file'])
    link_d = ui.lib.png(ui.entry('link_dashed_h')['file'])
    arrow = ui.lib.png(ui.entry('link_arrow_right')['file'])

    def colored(im, col):
        b = im.astype(np.uint16)
        c = tok(col)
        for k in range(3):
            b[..., k] = b[..., k] * c[k] // 255
        return b.astype(np.uint8)
    deps = [('home', 'catalog', 'satisfied'), ('catalog', 'search', 'required'), ('auth', 'profile', 'required'),
            ('profile', 'payment', 'optional'), ('api', 'analytics', 'broken')]
    from ui.project_components import LINK_STATES
    for a, b, st in deps:
        (ax, ay), (bx, by) = pos[a], pos[b]
        y0 = ay + 30
        x0, x1 = ax + 112, bx - 6
        src = link_d if st == 'optional' else link_h
        blit(img, colored(nine(src, x1 - x0, 4, [0, 0, 0, 0], 'tile'), LINK_STATES[st]), x0, y0)
        blit(img, colored(arrow, LINK_STATES[st]), x1 - 1, y0 - 1)
    blit(img, colored(nine(link_v, 4, 52, [0, 0, 0, 0], 'tile'), LINK_STATES['required']), 84, 102)
    blit(img, colored(ui.lib.png(ui.entry('link_arrow_down')['file']), LINK_STATES['required']), 83, 146)
    for cid, cx, cy, st in cards:
        ui.draw9(img, 'comp_card', cx, cy, 112, 72, st)
        name = PC.COMPONENTS[cid][0]
        ui.text(img, cx + 5, cy + 1, name[:16], 'ink' if st in ('planned', 'locked', 'in_dev') else 'text_on_accent')
        ic = PC.COMPONENTS[cid][2]
        ui.icon(img, ic, cx + 5, cy + 19, 'teal' if st != 'locked' else 'text_disabled')
        blit(img, ui.lib.png(ui.entry(f'comp_{cid}')['preview']['file'])[::2, ::2], cx + 4, cy + 38)
        ui.text(img, cx + 32, cy + 18, state_lab[st], 'ink' if st != 'locked' else 'text_disabled')
        ui.text(img, cx + 32, cy + 31, f'{(len(cid) * 7) % 9 + 2} k CHF', 'text_muted')
        q = {'done': 0.9, 'in_dev': 0.55, 'planned': 0.0, 'bug': 0.35, 'locked': 0.0}[st]
        fill(img, cx + 32, cy + 60, cx + 106, cy + 62, tok('paper_3'))
        if q:
            fill(img, cx + 32, cy + 60, cx + 32 + int(74 * q), cy + 62, tok('success') if q > 0.6 else tok('sun'))
        for side, px, py in (('in_left', cx - 4, cy + 28), ('out_right', cx + 108, cy + 28)):
            pst = 'connected' if st in ('done', 'in_dev') else ('invalid' if st == 'bug' else 'idle')
            pe = ui.entry(f'port_{side}')
            blit(img, ui.lib.png(pe['states'][pst]['file']), px, py)
    so = ui.entry('comp_card')['selection_overlay']
    blit(img, nine(ui.lib.png(so['file']), 114, 76, so['nine_slice']), 159, 149)
    px, py, pw, ph = ui.window(img, 432, 30, 202, 322, 'Composant : Profil')
    blit(img, scale(ui.lib.png(ui.entry('comp_profile')['preview']['file']), 2), px + 2, py + 2)
    ui.badge(img, 'warning', px, py + 72, 'En développement')
    rows = [('Coût', '6 000 CHF'), ('Qualité', '55 %'), ('Équipe', '2 devs'), ('Fin estimée', 'Jour 151')]
    for i, (a, b) in enumerate(rows):
        ui.text(img, px, py + 92 + i * 14, a, 'text_muted')
        ui.text(img, px + 80, py + 92 + i * 14, b, 'ink')
    ui.section(img, px, py + 152, pw, 'Dépendances')
    for i, (ic, lab, ok) in enumerate((('comp_auth', 'Compte / auth.', True), ('storage', 'Stockage', False))):
        ui.icon(img, ic, px, py + 170 + i * 18, 'teal')
        ui.text(img, px + 20, py + 172 + i * 18, lab, 'ink')
        ui.icon(img, 'success' if ok else 'warning', px + pw - 18, py + 170 + i * 18, 'success' if ok else 'warning')
    ui.button(img, 'primary', px, py + ph - 18, pw, 'Assigner une équipe', icon='assignment')
    return img


def scr_hosting(ui):
    img = canvas(W, H)
    ui.backdrop(img, 'room_server_compact', dx=-160, dy=10)
    ui.topbar(img, money='231 900 CHF', date='Jour 210', company='Orbitale Cloud')
    x, y, w, h = ui.window(img, 300, 28, 336, 326, 'Hébergement et serveurs')
    for i, (ic, lab, v, k) in enumerate((('cpu', 'CPU', 0.68, 'default'), ('memory', 'RAM', 0.81, 'warning'),
                                         ('network', 'Réseau', 0.42, 'default'), ('energy', 'Énergie', 0.57, 'default'))):
        cx = x + (i % 2) * 164; cy = y + (i // 2) * 30
        ui.icon(img, ic, cx, cy, 'teal')
        ui.text(img, cx + 20, cy + 2, lab, 'ink')
        ui.text(img, cx + 130, cy + 2, f'{int(v * 100)}%', 'text_muted')
        ui.progress(img, cx + 20, cy + 16, 132, v, k)
    ui.panel(img, x, y + 64, w, 200, 'panel_inset')
    g0 = ui.lib.png(ui.entry('hud_gauge_base')['file']); g1 = ui.lib.png(ui.entry('hud_gauge_fill')['file'])
    gm = ui.lib.png(ui.entry('hud_gauge_segment_map')['file'])
    servers = [('srv-web-01', 'server', 6, 'success', 'En ligne', '99,98 %'),
               ('srv-db-01', 'storage', 8, 'warning', 'Charge', '99,90 %'),
               ('gpu-ia-02', 'gpu', 9, 'danger', 'Surchauffe', '98,70 %'),
               ('nas-backup', 'storage', 3, 'success', 'En ligne', '100 %'),
               ('edge-geneve', 'network', 4, 'info', 'Maintenance', '99,50 %')]
    for i, (nm, ic, load, bk, bl, up) in enumerate(servers):
        ry = y + 68 + i * 38
        ui.draw9(img, 'list_row', x + 2, ry, w - 4, 38, 'hover' if i == 2 else 'normal')
        ui.icon(img, ic, x + 6, ry + 10, 'teal')
        ui.text(img, x + 26, ry + 6, nm, 'ink', mono=True)
        ui.text(img, x + 26, ry + 20, 'Dispo ' + up, 'text_muted')
        g = np.where((gm[..., 0:1] > 0) & (gm[..., 0:1] <= load), g1, g0).astype(np.uint8)
        blit(img, g, x + 160, ry + 10)
        ui.badge(img, bk, x + 210, ry + 14, bl)
    ui.button(img, 'secondary', x, y + h - 18, 150, 'Ajouter un serveur', icon='plus')
    ui.button(img, 'primary', x + w - 150, y + h - 18, 150, 'Ouvrir la salle', icon='rack')
    return img


def scr_finances(ui):
    img = canvas(W, H)
    ui.backdrop(img, 'room_hq_floor', dim=140)
    x, y, w, h = ui.window(img, 18, 22, 604, 324, 'Finances')
    kpis = [('cash', 'Trésorerie', '48 300 CHF', 'ink'), ('income', 'Recettes / mois', '+21 400', 'success'),
            ('expense', 'Dépenses / mois', '-16 950', 'danger'), ('profit', 'Bénéfice', '+4 450', 'success')]
    for i, (ic, lab, val, col) in enumerate(kpis):
        cx = x + i * 150
        ui.panel(img, cx, y, 144, 40, 'panel_secondary')
        ui.icon(img, ic, cx + 6, y + 6, 'teal')
        ui.text(img, cx + 26, y + 5, lab, 'text_muted')
        ui.text(img, cx + 26, y + 20, val, col)
    months = ['J', 'F', 'M', 'A', 'M', 'J', 'J', 'A', 'S', 'O', 'N', 'D']
    rev = [4200, 5100, 6900, 8800, 9300, 11800, 13400, 15100, 16900, 18200, 19800, 21400]
    exp = [6100, 6400, 7200, 7900, 8800, 9900, 11200, 12100, 13900, 14600, 15800, 16950]
    ui.chart_bars(img, x, y + 48, 380, 196, [rev, exp], ['teal', 'coral'], months)
    for i, (col, lab) in enumerate((('teal', 'Recettes'), ('coral', 'Dépenses'))):
        lg = ui.lib.png(ui.entry('hud_chart_legend')['file']).astype(np.uint16)
        c = tok(col)
        for k in range(3):
            lg[..., k] = lg[..., k] * c[k] // 255
        blit(img, lg.astype(np.uint8), x + 30 + i * 70, y + 56)
        ui.text(img, x + 41 + i * 70, y + 54, lab, 'ink')
    ui.field(img, 'dropdown', x + 392, y + 48, w - 392, '12 derniers mois')
    ui.panel(img, x + 392, y + 70, w - 392, 174, 'panel_inset')
    tx = [('income', 'Abonnements Nebula', '+12 900', 'success'), ('expense', 'Salaires (7)', '-11 200', 'danger'),
          ('income', 'Contrat Alpaga Bio', '+6 000', 'success'), ('expense', 'Loyer bureau', '-3 400', 'danger'),
          ('expense', 'Serveurs', '-1 650', 'danger'), ('income', 'Ventes (boutique)', '+2 500', 'success')]
    for i, (ic, lab, val, col) in enumerate(tx):
        ry = y + 74 + i * 28
        ui.draw9(img, 'list_row', x + 394, ry, w - 396, 28, 'normal')
        ui.icon(img, ic, x + 398, ry + 6, col)
        ui.text(img, x + 418, ry + 8, lab, 'ink')
        ui.text(img, x + w - 64, ry + 8, val, col, align='right', w=56)
    ui.button(img, 'secondary', x, y + h - 18, 140, 'Demander un prêt', icon='loan')
    ui.button(img, 'primary', x + w - 140, y + h - 18, 140, 'Exporter le bilan', icon='invoice')
    return img


def scr_offices(ui):
    img = canvas(W, H)
    fill(img, 0, 0, W - 1, H - 1, tok('navy'))
    ui.topbar(img, money='86 200 CHF', date='Jour 260', company='Nebula Labs')
    ui.text(img, 20, 30, 'Choisir de nouveaux locaux', 'text_on_dark', k=2)
    offers = [('room_bedroom_start', 'Chambre', 'Actuel', 'neutral', '0', '1', '20 m2'),
              ('room_startup_small', 'Petit bureau', 'Disponible', 'success', '1 800', '4', '48 m2'),
              ('room_office_medium', 'Bureau moyen', 'Disponible', 'success', '4 900', '12', '110 m2'),
              ('room_open_space', 'Grand open space', 'Verrouillé', 'danger', '12 400', '40', '320 m2')]
    for i, (rid, name, status, bk, rent, cap, size) in enumerate(offers):
        cx = 20 + i * 152
        st = 'selected' if i == 2 else ('disabled' if i == 3 else 'normal')
        ui.draw9(img, 'card', cx, 66, 146, 262, st)
        ui.text(img, cx + 6, 67, name, 'text_on_accent' if st != 'disabled' else 'text_muted')
        r = ui.room(rid)
        if r is not None:
            k = max(1, int(np.ceil(r.shape[1] / 136)))
            t = r[::k, ::k]
            th = min(t.shape[0], 100)
            oy = (t.shape[0] - th) // 2
            fill(img, cx + 4, 86, cx + 141, 187, (46, 40, 50, 255))
            blit(img, t[oy:oy + th, :136], cx + 5 + max(0, (136 - t.shape[1]) // 2), 87)
        ui.badge(img, bk, cx + 6, 194, status)
        for j, (ic, lab) in enumerate((('cash', rent + ' CHF / mois'), ('team', cap + ' postes'), ('office', size))):
            ui.icon(img, ic, cx + 6, 212 + j * 18, 'teal' if st != 'disabled' else 'text_disabled')
            ui.text(img, cx + 26, 214 + j * 18, lab, 'ink' if st != 'disabled' else 'text_disabled')
        if i == 3:
            ui.icon(img, 'lock', cx + 124, 194, 'danger')
            ui.text(img, cx + 6, 268, 'Requiert 20 employés', 'text_disabled')
        bstate = 'disabled' if i in (0, 3) else 'normal'
        ui.button(img, 'primary' if i == 2 else 'secondary', cx + 8, 300, 130, 'Emménager' if i else 'Ici', bstate)
    return img


def scr_industry(ui):
    img = canvas(W, H)
    ui.backdrop(img, 'room_factory_hall', dim=150)
    x, y, w, h = ui.window(img, 30, 24, 580, 318, 'Production matérielle')
    ui.draw9(img, 'banner', x + 150, y, 280, 18)
    blit(img, ui.extra('banner', 'banner_end_left'), x + 143, y - 2)
    blit(img, ui.extra('banner', 'banner_end_right'), x + 429, y - 2)
    ui.text(img, x + 150, y + 3, 'Fonctionnalité future · aperçu', 'ink', align='center', w=280)
    ui.panel(img, x, y + 26, w, 96, 'panel_secondary')
    steps = [('stock', 'Composants'), ('conveyor', 'Convoyeur'), ('assembly', 'Assemblage'), ('test_station', 'Test'),
             ('packaging', 'Emballage'), ('product', 'Produit fini')]
    for i, (ic, lab) in enumerate(steps):
        cx = x + 12 + i * 94
        ui.draw9(img, 'button_icon', cx, y + 40, 34, 34, 'toggled' if i == 2 else 'normal')
        ui.icon(img, ic, cx + 9, y + 48, 'text_on_accent' if i == 2 else 'ink')
        ui.text(img, cx - 10, y + 82, lab, 'ink', align='center', w=54)
        if i < len(steps) - 1:
            for xx in range(cx + 40, cx + 88, 4):
                fill(img, xx, y + 56, xx + 1, y + 57, tok('teal'))
            ui.icon(img, 'chevron_right', cx + 76, y + 49, 'teal')
    ui.panel(img, x, y + 128, 340, 130, 'panel_inset')
    machines = [('Machine de placement', 'warning', 'Réglage', 0.64), ('Four de refusion', 'success', 'Actif', 0.92),
                ('Inspection optique', 'success', 'Actif', 0.88), ('Banc de stress-test', 'neutral', 'Arrêt', 0.0)]
    for i, (nm, bk, bl, v) in enumerate(machines):
        ry = y + 132 + i * 31
        ui.draw9(img, 'list_row', x + 2, ry, 336, 31, 'normal')
        ui.icon(img, 'manufacturing', x + 6, ry + 7, 'teal')
        ui.text(img, x + 26, ry + 3, nm, 'ink')
        ui.progress(img, x + 26, ry + 18, 180, v, 'success' if v > 0.8 else 'warning')
        ui.badge(img, bk, x + 250, ry + 9, bl)
    ui.panel(img, x + 348, y + 128, w - 348, 130, 'panel_secondary')
    ui.section(img, x + 354, y + 134, w - 360, 'Stock de composants')
    for i, (ic, lab, n) in enumerate((('cpu', 'Processeurs', '1 240'), ('memory', 'Mémoire', '3 980'),
                                      ('gpu', 'Cartes GPU', '312'), ('components', 'Divers', '15 600'))):
        ui.icon(img, ic, x + 356, y + 154 + i * 24, 'teal')
        ui.text(img, x + 376, y + 156 + i * 24, lab, 'ink')
        ui.text(img, x + w - 70, y + 156 + i * 24, n, 'ink', align='right', w=56)
    ui.button(img, 'secondary', x, y + h - 18, 160, 'Concevoir un PC', icon='pc', state='disabled')
    ui.button(img, 'primary', x + w - 160, y + h - 18, 160, 'Lancer la ligne', icon='speed_1', state='disabled')
    ui.icon(img, 'lock', x + w - 182, y + h - 17, 'text_muted')
    return img


def scr_terminal(ui):
    img = canvas(W, H)
    ui.backdrop(img, 'room_startup_small', dim=150)
    tx, ty, tw, th = 70, 30, 500, 300
    ui.draw9(img, 'term_frame', tx, ty, tw, th)
    ui.text(img, tx + 24, ty + 1, 'terminal · nebula (déploiement local simulé)', 'text_on_dark')
    M = ui.M
    lines = [
        ('prompt', 'deploy --env local --app nebula --version 1.3.0', 'term_text'),
        ('info', 'Lecture de la configuration (simulation, aucun serveur réel)', 'term_dim'),
        ('ok', 'Tests unitaires : 214 / 214 réussis', 'term_ok'),
        ('ok', 'Construction du paquet nebula-1.3.0', 'term_ok'),
        ('warning', 'Avertissement : 2 dépendances à mettre à jour', 'term_warn'),
        ('running', 'Envoi vers le bac à sable local…', 'term_text'),
        (None, 'PROGRESS', None),
        ('error', 'Échec du test de fumée #3 (délai dépassé)', 'term_err'),
        ('info', 'Nouvelle tentative 1/3…', 'term_dim'),
        ('ok', 'Test de fumée #3 réussi', 'term_ok'),
        ('ok', 'Déploiement local simulé terminé en 4,2 s', 'term_ok'),
    ]
    ui.draw9(img, 'term_selection', tx + 4, ty + 18 + 13 * 6, tw - 18, 12)
    y = ty + 18
    for mk, s, col in lines:
        if s == 'PROGRESS':
            cx = tx + 14
            ui.draw9(img, 'term_progress_cap_left', cx, y, 6, 12); cx += 6
            for k in range(24):
                kind = 'full' if k < 18 else ('half' if k == 18 else 'empty')
                ui.draw9(img, f'term_progress_{kind}', cx, y, 6, 12); cx += 6
            ui.draw9(img, 'term_progress_cap_right', cx, y, 6, 12)
            ui.text(img, cx + 12, y, '76 %   12,4 Mo / 16,3 Mo', 'term_text', mono=True)
        else:
            ui.draw9(img, f'term_mark_{mk}', tx + 6, y, 6, 12)
            ui.text(img, tx + 18, y, s, col, mono=True)
        y += 13
    ui.draw9(img, 'term_scroll_track', tx + tw - 10, ty + 16, 6, th - 48)
    ui.draw9(img, 'term_scroll_thumb', tx + tw - 10, ty + 70, 6, 90)
    ui.draw9(img, 'term_input', tx + 6, ty + th - 26, tw - 12, 16, 'focus')
    cmd = 'status --env local'
    ui.text(img, tx + 18, ty + th - 24, cmd, 'term_text', mono=True)
    ui.draw9(img, 'term_cursor_block', tx + 18 + len(cmd) * 6, ty + th - 24, 6, 12)
    return img


def scr_settings_confirm(ui):
    """Controles restants en situation : cases, radios, interrupteur, glissiere, liste,
    champ numerique, multiligne, pastilles couleur/motif, popover, menu contextuel,
    dialogue de confirmation (modal + voile)."""
    img = canvas(W, H)
    ui.backdrop(img, 'room_hq_floor')
    x, y, w, h = ui.window(img, 14, 28, 318, 324, 'Paramètres')
    ui.section(img, x, y, w, 'Affichage')
    rows = [('checkbox', 'on_normal', 'Plein écran'), ('checkbox', 'off_hover', 'Afficher la grille au sol'),
            ('checkbox', 'mixed_normal', 'Effets : certains activés'), ('checkbox', 'on_disabled', 'Pixels nets (imposé)')]
    for i, (cid, st, lab) in enumerate(rows):
        ui.draw9(img, cid, x + 2, y + 18 + i * 15, 12, 12, st)
        ui.text(img, x + 18, y + 18 + i * 15, lab, 'ink' if 'disabled' not in st else 'text_disabled')
    ui.text(img, x + 170, y + 18, 'Échelle', 'text_muted')
    for i, (lab, on) in enumerate((('×2', False), ('×3', True), ('×4', False))):
        ui.draw9(img, 'radio', x + 170, y + 32 + i * 15, 12, 12, 'on_normal' if on else 'off_normal')
        ui.text(img, x + 186, y + 32 + i * 15, lab, 'ink')
    ui.draw9(img, 'switch', x + 230, y + 32, 22, 12, 'on_normal')
    ui.text(img, x + 256, y + 32, 'Infobulles', 'ink')
    ui.draw9(img, 'switch', x + 230, y + 50, 22, 12, 'off_normal')
    ui.text(img, x + 256, y + 50, 'Tutoriel', 'ink')
    ui.text(img, x + 2, y + 82, 'Défilement de la caméra', 'ink')
    ui.draw9(img, 'slider_track', x + 2, y + 99, 180, 6)
    ui.draw9(img, 'slider_fill', x + 2, y + 99, 108, 6)
    ui.draw9(img, 'slider_knob', x + 105, y + 95, 10, 14, 'hover')
    ui.draw9(img, 'popover', x + 194, y + 90, 96, 22)
    blit(img, ui.extra('popover', 'popover_arrow_left'), x + 190, y + 96)
    ui.text(img, x + 200, y + 94, '60 % · fluide', 'ink')
    ui.text(img, x + 2, y + 116, 'Langue', 'text_muted')
    ui.field(img, 'dropdown', x + 2, y + 128, 140, 'Français')
    ui.text(img, x + 156, y + 116, 'Sauvegarde auto (min)', 'text_muted')
    ui.field(img, 'field_numeric', x + 156, y + 128, 90, '10')
    ui.section(img, x, y + 152, w, 'Couleurs de l’entreprise')
    sw = ['teal', 'coral', 'sun', 'sky', 'sage', 'navy', 'rose']
    for i, c in enumerate(sw):
        cx = x + 2 + i * 22
        fill(img, cx + 4, y + 172, cx + 13, y + 181, tok('teal') if False else __import__('srgen.palette', fromlist=['RAMPS']).RAMPS[c][3])
        ui.draw9(img, 'swatch_cell', cx, y + 168, 18, 18, 'selected' if i == 1 else ('hover' if i == 4 else 'normal'))
    pats = ['two_tone', 'brick', 'wood_panels', 'acoustic']
    for i, pn in enumerate(pats):
        cx = x + 160 + i * 30
        p = os.path.join(ROOT, 'sources', 'wall_patterns', f'pattern_{pn}_flat.png')
        if os.path.exists(p):
            src = load_png(p)
            blit(img, src[60:78, 7:25], cx + 4, y + 168 + 4)
        ui.draw9(img, 'pattern_cell', cx, y + 168, 26, 26, 'selected' if i == 1 else 'normal')
    ui.text(img, x + 2, y + 200, 'Devise de l’équipe', 'text_muted')
    ui.draw9(img, 'field_multiline', x + 2, y + 212, w - 4, 52, 'focus')
    ui.text(img, x + 6, y + 215, 'Livrer petit, livrer souvent,', 'ink')
    ui.text(img, x + 6, y + 228, 'et toujours garder le sourire.', 'ink')
    vline(img, x + 6 + ui.measure('et toujours garder le sourire.') + 1, y + 230, y + 239, tok('ink'))
    ui.button(img, 'text', x + w - 196, y + h - 18, 90, 'Rétablir')
    ui.button(img, 'primary', x + w - 100, y + h - 18, 100, 'Appliquer', icon='confirm')
    blit(img, canvas(W - 340, H, tok('scrim')), 340, 0)
    # menu contextuel (clic droit sur un employe)
    mx, my, mw = 360, 36, 170
    items = [('Voir la fiche', 'normal', 'user'), ('Assigner à un projet', 'hover', 'assignment'),
             ('Formation en cours', 'checked', None), (None, None, None),
             ('Promouvoir (niv. 5 requis)', 'disabled', 'lock'), ('Licencier…', 'normal', 'delete')]
    mh = 4 + sum(5 if it[0] is None else 14 for it in items) + 4
    ui.draw9(img, 'menu_panel', mx, my, mw, mh)
    yy = my + 3
    for lab, st, ic in items:
        if lab is None:
            ui.draw9(img, 'menu_separator', mx + 3, yy, mw - 6, 5)
            yy += 5
            continue
        ui.draw9(img, 'menu_item', mx + 2, yy, mw - 4, 14, st)
        col = {'hover': 'text_on_accent', 'disabled': 'text_disabled'}.get(st, 'ink')
        if lab.startswith('Licencier'):
            col = 'danger'
        if ic:
            ui.icon(img, ic, mx + 2, yy - 1, col if st != 'hover' else 'text_on_accent')
        ui.text(img, mx + 20, yy + 1, lab, col)
        yy += 14
    # dialogue de confirmation (modal, voile deja pose)
    dx, dy, dw, dh = 366, 170, 250, 122
    cx, cy, cw, chh = ui.window(img, dx, dy, dw, dh, 'Licencier Camille ?', modal=True)
    ui.icon(img, 'warning', dx + 8, dy + 23, 'warning')
    for i, l in enumerate(('Camille Roy quittera l’entreprise', 'à la fin de la journée.', 'Indemnité : 10 800 CHF.')):
        ui.text(img, dx + 30, dy + 22 + i * 13, l, 'ink')
    ui.button(img, 'secondary', dx + dw - 196, dy + dh - 34, 88, 'Annuler', 'focus')
    ui.button(img, 'destructive', dx + dw - 100, dy + dh - 34, 92, 'Licencier')
    return img


SCREENS = [('01_menu_principal', scr_main_menu),('02_navigateur_campagnes', scr_campaigns), ('03_hud_compact', scr_hud),
           ('04_panneau_amenagement', scr_furnish), ('05_fiche_employe', scr_employee),
           ('06_projets_contrats', scr_projects), ('07_produit_composants', scr_product),
           ('08_hebergement_serveurs', scr_hosting), ('09_finances', scr_finances), ('10_selection_locaux', scr_offices),
           ('11_panneau_industriel_futur', scr_industry), ('12_terminal_deploiement_local', scr_terminal),
           ('13_parametres_dialogue_confirmation', scr_settings_confirm)]


def main(only=None):
    os.makedirs(OUT, exist_ok=True)
    ui = UI()
    done = []
    for name, fn in SCREENS:
        if only and not any(o in name for o in only):
            continue
        img = fn(ui)
        img[..., 3] = 255
        save_png(os.path.join(OUT, f'{name}.png'), img)
        save_png(os.path.join(OUT, f'{name}_x2.png'), scale(img, 2))
        done.append(name)
        print('ecran', name)
    return done


if __name__ == '__main__':
    main(sys.argv[1].split(',') if len(sys.argv) > 1 else None)
