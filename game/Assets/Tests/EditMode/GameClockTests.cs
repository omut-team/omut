using NUnit.Framework;
using Omut.World;

namespace Omut.Tests
{
    // Автотесты игровых часов: время -> фаза, скорость дня и ночи, колокол, новый день.
    // Запуск: Window > General > Test Runner > EditMode > Run All.
    // Во всех тестах: день 8 минут, ночь 4 минуты (как в игре).
    public class GameClockTests
    {
        private const float Tolerance = 0.01f;

        private static GameClock CreateClock(int hour = 6, int minute = 0)
            => new GameClock(8f, 4f, hour * 60 + minute);

        [TestCase(6, 0, DayPhase.Morning)]
        [TestCase(9, 59, DayPhase.Morning)]
        [TestCase(10, 0, DayPhase.Day)]
        [TestCase(16, 59, DayPhase.Day)]
        [TestCase(17, 0, DayPhase.Evening)]
        [TestCase(19, 59, DayPhase.Evening)]
        [TestCase(20, 0, DayPhase.Night)]
        [TestCase(23, 59, DayPhase.Night)]
        [TestCase(0, 0, DayPhase.Night)]
        [TestCase(5, 59, DayPhase.Night)]
        public void TimeToPhase(int hour, int minute, DayPhase expected)
        {
            Assert.AreEqual(expected, GameClock.GetPhase(hour * 60 + minute));
        }

        [Test]
        public void StartsAtSixMorning_DayOne()
        {
            var clock = new GameClock();

            Assert.AreEqual("06:00", clock.ToString());
            Assert.AreEqual(1, clock.Day);
            Assert.AreEqual(DayPhase.Morning, clock.Phase);
        }

        [Test]
        public void WholeDay_LastsEightRealMinutes()
        {
            var clock = CreateClock(6, 0);

            clock.Tick(8 * 60 - 1f); // за секунду до колокола ещё вечер

            Assert.AreEqual(DayPhase.Evening, clock.Phase);

            clock.Tick(1f);

            Assert.AreEqual(20 * 60, clock.Minutes, Tolerance);
        }

        [Test]
        public void WholeNight_LastsFourRealMinutes()
        {
            var clock = CreateClock(20, 0);

            clock.Tick(4 * 60);

            Assert.AreEqual(6 * 60, clock.Minutes, Tolerance);
            Assert.AreEqual(DayPhase.Morning, clock.Phase);
        }

        [Test]
        public void FullCycle_IsTwelveMinutes()
        {
            var clock = CreateClock(6, 0);

            clock.Tick(12 * 60);

            Assert.AreEqual("06:00", clock.ToString());
            Assert.AreEqual(2, clock.Day);
        }

        [Test]
        public void Bell_RingsOnceAtEight()
        {
            var clock = CreateClock(19, 0);
            int rings = 0;
            clock.BellRang += () => rings++;

            for (int i = 0; i < 600; i++)
                clock.Tick(0.1f); // 60 секунд мелкими шагами, как кадры

            Assert.AreEqual(1, rings);
            Assert.IsTrue(clock.IsNight);
        }

        [Test]
        public void Bell_NotRing_DuringDay()
        {
            var clock = CreateClock(6, 0);
            int rings = 0;
            clock.BellRang += () => rings++;

            clock.Tick(60f);

            Assert.AreEqual(0, rings);
        }

        [Test]
        public void BigStep_DoesNotSkipBell()
        {
            var clock = CreateClock(19, 50);
            int rings = 0;
            clock.BellRang += () => rings++;

            clock.Tick(60f); // одним большим шагом через 20:00

            Assert.AreEqual(1, rings);
            Assert.AreEqual(DayPhase.Night, clock.Phase);
        }

        [Test]
        public void NightGoesThroughMidnight()
        {
            var clock = CreateClock(23, 0);

            clock.Tick(4 * 60 * 120f / 600f); // ночью 600 минут за 240 с, значит 120 минут за 48 с

            Assert.AreEqual("01:00", clock.ToString());
            Assert.AreEqual(1, clock.Day);
        }

        [Test]
        public void NewDay_AtSixMorning()
        {
            var clock = CreateClock(5, 0);
            int newDay = 0;
            clock.NewDay += day => newDay = day;

            clock.Tick(60f);

            Assert.AreEqual(2, newDay);
            Assert.AreEqual(2, clock.Day);
        }

        [Test]
        public void PhaseChanged_ReportsFromAndTo()
        {
            var clock = CreateClock(9, 59);
            DayPhase from = DayPhase.Night, to = DayPhase.Night;
            clock.PhaseChanged += (a, b) => { from = a; to = b; };

            clock.Tick(10f);

            Assert.AreEqual(DayPhase.Morning, from);
            Assert.AreEqual(DayPhase.Day, to);
        }

        [Test]
        public void NightRuns_FasterThanDay()
        {
            var day = CreateClock(12, 0);
            var night = CreateClock(22, 0);

            Assert.Greater(night.MinutesPerSecond, day.MinutesPerSecond);
        }

        [Test]
        public void SetTime_DoesNotRingBell()
        {
            var clock = CreateClock(12, 0);
            int rings = 0;
            clock.BellRang += () => rings++;

            clock.SetTime(21, 30);

            Assert.AreEqual(0, rings);
            Assert.AreEqual("21:30", clock.ToString());
        }

        [Test]
        public void NegativeDelta_DoesNothing()
        {
            var clock = CreateClock(12, 0);

            clock.Tick(-5f);

            Assert.AreEqual("12:00", clock.ToString());
        }
    }
}
