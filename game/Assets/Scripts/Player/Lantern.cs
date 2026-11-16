using Omut.Core;
using Omut.UI;
using Omut.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Omut.Player
{
    // Фонарь на лодке (F). Светит на радиус из BoatData.lightRadius, тратит керосин из GameSession.
    // Без фонаря ночью видно меньше 1 клетки и быстро растёт паника (этап 18).
    public class Lantern : MonoBehaviour
    {
        public static Lantern Instance { get; private set; }

        [SerializeField] private Light2D lanternLight;
        [SerializeField] private Light2D walkerLight;

        [Tooltip("Слабый свет вокруг лодки без фонаря (луна), клеток")]
        [SerializeField] private float moonRadius = 1f;

        public bool IsOn => Fuel != null && Fuel.IsOn;
        public LanternFuel Fuel { get; private set; }

        private System.Func<string> hudLine;

        private void Awake()
        {
            Instance = this;
            hudLine = () => PlayerWalker.OnFoot || Fuel == null ? null
                : $"Фонарь [F]: {(IsOn ? "<color=#f2c94c>горит</color>" : "погашен")}, керосин {Mathf.CeilToInt(Fuel.Fuel)}%";
            Hud.AddLine(hudLine);
        }

        private void OnDestroy()
        {
            Hud.RemoveLine(hudLine);
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable() => GameEvents.NewDayStarted += OnMorning;

        private void OnDisable() => GameEvents.NewDayStarted -= OnMorning;

        private void Start() => SyncFromSession();

        // Бак берём из GameSession (керосин покупается у Тараса и сохраняется).
        private void SyncFromSession()
        {
            GameSession session = GameSession.Instance;
            Fuel = new LanternFuel(session != null ? session.Kerosene : 100f);
        }

        private void OnMorning(int day)
        {
            if (IsOn)
            {
                Fuel.SetOn(false);
                Notifications.Show("Рассвело, фонарь погашен.");
            }
        }

        private void Update()
        {
            GameSession session = GameSession.Instance;
            GameManager game = GameManager.Instance;
            if (Fuel == null)
                SyncFromSession();

            // Керосин могли купить у Тараса или загрузить сохранение: берём значение из сессии.
            if (session != null && !Mathf.Approximately(session.Kerosene, Fuel.Fuel))
                Fuel.Set(session.Kerosene);

            bool playing = game != null && game.CurrentState != GameState.Menu && game.CurrentState != GameState.Paused;
            if (playing && !PlayerWalker.OnFoot && game.CurrentState == GameState.Sailing && Controls.Pressed(Controls.Lantern))
                Toggle();

            if (playing && Fuel.Burn(Time.deltaTime))
                Notifications.Show("Керосин кончился, фонарь погас! Плыви к маяку.", UiFactory.Theme.Bad);

            if (session != null)
                session.Kerosene = Fuel.Fuel;

            UpdateLights(session);
        }

        public void Toggle()
        {
            if (Fuel.IsEmpty && !Fuel.IsOn)
            {
                Notifications.Show("В фонаре нет керосина. Купи у Тараса.");
                return;
            }

            Fuel.Toggle();
            GameEvents.RaisePlaySound("lantern");
        }

        private void UpdateLights(GameSession session)
        {
            float radius = session != null && session.CurrentBoat != null ? session.CurrentBoat.lightRadius : 2.5f;
            bool dark = DayNightCycle.Instance == null || DayNightCycle.Instance.Clock.Phase == DayPhase.Night
                        || DayNightCycle.Instance.Clock.Phase == DayPhase.Evening || FogController.InEternalFog;

            if (lanternLight != null)
            {
                bool lit = !PlayerWalker.OnFoot && dark;
                lanternLight.enabled = lit;
                lanternLight.pointLightOuterRadius = IsOn ? radius : moonRadius;
                lanternLight.pointLightInnerRadius = IsOn ? radius * 0.35f : 0f;
                lanternLight.intensity = IsOn ? 1.1f + Mathf.Sin(Time.time * 9f) * 0.04f : 0.35f; // лёгкое мерцание огня
            }

            if (walkerLight != null)
                walkerLight.enabled = PlayerWalker.OnFoot && dark;
        }

        // Светит ли что-то рядом с этой точкой (для паники): фонарь лодки.
        public float LitRadius => IsOn && !PlayerWalker.OnFoot && GameSession.Instance?.CurrentBoat != null ? GameSession.Instance.CurrentBoat.lightRadius : 0f;
    }
}
