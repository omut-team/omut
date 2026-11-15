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
            string text = cycle == null ? "" : FormatClock(cycle.Clock);

            // Обновляем текст только когда он поменялся, чтобы не пересобирать UI каждый кадр.
            if (text != lastShown)
            {
                clockText.text = text;
                lastShown = text;
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

        // Создаём Canvas и два текста кодом, пока нет нарисованного интерфейса.
        private void BuildDefaultUi()
        {
            var canvasObject = new GameObject("ClockCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (clockText == null)
                clockText = CreateText(canvas.transform, "ClockText", font, 36, TextAnchor.UpperRight,
                    new Vector2(1f, 1f), new Vector2(-30f, -20f), new Vector2(700f, 60f));

            if (messageText == null)
                messageText = CreateText(canvas.transform, "MessageText", font, 44, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1400f, 80f));
        }

        private static Text CreateText(Transform parent, string name, Font font, int size, TextAnchor alignment,
            Vector2 anchor, Vector2 position, Vector2 boxSize)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = boxSize;

            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false; // текст не должен перехватывать клики мышью

            // Чёрная обводка, чтобы текст читался и на светлой воде, и ночью.
            textObject.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            return text;
        }
    }
}
