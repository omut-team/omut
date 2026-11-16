using System;
using UnityEngine;

namespace Omut.Core
{
    // Настройки игрока. Обычный класс с данными: сохраняется в JSON (PlayerPrefs),
    // проверяется автотестами (Tests/EditMode/SettingsDataTests.cs).
    [Serializable]
    public class SettingsData
    {
        public float masterVolume = 1f;   // общая громкость 0..1
        public float musicVolume = 0.7f;  // музыка 0..1
        public float sfxVolume = 1f;      // звуки 0..1
        public float voiceVolume = 1f;    // голоса персонажей 0..1
        public bool fullscreen = true;
        public int resolutionWidth;       // 0 = как у монитора
        public int resolutionHeight;
        public bool vSync = true;

        // Привести значения в допустимые пределы (если файл настроек испорчен руками).
        public void Clamp()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            voiceVolume = Mathf.Clamp01(voiceVolume);
            resolutionWidth = Mathf.Max(0, resolutionWidth);
            resolutionHeight = Mathf.Max(0, resolutionHeight);
        }

        // Итоговая громкость музыки и звуков с учётом общей.
        public float MusicLevel => masterVolume * musicVolume;
        public float SfxLevel => masterVolume * sfxVolume;

        public string ToJson() => JsonUtility.ToJson(this);

        // Прочитать из JSON. Пустая или битая строка = настройки по умолчанию.
        public static SettingsData FromJson(string json)
        {
            var data = new SettingsData();
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(json, data);
                }
                catch (ArgumentException)
                {
                    data = new SettingsData();
                }
            }

            data.Clamp();
            return data;
        }

        public SettingsData Clone() => FromJson(ToJson());
    }
}
