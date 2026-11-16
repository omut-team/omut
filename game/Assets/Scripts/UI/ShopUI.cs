using System.Collections.Generic;
using Omut.Core;
using Omut.Inventory;
using Omut.NPC;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Магазин Тараса: слева продать улов из трюма, справа купить товары.
    public class ShopUI : GameWindow
    {
        public static ShopUI Instance { get; private set; }

        private Merchant merchant;
        private Text title;
        private Text money;
        private RectTransform sellList;
        private RectTransform buyList;
        private Text sellTotal;

        protected override GameState WindowState => GameState.Shop;

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
        }

        protected override RectTransform Build(Transform canvas)
        {
            RectTransform paper = UiFactory.Window(canvas, "Shop", new Vector2(1500f, 880f), "Лавка Тараса", out title);

            money = UiFactory.Label(paper, "", 32, TextAnchor.UpperRight, UiFactory.Theme.Accent);
            money.fontStyle = FontStyle.Bold;
            UiFactory.Place(money.rectTransform, new Vector2(1f, 1f), new Vector2(420f, 50f), new Vector2(-40f, -24f));

            // Левая колонка: продать.
            Text sellTitle = UiFactory.Label(paper, "Продать улов", 32, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            UiFactory.Place(sellTitle.rectTransform, new Vector2(0f, 1f), new Vector2(680f, 50f), new Vector2(50f, -90f));

            RectTransform sellArea = UiFactory.Rect(paper, "SellArea");
            UiFactory.Place(sellArea, new Vector2(0f, 1f), new Vector2(680f, 560f), new Vector2(50f, -140f));
            sellList = UiFactory.ScrollArea(sellArea);
            var grid = sellList.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(330f, 60f);
            grid.spacing = new Vector2(10f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            sellTotal = UiFactory.Label(paper, "", 26, TextAnchor.UpperLeft, UiFactory.Theme.InkLight);
            UiFactory.Place(sellTotal.rectTransform, new Vector2(0f, 0f), new Vector2(680f, 40f), new Vector2(50f, 110f));

            Button sellAll = UiFactory.Button(paper, "Продать всё", SellAll, new Vector2(330f, 64f));
            UiFactory.Place((RectTransform)sellAll.transform, new Vector2(0f, 0f), new Vector2(330f, 64f), new Vector2(50f, 30f));

            // Правая колонка: купить.
            Text buyTitle = UiFactory.Label(paper, "Купить", 32, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            UiFactory.Place(buyTitle.rectTransform, new Vector2(1f, 1f), new Vector2(680f, 50f), new Vector2(-50f, -90f));

            RectTransform buyArea = UiFactory.Rect(paper, "BuyArea");
            UiFactory.Place(buyArea, new Vector2(1f, 1f), new Vector2(680f, 600f), new Vector2(-50f, -140f));
            buyList = UiFactory.ScrollArea(buyArea);
            VerticalLayoutGroup column = UiFactory.Column(buyList, 10f, TextAnchor.UpperLeft);
            column.childControlHeight = true;

            Button close = UiFactory.Button(paper, "Уйти (Esc)", Close, new Vector2(300f, 64f));
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 0f), new Vector2(300f, 64f), new Vector2(-50f, 30f));

            return (RectTransform)paper.parent;
        }

        public void OpenFor(Merchant who)
        {
            merchant = who;
            Open();
        }

        protected override void Refresh()
        {
            GameSession session = GameSession.Instance;
            if (session == null)
                return;

            title.text = merchant != null ? "Лавка: " + merchant.NpcName : "Лавка";
            money.text = $"Монеты: {session.Wallet.Money}";

            Clear(sellList);
            int total = 0;
            foreach (GridItem item in session.Hold.Items)
            {
                if (!CanSell(item))
                    continue;

                GridItem captured = item;
                total += item.catchInfo.price;
                string night = item.catchInfo.caughtAtNight ? " ночн." : "";
                UiFactory.Button(sellList, $"{item.catchInfo.Title} {item.catchInfo.weight:0.0} кг{night}: {item.catchInfo.price}",
                    () => Sell(captured), new Vector2(330f, 60f), 22);
            }

            sellTotal.text = total > 0 ? $"За весь улов Тарас даст {total} мон." : "Продавать нечего. Металлолом носи Михалычу.";

            Clear(buyList);
            if (merchant != null)
            {
                foreach (ShopGood good in merchant.Catalog())
                    AddGood(good);
            }
        }

        private static bool CanSell(GridItem item) => item.catchInfo.fish != null && item.catchInfo.fishId != "scrap";

        private void AddGood(ShopGood good)
        {
            Image row = UiFactory.Panel(buyList, good.Title, new Color(1f, 1f, 1f, 0.35f));
            row.rectTransform.sizeDelta = new Vector2(680f, 84f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 84f;

            Image icon = UiFactory.Icon(row.transform, good.Icon, new Vector2(64f, 64f));
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(64f, 64f), new Vector2(10f, 0f));

            Text label = UiFactory.Label(row.transform, $"<b>{good.Title}</b>  {good.Price} мон.\n<size=20>{good.Description} {good.Owned()}</size>",
                24, TextAnchor.MiddleLeft, UiFactory.Theme.Ink);
            label.supportRichText = true;
            UiFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(440f, 80f), new Vector2(84f, 0f));

            string reason = good.CannotBuyReason();
            bool affordable = GameSession.Instance.Wallet.CanAfford(good.Price);
            Button buy = UiFactory.Button(row.transform, "Купить", () => Buy(good), new Vector2(130f, 56f), 24);
            UiFactory.Place((RectTransform)buy.transform, new Vector2(1f, 0.5f), new Vector2(130f, 56f), new Vector2(-10f, 0f));
            buy.interactable = reason == null && affordable;
        }

        private void Buy(ShopGood good)
        {
            Wallet wallet = GameSession.Instance.Wallet;
            string reason = good.CannotBuyReason();
            if (reason != null)
            {
                Notifications.Show(reason);
                return;
            }

            if (!wallet.TrySpend(good.Price))
            {
                Notifications.Show("Не хватает денег.");
                return;
            }

            good.OnBuy?.Invoke();
            GameEvents.RaisePlaySound("coins");
            Notifications.Show($"Куплено: {good.Title} (-{good.Price} мон.)");
            Refresh();
        }

        private void Sell(GridItem item)
        {
            GameSession session = GameSession.Instance;
            session.Hold.Remove(item);
            session.Wallet.Earn(item.catchInfo.price);
            GameEvents.RaiseFishSold(item.catchInfo);
            GameEvents.RaisePlaySound("coins");
            Notifications.Show($"Продано: {item.catchInfo.Title} (+{item.catchInfo.price} мон.)");
            Refresh();
        }

        private void SellAll()
        {
            GameSession session = GameSession.Instance;
            var sold = new List<GridItem>(session.Hold.Items).FindAll(CanSell);
            if (sold.Count == 0)
                return;

            int sum = 0;
            foreach (GridItem item in sold)
            {
                session.Hold.Remove(item);
                sum += item.catchInfo.price;
                GameEvents.RaiseFishSold(item.catchInfo);
            }

            session.Wallet.Earn(sum);
            GameEvents.RaisePlaySound("coins");
            Notifications.Show($"Тарас забрал весь улов: +{sum} мон.", UiFactory.Theme.Gold);
            Refresh();
        }

        private static void Clear(Transform parent) => UiFactory.ClearChildren(parent);
    }
}
