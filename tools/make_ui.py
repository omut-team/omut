# Рисует элементы интерфейса в стиле макета Ульяны (бумага в деревянной рамке).
# Текстуру дерева и бумаги берём с её дощечки game/Assets/Art/UI/ui_button_plank.png.
# Запуск из корня репозитория:  python tools/make_ui.py   (нужны pillow и numpy)
# Картинки: game/Assets/Art/UI/ui_*.png. Края для растяжения (9-slice) ставит Editor/ArtImportSettings.cs.
import os
import random

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.join(os.path.dirname(__file__), '..')
UI = os.path.join(ROOT, 'game', 'Assets', 'Art', 'UI')
plank = np.asarray(Image.open(os.path.join(UI, 'ui_button_plank.png')).convert('RGB')).astype(np.float32)
rnd = np.random.default_rng(11)

# Образцы: дерево (верхняя кромка дощечки) и бумага (середина).
wood_strip = plank[2:12, 60:500]                 # 10 x 440
paper_patch = plank[30:70, 140:420]              # 40 x 280
paper_color = np.median(paper_patch.reshape(-1, 3), axis=0)
wood_color = np.median(wood_strip.reshape(-1, 3), axis=0)


def paper(w, h, vignette=0.18):
    base = np.ones((h, w, 3), np.float32) * paper_color
    noise = rnd.normal(0, 6, (h, w, 1))
    fibres = np.asarray(Image.fromarray((rnd.random((h // 4 + 1, w // 4 + 1)) * 255).astype(np.uint8))
                        .resize((w, h), Image.BICUBIC)).astype(np.float32)[..., None] / 255 * 12 - 6
    yy, xx = np.mgrid[0:h, 0:w]
    edge = np.minimum(np.minimum(xx, w - 1 - xx), np.minimum(yy, h - 1 - yy)).astype(np.float32)
    dark = np.clip(1 - edge / (min(w, h) * 0.35), 0, 1)[..., None] * vignette
    return np.clip((base + noise + fibres) * (1 - dark) - dark * 20, 0, 255)


def wood(w, h, vertical=False):
    strip = wood_strip if not vertical else np.transpose(wood_strip, (1, 0, 2))
    tile = np.asarray(Image.fromarray(strip.astype(np.uint8)).resize((w, h), Image.BILINEAR)).astype(np.float32)
    grain = rnd.normal(0, 8, (h, w, 1))
    return np.clip(tile * 0.85 + grain, 0, 255)


def frame(img, border, color_dark=(38, 24, 14)):
    h, w, _ = img.shape
    out = img.copy()
    out[:border] = wood(w, border)
    out[-border:] = wood(w, border)
    out[:, :border] = wood(border, h, True)
    out[:, -border:] = wood(border, h, True)
    pil = Image.fromarray(out.astype(np.uint8))
    d = ImageDraw.Draw(pil)
    d.rectangle([0, 0, w - 1, h - 1], outline=color_dark, width=2)
    d.rectangle([border - 2, border - 2, w - border + 1, h - border + 1], outline=color_dark, width=2)
    # Гвоздики по углам.
    for x, y in ((border // 2, border // 2), (w - border // 2 - 1, border // 2),
                 (border // 2, h - border // 2 - 1), (w - border // 2 - 1, h - border // 2 - 1)):
        d.ellipse([x - 3, y - 3, x + 3, y + 3], fill=(150, 140, 120), outline=(40, 30, 20))
    return pil


def save(img, name):
    img.save(os.path.join(UI, name + '.png'))
    print('ok', name, img.size)


# Большая панель окна: бумага в деревянной рамке. 9-slice: края 28 px.
save(frame(paper(160, 160, 0.22), 22), 'ui_panel')

# Маленькая деревянная кнопка (для узких кнопок). 9-slice: края 14 px.
btn = wood(96, 44)
btn = Image.fromarray(btn.astype(np.uint8))
d = ImageDraw.Draw(btn)
d.rectangle([0, 0, 95, 43], outline=(35, 22, 12), width=2)
d.line([(3, 3), (92, 3)], fill=tuple(int(c) for c in np.clip(wood_color * 1.3, 0, 255)), width=1)
for x in (8, 87):
    d.ellipse([x - 3, 19, x + 3, 25], fill=(150, 140, 120), outline=(40, 30, 20))
save(btn, 'ui_button_wood')

# Ячейка инвентаря: тёмное углубление с деревянной рамкой. 9-slice: края 10 px.
slot = np.ones((64, 64, 3), np.float32) * np.array([42, 36, 28]) + rnd.normal(0, 4, (64, 64, 1))
slot_img = frame(slot, 8)
save(slot_img, 'ui_slot')

# Выбранная ячейка: золотая рамка.
sel = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
d = ImageDraw.Draw(sel)
d.rectangle([1, 1, 62, 62], outline=(242, 201, 76, 255), width=4)
d.rectangle([5, 5, 58, 58], outline=(120, 90, 30, 255), width=1)
save(sel, 'ui_slot_selected')

# Панель HUD: тёмное дерево, полупрозрачная середина. 9-slice: края 12 px.
hud = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
hud_rgb = frame(np.ones((64, 64, 3), np.float32) * np.array([30, 26, 22]), 7)
hud.paste(hud_rgb, (0, 0))
a = np.full((64, 64), 225, np.uint8)
a[7:57, 7:57] = 190
hud.putalpha(Image.fromarray(a))
save(hud, 'ui_hud')

# Рамка шкалы (страх, натяжение, сила заброса). 9-slice: края 8 px.
bar = frame(np.ones((24, 96, 3), np.float32) * np.array([25, 20, 16]), 5)
save(bar, 'ui_bar')

# Ярлык с именем над NPC: бумажная табличка. 9-slice: края 10 px.
tag = Image.new('RGBA', (96, 32), (0, 0, 0, 0))
p = Image.fromarray(paper(96, 32, 0.25).astype(np.uint8)).convert('RGBA')
mask = Image.new('L', (96, 32), 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, 95, 31], 8, fill=235)
tag.paste(p, (0, 0), mask)
ImageDraw.Draw(tag).rounded_rectangle([0, 0, 95, 31], 8, outline=(60, 40, 25, 255), width=2)
save(tag, 'ui_tag')

# Значки над NPC: "!" есть задание, "?" можно сдать. 32x40.
for name, ch_color, mark in (('ui_mark_quest', (242, 201, 76), '!'), ('ui_mark_turnin', (120, 220, 140), '?')):
    m = Image.new('RGBA', (32, 40), (0, 0, 0, 0))
    d = ImageDraw.Draw(m)
    d.ellipse([2, 2, 30, 30], fill=(40, 28, 16, 235), outline=ch_color + (255,), width=3)
    d.polygon([(10, 28), (22, 28), (16, 38)], fill=(40, 28, 16, 235))
    if mark == '!':
        d.rectangle([14, 7, 18, 19], fill=ch_color + (255,))
        d.rectangle([14, 22, 18, 25], fill=ch_color + (255,))
    else:
        d.arc([10, 6, 22, 17], 200, 20, fill=ch_color + (255,), width=3)
        d.line([(19, 15), (16, 19), (16, 21)], fill=ch_color + (255,), width=3)
        d.rectangle([14, 23, 18, 26], fill=ch_color + (255,))
    save(m, name)

# Стрелка к цели (у края экрана). 48x48, смотрит вправо.
arrow = Image.new('RGBA', (48, 48), (0, 0, 0, 0))
d = ImageDraw.Draw(arrow)
d.polygon([(6, 12), (30, 12), (30, 4), (46, 24), (30, 44), (30, 36), (6, 36)], fill=(242, 201, 76, 255), outline=(60, 40, 10, 255))
save(arrow, 'ui_arrow')
