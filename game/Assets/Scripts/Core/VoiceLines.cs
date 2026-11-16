using System.Text;
using System.Text.RegularExpressions;

namespace Omut.Core
{
    // Озвучка реплик. Для каждой реплики заранее записан файл Resources/Voice/v<код>.wav
    // (голос Windows, скрипт tools/make_voice.ps1). Код считается из имени говорящего и текста,
    // поэтому если текст поменять, старый файл просто не найдётся и включится "бормотание".
    public static class VoiceLines
    {
        public const string Folder = "Voice";

        public static string ClipName(string speaker, string text) => "v" + Hash((speaker ?? "") + "|" + (text ?? "")).ToString("x8");

        // FNV-1a: простой и одинаковый на всех компьютерах хэш строки.
        public static uint Hash(string s)
        {
            uint h = 2166136261;
            foreach (char c in s)
            {
                h ^= c;
                h *= 16777619;
            }

            return h;
        }

        // Текст без разметки <b>..</b>, чтобы голос не читал теги.
        public static string Plain(string text) => Regex.Replace(text ?? "", "<[^>]+>", "");

        // Голос по говорящему: высота тона для "бормотания" (если файла нет).
        public static float Pitch(string speaker)
        {
            switch (speaker)
            {
                case "Тарас": return 0.8f;
                case "Михалыч": return 0.95f;
                case "Сымон": return 1.15f;
                default: return 0.7f;
            }
        }

        public static int Syllables(string text)
        {
            int n = 0;
            foreach (char c in Plain(text).ToLowerInvariant())
                if ("аеёиоуыэюя".IndexOf(c) >= 0)
                    n++;
            return n;
        }
    }
}
