using Omut.Fishing;
using Omut.Inventory;
using Omut.NPC;
using Omut.Player;
using Omut.UI;
using UnityEngine;

namespace Omut.Core
{
    // Прогресс текущей игры (паттерн Singleton): журнал, трюм, деньги, наживки, квесты...
    // Сами расчёты лежат в обычных C#-классах (Journal, InventoryGrid, Wallet...), а этот объект
    // их хранит, связывает с событиями GameEvents и отдаёт остальным скриптам.
    // Лежит в сцене на объекте Systems (ставит сборщик сцены).
    public partial class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [SerializeField] private GameDatabase database;

        public GameDatabase Database => database;
        public Journal Journal { get; } = new Journal();
        public InventoryGrid Hold { get; private set; }
        public Wallet Wallet { get; private set; } = new Wallet();
        public TackleBox Tackle { get; private set; } = new TackleBox();

        // Паника 0..100 (этап 18). Один объект на всю игру: на него подписаны эффекты.
        public PanicMeter Panic { get; } = new PanicMeter();

        // Запасы: керосин для фонаря (0..100 %) и термосы с чаем.
        public float Kerosene { get; set; }
        public int Thermos { get; set; }

        public const int StartMoney = 30;
        public const int MaxThermos = 3;

        // Лодка, на которой плаваем сейчас (меняется на верфи, этап 14).
        public BoatData CurrentBoat { get; private set; }

        // Сколько рыбы поймано за игру (для рекордов).
        public int TotalCaught { get; private set; }

        private void Awake()
        {
            Instance = this;
            ResetProgress();
            CatchCard.TryStore = StoreCatch;

            FishingController.CurrentBait = () => Tackle.Current;
            HookSaving();
            Merchant.AddMoreGoods = AddBaitGoods;

            hudLines.Add(() => $"Монеты: <b>{Wallet.Money}</b>");
            hudLines.Add(BoatHudLine);
            hudLines.Add(QuestHudLine);
            hudLines.Add(() => $"Наживка: <b>{BaitTitle(Tackle.Current)}</b>{(TackleBox.IsInfinite(Tackle.Current) ? "" : $" ({Tackle.Count(Tackle.Current)})")}  [Q]");
            foreach (System.Func<string> line in hudLines)
                Hud.AddLine(line);
        }

