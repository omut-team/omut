# Звуки рыбалки (design/audio/*.mp3) -> игра (game/Assets/Audio/SFX/sfx_<id>.wav).
# Обрезаем тишину в начале, чтобы звук шёл сразу, делаем моно 44.1 кГц и выравниваем громкость.
# Катушку склеиваем в бесшовную петлю: она крутится, пока игрок тянет рыбу.
# Сборщик сцены (Omut -> Собрать сцену Main) сам подключит файлы по имени sfx_<id>.
# Запуск из корня репозитория: pip install miniaudio, потом python tools/make_sfx.py
import array
import math
import wave

import miniaudio

SRC = "design/audio"
DST = "game/Assets/Audio/SFX"
RATE = 44100

# id в игре: (файл, начало и конец в секундах, громкость пика, петля)
SOUNDS = {
    "cast": ("rod_cast.mp3", 0.25, 1.5, 0.9, False),        # свист удочки при забросе
    "fish_splash": ("fish_splash.mp3", 0.05, 1.7, 0.85, False),  # рыба бьётся в воде: подсечка и вытаскивание
    "reel": ("rod_reel.mp3", 0.1, 3.85, 0.6, True),          # треск катушки, пока тянешь
}


def load(name):
    decoded = miniaudio.decode_file(f"{SRC}/{name}", output_format=miniaudio.SampleFormat.FLOAT32,
                                    nchannels=1, sample_rate=RATE)
    return list(decoded.samples)


def process(samples, start, end, peak, loop):
    s = samples[int(start * RATE):int(end * RATE)]
    if loop:
        # Последние 0.1 с плавно перетекают в первые: на стыке петли нет щелчка.
        n = int(0.1 * RATE)
        head, body, tail = s[:n], s[n:-n], s[-n:]
        mixed = [tail[i] * math.cos(i / n * math.pi / 2) + head[i] * math.sin(i / n * math.pi / 2) for i in range(n)]
        s = body + mixed
    else:
        n = int(0.15 * RATE)  # короткое затухание в конце
        for i in range(n):
            s[-n + i] *= 1 - i / n
        n = int(0.005 * RATE)  # и в начале, чтобы не щёлкало
        for i in range(n):
            s[i] *= i / n
    top = max(abs(x) for x in s) or 1.0
    return [x * peak / top for x in s]


def save(path, samples):
    data = array.array("h", (int(max(-1.0, min(1.0, x)) * 32767) for x in samples))
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())


for sound_id, (name, start, end, peak, loop) in SOUNDS.items():
    out = process(load(name), start, end, peak, loop)
    save(f"{DST}/sfx_{sound_id}.wav", out)
    print(f"sfx_{sound_id}.wav: {len(out) / RATE:.2f} c")
