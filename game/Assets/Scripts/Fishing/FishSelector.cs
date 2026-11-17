using System;
using System.Collections.Generic;
using Omut.World;

namespace Omut.Fishing
{
    // Условия заброса: где, когда, на что и как далеко.
    public struct CastConditions
    {
        public ZoneId zone;
        public DayPhase phase;
        public BaitType bait;
        public float castDistance;
        public bool inSpot;        // поплавок попал в круги на воде
        public bool chestAllowed;  // ещё остались страницы дневника на дне

        public CastConditions(ZoneId zone, DayPhase phase, BaitType bait, float castDistance, bool inSpot = false, bool chestAllowed = true)
        {
            this.zone = zone;
            this.phase = phase;
            this.bait = bait;
            this.castDistance = castDistance;
            this.inSpot = inSpot;
            this.chestAllowed = chestAllowed;
        }
    }

    // Выбирает, какая рыба клюнет. Обычный C#-класс, проверяется тестами (FishSelectorTests).
    // 1) Отбираем рыбу, которая подходит по зоне, времени, наживке и дальности.
    // 2) Из подходящих берём случайную, обычные чаще, редкие реже (веса по редкости).
    public class FishSelector
    {
        private readonly IReadOnlyList<FishData> allFish;
        private readonly Random random;

        public FishSelector(IReadOnlyList<FishData> allFish, int? seed = null)
        {
            this.allFish = allFish;
            random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // Вес редкости: из 100 забросов примерно столько раз клюнет рыба этой редкости.
        public static float RarityWeight(Rarity rarity, bool inSpot)
        {
            float bonus = inSpot ? 1.6f : 1f; // в кругах чаще клюёт редкая рыба
            switch (rarity)
            {
                case Rarity.Common: return 60f;
                case Rarity.Uncommon: return 28f * bonus;
                case Rarity.Rare: return 12f * bonus;
                case Rarity.Epic: return 5f * bonus;
                default: return 0f; // легендарную случайно не поймать, только в финале
            }
        }

        // Какая доля клёва может прийтись на мусор и железо вместе (остальное рыба).
        public const float ExtraShare = 0.28f;

        // Настоящая рыба (есть в журнале): не мусор и не металлолом.
        public static bool IsRealFish(FishData fish) => fish != null && !fish.isJunk && fish.InJournal;

        // Мусор клюёт редко и одинаково везде, круги на воде его не приманивают.
        public const float JunkWeight = 6f;

        public static float Weight(FishData fish, bool inSpot) => fish.isJunk ? JunkWeight : RarityWeight(fish.rarity, inSpot);

        public bool Fits(FishData fish, CastConditions c)
        {
            if (fish == null || fish.isBoss)
                return false;
            if (fish.isChest && !c.chestAllowed)
                return false;
            if ((fish.zones & Zone.ToMask(c.zone)) == 0)
                return false;
            if (!fish.BitesAt(c.phase))
                return false;
            if (!fish.BitesOn(c.bait))
                return false;
            return c.castDistance >= fish.minCastDistance;
        }

        public List<FishData> Candidates(CastConditions c)
        {
            var list = new List<FishData>();
            foreach (FishData fish in allFish)
            {
                if (Fits(fish, c))
                    list.Add(fish);
            }

            return list;
        }

        // Кто клюнет. null = на эту наживку здесь сейчас никто не клюёт.
        public FishData Pick(CastConditions c)
        {
            List<FishData> candidates = Candidates(c);

            // Мусор и железо цепляются только как попутный улов: если настоящая рыба здесь
            // на эту наживку не клюёт, не клюёт ничего, и игрок видит подсказку сменить наживку или место.
            // (Раньше в омуте на червя ловились одни сапоги и железо, и подсказки не было.)
            if (!candidates.Exists(IsRealFish))
                return null;

            // Мусор и железо вместе не больше ExtraShare от всего клёва, даже где рыбы мало (омут в сумерках).
            float fishTotal = 0f, extraTotal = 0f;
            foreach (FishData fish in candidates)
            {
                if (IsRealFish(fish)) fishTotal += Weight(fish, c.inSpot);
                else extraTotal += Weight(fish, c.inSpot);
            }

            float maxExtra = fishTotal * ExtraShare / (1f - ExtraShare);
            float extraScale = extraTotal > maxExtra && extraTotal > 0f ? maxExtra / extraTotal : 1f;
            float total = fishTotal + extraTotal * extraScale;
            if (total <= 0f)
                return null;

            double roll = random.NextDouble() * total;
            foreach (FishData fish in candidates)
            {
                roll -= Weight(fish, c.inSpot) * (IsRealFish(fish) ? 1f : extraScale);
                if (roll <= 0)
                    return fish;
            }

            return candidates[candidates.Count - 1];
        }

        // Вес пойманной рыбы. Крупные попадаются реже (квадрат случайного числа).
        public float RollWeight(FishData fish)
        {
            double r = random.NextDouble();
            float weight = fish.minWeight + (fish.maxWeight - fish.minWeight) * (float)(r * r);
            return (float)Math.Round(weight, 2);
        }

        // Цена у Тараса: вес x цена за кг, ночью x3.
        public static int Price(FishData fish, float weight, bool caughtAtNight)
        {
            float price = fish.pricePerKg * weight * (caughtAtNight ? 3f : 1f);
            return (int)Math.Round(price);
        }
    }
}