        private readonly System.Collections.Generic.List<System.Func<string>> hudLines = new System.Collections.Generic.List<System.Func<string>>();

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game != null && game.CurrentState == GameState.Sailing && Controls.Pressed(Controls.Bait))
            {
                Tackle.Next();
                Notifications.Show("На крючке: " + BaitTitle(Tackle.Current));
            }
        }

        public string BaitTitle(BaitType type)
        {
            BaitData data = database != null ? database.FindBait(type) : null;
            return data != null ? data.displayName : type.ToString();
        }

        // Наживки в лавке Тараса.
        private void AddBaitGoods(System.Collections.Generic.List<ShopGood> goods, GameSession session)
        {
            foreach (BaitData bait in database.baits)
            {
                BaitData b = bait;
                if (b == null || b.infinite)
                    continue;

                goods.Insert(0, new ShopGood
                {
                    Title = $"{b.displayName} x{b.packSize}",
                    Description = b.description,
                    Price = b.packPrice,
                    Icon = b.sprite,
                    Owned = () => $"(у тебя {Tackle.Count(b.type)})",
                    OnBuy = () => Tackle.Add(b.type, b.packSize)
                });
            }
        }

        private void OnBaitUsed(BaitType bait)
        {
            if (Tackle.Use(bait))
                Notifications.Show($"{BaitTitle(bait)} кончилась. На крючке червь. Купи ещё у Тараса.", new Color(1f, 0.8f, 0.5f));
        }

        private void OnDestroy()
        {
            foreach (System.Func<string> line in hudLines)
                Hud.RemoveLine(line);
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.NewGameStarted += ResetProgress;
            GameEvents.FishCaught += OnFishCaught;
            GameEvents.BaitUsed += OnBaitUsed;
            GameEvents.ContinueRequested += OnContinue;
            GameEvents.NewDayStarted += OnNewDayAutoSave;
            GameEvents.DockChanged += OnDockAutoSave;
        }

        private void OnDisable()
        {
            GameEvents.NewGameStarted -= ResetProgress;
            GameEvents.FishCaught -= OnFishCaught;
            GameEvents.BaitUsed -= OnBaitUsed;
            GameEvents.ContinueRequested -= OnContinue;
            GameEvents.NewDayStarted -= OnNewDayAutoSave;
            GameEvents.DockChanged -= OnDockAutoSave;
        }

        // Новая игра: всё с нуля.
        public void ResetProgress()
        {
            Journal.Clear();
            CurrentBoat = database != null && database.boats.Count > 0 ? database.boats[0] : null;
            Hold = new InventoryGrid(CurrentBoat != null ? CurrentBoat.holdWidth : 4, CurrentBoat != null ? CurrentBoat.holdHeight : 5);
            Wallet.Set(StartMoney, 0);
            Kerosene = 100f;
            Thermos = 1;
            Tackle = new TackleBox();
            Tackle.Add(BaitType.Spoon, 3);
            TotalCaught = 0;
            ResetQuests();
            ResetBoats();
            Panic.Reset();
            BoatChanged?.Invoke(CurrentBoat);
        }

        // Пересесть на другую лодку: другой размер трюма. Что не влезло, остаётся на берегу (продаётся Тарасу автоматически).
        public void SetBoat(BoatData boat)
        {
            CurrentBoat = boat;
            AddOwnedBoat(boat);
            Boat.SetMax(boat.durability, true);
            System.Collections.Generic.List<GridItem> overflow = Hold.Resize(boat.holdWidth, boat.holdHeight);
            foreach (GridItem item in overflow)
                Wallet.Earn(item.catchInfo.price);
            if (overflow.Count > 0)
                Notifications.Show($"Не влезло в новый трюм: {overflow.Count} шт., Тарас купил их сразу.");
            BoatChanged?.Invoke(boat);
        }

        public static event System.Action<BoatData> BoatChanged;

        // ---------- запасы (магазин Тараса) ----------

        public string SupplyText(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Kerosene: return $"(в баке {Mathf.RoundToInt(Kerosene)}%)";
                case ItemKind.Thermos: return $"(у тебя {Thermos} из {MaxThermos})";
                default: return "";
            }
        }

        public string CannotBuySupply(ItemData item)
        {
            if (item.kind == ItemKind.Kerosene && Kerosene >= 100f)
                return "Бак фонаря и так полный.";
            if (item.kind == ItemKind.Thermos && Thermos >= MaxThermos)
                return "Больше термосов не унести.";
            return null;
        }

        public void AddSupply(ItemData item)
        {
            if (item.kind == ItemKind.Kerosene)
                Kerosene = Mathf.Min(100f, Kerosene + item.amount);
            else if (item.kind == ItemKind.Thermos)
                Thermos = Mathf.Min(MaxThermos, Thermos + item.amount);
        }

        private void OnFishCaught(CatchInfo info)
        {
            TotalCaught++;
            Quests.OnFishCaught(info);
            info.isNewRecord = Journal.Register(info);
            if (info.isNewRecord)
                Notifications.Show($"Новая запись в журнале: {info.Title} ({Journal.CaughtCount} из {Journal.TotalEntries})", UiFactory.Theme.Gold);
        }

        // Положить улов в трюм. null = положили, иначе текст для игрока.
        private string StoreCatch(CatchInfo info)
        {
            if (Hold.TryAdd(info) != null)
            {
                Notifications.Show($"{info.Title} в трюме. Улов стоит {Hold.TotalPrice} мон.");
                return null;
            }

            return "Трюм полон! Отпусти рыбу или выброси что-нибудь из трюма (Tab).";
        }
    }
}
