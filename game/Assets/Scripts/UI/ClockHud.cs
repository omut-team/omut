using Omut.Core;
using Omut.World;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Часы в правом верхнем углу: "День 1  06:00  Утро".
    // В 20:00 на несколько секунд показывает надпись про колокол.
    // Если поля Clock Text и Message Text пустые, скрипт сам создаёт Canvas и тексты,
    // поэтому его можно просто повесить на любой объект в сцене.
    // Когда Ульяна нарисует интерфейс, тексты можно заменить своими и перетащить в поля.
    public class ClockHud : MonoBehaviour
    {
        [Tooltip("Текст для часов. Пусто = создать самому")]
        [SerializeField] private Text clockText;

        [Tooltip("Текст для сообщений (колокол). Пусто = создать самому")]
        [SerializeField] private Text messageText;

        [Tooltip("Сколько секунд висит надпись про колокол")]
        [Min(0f)] [SerializeField] private float messageSeconds = 4f;

        private float messageTimer;
        private string lastShown;

        private void Awake()
        {
            if (clockText == null || messageText == null)
                BuildDefaultUi();

            messageText.text = "";
        }

        private void OnEnable()
        {
            GameEvents.BellRang += OnBellRang;
            GameEvents.NewDayStarted += OnNewDay;
        }

        private void OnDisable()
        {
            GameEvents.BellRang -= OnBellRang;
            GameEvents.NewDayStarted -= OnNewDay;
        }

        private void Update()
        {
            DayNightCycle cycle = DayNightCycle.Instance;
            bool inMenu = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Menu;
            string text = cycle == null || inMenu ? "" : FormatClock(cycle.Clock);

            // Обновляем текст только когда он поменялся, чтобы не пересобирать UI каждый кадр.
            if (text != lastShown)
            {
                clockText.text = text;
                lastShown = text;
                if (clockPanel != null)
                    clockPanel.gameObject.SetActive(text.Length > 0);
            }

            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f)
                    messageText.text = "";
            }
        }

        public static string FormatClock(GameClock clock)
            => $"День {clock.Day}   {clock}   {PhaseName(clock.Phase)}";

        public static string PhaseName(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Morning: return "Утро";
                case DayPhase.Day:     return "День";
                case DayPhase.Evening: return "Вечер";
                default:               return "Ночь";
            }
        }

        private void OnBellRang() => ShowMessage("Бьёт колокол маяка. Держись у света!");

        private void OnNewDay(int day) => ShowMessage($"Рассвет. День {day}");

        private void ShowMessage(string message)
        {
            messageText.text = message;
            messageTimer = messageSeconds;
        }

        // Часы в правой колонке HUD (тёмная деревянная панель), надпись про колокол по центру сверху.
        private void BuildDefaultUi()
        {
            if (clockText == null)
            {
                clockPanel = HudLayout.AddPanel(HudLayout.Right, "Clock", 470f, 64f);
                clockText = UiFactory.HudLabel(clockPanel.transform, "", 32, TextAnchor.MiddleCenter);
                UiFactory.Stretch(clockText.rectTransform, 8f);
            }

            if (messageText == null)
            {
                Canvas canvas = UiFactory.CreateCanvas("ClockMessageCanvas", 24, transform);
                messageText = UiFactory.HudLabel(canvas.transform, "", 46, TextAnchor.MiddleCenter);
                messageText.color = UiFactory.Theme.Gold;
                UiFactory.Place(messageText.rectTransform, new Vector2(0.5f, 1f), new Vector2(1400f, 80f), new Vector2(0f, -250f));
            }
        }

        private Image clockPanel;
    }
}
