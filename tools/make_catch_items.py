# Предметы улова и сундук Ильи (design/export/items) -> игра.
# Как у рыб: 64 пикселя на клетку трюма (сапог 1x1 = 64x64, шина 2x2 = 128x128).
# Сундук: сундучок деда со дна (улов 2x2) и сундук у сарая (на земле, 32 пикселя = 1 клетка).
# Запуск из корня репозитория: python tools/make_catch_items.py
import os
from PIL import Image

SRC = "design/export/items"
FISH = "game/Assets/Art/Fish"
ENV = "game/Assets/Art/Environment"

# имя в игре: (файл Ильи, ширина и высота в клетках трюма)
CATCH = {
    "fish_boot": ("boot", 1, 1),
    "fish_bottle": ("bottle", 1, 1),
    "fish_can": ("can", 1, 1),
    "fish_plastic_bag": ("plastic_bag", 1, 1),
    "fish_tire": ("tire", 2, 2),
    "fish_grandpa_chest": ("chest", 2, 2),
}


def load(name):
    img = Image.open(os.path.join(SRC, name + ".png")).convert("RGBA")
    # У сундука по всему полю почти прозрачный шум: убираем, иначе не обрезать края.
    alpha = img.split()[3].point(lambda v: 0 if v <= 20 else v)
    img.putalpha(alpha)
    return img.crop(img.getbbox())


def fit(img, width, height, pad=0.06):
    # Вписать с полями по центру прозрачного холста, пропорции не меняем.
    inner_w, inner_h = int(width * (1 - pad * 2)), int(height * (1 - pad * 2))
    k = min(inner_w / img.width, inner_h / img.height)
    small = img.resize((max(1, round(img.width * k)), max(1, round(img.height * k))), Image.LANCZOS)
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    canvas.alpha_composite(small, ((width - small.width) // 2, (height - small.height) // 2))
    return canvas


def main():
    os.makedirs(FISH, exist_ok=True)
    os.makedirs(ENV, exist_ok=True)
    for name, (source, w, h) in CATCH.items():
        fit(load(source), w * 64, h * 64).save(os.path.join(FISH, name + ".png"))
        print(name)

    # Сундук у сарая стоит на земле: низ картинки = низ сундука.
    chest = load("chest")
    k = 48 / chest.width
    small = chest.resize((48, round(chest.height * k)), Image.LANCZOS)
    small.save(os.path.join(ENV, "obj_dock_chest.png"))
    print("obj_dock_chest", small.size)


if __name__ == "__main__":
    main()
