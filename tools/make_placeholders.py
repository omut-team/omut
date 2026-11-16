# Рисует простые картинки-заглушки для игры (пока нет арта Ульяны и Лизы).
# Запуск из корня репозитория:  python tools/make_placeholders.py   (нужен Pillow: pip install pillow)
# Картинки ложатся в game/Assets/Art/Placeholders/. Настоящую картинку с тем же именем
# кладите в нужную папку Art/..., игра возьмёт её вместо заглушки (см. docs/ART.md).
import math
import os
import random

from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), '..', 'game', 'Assets', 'Art', 'Placeholders')
os.makedirs(OUT, exist_ok=True)


def save(img, name):
    img.save(os.path.join(OUT, name + '.png'))


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3]) + (255,)


def new(w, h, color=(0, 0, 0, 0)):
    img = Image.new('RGBA', (w, h), color)
    return img, ImageDraw.Draw(img)


# ---------- рыбы: размер картинки = клетки трюма x 32 px, головой вправо ----------
FISH = [
    ('crucian', 1, 1, (200, 170, 70)), ('perch', 1, 1, (120, 160, 70)), ('bream', 2, 1, (170, 160, 120)),
    ('tench', 2, 1, (90, 120, 60)), ('carp', 2, 2, (190, 140, 60)), ('pike', 3, 1, (100, 130, 80)),
    ('zander', 3, 1, (140, 150, 130)), ('burbot', 2, 1, (110, 100, 70)), ('glow_eel', 3, 1, (80, 230, 200)),
    ('twohead_perch', 1, 1, (170, 90, 160)), ('pale_blindfish', 2, 2, (225, 225, 210)),
    ('black_catfish', 3, 2, (45, 45, 55)), ('lake_master', 6, 3, (40, 60, 50)),
]


def fish(name, w, h, color):
    W, H = w * 32, h * 32
    img, d = new(W, H)
    body_w = W * 0.78
    tail = W - body_w
    cy = H / 2
    bh = H * 0.42 if h == 1 else H * 0.36
    d.polygon([(2, cy - bh * 0.9), (tail + 4, cy), (2, cy + bh * 0.9)], fill=shade(color, 0.75))
    d.ellipse([tail - 2, cy - bh, W - 2, cy + bh], fill=color + (255,), outline=shade(color, 0.5))
    d.ellipse([tail + body_w * 0.3, cy - bh * 0.1, W - 8, cy + bh * 0.75], fill=shade(color, 1.25))
    ex = W - max(7, W * 0.12)
    d.ellipse([ex - 3, cy - bh * 0.45 - 3, ex + 3, cy - bh * 0.45 + 3], fill=(250, 250, 240, 255))
    d.point((ex, cy - bh * 0.45), fill=(0, 0, 0, 255))
    if name == 'twohead_perch':
        d.ellipse([W - 14, 2, W - 2, 14], fill=color + (255,), outline=shade(color, 0.5))
    if name in ('perch', 'twohead_perch', 'zander'):
        for i in range(3):
            x = tail + body_w * (0.25 + i * 0.2)
            d.line([(x, cy - bh * 0.9), (x, cy + bh * 0.5)], fill=shade(color, 0.55), width=2)
    if name == 'glow_eel':
        d.ellipse([tail, cy - bh * 0.4, W - 4, cy + bh * 0.4], outline=(220, 255, 240, 255))
    if name == 'lake_master':
        for i in range(6):
            x = tail + body_w * (0.1 + i * 0.13)
            d.ellipse([x, cy - bh * 0.2, x + 10, cy - bh * 0.2 + 10], fill=(140, 255, 160, 255))
        d.line([(W - 20, cy + 4), (W - 60, cy + bh * 0.9)], fill=(20, 30, 20, 255), width=4)
    save(img, 'fish_' + name)


for f in FISH:
    fish(*f)

img, d = new(64, 64)
d.rectangle([6, 18, 58, 58], fill=(110, 70, 35), outline=(50, 30, 10), width=2)
d.rectangle([6, 10, 58, 26], fill=(130, 85, 40), outline=(50, 30, 10), width=2)
d.rectangle([28, 24, 36, 36], fill=(200, 170, 60))
d.line([(10, 40), (54, 40)], fill=(60, 40, 20), width=2)
save(img, 'fish_grandpa_chest')

