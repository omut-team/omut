using System;
using UnityEngine;

namespace Omut.Core
{
    // Хранит и применяет настройки (паттерн Singleton). Создаётся сам при запуске, как GameManager.
    // Сохраняет в PlayerPrefs (на Windows это реестр), поэтому настройки остаются после перезапуска.
    public class SettingsManager : MonoBehaviour
    {
        private const string PrefsKey = "omut.settings";

        public static SettingsManager Instance { get; private set; }

        public SettingsData Data { get; private set; } = new SettingsData();

        // Настройки поменялись (звук подписывается и меняет громкость).
        public static event Action<SettingsData> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnStart()
        {
            Instance = null;
            Changed = null;
            var go = new GameObject("SettingsManager");
            go.AddComponent<SettingsManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Load()
        {
            Data = SettingsData.FromJson(PlayerPrefs.GetString(PrefsKey, ""));
            Apply();
        }

        // Применить и сохранить.
        public void Save(SettingsData data)
        {
            data.Clamp();
            Data = data;
            PlayerPrefs.SetString(PrefsKey, Data.ToJson());
            PlayerPrefs.Save();
            Apply();
        }

        private void Apply()
        {
            AudioListener.volume = Data.masterVolume;
            QualitySettings.vSyncCount = Data.vSync ? 1 : 0;

            // В редакторе окно Game не меняет размер, поэтому экран трогаем только в сборке.
            if (!Application.isEditor)
            {
                int width = Data.resolutionWidth > 0 ? Data.resolutionWidth : Screen.currentResolution.width;
                int height = Data.resolutionHeight > 0 ? Data.resolutionHeight : Screen.currentResolution.height;
                FullScreenMode mode = Data.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                Screen.SetResolution(width, height, mode);
            }

            Changed?.Invoke(Data);
        }
    }
}
