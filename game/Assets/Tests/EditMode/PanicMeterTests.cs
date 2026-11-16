using NUnit.Framework;
using Omut.Player;

namespace Omut.Tests
{
    // Автотесты паники: рост с фонарём и без, маяк, пороги 50/80/100, термос.
    public class PanicMeterTests
    {
        private static readonly PanicConditions NightLantern = new PanicConditions(true, false, true, false);
        private static readonly PanicConditions NightDark = new PanicConditions(true, false, false, false);
        private static readonly PanicConditions Lighthouse = new PanicConditions(true, false, false, true);
        private static readonly PanicConditions Day = new PanicConditions(false, false, false, false);

        [Test]
        public void WithLantern_SurvivesWholeNight()
        {
            var panic = new PanicMeter();

            panic.Tick(240f, NightLantern); // вся ночь

            Assert.Less(panic.Value, PanicMeter.SteeringLevel);
        }

        [Test]
        public void WithoutLight_FaintsInAbout40Seconds()
        {
            var panic = new PanicMeter();
            bool fainted = false;
            panic.Fainted += () => fainted = true;

            panic.Tick(30f, NightDark);
            Assert.IsFalse(fainted);
            panic.Tick(15f, NightDark);
            Assert.IsTrue(fainted);
            Assert.AreEqual(3, panic.Level);
        }

        [Test]
        public void Lighthouse_CalmsDown()
        {
            var panic = new PanicMeter();
            panic.Set(60f);

            panic.Tick(5f, Lighthouse);

            Assert.AreEqual(20f, panic.Value, 0.01f);
        }

        [Test]
        public void Day_PanicGoesAway()
        {
            var panic = new PanicMeter();
            panic.Set(40f);

            panic.Tick(20f, Day);

            Assert.AreEqual(0f, panic.Value);
        }

        [Test]
        public void Levels_At50_80_100()
        {
            var panic = new PanicMeter();

            panic.Set(49f);
            Assert.AreEqual(0, panic.Level);
            panic.Set(50f);
            Assert.AreEqual(1, panic.Level);
            panic.Set(80f);
            Assert.AreEqual(2, panic.Level);
            panic.Set(100f);
            Assert.AreEqual(3, panic.Level);
        }

        [Test]
        public void LevelChanged_Reports()
        {
            var panic = new PanicMeter();
            int from = -1, to = -1;
            panic.LevelChanged += (a, b) => { from = a; to = b; };

            panic.Set(55f);

            Assert.AreEqual(0, from);
            Assert.AreEqual(1, to);
        }

        [Test]
        public void Thermos_Minus35()
        {
            var panic = new PanicMeter();
            panic.Set(70f);

            panic.Calm();

            Assert.AreEqual(35f, panic.Value, 0.01f);
        }

        [Test]
        public void Okhotnik_PanicGrowsSlower()
        {
            var barkas = new PanicConditions(true, false, false, false, 1f);
            var okhotnik = new PanicConditions(true, false, false, false, 0.6f);

            Assert.Less(PanicMeter.Rate(okhotnik), PanicMeter.Rate(barkas));
        }

        [Test]
        public void ReefFog_ScaryEvenByDay()
        {
            Assert.Greater(PanicMeter.Rate(new PanicConditions(false, true, false, false)), 0f);
        }
    }
}