# ---------- предметы 32x32 ----------
img, d = new(32, 32)
for i in range(3):
    d.arc([3 + i * 7, 9 + (i % 2) * 6, 15 + i * 7, 21 + (i % 2) * 6], 0, 360, fill=(200, 110, 120), width=3)
save(img, 'item_worm')

for name, fill, outline in (('spoon', (200, 200, 210), (120, 120, 130)), ('phosphor_spoon', (120, 255, 190), (40, 160, 110))):
    img, d = new(32, 32)
    d.ellipse([10, 4, 22, 24], fill=fill, outline=outline)
    d.line([(16, 24), (16, 30)], fill=(90, 90, 90), width=2)
    save(img, 'item_' + name)

img, d = new(32, 32)
d.rectangle([8, 8, 24, 28], fill=(170, 40, 30), outline=(80, 20, 10))
d.rectangle([12, 3, 18, 8], fill=(90, 90, 90))
save(img, 'item_kerosene')

img, d = new(32, 32)
d.rectangle([11, 6, 21, 29], fill=(60, 110, 160), outline=(30, 50, 80))
d.rectangle([10, 3, 22, 7], fill=(40, 40, 40))
save(img, 'item_thermos')

img, d = new(32, 32)
d.polygon([(4, 24), (12, 8), (20, 14), (28, 6), (26, 26)], fill=(120, 110, 100), outline=(60, 55, 50))
d.line([(8, 20), (22, 12)], fill=(160, 90, 60), width=2)
save(img, 'item_scrap')

img, d = new(32, 32)
d.ellipse([6, 6, 26, 26], fill=(230, 190, 60), outline=(150, 110, 20), width=2)
d.ellipse([12, 12, 20, 20], outline=(150, 110, 20), width=2)
save(img, 'item_coin')

img, d = new(32, 32)
d.rectangle([7, 3, 25, 29], fill=(235, 225, 195), outline=(120, 100, 70))
for i in range(5):
    d.line([(10, 8 + i * 4), (22, 8 + i * 4)], fill=(90, 80, 70))
save(img, 'item_diary_page')


# ---------- лодки 64x32, носом вправо ----------
def boat(name, hull, deck, cabin, lamp, folder=None):
    # Вид 3/4 под 45°: сверху палуба, снизу виден передний борт. 64x48, низ = ватерлиния.
    img, d = new(64, 48)
    d.ellipse([4, 40, 60, 47], fill=(0, 0, 0, 60))                                  # тень на воде
    d.polygon([(2, 22), (52, 20), (62, 28), (52, 42), (6, 42)], fill=shade(hull, 0.7), outline=shade(hull, 0.4))  # борт
    d.polygon([(2, 14), (50, 10), (62, 22), (50, 30), (2, 28)], fill=hull, outline=shade(hull, 0.5))            # верх борта
    d.polygon([(8, 16), (48, 13), (56, 22), (48, 27), (8, 26)], fill=deck)                                         # палуба
    d.rectangle([18, 8, 34, 22], fill=cabin, outline=(20, 20, 20))
    d.rectangle([18, 8, 34, 11], fill=shade(cabin, 0.8))
    d.ellipse([40, 15, 46, 21], fill=lamp)
    d.line([(6, 36), (52, 36)], fill=shade(hull, 0.5))
    if folder:
        img.save(os.path.join(folder, 'boat_' + name + '.png'))
    else:
        save(img, 'boat_' + name)


boat('sledopyt', (60, 90, 120), (170, 150, 110), (200, 200, 190), (255, 240, 170))
boat('okhotnik', (40, 40, 50), (90, 80, 70), (60, 70, 80), (255, 230, 120))
# Заглушка баркаса лежит в Art/Boats (её заменит рисунок Ульяны с тем же именем).
boat('barkas', (120, 80, 45), (170, 130, 80), (140, 100, 60), (230, 80, 50),
     os.path.join(os.path.dirname(__file__), '..', 'game', 'Assets', 'Art', 'Boats'))


# ---------- люди 32x48 и портреты 96x96 ----------
def person(name, coat, hat, beard=False):
    img, d = new(32, 48)
    d.ellipse([8, 40, 24, 47], fill=(0, 0, 0, 70))
    d.rectangle([10, 34, 14, 44], fill=(50, 45, 40))
    d.rectangle([18, 34, 22, 44], fill=(50, 45, 40))
    d.rounded_rectangle([7, 18, 25, 36], 4, fill=coat, outline=shade(coat, 0.5))
    d.ellipse([10, 6, 22, 19], fill=(230, 190, 160), outline=(140, 100, 80))
    d.rectangle([9, 4, 23, 9], fill=hat)
    if beard:
        d.polygon([(11, 14), (21, 14), (16, 23)], fill=(220, 220, 215))
    d.point((14, 12), fill=(0, 0, 0))
    d.point((18, 12), fill=(0, 0, 0))
    save(img, name)


