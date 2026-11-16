using NUnit.Framework;
using Omut.Player;

namespace Omut.Tests
{
    // Автотесты керосина в фонаре.
    public class LanternFuelTests
    {
        [Test]
        public void Off_DoesNotBurn()
        {
            var fuel = new LanternFuel(50f);

            fuel.Burn(100f);

            Assert.AreEqual(50f, fuel.Fuel, 0.001f);
        }

        [Test]
        public void On_Burns_FullTankIn300Seconds()
        {
            var fuel = new LanternFuel(100f);
            fuel.SetOn(true);

            fuel.Burn(150f);

            Assert.AreEqual(50f, fuel.Fuel, 0.01f);
        }

        [Test]
        public void Empty_TurnsOff_AndReportsOnce()
        {
            var fuel = new LanternFuel(1f);
            fuel.SetOn(true);

            Assert.IsTrue(fuel.Burn(10f));
            Assert.IsFalse(fuel.IsOn);
            Assert.IsFalse(fuel.Burn(10f));
        }

        [Test]
        public void Empty_CannotTurnOn()
        {
            var fuel = new LanternFuel(0f);

            Assert.IsFalse(fuel.Toggle());
        }

        [Test]
        public void Refill_CappedAt100()
        {
            var fuel = new LanternFuel(80f);

            fuel.Refill(50f);

            Assert.AreEqual(100f, fuel.Fuel);
        }
    }
}
