# Сымон Ульяны (design/export/characters/hero_simon) -> игра.
# 9 кадров ходьбы 32x48 кладём как hero_symon_*, из них же делаем "сидит в лодке" и портрет 96x96.
# Запуск из корня репозитория: python tools/make_hero.py
import os
from PIL import Image

SRC = "design/export/characters/hero_simon/hero_simon_"
DST = "game/Assets/Art/Characters"
FRAMES = ["down_idle", "down_walk_01", "down_walk_02", "side_idle", "side_walk_01", "side_walk_02",
          "up_idle", "up_walk_01", "up_walk_02"]


def main():
    folder = os.path.join(DST, "hero_symon")
    os.makedirs(folder, exist_ok=True)
    for frame in FRAMES:
        Image.open(SRC + frame + ".png").convert("RGBA").save(os.path.join(folder, "hero_symon_" + frame + ".png"))

    down = Image.open(SRC + "down_idle.png").convert("RGBA")
    down.save(os.path.join(DST, "hero_symon.png"))

    # Сидит в лодке: голова и плечи, низ закрывает борт.
    sit = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    sit.alpha_composite(down.crop((0, 0, 32, 32)))
    sit.save(os.path.join(DST, "hero_symon_sit.png"))

    # Портрет: голова из большого кадра Ульяны на бумажном фоне, как у жителей.
    hd = Image.open(SRC + "down_idle_hd.png").convert("RGBA")
    box = hd.getbbox()
    size = box[2] - box[0]
    face = hd.crop((box[0], box[1], box[0] + size, box[1] + size)).resize((96, 96), Image.LANCZOS)
    portrait = Image.new("RGBA", (96, 96), (230, 220, 190, 255))
    portrait.alpha_composite(face)
    portrait.save(os.path.join(DST, "hero_symon_portrait.png"))
    print("hero_symon: 9 frames, sit, portrait")


if __name__ == "__main__":
    main()