def portrait(name, coat, hat, beard=False):
    img, d = new(96, 96, (230, 220, 190, 255))
    d.rectangle([0, 0, 95, 95], outline=(90, 70, 50), width=3)
    d.rounded_rectangle([14, 62, 82, 100], 10, fill=coat)
    d.ellipse([26, 18, 70, 68], fill=(230, 190, 160), outline=(140, 100, 80), width=2)
    d.rectangle([22, 12, 74, 28], fill=hat)
    d.ellipse([36, 38, 42, 44], fill=(0, 0, 0))
    d.ellipse([54, 38, 60, 44], fill=(0, 0, 0))
    if beard:
        d.polygon([(32, 54), (64, 54), (48, 82)], fill=(225, 225, 220))
    else:
        d.line([(40, 58), (56, 58)], fill=(120, 60, 50), width=2)
    save(img, name)


for who, coat, hat, beard in (('hero_symon', (70, 100, 70), (60, 60, 60), False),
                              ('npc_taras', (40, 60, 110), (30, 30, 50), True),
                              ('npc_mikhalych', (120, 80, 40), (90, 60, 30), False)):
    person(who, coat, hat, beard)
    portrait(who + '_portrait', coat, hat, beard)


# ---------- тайлы 32x32 ----------
def tile(name, base, length, spot, count=10):
    img, d = new(32, 32, base + (255,))
    rnd = random.Random(name)
    for _ in range(count):
        x, y = rnd.randrange(32), rnd.randrange(32)
        d.line([(x, y), (x + length, y)], fill=spot)
    save(img, 'tile_' + name)


tile('water_bay', (70, 140, 150), 4, (110, 180, 185))
tile('water_deep', (30, 70, 90), 5, (55, 100, 120))
tile('water_reef', (35, 65, 75), 3, (70, 95, 100), 6)
tile('water_dock', (60, 120, 135), 4, (100, 160, 170))
tile('grass', (80, 120, 60), 2, (110, 150, 70), 18)
tile('sand', (200, 180, 130), 1, (170, 150, 100), 14)

img, d = new(32, 32, (150, 110, 70, 255))
for y in (0, 8, 16, 24):
    d.line([(0, y), (31, y)], fill=(100, 70, 40))
for y, x in ((0, 10), (8, 22), (16, 5), (24, 17)):
    d.line([(x, y), (x, y + 7)], fill=(100, 70, 40))
save(img, 'tile_dock')

# ---------- объекты ----------
img, d = new(64, 128)
d.polygon([(18, 120), (46, 120), (40, 30), (24, 30)], fill=(235, 230, 220), outline=(90, 80, 70))
for y in (55, 85, 112):
    d.rectangle([23, y - 8, 41, y], fill=(190, 50, 40))
d.rectangle([20, 18, 44, 32], fill=(60, 60, 60))
d.rectangle([24, 20, 40, 30], fill=(255, 230, 120))
d.polygon([(18, 18), (46, 18), (32, 4)], fill=(150, 40, 30))
save(img, 'obj_lighthouse')

img, d = new(96, 80)
d.rectangle([8, 30, 88, 78], fill=(140, 100, 60), outline=(70, 50, 30), width=2)
d.polygon([(0, 34), (48, 2), (96, 34)], fill=(120, 110, 70), outline=(70, 60, 40))
d.rectangle([40, 50, 56, 78], fill=(80, 55, 30))
d.rectangle([16, 44, 30, 56], fill=(250, 220, 140))
d.rectangle([66, 44, 80, 56], fill=(250, 220, 140))
save(img, 'obj_hut')

img, d = new(96, 80)
d.rectangle([4, 24, 92, 78], fill=(110, 115, 120), outline=(50, 50, 55), width=2)
d.polygon([(0, 28), (48, 6), (96, 28)], fill=(90, 70, 60))
d.rectangle([24, 42, 72, 78], fill=(70, 70, 75))
for y in range(46, 78, 6):
    d.line([(24, y), (72, y)], fill=(50, 50, 55))
