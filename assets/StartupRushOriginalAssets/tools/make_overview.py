"""Planche d'ensemble des 10 pieces (rendus natifs, sans redimensionnement) : previews/rooms_overview.png"""
import os, sys, json, glob
import numpy as np
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'tools'))
from srgen.imgio import load_png, save_png, over
from srgen.preview import draw_text

def main():
    rooms = sorted(glob.glob(os.path.join(ROOT, 'rooms', 'room_*.json')),
                   key=lambda f: json.load(open(f, encoding='utf-8')).get('tier') or 0)
    imgs = []
    for f in rooms:
        r = json.load(open(f, encoding='utf-8'))
        p = os.path.join(ROOT, 'previews', 'rooms', r['id'] + '.png')
        if os.path.exists(p):
            imgs.append((r, load_png(p)))
    cols = 2
    rows = [imgs[i:i + cols] for i in range(0, len(imgs), cols)]
    W = max(sum(im.shape[1] for _, im in row) + 12 * (len(row) + 1) for row in rows)
    H = sum(max(im.shape[0] for _, im in row) + 20 for row in rows) + 16
    sh = np.zeros((H, W, 4), np.uint8); sh[..., :3] = (36, 31, 40); sh[..., 3] = 255
    draw_text(sh, 8, 5, 'STARTUPRUSH - 10 PIECES PREDEFINIES (RENDUS NATIFS, COMPOSITEUR DE REFERENCE)', (239, 228, 204, 255))
    y = 16
    for row in rows:
        x = 12
        for r, im in row:
            draw_text(sh, x, y + 2, f"{r['id']}  {r['size_tiles'][0]}X{r['size_tiles'][1]}", (239, 228, 204, 255))
            over(sh, im, x, y + 10)
            x += im.shape[1] + 12
        y += max(im.shape[0] for _, im in row) + 20
    save_png(os.path.join(ROOT, 'previews', 'rooms_overview.png'), sh)
    print(sh.shape)

if __name__ == '__main__':
    main()
