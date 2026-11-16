using System.Collections.Generic;
using Omut.Fishing;
using Omut.NPC;
using Omut.UI;

namespace Omut.Core
{
    // Удочки Лизы: какие куплены и какая в руке. Стартовая бамбуковая деда, остальные у Тараса,
    // удочку Хозяина дарит дед Язэп после финала.
    public partial class GameSession
    {
        // Мир по карте Ульяны (для старых сохранений, см. Apply).
        public const int MapVersion = 2;

        private readonly HashSet<string> ownedRods = new HashSet<string>();

        public RodData CurrentRod { get; private set; }

        public bool OwnsRod(string id) => ownedRods.Contains(id);

        private void ResetRods()
        {
            ownedRods.Clear();
            ownedRods.Add("bamboo");
            CurrentRod = database != null ? database.FindRod("bamboo") : null;
        }

        public List<RodData> OwnedRods()
        {
            var list = new List<RodData>();
            if (database == null)
                return list;
            foreach (RodData rod in database.rods)
                if (rod != null && ownedRods.Contains(rod.id))
                    list.Add(rod);
            return list;
        }

        public void GiveRod(RodData rod, bool equip = true)
        {
            if (rod == null)
                return;
            ownedRods.Add(rod.id);
            if (equip)
                EquipRod(rod);
        }

        public bool EquipRod(RodData rod)
        {
            if (rod == null || !ownedRods.Contains(rod.id))
                return false;
            CurrentRod = rod;
            Notifications.Show("В руке: " + rod.displayName);
            return true;
        }

        public static string RodStats(RodData rod)
        {
            var parts = new List<string>();
            if (rod.castBonus > 0f) parts.Add($"заброс +{rod.castBonus:0.#}");
            if (rod.biteSpeed < 1f) parts.Add($"клюёт быстрее на {UnityEngine.Mathf.RoundToInt((1f - rod.biteSpeed) * 100f)}%");
            if (rod.pull < 1f) parts.Add($"тянуть легче на {UnityEngine.Mathf.RoundToInt((1f - rod.pull) * 100f)}%");
            return parts.Count > 0 ? string.Join(", ", parts) : "обычная";
        }

        // Удочки в лавке Тараса.
        private void AddRodGoods(List<ShopGood> goods)
        {
            if (database == null)
                return;
            foreach (RodData rod in database.rods)
            {
                RodData r = rod;
                if (r == null || !r.inShop)
                    continue;
                goods.Add(new ShopGood
                {
                    Title = r.displayName,
                    Description = r.description + " (" + RodStats(r) + ")",
                    Price = r.price,
                    Icon = r.sprite,
                    Owned = () => OwnsRod(r.id) ? "(уже есть)" : "",
                    CannotBuyReason = () => OwnsRod(r.id) ? "Эта удочка у тебя уже есть." : null,
                    OnBuy = () => GiveRod(r)
                });
            }
        }

        private void CaptureRods(SaveData data)
        {
            data.rods = new List<string>(ownedRods);
            data.rod = CurrentRod != null ? CurrentRod.id : "bamboo";
        }

        private void ApplyRods(SaveData data)
        {
            if (data.rods != null)
                foreach (string id in data.rods)
                    ownedRods.Add(id);
            RodData rod = database.FindRod(data.rod);
            if (rod != null && ownedRods.Contains(rod.id))
                CurrentRod = rod;
        }
    }
}
