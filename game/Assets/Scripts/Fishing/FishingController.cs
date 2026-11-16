using System;
using Omut.Core;
using Omut.UI;
using Omut.World;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Omut.Fishing
{
    // Рыбалка с лодки. Висит на лодке. Хранит текущее состояние (паттерн State, см. FishingStates.cs)
    // и всё, что нужно состояниям: поплавок, силу заброса, рыбу на крючке, модель натяжения.
    // Интерфейс (CastPowerBar, BitePrompt, TensionBar, CatchCard) только читает отсюда данные.
    public class FishingController : MonoBehaviour
    {
        public static FishingController Instance { get; private set; }

        [SerializeField] private GameDatabase database;

        [Header("Картинки")]
        [SerializeField] private Sprite bobberSprite;
        [SerializeField] private Sprite biteSprite;
        [SerializeField] private Sprite lineSprite;

        [Header("Заброс")]
        [Tooltip("Дальность при минимальной силе, клеток")]
        [SerializeField] private float minCastDistance = 1.5f;

        [Tooltip("Дальность при полной силе, клеток")]
        [SerializeField] private float maxCastDistance = 7f;

        [Tooltip("Как быстро бегает шкала силы (раз в секунду от 0 до 1)")]
        [SerializeField] private float chargeSpeed = 0.9f;

        [Header("Поклёвка")]
        [Tooltip("Сколько секунд есть на подсечку")]
        [SerializeField] private float biteWindow = 0.9f;

        [SerializeField] private Vector2 biteDelayInSpot = new Vector2(2f, 5f);
        [SerializeField] private Vector2 biteDelayOutside = new Vector2(6f, 13f);

        // Связи с другими системами. По умолчанию простые, следующие этапы подставляют свои.
        public static Func<BaitType> CurrentBait = () => BaitType.Worm;        // TackleBox, этап 11
        public static Func<bool> ChestAllowed = () => true;                     // дневник, этап 20
        public static Func<bool> CanFishHere = () => true;                      // на причале нельзя, этап 10
        public static Func<float> FakeBitesPerSecond = () => 0f;                // паника, этап 18
        public static Func<FishData> ForcedFish = () => null;                   // финал, этап 22

        // ---- данные для состояний и интерфейса ----
        public float CastPower { get; set; }
        public Vector2 AimTarget { get; set; }
        public bool IsBiting { get; set; }
        public bool FakeBitePending { get; set; }
        public TensionModel Reel { get; set; }
        public FishData HookedFish { get; private set; }
        public CatchInfo LastCatch { get; private set; }
        public bool InSpot { get; private set; }
        public float CastDistance { get; private set; }
        public ZoneId CastZone { get; private set; }
        public float ChargeSpeed => chargeSpeed;
        public float BiteWindow => biteWindow;
        public float MaxCastDistance => maxCastDistance;
        public Vector2 BobberPosition => bobber != null ? (Vector2)bobber.position : Vector2.zero;
        public FishingState State => state;
        public GameDatabase Database => database;

        // ---- состояния ----
        public FishingState Idle { get; private set; }
        public FishingState Charging { get; private set; }
        public FishingState Flying { get; private set; }
        public FishingState Waiting { get; private set; }
        public FishingState Bite { get; private set; }
        public FishingState Reeling { get; private set; }
        public FishingState Caught { get; private set; }

        private FishingState state;
        private FishSelector selector;
        private Transform bobber;
        private SpriteRenderer bobberRenderer;
        private SpriteRenderer biteIcon;
        private SpriteRenderer line;
        private Vector2 bobberRest;
        private Vector2 landPoint;
        private float facing = 1f;

        private void Awake()
        {
            Instance = this;
            selector = new FishSelector(database != null ? database.fish : new System.Collections.Generic.List<FishData>());

            Idle = new IdleState(this);
            Charging = new ChargingState(this);
            Flying = new FlyingState(this);
            Waiting = new WaitingState(this);
            Bite = new BiteState(this);
            Reeling = new ReelingState(this);
            Caught = new CaughtState(this);

            CreateTackle();
            SetState(Idle);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable() => GameEvents.GameStateChanged += OnGameStateChanged;

        private void OnDisable() => GameEvents.GameStateChanged -= OnGameStateChanged;

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game != null && (game.CurrentState == GameState.Paused || game.CurrentState == GameState.Menu))
                return;

            UpdateFacing();
            state.Tick(Time.deltaTime);
            UpdateLine();
        }

        public void SetState(FishingState next)
        {
            state?.Exit();
            state = next;
            state.Enter();
        }

        // Если игру вывели из рыбалки снаружи (обморок, меню), сматываем снасть.
        private void OnGameStateChanged(GameState from, GameState to)
        {
            if (from == GameState.Fishing && to != GameState.Paused && state != Idle && state != Caught)
                SetState(Idle);
        }

        public bool CanStartFishing()
        {
            GameManager game = GameManager.Instance;
            return (game == null || game.CurrentState == GameState.Sailing) && GameWindow.Active == null && CanFishHere();
        }

        // ---------- заброс ----------

        // Опора лодки внизу (ватерлиния), удочка на уровне палубы.
        public Vector2 RodTip => Omut.Player.BoatRider.Instance != null
            ? Omut.Player.BoatRider.Instance.RodTip
            : (Vector2)transform.position + new Vector2(facing * 0.9f, 0.8f);

        public Vector2 ComputeTarget(float power)
        {
            Vector2 origin = transform.position;
            Vector2 direction = new Vector2(facing, 0f);

            Camera cam = Camera.main;
            if (cam != null && UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 mouse = cam.ScreenToWorldPoint(Controls.MouseScreenPosition);
                Vector2 toMouse = mouse - origin;
                if (toMouse.sqrMagnitude > 0.25f)
                    direction = toMouse.normalized;
            }

            return origin + direction * Mathf.Lerp(minCastDistance, maxCastDistance, power);
        }

        // Вода ли тут: внутри какой-то зоны и не камень, не берег, не причал.
        public bool IsWater(Vector2 point)
        {
            if (Zone.Find(point) == null)
                return false;

            foreach (Collider2D hit in Physics2D.OverlapPointAll(point))
            {
                if (!hit.isTrigger)
                    return false;
            }

            return true;
        }

        public void Landed(Vector2 point)
        {
            landPoint = point;
            bobberRest = point;
            Zone zone = Zone.Find(point);
            CastZone = zone != null ? zone.Id : ZoneId.Dock;
            CastDistance = Vector2.Distance(transform.position, point);
            InSpot = FishingSpot.FindAt(point) != null;
            if (InSpot)
                Notifications.Show("Прямо в круги! Клюнет быстрее.", new Color(0.7f, 1f, 0.8f));
            RepickFish();
        }

        // Выбрать, кто клюнет (заново после пропущенной поклёвки).
        public void RepickFish()
        {
            FishData forced = ForcedFish();
            if (forced != null)
            {
                HookedFish = forced;
                return;
            }

            var conditions = new CastConditions(CastZone, CurrentPhase(), CurrentBait(), CastDistance, InSpot, ChestAllowed());
            HookedFish = selector.Pick(conditions);
        }

        public float NextBiteDelay()
        {
            Vector2 range = InSpot ? biteDelayInSpot : biteDelayOutside;
            return Random.Range(range.x, range.y);
        }

        public string NoBiteHint()
        {
            switch (CurrentBait())
            {
                case BaitType.Phosphor when CurrentPhase() != DayPhase.Night:
                    return "Фосфорная блесна днём бесполезна. Смени наживку (Q).";
                case BaitType.Worm when CastZone == ZoneId.Pool || CastZone == ZoneId.Reef:
                    return "Тут на червя не клюёт. Хищнику нужна блесна (Q).";
                case BaitType.Spoon when CastZone == ZoneId.Bay:
                    return "В заливе мирная рыба. Попробуй червя (Q).";
                default:
                    return "Не клюёт. Попробуй другое место, наживку или время.";
            }
        }

        public bool FakeBiteNow(float dt)
        {
            float chance = FakeBitesPerSecond();
            if (chance <= 0f || Random.value >= chance * dt)
                return false;

            FakeBitePending = true;
            return true;
        }

        // ---------- итог ----------

        public void OnCaught()
        {
            FishData fish = HookedFish;
            float weight = selector.RollWeight(fish);
            bool night = CurrentPhase() == DayPhase.Night;
            int day = DayNightCycle.Instance != null ? DayNightCycle.Instance.Clock.Day : 1;

            LastCatch = new CatchInfo(fish, weight, night, CastZone, day);
            GameEvents.RaiseBaitUsed(CurrentBait());
            SetState(Caught);
            Sfx("caught");
            GameEvents.RaiseFishCaught(LastCatch);
        }

        public void OnLost(bool lineBroke)
        {
            if (lineBroke)
            {
                Notifications.Show("Леска оборвалась! Рыба ушла вместе с наживкой.", UiFactory.Theme.Bad);
                GameEvents.RaiseBaitUsed(CurrentBait());
                Sfx("line_break");
            }
            else
            {
                Notifications.Show("Натяжение упало до нуля. Рыба сошла.", new Color(1f, 0.8f, 0.5f));
            }

            GameEvents.RaiseCatchLost(HookedFish, lineBroke);
            StopFishing();
        }

        // Карточка трофея закрыта: возвращаемся к лодке.
        public void FinishCatch()
        {
            LastCatch = null;
            StopFishing();
        }

        public void StopFishing()
        {
            SetState(Idle);
            HookedFish = null;
            GameManager game = GameManager.Instance;
            if (game != null && game.CurrentState == GameState.Fishing)
                game.ChangeState(GameState.Sailing);
        }

        public static DayPhase CurrentPhase()
        {
            return DayNightCycle.Instance != null ? DayNightCycle.Instance.Clock.Phase : DayPhase.Day;
        }

        public void Sfx(string id) => GameEvents.RaisePlaySound(id);

        // ---------- поплавок и леска ----------

        private void CreateTackle()
        {
            bobber = new GameObject("Bobber").transform;
            bobberRenderer = bobber.gameObject.AddComponent<SpriteRenderer>();
            bobberRenderer.sprite = bobberSprite;
            bobberRenderer.sortingOrder = -4; // на воде: под лодкой и камнями, над кругами

            var icon = new GameObject("BiteIcon");
            icon.transform.SetParent(bobber, false);
            icon.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            biteIcon = icon.AddComponent<SpriteRenderer>();
            biteIcon.sprite = biteSprite;
            biteIcon.sortingOrder = 230;
            if (database != null && database.unlitMaterial != null)
                biteIcon.sharedMaterial = database.unlitMaterial; // "!" видно и ночью в тумане

            var lineObject = new GameObject("FishingLine");
            line = lineObject.AddComponent<SpriteRenderer>();
            line.sprite = lineSprite;
            line.color = new Color(0.95f, 0.95f, 0.9f, 0.8f);
            line.sortingOrder = 6;

            HideTackle();
        }

        public void HideTackle()
        {
            if (bobber == null)
                return;
            bobber.gameObject.SetActive(false);
            line.gameObject.SetActive(false);
        }

        public void ShowBobber(Vector2 position)
        {
            bobber.gameObject.SetActive(true);
            line.gameObject.SetActive(true);
            MoveBobber(position);
        }

        public void MoveBobber(Vector2 position)
        {
            bobber.position = position;
            bobberRest = position;
        }

        // Поплавок покачивается на воде (сильнее при поклёвке).
        public void BobBobber(float amount)
        {
            float wobble = Mathf.Sin(Time.time * (IsBiting ? 22f : 3f)) * amount;
            bobber.position = bobberRest + new Vector2(0f, wobble * 0.5f);
            biteIcon.enabled = IsBiting;
        }

        // Во время вываживания поплавок подтягивается к лодке по мере прогресса.
        public void PullBobberTowardsBoat(float progress)
        {
            bobberRest = Vector2.Lerp(landPoint, RodTip, progress * 0.85f);
        }

        private void UpdateLine()
        {
            if (!line.gameObject.activeSelf)
                return;

            Vector2 a = RodTip;
            Vector2 b = bobber.position;
            Vector2 d = b - a;
            line.transform.position = (a + b) * 0.5f;
            line.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            // fx_white 4x4 px = 0.125 клетки, растягиваем до длины лески и толщины ~1 px.
            line.transform.localScale = new Vector3(d.magnitude / 0.125f, 0.25f, 1f);
        }

        private void UpdateFacing()
        {
            var body = GetComponent<Rigidbody2D>();
            if (body == null)
                return;
            if (body.linearVelocity.x > 0.1f) facing = 1f;
            else if (body.linearVelocity.x < -0.1f) facing = -1f;
        }
    }
}
