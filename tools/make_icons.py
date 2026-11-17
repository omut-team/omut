# Иконки вещей Ульяны (design/export/items/*_hd.png) -> игра.
# Большие картинки в ячейке смотрятся лучше маленьких 32x32: обрезаем пустые поля,
# ставим по центру квадрата и ужимаем до 128x128 (в Unity 128 пикселей = 1 клетка, как у остальных предметов).
# Запуск из корня репозитория: python tools/make_icons.py
import os
from PIL import Image

SRC = "design/export/items"
DST = "game/Assets/Art/Items"
SIZE = 128

# имя в игре: файл Ульяны
ICONS = {
    "item_coin": "item_coins_hd",
    "item_diary_page": "item_diary_pages_hd",
    "item_lantern": "item_lantern_hd",
    "item_map": "item_map_hd",
    "item_scrap": "item_scrap_hd",
    "item_thermos": "item_thermos_hd",
}


def icon(path):
    img = Image.open(path).convert("RGBA")
    img = img.crop(img.getbbox())
    side = max(img.size)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.alpha_composite(img, ((side - img.width) // 2, (side - img.height) // 2))
    # Небольшой отступ, чтобы вещь не упиралась в рамку ячейки.
    pad = side // 16
    framed = Image.new("RGBA", (side + pad * 2, side + pad * 2), (0, 0, 0, 0))
    framed.alpha_composite(square, (pad, pad))
    return framed.resize((SIZE, SIZE), Image.LANCZOS)


def main():
    os.makedirs(DST, exist_ok=True)
    for name, source in ICONS.items():
        icon(os.path.join(SRC, source + ".png")).save(os.path.join(DST, name + ".png"))
        print(name)


if __name__ == "__main__":
    main()