save(img, 'obj_garage')

img, d = new(48, 40)
d.rectangle([2, 14, 46, 38], fill=(100, 60, 30), outline=(40, 25, 10), width=2)
d.rectangle([2, 6, 46, 18], fill=(120, 75, 35), outline=(40, 25, 10), width=2)
d.rectangle([20, 14, 28, 24], fill=(90, 90, 100))
d.ellipse([22, 17, 26, 21], fill=(20, 20, 20))
save(img, 'obj_dock_chest')

img, d = new(48, 32)
d.line([(4, 24), (44, 10)], fill=(70, 50, 30), width=6)
d.line([(14, 20), (10, 6)], fill=(70, 50, 30), width=4)
d.line([(30, 15), (40, 28)], fill=(60, 40, 25), width=4)
save(img, 'obj_snag')

img, d = new(32, 32)
d.polygon([(4, 26), (8, 8), (18, 3), (28, 10), (29, 26)], fill=(105, 105, 110), outline=(55, 55, 60))
d.line([(10, 12), (18, 8)], fill=(150, 150, 155), width=2)
save(img, 'obj_reef_rock')

img, d = new(128, 96)
d.rectangle([8, 30, 120, 90], fill=(90, 100, 95), outline=(40, 45, 42), width=3)
for x in (20, 50, 80):
    d.rectangle([x, 44, x + 18, 64], fill=(25, 35, 35))
d.polygon([(8, 30), (40, 6), (70, 30)], fill=(70, 80, 75))
d.line([(90, 30), (110, 8)], fill=(60, 60, 60), width=4)
save(img, 'obj_biostation')

img, d = new(32, 32)
for i, x in enumerate((6, 12, 18, 24)):
    off = (i % 2) * 3
    d.line([(x, 31), (x + off - 1, 4 + i * 2)], fill=(90, 130, 60), width=2)
    d.ellipse([x - 2 + off, 2 + i * 2, x + 2 + off, 10 + i * 2], fill=(120, 80, 40))
save(img, 'env_reed')

img, d = new(32, 32)
d.pieslice([4, 6, 28, 28], 30, 340, fill=(70, 140, 60))
d.ellipse([14, 12, 20, 18], fill=(240, 240, 230))
save(img, 'env_lily')

img, d = new(16, 16)
d.ellipse([2, 9, 14, 15], fill=(255, 255, 255, 90))                    # круг на воде, овал 2:1
d.ellipse([5, 6, 11, 12], fill=(230, 60, 40), outline=(90, 20, 10))    # поплавок
d.rectangle([7, 0, 9, 7], fill=(240, 240, 240))
save(img, 'obj_bobber')

# ---------- эффекты (белые, цвет и прозрачность задаёт код) ----------
img, d = new(64, 64)
for r, a in ((10, 230), (19, 170), (28, 110)):
    d.ellipse([32 - r, 32 - r, 32 + r, 32 + r], outline=(255, 255, 255, a), width=2)
save(img, 'fx_ripple')

img, d = new(16, 24)
d.rectangle([5, 1, 11, 15], fill=(255, 220, 60), outline=(120, 80, 0))
d.rectangle([5, 18, 11, 23], fill=(255, 220, 60), outline=(120, 80, 0))
save(img, 'fx_bite')

save(Image.new('RGBA', (4, 4), (255, 255, 255, 255)), 'fx_white')


def radial(name, size, alpha_of_r):
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    px = img.load()
    c = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            r = math.hypot(x - c, y - c) / (size / 2)
            px[x, y] = (255, 255, 255, int(255 * max(0.0, min(1.0, alpha_of_r(r, x, y)))))
    save(img, name)


radial('fx_soft_circle', 128, lambda r, x, y: max(0.0, 1 - r) ** 1.5)
radial('fx_fog_hole', 256, lambda r, x, y: (r - 0.18) / 0.55)
radial('fx_fog_puff', 128, lambda r, x, y: 0.6 * max(0.0, 1 - r) * (0.75 + 0.25 * math.sin(x * 0.2 + y * 0.13)))
radial('fx_vignette', 256, lambda r, x, y: ((r - 0.55) / 0.6) ** 1.3 if r > 0.55 else 0)

print('готово:', len(os.listdir(OUT)), 'файлов в', os.path.abspath(OUT))
