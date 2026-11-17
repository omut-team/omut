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
            // F работает и в лодке, и на берегу (фонарь в руке).
            if (playing && game.CurrentState == GameState.Sailing && Controls.Pressed(Controls.Lantern))
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
            // Зажёг: фонарь сам оказывается в руке. Погасил: снова удочка.
            Hotbar.Instance?.TakeInHand(IsOn);
        }

        // ---------- анимация огня ----------

        private float glow;          // 0 = погашен, 1 = горит (плавно разгорается и гаснет)
        private SpriteRenderer boatGlow;
        private SpriteRenderer handGlow;
        private static Sprite glowSprite;

        // Тёплое пятно света у фонаря: видно даже днём, мерцает как огонь.
        private SpriteRenderer CreateGlow(Transform parent, Vector2 position)
        {
            if (parent == null)
                return null;
            if (glowSprite == null)
            {
                const int size = 32;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
                texture.Apply();
                glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            }

            var go = new GameObject("FlameGlow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = glowSprite;
            sr.sortingOrder = 3;
            GameSession session = GameSession.Instance;
            if (session != null && session.Database != null && session.Database.unlitMaterial != null)
                sr.sharedMaterial = session.Database.unlitMaterial;
            return sr;
        }

        private void AnimateGlow(SpriteRenderer sr, bool visible, float flicker)
        {
            if (sr == null)
                return;
            sr.enabled = visible && glow > 0.01f;
            sr.color = new Color(1f, 0.78f, 0.35f, 0.75f * glow);
            sr.transform.localScale = Vector3.one * (0.9f + 0.6f * glow) * flicker;
        }

        private void UpdateLights(GameSession session)
        {
            glow = Mathf.MoveTowards(glow, IsOn ? 1f : 0f, Time.deltaTime * 4f);
            float flicker = 1f + Mathf.Sin(Time.time * 11f) * 0.05f + Mathf.Sin(Time.time * 23f) * 0.03f;
            if (boatGlow == null && lanternLight != null)
                boatGlow = CreateGlow(lanternLight.transform, Vector2.zero);
            if (handGlow == null && walkerLight != null)
                handGlow = CreateGlow(walkerLight.transform, new Vector2(0.3f, -0.05f));
            AnimateGlow(boatGlow, !PlayerWalker.OnFoot, flicker);
            AnimateGlow(handGlow, PlayerWalker.OnFoot && Hotbar.HoldsLantern, flicker);

            float radius = session != null && session.CurrentBoat != null ? session.CurrentBoat.lightRadius : 2.5f;
            bool dark = DayNightCycle.Instance == null || DayNightCycle.Instance.Clock.Phase == DayPhase.Night
                        || DayNightCycle.Instance.Clock.Phase == DayPhase.Evening || FogController.InEternalFog;

            if (lanternLight != null)
            {
                // Свет разгорается плавно (glow) и мерцает как огонь.
                bool lit = !PlayerWalker.OnFoot && (dark || glow > 0.01f);
                lanternLight.enabled = lit;
                lanternLight.pointLightOuterRadius = Mathf.Lerp(moonRadius, radius, glow);
                lanternLight.pointLightInnerRadius = radius * 0.35f * glow;
                lanternLight.intensity = Mathf.Lerp(dark ? 0.35f : 0f, 1.1f * flicker, glow);
            }

            if (walkerLight != null)
            {
                // На берегу: в темноте немного видно и так, с фонарём в руке свет шире и мерцает.
                walkerLight.enabled = PlayerWalker.OnFoot && (dark || glow > 0.01f);
                walkerLight.pointLightOuterRadius = Mathf.Lerp(2.2f, 4f, glow);
                walkerLight.intensity = Mathf.Lerp(dark ? 0.7f : 0f, 1f * flicker, glow);
                if (handGlow != null)
                {
                    // Огонёк там, где фонарь в руке (рука зависит от того, куда смотрит Сымон).
                    PlayerWalker walker = PlayerWalker.Instance;
                    if (walker != null)
                    {
                        HeldItemView.Place(walker.Facing, true, out Vector2 hand, out _, out _);
                        handGlow.transform.position = (Vector2)walker.transform.position + hand;
                    }
                }
            }
        }

        // Светит ли что-то рядом с этой точкой (для паники): фонарь лодки.
        public float LitRadius => IsOn && !PlayerWalker.OnFoot && GameSession.Instance?.CurrentBoat != null ? GameSession.Instance.CurrentBoat.lightRadius : 0f;
    }
}
