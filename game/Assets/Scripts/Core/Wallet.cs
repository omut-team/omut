using System;

namespace Omut.Core
{
    // Кошелёк: монеты игрока. Обычный C#-класс, проверяется тестами (WalletTests).
    public class Wallet
    {
        public int Money { get; private set; }

        // Всего заработано за игру (для рекордов на сайте).
        public int TotalEarned { get; private set; }

        public event Action<int> Changed;

        public Wallet(int startMoney = 0)
        {
            Money = Math.Max(0, startMoney);
        }

        public bool CanAfford(int price) => price >= 0 && Money >= price;

        public void Earn(int amount)
        {
            if (amount <= 0)
                return;

            Money += amount;
            TotalEarned += amount;
            Changed?.Invoke(Money);
        }

        // Заплатить. false = не хватает денег, ничего не списано.
        public bool TrySpend(int price)
        {
            if (price < 0 || !CanAfford(price))
                return false;

            Money -= price;
            Changed?.Invoke(Money);
            return true;
        }

        public void Set(int money, int totalEarned)
        {
            Money = Math.Max(0, money);
            TotalEarned = Math.Max(0, totalEarned);
            Changed?.Invoke(Money);
        }
    }
}
