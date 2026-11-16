# Лодки Ульяны (design/export/boats, 1448x1086) -> спрайты игры 64x48 для вида 45°.
# Обрезаем прозрачные края, вписываем с сохранением пропорций, ставим по низу (опора внизу).
# Запуск из корня репозитория: python tools/make_boats.py
from PIL import Image

SRC = "design/export/boats/{}.png"
DST = "game/Assets/Art/Boats/{}.png"
W, H = 64, 48

for name in ["boat_barkas", "boat_sledopyt", "boat_okhotnik"]:
    im = Image.open(SRC.format(name)).convert("RGBA")
    alpha = im.getchannel("A").point(lambda a: 255 if a > 40 else 0)
    im = im.crop(alpha.getbbox())
    scale = min(W / im.width, H / im.height)
    size = (max(1, round(im.width * scale)), max(1, round(im.height * scale)))
    small = im.resize(size, Image.LANCZOS)
    # Пиксель-арт: альфа только 0 или 255, чтобы не было мыльной каймы.
    a = small.getchannel("A").point(lambda v: 255 if v > 110 else 0)
    small.putalpha(a)
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.alpha_composite(small, ((W - size[0]) // 2, H - size[1]))
    out.save(DST.format(name))
    print(name, size)
