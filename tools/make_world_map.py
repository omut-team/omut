# Карта озера Ульяны (design/export/ui/ui_map_bg.png) -> мир игры.
# 1) game/Assets/Art/Map/world_map.png: картинка мира (8192 px в ширину, 100 x 56.25 клеток).
# 2) game/Assets/Art/Map/world_mask.txt: сетка 0.5 клетки: '~' вода, '#' земля, скалы, дома.
#    По ней сборщик сцены ставит берег (лодка не заплывает на сушу) и места для рыбы.
# Запуск из корня репозитория: python tools/make_world_map.py
import sys
from PIL import Image, ImageFilter

Image.MAX_IMAGE_PIXELS = None
SRC = "design/export/ui/ui_map_bg.png"
OUT_IMG = "game/Assets/Art/Map/world_map.png"
OUT_MASK = "game/Assets/Art/Map/world_mask.txt"
WORLD_W = 100.0
CELL = 0.5

src = Image.open(SRC).convert("RGB")
W, H = src.size
world_h = WORLD_W * H / W
cols, rows = int(WORLD_W / CELL), int(round(world_h / CELL))

big = src.resize((8192, round(8192 * H / W)), Image.LANCZOS)
big.save(OUT_IMG, optimize=True)

# Маленькая копия: на клетку 6x6 пикселей, по ним голосуем вода/суша.
k = 6
small = src.resize((cols * k, rows * k), Image.BOX).filter(ImageFilter.MedianFilter(3))
px = small.load()


def is_water(r, g, b):
    if g > b + 25 and g > r:          # трава, кувшинки, деревья
        return False
    if r > b + 12:                    # дерево, мостки, крыши, песок
        return False
    if b > r + 12:                    # вода тёмная и зелёная (в заливе)
        return True
    bright = (r + g + b) / 3
    return bright > 150               # белый туман над рифом = вода, серые камни = суша


lines = []
for y in range(rows):
    row = []
    for x in range(cols):
        water = 0
        for dy in range(k):
            for dx in range(k):
                if is_water(*px[x * k + dx, y * k + dy]):
                    water += 1
        row.append('~' if water >= k * k * 0.5 else '#')
    lines.append(''.join(row))

# Края картинки (лес сверху и по бокам): всегда суша, чтобы лодка не уплыла за карту.
for r in range(rows):
    for c in range(cols):
        if r < 7 or r >= rows - 2 or c < 2 or c >= cols - 2:
            lines[r] = lines[r][:c] + '#' + lines[r][c + 1:]

# Луч маяка над водой нарисован оранжевым: это вода (сам маяк оставляем сушей).
grid = [list(line) for line in lines]
for r in range(64, 80):
    for c in range(128, 166):
        if not (143 <= c <= 153):
            grid[r][c] = '~'


def world(c, r):
    return -50 + (c + 0.5) * CELL, world_h / 2 - (r + 0.5) * CELL


# Мелкие пятна суши: в омуте это коряги ('x', бьют лодку), на рифе скалы ('r'),
# в остальных местах кувшинки и блики, их убираем (вода).
seen = [[False] * cols for _ in range(rows)]
for r0 in range(rows):
    for c0 in range(cols):
        if grid[r0][c0] != '#' or seen[r0][c0]:
            continue
        stack, cells = [(r0, c0)], []
        seen[r0][c0] = True
        while stack:
            r, c = stack.pop()
            cells.append((r, c))
            for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                rr, cc = r + dr, c + dc
                if 0 <= rr < rows and 0 <= cc < cols and not seen[rr][cc] and grid[rr][cc] == '#':
                    seen[rr][cc] = True
                    stack.append((rr, cc))
        x = sum(world(c, r)[0] for r, c in cells) / len(cells)
        y = sum(world(c, r)[1] for r, c in cells) / len(cells)
        border = any(r < 8 or c < 3 or c >= cols - 3 for r, c in cells)
        in_reef = y > 13 and x > -38 and not border
        in_pool = -14 < x and -9 < y <= 13
        if in_reef and len(cells) < 400:
            mark = 'r'
        elif in_pool and len(cells) <= 12:
            mark = 'x'
        elif len(cells) <= 4:
            mark = '~'
        else:
            continue
        for r, c in cells:
            grid[r][c] = mark
lines = [''.join(row) for row in grid]

with open(OUT_MASK, 'w', encoding='utf-8', newline='\n') as f:
    f.write(f"{cols} {rows} {CELL} {WORLD_W} {world_h:.4f}\n")
    f.write('\n'.join(lines) + '\n')
print(cols, rows, f"world {WORLD_W} x {world_h:.2f}")

if len(sys.argv) > 1:   # превью маски поверх карты
    prev = src.resize((cols * 8, rows * 8))
    over = Image.new("RGBA", prev.size, (0, 0, 0, 0))
    op = over.load()
    for y, line in enumerate(lines):
        for x, ch in enumerate(line):
            color = {'#': (255, 0, 0, 90), 'r': (255, 255, 0, 140), 'x': (255, 0, 255, 160)}.get(ch)
            if color:
                for dy in range(8):
                    for dx in range(8):
                        op[x * 8 + dx, y * 8 + dy] = color
    prev = prev.convert("RGBA")
    prev.alpha_composite(over)
    prev.save(sys.argv[1])
