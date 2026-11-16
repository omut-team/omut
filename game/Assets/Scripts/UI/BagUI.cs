using System.Collections.Generic;
using Omut.Core;
using Omut.Fishing;
using Omut.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Omut.UI
{
    // Сумка (B): все вещи Сымона в ячейках с описанием. Рыба лежит отдельно, в трюме (Tab).
    // Клик по удочке или фонарю: взять в руку (то же, что цифры 1-4 на панели предметов).
    public class BagUI : GameWindow
    {
        private struct Entry
        {
            public string title;
            public string count;
            public string description;
            public Sprite icon;
            public int hotbarSlot; // -1 = нельзя взять в руку
            public RodData rod;    // удочка: клик берёт её в руку
        }

        [SerializeField] private Sprite rodSprite;
        [SerializeField] private Sprite lanternSprite;
        [SerializeField] private Sprite thermosSprite;
        [SerializeField] private Sprite mapSprite;
        [SerializeField] private Sprite scrapSprite;
        [SerializeField] private Sprite pageSprite;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite[] baitSprites = new Sprite[3];

        private RectTransform cells;
        private Text info;
        private readonly List<Entry> entries = new List<Entry>();

        protected override Key ToggleKey => Controls.Bag;

        public void SetSprites(Sprite rod, Sprite lantern, Sprite thermos, Sprite map, Sprite scrap, Sprite page, Sprite coin, Sprite[] baits)
        {
            rodSprite = rod;
            lanternSprite = lantern;
            thermosSprite = thermos;
            mapSprite = map;
            scrapSprite = scrap;
            pageSprite = page;
            coinSprite = coin;
            baitSprites = baits;
        }

        protected override RectTransform Build(Transform canvas)
        {
            RectTransform paper = UiFactory.Window(canvas, "Bag", new Vector2(1500f, 780f), "Сумка", out _);

            cells = UiFactory.Rect(paper, "Cells");
            UiFactory.Place(cells, new Vector2(0f, 1f), new Vector2(880f, 560f), new Vector2(60f, -110f));
            var grid = cells.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(132f, 132f);
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;

            info = UiFactory.Label(paper, "", 28, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            info.supportRichText = true;
            UiFactory.Place(info.rectTransform, new Vector2(1f, 1f), new Vector2(440f, 480f), new Vector2(-60f, -110f));

            Text help = UiFactory.Label(paper, "Наведи на вещь, чтобы прочитать. Клик по удочке, наживке или фонарю: взять в руку.\nРыба лежит в трюме (Tab).",
                22, TextAnchor.LowerLeft, UiFactory.Theme.InkLight);
            UiFactory.Place(help.rectTransform, new Vector2(0f, 0f), new Vector2(1100f, 70f), new Vector2(60f, 34f));

            return (RectTransform)paper.parent;
        }

        protected override void Refresh()
        {
            GameSession session = GameSession.Instance;
            if (session == null)
                return;

            Collect(session);
            UiFactory.ClearChildren(cells);
            foreach (Entry entry in entries)
                AddCell(entry);
            info.text = entries.Count > 0 ? Describe(entries[0]) : "";
        }

        private void Collect(GameSession session)
        {
            entries.Clear();
            TackleBox tackle = session.Tackle;
            int scrap = 0;
            foreach (GridItem item in session.Hold.Items)
                if (item.catchInfo.fishId == "scrap")
                    scrap++;

            // Удочки: клик берёт в руку.
            foreach (RodData rod in session.OwnedRods())
            {
                bool current = session.CurrentRod == rod;
                Add(rod.displayName, current ? "в руке" : "", rod.description + "\n" + GameSession.RodStats(rod), rod.sprite, -1, rod);
            }

            // Наживки: клик ставит на крючок (как цифры 1-6).
            for (int i = 0; i < TackleBox.Order.Length; i++)
            {
                BaitType type = TackleBox.Order[i];
                BaitData data = session.Database.FindBait(type);
                if (data == null || (!TackleBox.IsInfinite(type) && tackle.Count(type) == 0 && type != BaitType.Spoon && type != BaitType.Phosphor))
                    continue;
                Add(data.displayName, TackleBox.IsInfinite(type) ? "∞" : tackle.Count(type).ToString(), data.description, data.sprite, i);
            }

            Add("Фонарь", Mathf.RoundToInt(session.Kerosene) + "%", "F или 7: зажечь. Керосин горит, докупай у Тараса.", lanternSprite, Hotbar.LanternSlot);
            Add("Термос", session.Thermos + "/" + GameSession.MaxThermos, "C или 8: выпить чай, страх уходит.", thermosSprite, -1);
            Add("Карта", "", "M или 9: карта озера.", mapSprite, -1);
            Add("Металлолом", scrap.ToString(), "Лежит в трюме. Михалыч берёт его за ремонт и улучшения.", scrapSprite, -1);
            Add("Страницы дневника", session.DiaryPages + "/" + GameSession.DiaryPagesTotal, "Записи деда. Читать в журнале (J).", pageSprite, -1);
            Add("Монеты", session.Wallet.Money.ToString(), "Зарабатываются продажей рыбы Тарасу.", coinSprite, -1);
        }

        private Sprite Bait(int i) => baitSprites != null && i < baitSprites.Length ? baitSprites[i] : null;

        private void Add(string title, string count, string description, Sprite icon, int slot, RodData rod = null)
        {
            entries.Add(new Entry { title = title, count = count, description = description, icon = icon, hotbarSlot = slot, rod = rod });
        }

        private static string Describe(Entry e) =>
            $"<b>{e.title}</b>{(e.count.Length > 0 ? "  " + e.count : "")}\n\n{e.description}";

        private void AddCell(Entry entry)
        {
            Image frame = UiFactory.SlotPanel(cells, entry.title);
            frame.raycastTarget = true;

            Image icon = UiFactory.Icon(frame.transform, entry.icon, new Vector2(80f, 80f));
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(80f, 80f), new Vector2(0f, 8f));
            icon.raycastTarget = false;

            Text count = UiFactory.HudLabel(frame.transform, entry.count, 24, TextAnchor.LowerRight);
            UiFactory.Place(count.rectTransform, new Vector2(1f, 0f), new Vector2(130f, 32f), new Vector2(-12f, 8f));
            count.raycastTarget = false;

            Entry captured = entry;
            var button = frame.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                info.text = Describe(captured);
                if (captured.rod != null && GameSession.Instance != null)
                {
                    GameSession.Instance.EquipRod(captured.rod);
                    Refresh();
                    info.text = Describe(captured) + "\n\n<color=#8a3d24>Теперь в руке.</color>";
                }
                else if (captured.hotbarSlot >= 0 && Hotbar.Instance != null)
                {
                    Hotbar.Instance.Use(captured.hotbarSlot);
                    Refresh();
                    info.text = Describe(captured) + "\n\n<color=#8a3d24>Теперь в руке.</color>";
                }
            });

            var hover = frame.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => info.text = Describe(captured));
            hover.triggers.Add(enter);
        }
    }
}
