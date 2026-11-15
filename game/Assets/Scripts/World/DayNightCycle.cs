using Omut.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Omut.World
{
    // Смена дня и ночи в сцене. Крутит игровые часы (GameClock), меняет общий свет (Global Light 2D)
    // и сообщает всем о смене фазы, колоколе в 20:00 и новом дне через GameEvents (паттерн Observer).
    // На сцене он один, остальные скрипты берут время через DayNightCycle.Instance.Clock.
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }

        [Header("Длина суток")]
        [Tooltip("Сколько реальных минут длится день (06:00-20:00)")]
        [Min(0.1f)] [SerializeField] private float realDayMinutes = 8f;

        [Tooltip("Сколько реальных минут длится ночь (20:00-06:00)")]
        [Min(0.1f)] [SerializeField] private float realNightMinutes = 4f;

        [Tooltip("Во сколько начинается игра (часы)")]
        [Range(0, 23)] [SerializeField] private int startHour = 6;

        [Header("Свет")]
        [Tooltip("Общий свет сцены. Если пусто, найдётся сам (Global Light 2D)")]
        [SerializeField] private Light2D globalLight;

        [Tooltip("Цвет света по времени суток: слева 00:00, в середине 12:00, справа 24:00")]
        [SerializeField] private Gradient lightColor = CreateDefaultColors();

        [Tooltip("Яркость света по времени суток: по X доля суток (0 = 00:00, 0.5 = 12:00), по Y яркость")]
        [SerializeField] private AnimationCurve lightIntensity = CreateDefaultIntensity();

        [Header("Колокол")]
        [Tooltip("Звук колокола маяка. Пока можно оставить пустым, звук добавит Лиза")]
        [SerializeField] private AudioClip bellSound;

        [Header("Отладка (только в редакторе)")]
        [Tooltip("T: +1 час, Y: сразу 19:55 (проверить колокол)")]
        [SerializeField] private bool debugKeys = true;

        public GameClock Clock { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            Clock = new GameClock(realDayMinutes, realNightMinutes, startHour * 60f);
            Clock.PhaseChanged += OnPhaseChanged;
            Clock.BellRang += OnBellRang;
            Clock.NewDay += OnNewDay;

            if (globalLight == null)
                globalLight = FindGlobalLight();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            ApplyLight();
        }

        private void Update()
        {
            if (debugKeys && Debug.isDebugBuild)
                HandleDebugKeys();

            // В главном меню время стоит. На паузе тоже: там Time.deltaTime = 0.
            GameManager game = GameManager.Instance;
            if (game != null && game.CurrentState == GameState.Menu)
                return;

            Clock.Tick(Time.deltaTime);
            ApplyLight();
        }

        private void ApplyLight()
        {
            if (globalLight == null)
                return;

            float t = Clock.DayFraction;
            globalLight.color = lightColor.Evaluate(t);
            globalLight.intensity = lightIntensity.Evaluate(t);
        }

        private void OnPhaseChanged(DayPhase from, DayPhase to)
        {
            Debug.Log($"[Часы] {Clock}: {from} -> {to}");
            GameEvents.RaiseDayPhaseChanged(from, to);
        }

        private void OnBellRang()
        {
            Debug.Log("[Часы] 20:00, бьёт колокол маяка");

            if (bellSound != null)
                AudioSource.PlayClipAtPoint(bellSound, transform.position);

            GameEvents.RaiseBellRang();
        }

        private void OnNewDay(int day)
        {
            Debug.Log($"[Часы] Наступил день {day}");
            GameEvents.RaiseNewDayStarted(day);
        }

        private void HandleDebugKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            // +1 час проматываем через Tick, чтобы сработали колокол и смена фазы.
            if (keyboard.tKey.wasPressedThisFrame)
                Clock.Tick(60f / Clock.MinutesPerSecond);

            if (keyboard.yKey.wasPressedThisFrame)
                Clock.SetTime(19, 55);
        }

        private static Light2D FindGlobalLight()
        {
            foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            {
                if (light.lightType == Light2D.LightType.Global)
                    return light;
            }

            Debug.LogWarning("[DayNightCycle] В сцене нет Global Light 2D, свет меняться не будет");
            return null;
        }

        // Цвета по умолчанию: ночь синяя, утро тёплое, день белый, закат оранжевый.
        private static Gradient CreateDefaultColors()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.25f, 0.30f, 0.55f), 0.00f),  // 00:00 ночь
                    new GradientColorKey(new Color(0.25f, 0.30f, 0.55f), 0.22f),  // 05:15
                    new GradientColorKey(new Color(1.00f, 0.80f, 0.65f), 0.27f),  // 06:30 рассвет
                    new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.42f),  // 10:00 день
                    new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.70f),  // 17:00
                    new GradientColorKey(new Color(1.00f, 0.60f, 0.40f), 0.80f),  // 19:10 закат
                    new GradientColorKey(new Color(0.25f, 0.30f, 0.55f), 0.86f),  // 20:40 ночь
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        // Яркость по умолчанию: ночью 0.25, днём 1.
        private static AnimationCurve CreateDefaultIntensity()
        {
            return new AnimationCurve(
                new Keyframe(0.00f, 0.25f),  // 00:00
                new Keyframe(0.22f, 0.25f),  // 05:15
                new Keyframe(0.30f, 0.90f),  // 07:10
                new Keyframe(0.42f, 1.00f),  // 10:00
                new Keyframe(0.70f, 1.00f),  // 17:00
                new Keyframe(0.83f, 0.30f),  // 20:00
                new Keyframe(1.00f, 0.25f)); // 24:00
        }
    }
}
