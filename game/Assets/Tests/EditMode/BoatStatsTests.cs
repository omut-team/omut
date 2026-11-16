using NUnit.Framework;
using Omut.Player;

namespace Omut.Tests
{
    // Автотесты прочности лодки: удары, пробитие, ремонт, смена лодки.
    public class BoatStatsTests
    {
        [Test]
        public void Barkas_BreaksAfterTwoHits()
        {
            var boat = new BoatStats(2);

            Assert.IsFalse(boat.Hit(1));
            Assert.AreEqual(1, boat.Durability);
            Assert.IsTrue(boat.Hit(1));
            Assert.IsTrue(boat.IsBroken);
        }

        [Test]
        public void Durability_NeverBelowZero()
        {
            var boat = new BoatStats(2);

            boat.Hit(5);

            Assert.AreEqual(0, boat.Durability);
        }

        [Test]
        public void Repair_NotAboveMax()
        {
            var boat = new BoatStats(4);
            boat.Hit(3);

            boat.Repair(10);

            Assert.AreEqual(4, boat.Durability);
            Assert.IsTrue(boat.IsFull);
        }

        [Test]
        public void Missing_CountsDamage()
        {
            var boat = new BoatStats(5);
            boat.Hit(2);

            Assert.AreEqual(2, boat.Missing);
        }

        [Test]
        public void NewBoat_IsFull()
        {
            var boat = new BoatStats(2);
            boat.Hit(1);

            boat.SetMax(5, true);

            Assert.AreEqual(5, boat.Durability);
        }

        [Test]
        public void Set_Minus1_MeansFull()
        {
            var boat = new BoatStats(4);
            boat.Hit(2);

            boat.Set(-1);

            Assert.AreEqual(4, boat.Durability);
        }
    }
}
