using System;
using System.Collections.Generic;
using Omut.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Панель настроек: громкость, полный экран, разрешение, VSync.
    // Используется и в главном меню, и в паузе. Изменения применяются кнопкой "Готово".
    public class SettingsPanel
    {
        private readonly RectTransform root;
        private readonly Action onClose;
        private SettingsData editing;

        private Slider master;
        private Slider music;
        private Slider sfx;
        private Slider voice;
        private Toggle fullscreen;
        private Toggle vSync;
        private Text resolutionText;
        private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
        private int resolutionIndex;

        public SettingsPanel(Transform parent, Action onClose)
        {
            this.onClose = onClose;
            root = UiFactory.Window(parent, "Settings", new Vector2(900f, 720f), "Настройки", out _);
            root = (RectTransform)root.parent; // прячем вместе с затемнением

            CollectResolutions();
            BuildContent(root.GetChild(0));
            root.gameObject.SetActive(false);
        }

        public bool IsOpen => root.gameObject.activeSelf;

        public void Open()
        {
            editing = (SettingsManager.Instance != null ? SettingsManager.Instance.Data : new SettingsData()).Clone();
            master.SetValueWithoutNotify(editing.masterVolume);
            music.SetValueWithoutNotify(editing.musicVolume);
            sfx.SetValueWithoutNotify(editing.sfxVolume);
            voice.SetValueWithoutNotify(editing.voiceVolume);
            fullscreen.SetIsOnWithoutNotify(editing.fullscreen);
            vSync.SetIsOnWithoutNotify(editing.vSync);
            resolutionIndex = FindResolution(editing.resolutionWidth, editing.resolutionHeight);
            UpdateResolutionText();
            root.gameObject.SetActive(true);
        }

        public void Close()
        {
            root.gameObject.SetActive(false);
            onClose?.Invoke();
        }

        private void BuildContent(Transform paper)
        {
            RectTransform column = UiFactory.Rect(paper, "Content");
            UiFactory.Stretch(column);
            column.offsetMin = new Vector2(100f, 100f);
            column.offsetMax = new Vector2(-100f, -100f);
            UiFactory.Column(column, 18f);

            master = UiFactory.Slider(column, "Общая громкость", 1f, v => editing.masterVolume = v);
            music = UiFactory.Slider(column, "Музыка", 1f, v => editing.musicVolume = v);
            sfx = UiFactory.Slider(column, "Звуки", 1f, v => editing.sfxVolume = v);
            voice = UiFactory.Slider(column, "Голоса", 1f, v => editing.voiceVolume = v);
            fullscreen = UiFactory.Toggle(column, "Полный экран", true, v => editing.fullscreen = v);
            vSync = UiFactory.Toggle(column, "Вертикальная синхронизация (VSync)", true, v => editing.vSync = v);
            resolutionText = UiFactory.Selector(column, "Разрешение", "", () => StepResolution(-1), () => StepResolution(1));

            RectTransform buttons = UiFactory.Rect(paper, "Buttons");
            UiFactory.Place(buttons, new Vector2(0.5f, 0f), new Vector2(700f, 70f), new Vector2(0f, 24f));
            UiFactory.Row(buttons, 30f);
            UiFactory.Button(buttons, "Готово", Apply, new Vector2(300f, 64f));
            UiFactory.Button(buttons, "Отмена", Close, new Vector2(300f, 64f));
        }

        private void Apply()
        {
            Vector2Int resolution = resolutions[resolutionIndex];
            editing.resolutionWidth = resolution.x;
            editing.resolutionHeight = resolution.y;
            SettingsManager.Instance?.Save(editing);
            Close();
        }

        // Список разрешений монитора без повторов. Первый пункт "как у монитора" (0x0).
        private void CollectResolutions()
        {
            resolutions.Add(Vector2Int.zero);
            foreach (Resolution r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!resolutions.Contains(size) && r.width >= 1024)
                    resolutions.Add(size);
            }
        }

        private int FindResolution(int width, int height)
        {
            int index = resolutions.IndexOf(new Vector2Int(width, height));
            return index < 0 ? 0 : index;
        }

        private void StepResolution(int step)
        {
            resolutionIndex = (resolutionIndex + step + resolutions.Count) % resolutions.Count;
            UpdateResolutionText();
        }

        private void UpdateResolutionText()
        {
            Vector2Int r = resolutions[resolutionIndex];
            resolutionText.text = r == Vector2Int.zero ? "как у монитора" : $"{r.x} x {r.y}";
        }
    }
}
