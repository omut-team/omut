using System;

namespace Omut.Player
{
    // Прочность лодки: сколько ударов о коряги и скалы она ещё выдержит.
    // Обычный C#-класс, проверяется тестами (BoatStatsTests).
    public class BoatStats
    {
        public int MaxDurability { get; private set; }
        public int Durability { get; private set; }

        public bool IsBroken => Durability <= 0;
        public bool IsFull => Durability >= MaxDurability;
        public int Missing => MaxDurability - Durability;

        public event Action Changed;

        public BoatStats(int maxDurability)
        {
            SetMax(maxDurability, true);
        }

        // Новая лодка: новый запас прочности (fill = сразу целая).
        public void SetMax(int maxDurability, bool fill)
        {
            MaxDurability = Math.Max(1, maxDurability);
            Durability = fill ? MaxDurability : Math.Min(Durability, MaxDurability);
            Changed?.Invoke();
        }

        // Удар. Возвращает true, если лодка пробита (прочность кончилась).
        public bool Hit(int damage)
        {
            if (damage <= 0 || IsBroken)
                return IsBroken;

            Durability = Math.Max(0, Durability - damage);
            Changed?.Invoke();
            return IsBroken;
        }

        public void Repair(int points)
        {
            if (points <= 0)
                return;

            Durability = Math.Min(MaxDurability, Durability + points);
            Changed?.Invoke();
        }

        public void Set(int durability)
        {
            Durability = durability < 0 ? MaxDurability : Math.Min(MaxDurability, durability);
            Changed?.Invoke();
        }
    }
}
