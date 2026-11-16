using System.Collections.Generic;
using Omut.Core;
using Omut.Inventory;
using Omut.UI;
using UnityEngine;

namespace Omut.NPC
{
    // Тарас, смотритель маяка: скупает рыбу, продаёт наживку, керосин и термос.
    public class Merchant : Interactable
    {
        [SerializeField] private string npcName = "Тарас";

        public string NpcName => npcName;

        public override string Prompt => "поговорить с " + (npcName == "Тарас" ? "Тарасом" : npcName);

        // Что открыть при разговоре. По умолчанию сразу магазин, диалоги подставят своё (этап 13).
        public static System.Action<Merchant> OnTalk = merchant => ShopUI.Instance?.OpenFor(merchant);

        public override void Interact() => OnTalk(this);

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

        // Наживки добавляются на этапе 11.
        public static System.Action<List<ShopGood>, GameSession> AddMoreGoods = (goods, session) => { };
    }
}
