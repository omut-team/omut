using Omut.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Omut.UI
{
    // Пауза по Esc: Продолжить, Сохранить, Настройки, Управление, В главное меню.
    // На паузе Time.timeScale = 0 (ставит GameManager), всё замирает.
    public class PauseMenu : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform buttons;
        private Button saveButton;
        private Text saveNote;
        private SettingsPanel settings;
        private InfoPanel info;

        // Сохранить игру. Подставляет SaveSystem (этап 12). Возвращает текст для игрока.
        public static System.Func<string> SaveGame;

        private void Awake()
        {
            canvas = UiFactory.CreateCanvas("PauseCanvas", 90, transform);
            Build();
            canvas.gameObject.SetActive(false);
        }

        private void OnEnable() => GameEvents.GameStateChanged += OnStateChanged;

        private void OnDisable() => GameEvents.GameStateChanged -= OnStateChanged;

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || !Controls.EscapePressed)
                return;

            GameState state = game.CurrentState;
            if (state == GameState.Paused)
            {
                Controls.ConsumeEscape();
                if (settings.IsOpen) settings.Close();
                else if (info.IsOpen) info.Close();
                else game.TogglePause();
            }
            else if (state == GameState.Sailing || state == GameState.Fishing)
            {
                Controls.ConsumeEscape();
                game.TogglePause();
            }
        }

        private void Build()
        {
            Image shade = UiFactory.Panel(canvas.transform, "Pause", UiFactory.Theme.Shade);
            RectTransform root = UiFactory.Stretch(shade.rectTransform);

            Text title = UiFactory.HudLabel(root, "Пауза", 80, TextAnchor.MiddleCenter);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 120f), new Vector2(0f, -120f));

            buttons = UiFactory.Rect(root, "Buttons");
            UiFactory.Place(buttons, new Vector2(0.5f, 0.5f), new Vector2(460f, 520f), new Vector2(0f, -60f));
            UiFactory.Column(buttons, 16f);

            UiFactory.Button(buttons, "Продолжить", () => GameManager.Instance?.TogglePause(), new Vector2(440f, 70f), 34);
            saveButton = UiFactory.Button(buttons, "Сохранить игру", OnSave, new Vector2(440f, 70f), 34);
            UiFactory.Button(buttons, "Настройки", OnSettings, new Vector2(440f, 70f), 34);
            UiFactory.Button(buttons, "Управление", OnControls, new Vector2(440f, 70f), 34);
            UiFactory.Button(buttons, "В главное меню", OnMenu, new Vector2(440f, 70f), 34);

            saveNote = UiFactory.HudLabel(root, "", 28, TextAnchor.MiddleCenter);
            UiFactory.Place(saveNote.rectTransform, new Vector2(0.5f, 0f), new Vector2(1200f, 50f), new Vector2(0f, 80f));

            settings = new SettingsPanel(canvas.transform, () => buttons.gameObject.SetActive(true));
            info = new InfoPanel(canvas.transform, new Vector2(1100f, 700f), () => buttons.gameObject.SetActive(true));
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            bool visible = to == GameState.Paused;
            canvas.gameObject.SetActive(visible);
            if (!visible)
                return;

            buttons.gameObject.SetActive(true);
            saveButton.gameObject.SetActive(SaveGame != null);
            saveNote.text = "";
        }

        private void OnSave()
        {
            if (SaveGame != null)
                saveNote.text = SaveGame();
        }

        private void OnSettings()
        {
            buttons.gameObject.SetActive(false);
            settings.Open();
        }

        private void OnControls()
        {
            buttons.gameObject.SetActive(false);
            info.Show("Управление", Controls.Help);
        }

        // Выход в меню: перезагружаем сцену, чтобы мир начался с чистого листа.
        private void OnMenu()
        {
            // Перед выходом в меню сохраняем, иначе "Продолжить" загрузит старое сохранение.
            SaveGame?.Invoke();
            GameManager.Instance?.BackToMenu();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
