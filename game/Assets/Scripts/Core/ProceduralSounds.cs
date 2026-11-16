using System;
using UnityEngine;

namespace Omut.Core
{
    // Звуки, синтезированные кодом. Нужны, пока Лиза не положила настоящие файлы в Assets/Audio.
    // Каждый звук: массив чисел -1..1 (волна), из него AudioClip.Create делает клип.
    public static class ProceduralSounds
    {
        public const int Rate = 22050;

        // Все звуки игры: id -> генератор.
        public static readonly string[] Ids =
        {
            "bell", "splash", "cast", "bite", "hook", "caught", "line_break", "coins", "hit", "quest",
            "whisper", "drink", "lantern", "chest", "faint", "click", "repair"
        };

        public static AudioClip Create(string id)
        {
            float[] data = Generate(id);
            if (data == null)
                return null;

            AudioClip clip = AudioClip.Create("proc_" + id, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static float[] Generate(string id)
        {
            var random = new System.Random(id.GetHashCode());
            switch (id)
            {
                case "bell": return Bell(3.5f, 196f);
                case "splash": return Noise(0.45f, random, t => Decay(t, 0.45f, 3f) * 0.6f, 0.25f);
                case "cast": return Noise(0.35f, random, t => Mathf.Sin(t / 0.35f * Mathf.PI) * 0.35f, 0.6f);
                case "bite": return Tone(0.18f, t => 520f - 900f * t, t => Decay(t, 0.18f, 4f) * 0.5f);
                case "hook": return Tone(0.12f, t => 900f + 2500f * t, t => Decay(t, 0.12f, 6f) * 0.4f);
                case "caught": return Chord(0.7f, new[] { 523f, 659f, 784f }, 0.12f);
                case "line_break": return Noise(0.25f, random, t => Decay(t, 0.25f, 10f) * 0.8f, 0.05f);
                case "coins": return Chord(0.35f, new[] { 1318f, 1760f }, 0.08f);
                case "hit": return Tone(0.35f, t => 90f - 50f * t, t => Decay(t, 0.35f, 5f) * 0.8f);
                case "quest": return Chord(0.9f, new[] { 392f, 494f, 587f, 784f }, 0.14f);
                case "whisper": return Noise(1.2f, random, t => Mathf.Pow(Mathf.Sin(t / 1.2f * Mathf.PI), 2f) * (0.18f + 0.08f * Mathf.Sin(t * 37f)), 0.9f);
                case "drink": return Tone(0.5f, t => 180f + 60f * Mathf.Sin(t * 30f), t => Mathf.Abs(Mathf.Sin(t * 18f)) * 0.3f * Decay(t, 0.5f, 1f));
                case "lantern": return Tone(0.08f, t => 2000f, t => Decay(t, 0.08f, 8f) * 0.3f);
                case "chest": return Tone(0.6f, t => 140f + 40f * Mathf.Sin(t * 9f), t => Mathf.Sin(t / 0.6f * Mathf.PI) * 0.35f);
                case "faint": return Tone(2.2f, t => 110f - 40f * t, t => Mathf.Sin(t / 2.2f * Mathf.PI) * 0.5f);
                case "click": return Tone(0.04f, t => 1200f, t => Decay(t, 0.04f, 10f) * 0.25f);
                case "repair": return Noise(0.5f, random, t => (Mathf.Repeat(t, 0.16f) < 0.03f ? 0.7f : 0f), 0.2f);
                default: return null;
            }
        }

        // ---------- музыка (петли) ----------

        // День: спокойные аккорды, пентатоника, медленная смена.
        public static AudioClip DayMusic() => Music("proc_music_day", new[]
        {
            new[] { 196f, 247f, 294f, 392f }, new[] { 220f, 262f, 330f, 440f },
            new[] { 175f, 220f, 262f, 349f }, new[] { 196f, 247f, 294f, 370f }
        }, 0.12f, 4f, false);

        // Ночь: низкий гул, тревожные интервалы, ветер.
        public static AudioClip NightMusic() => Music("proc_music_night", new[]
        {
            new[] { 55f, 82.4f, 116.5f }, new[] { 58.3f, 87.3f, 110f },
            new[] { 55f, 77.8f, 116.5f }, new[] { 51.9f, 82.4f, 103.8f }
        }, 0.16f, 5f, true);

        private static AudioClip Music(string name, float[][] chords, float volume, float chordSeconds, bool wind)
        {
            int perChord = (int)(chordSeconds * Rate);
            var data = new float[perChord * chords.Length];
            var random = new System.Random(name.GetHashCode());
            float lowPass = 0f;

            for (int c = 0; c < chords.Length; c++)
            {
                float[] chord = chords[c];
                float[] next = chords[(c + 1) % chords.Length];
                for (int i = 0; i < perChord; i++)
                {
                    float t = (float)(c * perChord + i) / Rate;
                    float k = (float)i / perChord;
                    float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1f, k)); // плавно в следующий аккорд
                    float sample = 0f;
                    for (int n = 0; n < chord.Length; n++)
                    {
                        float vibrato = 1f + 0.002f * Mathf.Sin(t * 5f + n);
                        float a = Mathf.Sin(2f * Mathf.PI * chord[n] * vibrato * t);
                        float b = Mathf.Sin(2f * Mathf.PI * next[n] * vibrato * t);
                        sample += Mathf.Lerp(a, b, fade) / chord.Length;
                    }

                    float swell = 0.75f + 0.25f * Mathf.Sin(t * 0.8f);
                    sample *= swell;

                    if (wind)
                    {
                        lowPass += ((float)random.NextDouble() * 2f - 1f - lowPass) * 0.02f;
                        sample += lowPass * (1.5f + Mathf.Sin(t * 0.3f));
                    }

                    data[c * perChord + i] = sample * volume;
                }
            }

            // Края петли плавно, чтобы не щёлкало.
            int edge = Rate / 20;
            for (int i = 0; i < edge; i++)
            {
                float k = (float)i / edge;
                data[i] *= k;
                data[data.Length - 1 - i] *= k;
            }

            AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---------- кирпичики ----------

        private static float Decay(float t, float length, float speed) => Mathf.Exp(-speed * t / length * 2f) * Mathf.Clamp01((length - t) / 0.02f);

        private static float[] Tone(float seconds, Func<float, float> frequency, Func<float, float> volume)
        {
            var data = new float[(int)(seconds * Rate)];
            float phase = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / Rate;
                phase += 2f * Mathf.PI * frequency(t) / Rate;
                data[i] = Mathf.Sin(phase) * volume(t);
            }

            return data;
        }

