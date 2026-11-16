using System;
using UnityEngine;

namespace Omut.NPC
{
    // Один товар в магазине: название, цена, что происходит при покупке.
    public class ShopGood
    {
        public string Title;
        public string Description;
        public int Price;
        public Sprite Icon;
        public Func<string> Owned = () => "";        // "у тебя: 5 шт."
        public Func<string> CannotBuyReason = () => null; // null = можно купить
        public Action OnBuy;
    }
}
