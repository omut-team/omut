using Omut.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Главное меню: Новая игра, Продолжить, Настройки, Управление, Об игре, Выход.
    // Видно только в состоянии Menu. За меню видно озеро, время там стоит.
    public class MainMenu : MonoBehaviour
    {
        public const string AboutText =
            "Полесье, деревня Тихие Вирки. Сымон, горожанин, получил в наследство от деда Язэпа хату и старый баркас. " +
            "Дед пропал три года назад.\n\n" +
            "На причале стоит запертый сундук деда, а в дневнике вырваны страницы.\n\n" +
            "Правило озера: в 20:00 бьёт колокол маяка. После него безопасно только у света.\n\n" +
            "Групповой проект по ТРПО, группа ПО-2409, 2026.";

        [Tooltip("Фон главного меню (Ульяна: Art/UI/ui_menu_bg)")]
        [SerializeField] private Sprite background;

        [Tooltip("Дощечка для кнопок (Art/UI/ui_button_plank). Пусто = обычные кнопки")]
        [SerializeField] private Sprite buttonSprite;

        private Canvas canvas;
        private RectTransform root;
        private RectTransform buttons;
        private Button continueButton;
        private SettingsPanel settings;
        private InfoPanel info;
        private Text online;

        // Есть ли сохранение, которое можно продолжить. Подставляет SaveSystem (этап 12).
        public static System.Func<bool> HasSave = () => false;

        private void Awake()
        {
            canvas = UiFactory.CreateCanvas("MainMenuCanvas", 100, transform);
            Build();
        }

        private void OnEnable() => GameEvents.GameStateChanged += OnStateChanged;

        private void OnDisable() => GameEvents.GameStateChanged -= OnStateChanged;

        private void Start() => Show(GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Menu);

        private void Build()
        {
            if (background != null)
            {
                Image bg = UiFactory.Panel(canvas.transform, "Background", Color.white);
                bg.sprite = background;
                UiFactory.Stretch(bg.rectTransform);
                // Растянуть по экрану без искажения (лишнее обрезается).
                var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = background.rect.width / background.rect.height;
            }

            Image shade = UiFactory.Panel(canvas.transform, "MainMenu", new Color(0.03f, 0.07f, 0.09f, background != null ? 0.45f : 0.72f));
            root = UiFactory.Stretch(shade.rectTransform);

            bool art = background != null;

            Text title = UiFactory.HudLabel(root, "ОМУТ", 150, TextAnchor.MiddleCenter);
            title.gameObject.SetActive(!art); // на фоне Ульяны вывеска уже нарисована
            title.fontStyle = FontStyle.Bold;
            title.color = UiFactory.Theme.Paper;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(1200f, 180f), new Vector2(0f, -110f));

            Text subtitle = UiFactory.HudLabel(root, "Полесье. Тихие Вирки. Колокол бьёт в 20:00.", 34, TextAnchor.MiddleCenter);
            subtitle.color = new Color(0.75f, 0.82f, 0.8f);
            UiFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(1200f, 50f), new Vector2(0f, -290f));
            subtitle.gameObject.SetActive(!art);

            buttons = UiFactory.Rect(root, "Buttons");
            UiFactory.Place(buttons, new Vector2(0.5f, 1f), new Vector2(520f, 700f), new Vector2(0f, art ? -318f : -360f));
            UiFactory.Column(buttons, art ? 8f : 16f);

            continueButton = UiFactory.Button(buttons, "Продолжить", OnContinue, new Vector2(440f, 70f), 34);
            UiFactory.Button(buttons, "Новая игра", OnNewGame, new Vector2(440f, 70f), 34);
            UiFactory.Button(buttons, "Настройки", OnSettings, new Vector2(440f, 70f), 34);
            UiFactory.Button(buttons, "Управление", () => ShowInfo("Управление", Controls.Help), new Vector2(440f, 70f), 34);
            UiFactory.Button(buttons, "Онлайн-рекорды", OnOnline, new Vector2(440f, 70f), 34);
            UiFactory.Button(buttons, "Об игре", () => ShowInfo("Об игре", AboutText), new Vector2(440f, 70f), 34);
            UiFactory.Button(buttons, "Выход", OnQuit, new Vector2(440f, 70f), 34);

            if (buttonSprite != null)
                foreach (Button b in buttons.GetComponentsInChildren<Button>(true))
                    StylePlank(b);

            Text version = UiFactory.HudLabel(root, "версия " + Application.version, 22, TextAnchor.LowerRight);
            UiFactory.Place(version.rectTransform, new Vector2(1f, 0f), new Vector2(400f, 40f), new Vector2(-20f, 14f));

            online = UiFactory.HudLabel(root, "", 24, TextAnchor.LowerLeft);
            UiFactory.Place(online.rectTransform, new Vector2(0f, 0f), new Vector2(900f, 40f), new Vector2(20f, 14f));

            settings = new SettingsPanel(canvas.transform, () => buttons.gameObject.SetActive(true));
            info = new InfoPanel(canvas.transform, new Vector2(1100f, 700f), () => buttons.gameObject.SetActive(true));
        }

        // Кнопка-дощечка как на макете Ульяны: бумага в деревянной рамке, тёмные буквы.
        private void StylePlank(Button button)
        {
            Text label = button.GetComponentInChildren<Text>();
            var image = button.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Simple;
            label.rectTransform.offsetMin = new Vector2(60f, 4f);
            label.rectTransform.offsetMax = new Vector2(-60f, -4f);
            image.color = Color.white;
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(480f, 82f);
            var layout = button.GetComponent<LayoutElement>();
            layout.preferredWidth = 480f;
            layout.preferredHeight = 82f;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.92f, 0.9f, 0.86f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = new Color(0.92f, 0.9f, 0.86f);
            colors.pressedColor = new Color(0.75f, 0.7f, 0.62f);
            button.colors = colors;

            label.color = UiFactory.Theme.Ink;
            label.font = UiFactory.BoldFont;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 30;
            label.resizeTextForBestFit = false;
            label.text = label.text.ToUpperInvariant();
        }

        private void Show(bool visible)
        {
            canvas.gameObject.SetActive(visible);
            if (!visible)
                return;

            buttons.gameObject.SetActive(true);
            continueButton.gameObject.SetActive(HasSave());
            Network.AuthService auth = Network.AuthService.Instance;
            online.text = auth != null && auth.IsLoggedIn ? "Онлайн: " + auth.PlayerName : "Без входа: трофеи не попадают в рекорды";
        }

        private void OnOnline()
        {
            if (LoginUI.Instance == null)
                return;
            buttons.gameObject.SetActive(false);
            LoginUI.Instance.Show(() => Show(true));
        }

        private void OnStateChanged(GameState from, GameState to) => Show(to == GameState.Menu);

        private void OnNewGame()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance?.StartGame();
        }

        private void OnContinue()
        {
            GameEvents.RaiseContinueRequested();
            GameManager.Instance?.StartGame();
        }

        private void OnSettings()
        {
            buttons.gameObject.SetActive(false);
            settings.Open();
        }

        private void ShowInfo(string title, string body)
        {
            buttons.gameObject.SetActive(false);
            info.Show(title, body);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