        // Шум, пропущенный через простой фильтр: smoothing 0 = резкий, 1 = глухой.
        private static float[] Noise(float seconds, System.Random random, Func<float, float> volume, float smoothing)
        {
            var data = new float[(int)(seconds * Rate)];
            float value = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / Rate;
                float white = (float)random.NextDouble() * 2f - 1f;
                value = Mathf.Lerp(white, value, smoothing);
                data[i] = value * volume(t);
            }

            return data;
        }

        // Ноты одна за другой (арпеджио), каждая звучит до конца.
        private static float[] Chord(float seconds, float[] notes, float step)
        {
            var data = new float[(int)(seconds * Rate)];
            for (int n = 0; n < notes.Length; n++)
            {
                int start = (int)(n * step * Rate);
                for (int i = start; i < data.Length; i++)
                {
                    float t = (float)(i - start) / Rate;
                    data[i] += Mathf.Sin(2f * Mathf.PI * notes[n] * t) * Mathf.Exp(-t * 5f) * 0.25f;
                }
            }

            return data;
        }

        // Колокол: основной тон и неровные обертоны, долгое затухание.
        private static float[] Bell(float seconds, float baseFrequency)
        {
            float[] partials = { 1f, 2f, 2.4f, 3f, 4.2f, 5.4f };
            float[] levels = { 0.5f, 0.3f, 0.25f, 0.15f, 0.1f, 0.06f };
            var data = new float[(int)(seconds * Rate)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / Rate;
                float sample = 0f;
                for (int p = 0; p < partials.Length; p++)
                    sample += Mathf.Sin(2f * Mathf.PI * baseFrequency * partials[p] * t) * levels[p] * Mathf.Exp(-t * (0.8f + p * 0.6f));
                data[i] = sample * Mathf.Clamp01(t / 0.005f) * 0.8f;
            }

            return data;
        }
    }
}
