using System;
using System.Collections.Generic;
using Omut.Core;
using Omut.Inventory;
using Omut.UI;

namespace Omut.NPC
{
    // Тарас, смотритель маяка: скупает рыбу, продаёт наживку, керосин и термос.
    // В разговоре у него пункт "Торговать".
    public class Merchant : Npc
    {
        public string NpcName => DisplayName;

        public override List<(string label, Action action)> ExtraOptions()
        {
            return new List<(string, Action)> { ("Торговать", () => ShopUI.Instance?.OpenFor(this)) };
        }

        // Список товаров: строится из базы данных игры.
        public List<ShopGood> Catalog()
        {
            var goods = new List<ShopGood>();
            GameSession session = GameSession.Instance;
            if (session == null)
                return goods;

            foreach (ItemData item in session.Database.items)
            {
                ItemData data = item;
                if (data == null)
                    continue;

                goods.Add(new ShopGood
                {
                    Title = data.displayName,
                    Description = data.description,
                    Price = data.price,
                    Icon = data.sprite,
                    Owned = () => session.SupplyText(data.kind),
                    CannotBuyReason = () => session.CannotBuySupply(data),
                    OnBuy = () => session.AddSupply(data)
                });
            }

            AddMoreGoods(goods, session);
            return goods;
        }

        // Наживки добавляет GameSession (этап 11).
        public static Action<List<ShopGood>, GameSession> AddMoreGoods = (goods, session) => { };
    }
}
