using System.Collections.Generic;
using System.Text;
using Omut.Core;
using Omut.Fishing;
using Omut.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Omut.UI
{
    // Журнал рыбака (J): 14 карточек, счётчик "N из 14", описание выбранной рыбы.
    // Вкладка "Дневник" для страниц дневника деда (этап 20).
    public class JournalUI : GameWindow
    {
        // Текст вкладки "Дневник". Подставляет дневник (этап 20).
        public static System.Func<string> DiaryText = () => "Страниц дневника пока нет.\nОни лежат в сундучках на дне у рифа.";

        private const int Columns = 7;

        private readonly List<(Image icon, Text label, Image back)> cards = new List<(Image, Text, Image)>();
        private List<FishData> entries = new List<FishData>();
        private Text counter;
        private Text details;
        private Text diary;
        private RectTransform fishTab;
        private RectTransform diaryTab;
        private int selected;

        protected override Key ToggleKey => Controls.Journal;

        protected override RectTransform Build(Transform canvas)
        {
            RectTransform paper = UiFactory.Window(canvas, "Journal", new Vector2(1500f, 900f), "Журнал рыбака", out _);

            counter = UiFactory.Label(paper, "", 30, TextAnchor.UpperRight, UiFactory.Theme.Accent);
            UiFactory.Place(counter.rectTransform, new Vector2(1f, 1f), new Vector2(400f, 50f), new Vector2(-40f, -24f));

            RectTransform tabs = UiFactory.Rect(paper, "Tabs");
            UiFactory.Place(tabs, new Vector2(0f, 1f), new Vector2(560f, 56f), new Vector2(40f, -20f));
            UiFactory.Row(tabs, 10f, TextAnchor.MiddleLeft);
            UiFactory.Button(tabs, "Рыбы", () => ShowTab(false), new Vector2(200f, 50f), 26);
            UiFactory.Button(tabs, "Дневник деда", () => ShowTab(true), new Vector2(260f, 50f), 26);

            fishTab = UiFactory.Rect(paper, "FishTab");
            UiFactory.Stretch(fishTab);
            fishTab.offsetMin = new Vector2(40f, 40f);
            fishTab.offsetMax = new Vector2(-40f, -96f);

            RectTransform grid = UiFactory.Rect(fishTab, "Grid");
            UiFactory.Place(grid, new Vector2(0.5f, 1f), new Vector2(1400f, 470f), Vector2.zero);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(186f, 222f);
            layout.spacing = new Vector2(16f, 16f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Columns;
            layout.childAlignment = TextAnchor.UpperCenter;

            for (int i = 0; i < Journal.TotalEntries; i++)
            {
                int index = i;
                Image back = UiFactory.Panel(grid, "Card " + (i + 1), UiFactory.Theme.PaperDark);
                var button = back.gameObject.AddComponent<Button>();
                button.onClick.AddListener(() => Select(index));

                Image icon = UiFactory.Icon(back.transform, null, new Vector2(160f, 110f));
                UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(160f, 110f), new Vector2(0f, -12f));

                Text label = UiFactory.Label(back.transform, "", 22, TextAnchor.UpperCenter, UiFactory.Theme.Ink);
                UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(176f, 92f), new Vector2(0f, 6f));
                cards.Add((icon, label, back));
            }

            Image detailsBack = UiFactory.Panel(fishTab, "Details", new Color(1f, 1f, 1f, 0.35f));
            UiFactory.Place(detailsBack.rectTransform, new Vector2(0.5f, 0f), new Vector2(1400f, 250f), Vector2.zero);
            details = UiFactory.Label(detailsBack.transform, "", 27, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            UiFactory.Stretch(details.rectTransform, 20f);
            details.supportRichText = true;

            diaryTab = UiFactory.Rect(paper, "DiaryTab");
            UiFactory.Stretch(diaryTab);
            diaryTab.offsetMin = new Vector2(80f, 60f);
            diaryTab.offsetMax = new Vector2(-80f, -110f);
            diary = UiFactory.Label(diaryTab, "", 28, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            UiFactory.Stretch(diary.rectTransform);
            diary.supportRichText = true;
            diaryTab.gameObject.SetActive(false);

            return (RectTransform)paper.parent;
        }

        protected override void Refresh()
        {
            GameSession session = GameSession.Instance;
            if (session == null)
                return;

            entries = session.Database.JournalEntries();
            Journal journal = session.Journal;
            counter.text = $"Поймано: {journal.CaughtCount} из {Journal.TotalEntries}";

            for (int i = 0; i < cards.Count; i++)
            {
                (Image icon, Text label, Image back) = cards[i];
                FishData fish = i < entries.Count ? entries[i] : null;
                JournalEntry entry = fish != null ? journal.Get(fish.id) : null;

                icon.sprite = fish != null ? fish.sprite : null;
                icon.color = fish == null || fish.sprite == null ? Color.clear : (entry != null ? Color.white : new Color(0f, 0f, 0f, 0.55f));
                label.text = fish == null ? "" : entry != null
                    ? $"{fish.journalNumber}. {fish.displayName}\nрекорд {entry.bestWeight:0.0} кг"
                    : $"{fish.journalNumber}. ???";
                back.color = i == selected ? new Color(0.95f, 0.85f, 0.6f) : UiFactory.Theme.PaperDark;
            }

            ShowDetails();
            diary.text = DiaryText();
        }

        private void Select(int index)
        {
            selected = index;
            Refresh();
        }

        private void ShowTab(bool showDiary)
        {
            fishTab.gameObject.SetActive(!showDiary);
            diaryTab.gameObject.SetActive(showDiary);
            Refresh();
        }

        private void ShowDetails()
        {
            if (selected >= entries.Count)
            {
                details.text = "";
                return;
            }

            FishData fish = entries[selected];
            JournalEntry entry = GameSession.Instance.Journal.Get(fish.id);
            var sb = new StringBuilder();

            if (entry != null)
            {
                sb.Append($"<b>{fish.journalNumber}. {fish.displayName}</b>   ({FishData.RarityName(fish.rarity)})\n");
                sb.Append(fish.description).Append('\n');
                sb.Append($"Поймано: {entry.count} шт., рекорд {entry.bestWeight:0.00} кг, впервые в день {entry.firstDay}.\n");
            }
            else
            {
                sb.Append($"<b>{fish.journalNumber}. ???</b>   ({FishData.RarityName(fish.rarity)})\n");
                sb.Append("Ещё не поймана.\n");
            }

            sb.Append($"Где: {ZoneNames(fish.zones)}.  Когда: {TimeNames(fish.time)}.  Наживка: {BaitName(fish.bait)}.");
            if (fish.minCastDistance > 0f)
                sb.Append($"  Только дальний заброс (от {fish.minCastDistance:0} клеток).");
            sb.Append($"\nВес {fish.minWeight:0.#}–{fish.maxWeight:0.#} кг.  Поведение: {FishBehaviours.Create(fish.behaviour).Name}.");

            details.text = sb.ToString();
        }

        public static string ZoneNames(ZoneMask zones)
        {
            if (zones == ZoneMask.All)
                return "везде";

            var list = new List<string>();
            if ((zones & ZoneMask.Dock) != 0) list.Add("у причала");
            if ((zones & ZoneMask.Bay) != 0) list.Add("залив");
            if ((zones & ZoneMask.Pool) != 0) list.Add("омут");
            if ((zones & ZoneMask.Reef) != 0) list.Add("риф");
            return string.Join(", ", list);
        }

        public static string TimeNames(FishTime time)
        {
            if (time == FishTime.Always)
                return "всегда";

            var list = new List<string>();
            if ((time & FishTime.Day) != 0) list.Add("днём");
            if ((time & FishTime.Dusk) != 0) list.Add("в сумерки");
            if ((time & FishTime.Night) != 0) list.Add("ночью");
            return string.Join(" и ", list);
        }

        public static string BaitName(BaitType bait)
        {
            switch (bait)
            {
                case BaitType.Worm: return "червь";
                case BaitType.Spoon: return "блесна";
                case BaitType.Phosphor: return "фосфорная блесна";
                default: return "любая";
            }
        }
    }
}
