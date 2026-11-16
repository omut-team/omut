using NUnit.Framework;
using Omut.Core;

namespace Omut.Tests
{
    // Автотесты кошелька.
    public class WalletTests
    {
        [Test]
        public void Earn_AddsMoney_AndTotal()
        {
            var wallet = new Wallet(30);

            wallet.Earn(70);

            Assert.AreEqual(100, wallet.Money);
            Assert.AreEqual(70, wallet.TotalEarned);
        }

        [Test]
        public void Spend_WhenEnough()
        {
            var wallet = new Wallet(50);

            Assert.IsTrue(wallet.TrySpend(20));
            Assert.AreEqual(30, wallet.Money);
        }

        [Test]
        public void Spend_WhenNotEnough_ChangesNothing()
        {
            var wallet = new Wallet(10);

            Assert.IsFalse(wallet.TrySpend(20));
            Assert.AreEqual(10, wallet.Money);
        }

        [Test]
        public void NegativeAmounts_Ignored()
        {
            var wallet = new Wallet(10);

            wallet.Earn(-5);
            Assert.IsFalse(wallet.TrySpend(-5));
            Assert.AreEqual(10, wallet.Money);
        }

        [Test]
        public void ChangedEvent_Fires()
        {
            var wallet = new Wallet(0);
            int last = -1;
            wallet.Changed += m => last = m;

            wallet.Earn(15);

            Assert.AreEqual(15, last);
        }
    }
}
