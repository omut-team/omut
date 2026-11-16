using System;
using System.Collections.Generic;

namespace Omut.Fishing
{
    // Коробка со снастями: сколько какой наживки и какая сейчас на крючке.
    // Червь бесконечный, блёсны кончаются (рыба съела или оборвала леску).
    // Обычный C#-класс, проверяется тестами (TackleBoxTests).
    public class TackleBox
    {
        private static readonly BaitType[] Order = { BaitType.Worm, BaitType.Spoon, BaitType.Phosphor };

        private readonly Dictionary<BaitType, int> counts = new Dictionary<BaitType, int>();

        public BaitType Current { get; private set; } = BaitType.Worm;

        public event Action Changed;

        public TackleBox()
        {
            foreach (BaitType b in Order)
                counts[b] = 0;
        }

        public static bool IsInfinite(BaitType bait) => bait == BaitType.Worm;

        public int Count(BaitType bait) => counts.TryGetValue(bait, out int n) ? n : 0;

        public bool Has(BaitType bait) => IsInfinite(bait) || Count(bait) > 0;

        public void Add(BaitType bait, int amount)
        {
            if (amount <= 0 || IsInfinite(bait) || !counts.ContainsKey(bait))
                return;

            counts[bait] += amount;
            Changed?.Invoke();
        }

        // Поставить наживку на крючок. false = такой нет.
        public bool Select(BaitType bait)
        {
            if (!Has(bait))
                return false;

            Current = bait;
            Changed?.Invoke();
            return true;
        }

        // Q: следующая наживка из тех, что есть.
        public BaitType Next()
        {
            int start = Array.IndexOf(Order, Current);
            for (int i = 1; i <= Order.Length; i++)
            {
                BaitType candidate = Order[(start + i) % Order.Length];
                if (Has(candidate))
                {
                    Current = candidate;
                    break;
                }
            }

            Changed?.Invoke();
            return Current;
        }

        // Наживка потрачена. Если кончилась, на крючок встаёт червь. Возвращает true, если кончилась.
        public bool Use(BaitType bait)
        {
            if (IsInfinite(bait) || Count(bait) <= 0)
                return false;

            counts[bait]--;
            bool ranOut = counts[bait] == 0 && Current == bait;
            if (ranOut)
                Current = BaitType.Worm;

            Changed?.Invoke();
            return ranOut;
        }

        public void Set(BaitType bait, int count)
        {
            if (counts.ContainsKey(bait) && !IsInfinite(bait))
                counts[bait] = Math.Max(0, count);
            if (!Has(Current))
                Current = BaitType.Worm;
            Changed?.Invoke();
        }
    }
}
