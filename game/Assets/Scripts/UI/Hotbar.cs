using Omut.Core;
using Omut.Fishing;
using Omut.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Omut.UI
{
    // Панель предметов внизу экрана, как в Minecraft.
    // 1-3: удочка с червём, блесной или фосфорной блесной (что на крючке), 4: фонарь в руке.
    // 5: выпить чай, 6: открыть карту (эти два сразу срабатывают, в руку не берутся).
    // Колесо мыши листает то, что можно держать в руке (1-4).
    public class Hotbar : MonoBehaviour
    {
        public static Hotbar Instance { get; private set; }

        public const int RodSlots = 3;
        public const int LanternSlot = 3;
        public const int ThermosSlot = 4;
        public const int MapSlot = 5;
        public const int SlotCount = 6;

        private static readonly BaitType[] RodBaits = { BaitType.Worm, BaitType.Spoon, BaitType.Phosphor };
        private static readonly string[] Names = { "Удочка: червь", "Удочка: блесна", "Удочка: фосфорная блесна", "Фонарь", "Термос с чаем", "Карта" };
        private static readonly Key[] Keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };

        [SerializeField] private Sprite rodSprite;
        [SerializeField] private Sprite lanternSprite;
        [SerializeField] private Sprite thermosSprite;
        [SerializeField] private Sprite mapSprite;
        [SerializeField] private Sprite[] baitSprites = new Sprite[RodSlots];

        private RectTransform bar;
        private readonly Image[] frames = new Image[SlotCount];
        private readonly Text[] counts = new Text[SlotCount];
        private Text caption;
        private float captionTimer;

        // Что выбрано в руке: 0-2 удочка (с наживкой), 3 фонарь.
        public int Selected { get; private set; }

        public static bool HoldsLantern => Instance != null && Instance.Selected == LanternSlot;

        // Картинка предмета в руке Сымона (удочка или фонарь).
        public static Sprite HeldSprite => Instance == null ? null : HoldsLantern ? Instance.lanternSprite : Instance.rodSprite;

        public void SetSprites(Sprite rod, Sprite lantern, Sprite thermos, Sprite map, Sprite[] baits)
        {
            rodSprite = rod;
            lanternSprite = lantern;
            thermosSprite = thermos;
            mapSprite = map;
            baitSprites = baits;
        }

        private void Awake()
        {
            Instance = this;
            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Build()
        {
            Canvas canvas = UiFactory.CreateCanvas("HotbarCanvas", 21, transform);
            const float size = 92f, gap = 8f;
            float width = SlotCount * size + (SlotCount - 1) * gap;

            bar = UiFactory.Rect(canvas.transform, "Hotbar");
            UiFactory.Place(bar, new Vector2(0.5f, 0f), new Vector2(width, size), new Vector2(0f, 16f));

            Sprite[] icons = { rodSprite, rodSprite, rodSprite, lanternSprite, thermosSprite, mapSprite };
            for (int i = 0; i < SlotCount; i++)
            {
                Image frame = UiFactory.SlotPanel(bar, "Slot" + (i + 1));
                UiFactory.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(size, size), new Vector2(i * (size + gap), 0f));
                frames[i] = frame;

                int index = i;
                frame.raycastTarget = true;
                frame.gameObject.AddComponent<Button>().onClick.AddListener(() => Use(index));

                Image icon = UiFactory.Icon(frame.transform, icons[i], new Vector2(60f, 60f));
                UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(60f, 60f), Vector2.zero);
                icon.raycastTarget = false;

                if (i < RodSlots && baitSprites != null && i < baitSprites.Length && baitSprites[i] != null)
                {
                    Image bait = UiFactory.Icon(frame.transform, baitSprites[i], new Vector2(34f, 34f));
                    UiFactory.Place(bait.rectTransform, new Vector2(1f, 1f), new Vector2(34f, 34f), new Vector2(-6f, -6f));
                    bait.raycastTarget = false;
                }

                Text number = UiFactory.HudLabel(frame.transform, (i + 1).ToString(), 20, TextAnchor.UpperLeft);
                UiFactory.Place(number.rectTransform, new Vector2(0f, 1f), new Vector2(30f, 26f), new Vector2(9f, -5f));
                number.raycastTarget = false;

                counts[i] = UiFactory.HudLabel(frame.transform, "", 20, TextAnchor.LowerRight);
                UiFactory.Place(counts[i].rectTransform, new Vector2(1f, 0f), new Vector2(80f, 26f), new Vector2(-8f, 5f));
                counts[i].raycastTarget = false;
            }

            caption = UiFactory.HudLabel(canvas.transform, "", 26, TextAnchor.MiddleCenter);
            UiFactory.Place(caption.rectTransform, new Vector2(0.5f, 0f), new Vector2(700f, 36f), new Vector2(0f, 16f + size + 6f));
            caption.color = UiFactory.Theme.Gold;
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            GameSession session = GameSession.Instance;
            GameState state = game != null ? game.CurrentState : GameState.Sailing;
            // Начал забрасывать с фонарём в руке: сам берёт удочку.
            if (session != null && state == GameState.Fishing && HoldsLantern)
                Selected = RodIndex(session.Tackle.Current);

            // Во время рыбалки панель прячется: внизу шкалы заброса и натяжения.
            bool visible = session != null && state == GameState.Sailing;
            bar.gameObject.SetActive(visible);
            caption.gameObject.SetActive(visible);
            if (!visible)
                return;

            FollowTackle(session.Tackle);

            if (state == GameState.Sailing && GameWindow.Active == null)
            {
                for (int i = 0; i < SlotCount; i++)
                    if (Controls.Pressed(Keys[i]))
                        Use(i);

                float wheel = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
                if (wheel > 0.01f) Scroll(-1);
                else if (wheel < -0.01f) Scroll(1);
            }

            UpdateView(session);
        }

        // Наживку сменили клавишей Q или она кончилась: удочка в руке показывает то, что на крючке.
        private void FollowTackle(TackleBox tackle)
        {
            if (Selected < RodSlots && RodBaits[Selected] != tackle.Current)
                Selected = RodIndex(tackle.Current);
        }

        private static int RodIndex(BaitType bait) => Mathf.Max(0, System.Array.IndexOf(RodBaits, bait));

        private void Scroll(int step)
        {
            GameSession session = GameSession.Instance;
            int next = Selected;
            for (int k = 0; k <= LanternSlot; k++)
            {
                next = (next + step + LanternSlot + 1) % (LanternSlot + 1);
                if (next == LanternSlot || session.Tackle.Has(RodBaits[next]))
                    break;
            }

            Use(next);
        }

        // Нажали цифру или кликнули по ячейке.
        public void Use(int slot)
        {
            GameSession session = GameSession.Instance;
            if (session == null)
                return;

            if (slot < RodSlots)
            {
                if (!session.Tackle.Select(RodBaits[slot]))
                {
                    Notifications.Show("Нет такой наживки. Купи у Тараса в лавке.");
                    return;
                }

                Selected = slot;
                ShowCaption(Names[slot]);
            }
            else if (slot == LanternSlot)
            {
                bool wasHolding = HoldsLantern;
                Selected = LanternSlot;
                ShowCaption(Names[slot]);
                // В лодке: взял фонарь, он и зажёгся. Нажал ещё раз: погасил.
                if (!PlayerWalker.OnFoot && Lantern.Instance != null && (wasHolding || !Lantern.Instance.IsOn))
                    Lantern.Instance.Toggle();
            }
            else if (slot == ThermosSlot)
            {
                PanicEffects.DrinkTea(session);
            }
            else if (slot == MapSlot)
            {
                var map = FindAnyObjectByType<MapUI>();
                if (map != null)
                    map.Toggle();
            }
        }

        private void ShowCaption(string text)
        {
            caption.text = text;
            captionTimer = 2f;
        }

        private void UpdateView(GameSession session)
        {
            UiSkin skin = UiSkin.Current;
            for (int i = 0; i < SlotCount; i++)
            {
                bool selected = i == Selected;
                if (skin != null && skin.slot != null)
                    frames[i].sprite = selected && skin.slotSelected != null ? skin.slotSelected : skin.slot;
                frames[i].color = selected ? Color.white : new Color(1f, 1f, 1f, 0.85f);
                frames[i].rectTransform.localScale = Vector3.one * (selected ? 1.08f : 1f);
            }

            for (int i = 0; i < RodSlots; i++)
            {
                BaitType bait = RodBaits[i];
                counts[i].text = TackleBox.IsInfinite(bait) ? "" : session.Tackle.Count(bait).ToString();
                counts[i].color = session.Tackle.Has(bait) ? Color.white : UiFactory.Theme.Bad;
            }

            counts[LanternSlot].text = Mathf.RoundToInt(session.Kerosene) + "%";
            counts[ThermosSlot].text = session.Thermos.ToString();
            counts[ThermosSlot].color = session.Thermos > 0 ? Color.white : UiFactory.Theme.Bad;
            counts[MapSlot].text = "";

            captionTimer -= Time.deltaTime;
            caption.color = new Color(caption.color.r, caption.color.g, caption.color.b, Mathf.Clamp01(captionTimer));
        }
    }
}
