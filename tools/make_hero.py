# Сымон из кадров Ульяны: берём деда Тараса (кепка, куртка, 9 кадров ходьбы) и делаем внука.
# Седые волосы -> тёмно-русые, бороду и усы -> кожа (Сымон молодой), синяя куртка -> красная,
# кепка -> коричневая. Получаются кадры hero_symon_*, сидячий спрайт для лодки и портрет.
# Запуск из корня репозитория: python tools/make_hero.py
import colorsys
import os
from PIL import Image

SRC = "game/Assets/Art/Characters/npc_taras/npc_taras_"
DST = "game/Assets/Art/Characters"
FRAMES = ["down_idle", "down_walk_01", "down_walk_02", "side_idle", "side_walk_01", "side_walk_02",
          "up_idle", "up_walk_01", "up_walk_02"]

SKIN = (222, 160, 118)
HAIR = (112, 78, 48)


def scale(color, k):
    return tuple(max(0, min(255, int(c * k))) for c in color)


def is_navy(p):
    h, s, v = colorsys.rgb_to_hsv(p[0] / 255, p[1] / 255, p[2] / 255)
    return v < 0.42 and (0.55 < h < 0.8 or s < 0.12) and v > 0.08


def is_grey_hair(p):
    h, s, v = colorsys.rgb_to_hsv(p[0] / 255, p[1] / 255, p[2] / 255)
    return s < 0.22 and v > 0.38


def face_box(kind):
    # Где у Тараса борода и усы (ниже глаз), по кадрам Ульяны.
    if kind == "down":
        return (11, 15, 23, 22)
    if kind == "side":
        return (14, 15, 24, 22)
    return None


def make(frame):
    img = Image.open(SRC + frame + ".png").convert("RGBA")
    kind = frame.split("_")[0]
    out = img.copy()
    cap = 12 if kind == "up" else 11  # кепка с козырьком: верхние ряды
    box = face_box(kind)
    for y in range(img.height):
        for x in range(img.width):
            p = img.getpixel((x, y))
            if p[3] == 0:
                continue
            h, s, v = colorsys.rgb_to_hsv(p[0] / 255, p[1] / 255, p[2] / 255)
            rgb = None
            if y <= 23 and is_grey_hair(p):
                in_face = box and box[0] <= x <= box[2] and box[1] <= y <= box[3]
                rgb = scale(SKIN, 0.55 + v * 0.5) if in_face else scale(HAIR, 0.45 + v * 0.75)
            elif is_navy(p) and y <= 37:
                if y <= cap:
                    # Кепка: коричневая.
                    r, g, b = colorsys.hsv_to_rgb(0.07, 0.45, min(1.0, v * 1.25))
                else:
                    # Куртка: тёмно-красная, хорошо видна и на траве, и на мостках.
                    r, g, b = colorsys.hsv_to_rgb(0.99, 0.5, min(1.0, v * 1.35))
                rgb = (int(r * 255), int(g * 255), int(b * 255))
            if rgb:
                out.putpixel((x, y), rgb + (p[3],))
    return out


def main():
    folder = os.path.join(DST, "hero_symon")
    os.makedirs(folder, exist_ok=True)
    frames = {}
    for frame in FRAMES:
        frames[frame] = make(frame)
        frames[frame].save(os.path.join(folder, "hero_symon_" + frame + ".png"))

    down = frames["down_idle"]
    down.save(os.path.join(DST, "hero_symon.png"))

    # Сидит в лодке: верх фигуры (голова и плечи), низ закрывает борт.
    sit = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    sit.alpha_composite(down.crop((0, 2, 32, 34)))
    sit.save(os.path.join(DST, "hero_symon_sit.png"))

    # Портрет: голова крупно (пиксели x4) на бумажном фоне, как у жителей.
    head = down.crop((4, 1, 28, 25)).resize((96, 96), Image.NEAREST)
    portrait = Image.new("RGBA", (96, 96), (230, 220, 190, 255))
    portrait.alpha_composite(head)
    portrait.save(os.path.join(DST, "hero_symon_portrait.png"))
    print("hero_symon: 9 кадров, sit, portrait")


if __name__ == "__main__":
    main()
