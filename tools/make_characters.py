# Персонажи Ульяны (design/export/characters) -> игра.
# Спрайты 32x48 и кадры ходьбы кладём в game/Assets/Art/Characters, портреты 96x96 режем из _sheet_hd.
# Запуск из корня репозитория: python tools/make_characters.py
import os
import shutil
from PIL import Image

SRC = "design/export/characters"
DST = "game/Assets/Art/Characters"
FRAMES = ["down_idle", "down_walk_01", "down_walk_02", "side_idle", "side_walk_01", "side_walk_02",
          "up_idle", "up_walk_01", "up_walk_02"]

for npc in ["taras", "mikhalych", "yazep", "yadviga"]:
    name = "npc_" + npc
    os.makedirs(os.path.join(DST, name), exist_ok=True)
    shutil.copy(os.path.join(SRC, name + ".png"), os.path.join(DST, name + ".png"))
    for frame in FRAMES:
        shutil.copy(os.path.join(SRC, name, f"{name}_{frame}.png"), os.path.join(DST, name, f"{name}_{frame}.png"))

    # Портрет: голова и плечи из большого кадра "лицом к нам", на бумажном фоне.
    hd = Image.open(os.path.join(SRC, name, name + "_sheet_hd.png")).convert("RGBA")
    face = hd.crop((22, 2, 122, 102)).resize((96, 96), Image.LANCZOS)
    portrait = Image.new("RGBA", (96, 96), (230, 220, 190, 255))
    portrait.alpha_composite(face)
    portrait.save(os.path.join(DST, name + "_portrait.png"))
    print(name)
