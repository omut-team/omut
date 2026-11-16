using Omut.Core;
using Omut.Inventory;
using Omut.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Мастерская Михалыча: ремонт лодки и покупка катеров.
    // Ремонт 1 удара: 25 монет и 1 металлолом из трюма, без железа 45 монет.
    public class ShipyardUI : GameWindow
    {
        public const int RepairWithScrap = 25;
        public const int RepairWithoutScrap = 45;

        public static ShipyardUI Instance { get; private set; }

        private Text status;
        private Button repairButton;
        private RectTransform boatsList;
        private Text money;

        protected override GameState WindowState => GameState.Shop;

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
        }

        protected override RectTransform Build(Transform canvas)
        {
            RectTransform paper = UiFactory.Window(canvas, "Shipyard", new Vector2(1500f, 860f), "Мастерская Михалыча", out _);

            money = UiFactory.Label(paper, "", 32, TextAnchor.UpperRight, UiFactory.Theme.Accent);
            money.fontStyle = FontStyle.Bold;
            UiFactory.Place(money.rectTransform, new Vector2(1f, 1f), new Vector2(420f, 50f), new Vector2(-40f, -24f));

            status = UiFactory.Label(paper, "", 30, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            status.supportRichText = true;
            UiFactory.Place(status.rectTransform, new Vector2(0f, 1f), new Vector2(1400f, 130f), new Vector2(50f, -100f));

            repairButton = UiFactory.Button(paper, "Починить", Repair, new Vector2(420f, 64f));
            UiFactory.Place((RectTransform)repairButton.transform, new Vector2(0f, 1f), new Vector2(420f, 64f), new Vector2(50f, -240f));

            Text boatsTitle = UiFactory.Label(paper, "Лодки", 32, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            UiFactory.Place(boatsTitle.rectTransform, new Vector2(0f, 1f), new Vector2(600f, 50f), new Vector2(50f, -330f));

            boatsList = UiFactory.Rect(paper, "Boats");
            UiFactory.Place(boatsList, new Vector2(0f, 1f), new Vector2(1400f, 380f), new Vector2(50f, -380f));
            UiFactory.Column(boatsList, 12f, TextAnchor.UpperLeft);

            Button close = UiFactory.Button(paper, "Уйти (Esc)", Close, new Vector2(300f, 64f));
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 0f), new Vector2(300f, 64f), new Vector2(-50f, 30f));

            return (RectTransform)paper.parent;
        }

        protected override void Refresh()
        {
            GameSession session = GameSession.Instance;
            if (session == null)
                return;

            BoatStats stats = session.Boat;
            int scrap = session.Hold.CountOf("scrap");
            money.text = $"Монеты: {session.Wallet.Money}";

            string cost = stats.IsFull ? "Лодка целая, чинить нечего."
                : session.HasFlag("free_repair") ? "Михалыч должен тебе один бесплатный ремонт."
                : $"Ремонт 1 удара: {RepairWithScrap} мон. + 1 металлолом (в трюме {scrap}) или {RepairWithoutScrap} мон. без железа. Всего нужно: {RepairCost(session)} мон.";
            status.text = $"<b>{session.CurrentBoat.displayName}</b>   прочность {stats.Durability}/{stats.MaxDurability}\n{cost}";
            repairButton.interactable = !stats.IsFull && session.Wallet.CanAfford(session.HasFlag("free_repair") ? 0 : RepairCost(session));

            UiFactory.ClearChildren(boatsList);
            foreach (BoatData boat in session.Database.boats)
                AddBoatRow(session, boat);
        }

        // Сколько монет за полный ремонт (железо из трюма уменьшает цену).
        public static int RepairCost(GameSession session)
        {
            int missing = session.Boat.Missing;
            int scrap = Mathf.Min(missing, session.Hold.CountOf("scrap"));
            return scrap * RepairWithScrap + (missing - scrap) * RepairWithoutScrap;
        }

        private void Repair()
        {
            GameSession session = GameSession.Instance;
            if (session.Boat.IsFull)
                return;

            if (session.HasFlag("free_repair"))
            {
                session.ClearFlag("free_repair");
                session.Boat.Repair(session.Boat.Missing);
                Notifications.Show("Михалыч починил лодку бесплатно, как обещал.", UiFactory.Theme.Gold);
                Refresh();
                return;
            }

            int missing = session.Boat.Missing;
            int cost = RepairCost(session);
            if (!session.Wallet.TrySpend(cost))
            {
                Notifications.Show("Не хватает денег на ремонт.");
                return;
            }

            int scrapUsed = 0;
            foreach (GridItem item in new System.Collections.Generic.List<GridItem>(session.Hold.Items))
            {
                if (scrapUsed < missing && item.catchInfo.fishId == "scrap")
                {
                    session.Hold.Remove(item);
                    scrapUsed++;
                }
            }

            session.Boat.Repair(missing);
            GameEvents.RaisePlaySound("repair");
            Notifications.Show($"Лодка как новая. -{cost} мон.{(scrapUsed > 0 ? $", железа: {scrapUsed}" : "")}");
            Refresh();
        }

        private void AddBoatRow(GameSession session, BoatData boat)
        {
            bool owned = session.OwnsBoat(boat);
            bool current = session.CurrentBoat == boat;

            Image row = UiFactory.Panel(boatsList, boat.displayName, current ? new Color(0.95f, 0.85f, 0.6f, 0.6f) : new Color(1f, 1f, 1f, 0.35f));
            row.rectTransform.sizeDelta = new Vector2(1400f, 110f);

            Image icon = UiFactory.Icon(row.transform, boat.sprite, new Vector2(160f, 80f));
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(160f, 80f), new Vector2(14f, 0f));

            string extra = boat.hasSonar ? ", эхолот, паника растёт медленнее" : "";
            Text text = UiFactory.Label(row.transform,
                $"<b>{boat.displayName}</b>   скорость {boat.maxSpeed:0.#}, трюм {boat.holdWidth}x{boat.holdHeight}, прочность {boat.durability}, свет {boat.lightRadius:0.#} кл.{extra}\n" +
                (owned ? (current ? "Ты на ней." : "Твоя.") : $"Цена: {boat.price} мон. + {boat.scrapPrice} металлолома"),
                24, TextAnchor.MiddleLeft, UiFactory.Theme.Ink);
            text.supportRichText = true;
            UiFactory.Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(950f, 100f), new Vector2(190f, 0f));

            if (current)
                return;

            string label = owned ? "Пересесть" : "Купить";
            Button button = UiFactory.Button(row.transform, label, () => BuyOrSwitch(boat), new Vector2(220f, 60f), 26);
            UiFactory.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(220f, 60f), new Vector2(-16f, 0f));
            button.interactable = owned || (session.Wallet.CanAfford(boat.price) && session.Hold.CountOf("scrap") >= boat.scrapPrice);
        }

        private void BuyOrSwitch(BoatData boat)
        {
            GameSession session = GameSession.Instance;
            if (!session.OwnsBoat(boat))
            {
                if (session.Hold.CountOf("scrap") < boat.scrapPrice || !session.Wallet.TrySpend(boat.price))
                {
                    Notifications.Show("Не хватает денег или железа.");
                    return;
                }

                int scrapLeft = boat.scrapPrice;
                foreach (GridItem item in new System.Collections.Generic.List<GridItem>(session.Hold.Items))
                {
                    if (scrapLeft > 0 && item.catchInfo.fishId == "scrap")
                    {
                        session.Hold.Remove(item);
                        scrapLeft--;
                    }
                }

                session.AddOwnedBoat(boat);
                GameEvents.RaisePlaySound("quest");
                Notifications.Show($"Куплен катер «{boat.displayName}»!", UiFactory.Theme.Gold);
            }

            session.SetBoat(boat);
            Refresh();
        }
    }
}
