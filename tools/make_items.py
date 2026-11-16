# Иконки для панели предметов (удочка, фонарь, карта) и Сымон, сидящий в лодке.
# Тот же простой стиль, что у заглушек. Запуск из корня: python tools/make_items.py
import os
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(__file__), '..', 'game', 'Assets', 'Art', 'Placeholders')


def new(w, h):
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def save(img, name):
    img.save(os.path.join(OUT, name + '.png'))


# Удочка: ручка снизу слева, кончик сверху справа, катушка и леска.
img, d = new(32, 32)
d.line([(5, 28), (27, 4)], fill=(110, 70, 35), width=2)
d.line([(5, 28), (11, 21)], fill=(60, 40, 25), width=3)
d.ellipse([9, 20, 14, 25], fill=(150, 150, 160), outline=(70, 70, 80))
d.line([(27, 4), (28, 18)], fill=(230, 230, 230))
d.ellipse([26, 17, 30, 21], fill=(220, 60, 50))
save(img, 'item_rod')

# Фонарь «летучая мышь».
img, d = new(32, 32)
d.arc([10, 1, 22, 11], 180, 360, fill=(60, 55, 50), width=2)
d.rectangle([10, 6, 22, 9], fill=(70, 60, 50))
d.rectangle([11, 9, 21, 25], fill=(255, 210, 110), outline=(70, 60, 50))
d.ellipse([13, 13, 19, 21], fill=(255, 245, 200))
d.rectangle([9, 25, 23, 29], fill=(70, 60, 50))
save(img, 'item_lantern')

# Карта: свёрнутый лист с озером и крестиком.
img, d = new(32, 32)
d.polygon([(3, 7), (11, 4), (20, 7), (29, 4), (29, 26), (20, 29), (11, 26), (3, 29)], fill=(225, 210, 165), outline=(120, 95, 60))
d.line([(11, 4), (11, 26)], fill=(180, 160, 120))
d.line([(20, 7), (20, 29)], fill=(180, 160, 120))
d.ellipse([7, 11, 22, 23], fill=(90, 140, 160))
d.line([(22, 9), (26, 13)], fill=(180, 40, 30), width=2)
d.line([(26, 9), (22, 13)], fill=(180, 40, 30), width=2)
save(img, 'item_map')

# Сымон сидит в лодке: верх тела 32x32, низ скрыт бортом.
img, d = new(32, 32)
coat, hat = (70, 95, 70), (60, 60, 60)
d.rounded_rectangle([8, 16, 24, 32], 4, fill=coat, outline=(35, 48, 35))
d.ellipse([10, 4, 22, 17], fill=(230, 190, 160), outline=(140, 100, 80))
d.rectangle([9, 2, 23, 7], fill=hat)
d.point((14, 10), fill=(0, 0, 0))
d.point((18, 10), fill=(0, 0, 0))
d.rectangle([22, 20, 26, 24], fill=(230, 190, 160))  # рука, держит удочку
save(img, 'hero_symon_sit')
print('ok')
