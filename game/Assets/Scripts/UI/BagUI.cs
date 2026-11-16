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
            RectTransform paper = UiFactory.Window(canvas, "Bag", new Vector2(1240f, 760f), "Сумка", out _);

            cells = UiFactory.Rect(paper, "Cells");
            UiFactory.Place(cells, new Vector2(0f, 1f), new Vector2(640f, 520f), new Vector2(60f, -110f));
            var grid = cells.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150f, 150f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            info = UiFactory.Label(paper, "", 28, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            info.supportRichText = true;
            UiFactory.Place(info.rectTransform, new Vector2(1f, 1f), new Vector2(460f, 420f), new Vector2(-60f, -110f));

            Text help = UiFactory.Label(paper, "Наведи на вещь, чтобы прочитать. Клик по удочке или фонарю: взять в руку.\nРыба лежит в трюме (Tab).",
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

            int rodSlot = Mathf.Max(0, System.Array.IndexOf(new[] { BaitType.Worm, BaitType.Spoon, BaitType.Phosphor }, tackle.Current));
            Add("Удочка", "", "Старая удочка деда. Space: заброс, ЛКМ: тянуть.", rodSprite, rodSlot);
            Add("Червь", "∞", "Берут все мирные рыбы. Не кончается.", Bait(0), 0);
            Add("Блесна", tackle.Count(BaitType.Spoon).ToString(), "На хищника: окунь, щука, судак. Тарас продаёт по 5 штук.", Bait(1), 1);
            Add("Фосфорная блесна", tackle.Count(BaitType.Phosphor).ToString(), "Светится. Только ночью, на мутантов в тумане.", Bait(2), 2);
            Add("Фонарь", Mathf.RoundToInt(session.Kerosene) + "%", "F или 4: зажечь. Керосин горит, докупай у Тараса.", lanternSprite, Hotbar.LanternSlot);
            Add("Термос", session.Thermos + "/" + GameSession.MaxThermos, "C или 5: выпить чай, страх уходит.", thermosSprite, -1);
            Add("Карта", "", "M или 6: карта озера.", mapSprite, -1);
            Add("Металлолом", scrap.ToString(), "Лежит в трюме. Михалыч берёт его за ремонт и улучшения.", scrapSprite, -1);
            Add("Страницы дневника", session.DiaryPages + "/" + GameSession.DiaryPagesTotal, "Записи деда. Читать в журнале (J).", pageSprite, -1);
            Add("Монеты", session.Wallet.Money.ToString(), "Зарабатываются продажей рыбы Тарасу.", coinSprite, -1);
        }

        private Sprite Bait(int i) => baitSprites != null && i < baitSprites.Length ? baitSprites[i] : null;

        private void Add(string title, string count, string description, Sprite icon, int slot)
        {
            entries.Add(new Entry { title = title, count = count, description = description, icon = icon, hotbarSlot = slot });
        }

        private static string Describe(Entry e) =>
            $"<b>{e.title}</b>{(e.count.Length > 0 ? "  " + e.count : "")}\n\n{e.description}";

        private void AddCell(Entry entry)
        {
            Image frame = UiFactory.SlotPanel(cells, entry.title);
            frame.raycastTarget = true;

            Image icon = UiFactory.Icon(frame.transform, entry.icon, new Vector2(84f, 84f));
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(84f, 84f), new Vector2(0f, 10f));
            icon.raycastTarget = false;

            Text count = UiFactory.HudLabel(frame.transform, entry.count, 24, TextAnchor.LowerRight);
            UiFactory.Place(count.rectTransform, new Vector2(1f, 0f), new Vector2(130f, 32f), new Vector2(-12f, 8f));
            count.raycastTarget = false;

            Entry captured = entry;
            var button = frame.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                info.text = Describe(captured);
                if (captured.hotbarSlot >= 0 && Hotbar.Instance != null)
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
