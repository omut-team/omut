using NUnit.Framework;
using Omut.Fishing;

namespace Omut.Tests
{
    // Автотесты вываживания: зелёный коридор, обрыв лески через 3 с в красной зоне, рыба сошла при нуле.
    public class TensionModelTests
    {
        private const float Dt = 0.05f;

        // Рыба с постоянной силой, чтобы тесты были предсказуемыми.
        private class ConstantPull : IFishBehaviour
        {
            private readonly float force;
            private readonly bool inverted;

            public ConstantPull(float force, bool inverted = false)
            {
                this.force = force;
                this.inverted = inverted;
            }

            public string Name => "тест";
            public float ProgressMultiplier => 1f;
            public float Force(float time, float strength) => force;
            public bool InvertsControls(float time) => inverted;
        }

        // Умный игрок: держит стрелку в середине зелёного коридора.
        private static ReelResult PlayWell(TensionModel model, float seconds = 120f)
        {
            for (float t = 0; t < seconds && model.Result == ReelResult.InProgress; t += Dt)
            {
                bool pull = model.Tension < 0.45f;
                bool slack = model.Tension > 0.6f;
                if (model.ControlsInverted)
                {
                    bool x = pull;
                    pull = slack;
                    slack = x;
                }

                model.Tick(Dt, pull, slack);
            }

            return model.Result;
        }

        [Test]
        public void StartsInMiddle_NoProgress()
        {
            var model = new TensionModel(new SteadyPull(), 0.3f);

            Assert.AreEqual(0.5f, model.Tension, 0.001f);
            Assert.AreEqual(0f, model.Progress);
            Assert.AreEqual(ReelResult.InProgress, model.Result);
        }

        [Test]
        public void KeepingGreen_CatchesFish()
        {
            Assert.AreEqual(ReelResult.Caught, PlayWell(new TensionModel(new SteadyPull(), 0.3f)));
        }

        [Test]
        public void KeepingGreen_CatchesEveryBehaviour()
        {
            Assert.AreEqual(ReelResult.Caught, PlayWell(new TensionModel(new HeavyPull(), 0.7f)));
            Assert.AreEqual(ReelResult.Caught, PlayWell(new TensionModel(new ReversePull(), 0.6f)));
            Assert.AreEqual(ReelResult.Caught, PlayWell(new TensionModel(new JerkPull(), 0.5f)));
        }

        [Test]
        public void AlwaysPulling_BreaksLine()
        {
            var model = new TensionModel(new HeavyPull(), 0.7f);

            for (int i = 0; i < 400 && model.Result == ReelResult.InProgress; i++)
                model.Tick(Dt, true, false);

            Assert.AreEqual(ReelResult.LineBroke, model.Result);
        }

        [Test]
        public void LineBreaks_OnlyAfterThreeSecondsInRed()
        {
            var model = new TensionModel(new ConstantPull(2f), 0.5f);

            for (float t = 0; t < 2.9f; t += Dt)
                model.Tick(Dt, false, false);

            Assert.IsTrue(model.InRed);
            Assert.AreEqual(ReelResult.InProgress, model.Result);

            for (float t = 0; t < 1f; t += Dt)
                model.Tick(Dt, false, false);

            Assert.AreEqual(ReelResult.LineBroke, model.Result);
        }

        [Test]
        public void RedTimer_ResetsWhenLeavingRed()
        {
            var model = new TensionModel(new ConstantPull(1.5f), 0.5f);
            for (float t = 0; t < 2f; t += Dt)
                model.Tick(Dt, false, false);
            Assert.Greater(model.RedTimer, 0f);

            // стравили: натяжение упало из красной зоны
            for (int i = 0; i < 20 && model.InRed; i++)
                model.Tick(Dt, false, true);

            Assert.AreEqual(0f, model.RedTimer);
        }

        [Test]
        public void DoingNothing_WeakFishEscapes()
        {
            var model = new TensionModel(new ConstantPull(0.2f), 0.1f);

            for (int i = 0; i < 400 && model.Result == ReelResult.InProgress; i++)
                model.Tick(Dt, false, false);

            Assert.AreEqual(ReelResult.Escaped, model.Result);
        }

        [Test]
        public void NoEscape_DuringGraceTime()
        {
            var model = new TensionModel(new ConstantPull(0f), 0.1f);

            model.Tick(1f, false, true); // натяжение сразу в ноль

            Assert.AreEqual(0f, model.Tension);
            Assert.AreEqual(ReelResult.InProgress, model.Result);
        }

        [Test]
        public void ReverseControls_LeftButtonLoosens()
        {
            var model = new TensionModel(new ConstantPull(0.75f, true), 0.3f);

            model.Tick(0.2f, true, false); // ЛКМ при обратной шкале отпускает

            Assert.Less(model.Tension, 0.5f);
        }

        [Test]
        public void ProgressGrows_OnlyInGreen()
        {
            var model = new TensionModel(new ConstantPull(TensionModel.SagRate), 0.3f); // сила рыбы = провисание, стрелка стоит

            model.Tick(1f, false, false);

            Assert.IsTrue(model.InGreen);
            Assert.Greater(model.Progress, 0f);
        }

        [Test]
        public void BossIsSlower()
        {
            var normal = new TensionModel(new ConstantPull(TensionModel.SagRate), 0.5f);
            var boss = new TensionModel(new ConstantPull(TensionModel.SagRate), 0.5f, 0.6f);

            normal.Tick(1f, false, false);
            boss.Tick(1f, false, false);

            Assert.Less(boss.Progress, normal.Progress);
        }

        [Test]
        public void AllPull_ChangesBehaviourEveryPhase()
        {
            var all = new AllPull(6f);

            Assert.IsInstanceOf<SteadyPull>(all.Current(1f));
            Assert.IsInstanceOf<JerkPull>(all.Current(7f));
            Assert.IsInstanceOf<HeavyPull>(all.Current(13f));
            Assert.IsInstanceOf<ReversePull>(all.Current(19f));
            Assert.IsTrue(all.InvertsControls(19f));
            Assert.IsInstanceOf<SteadyPull>(all.Current(25f));
        }

        [Test]
        public void Jerk_HasSpikes()
        {
            var jerk = new JerkPull(2f, 0.4f);

            Assert.IsTrue(jerk.IsJerking(2.1f));
            Assert.IsFalse(jerk.IsJerking(3f));
            Assert.Greater(jerk.Force(2.1f, 0.5f), jerk.Force(3f, 0.5f));
        }

        [Test]
        public void Factory_MapsEnum()
        {
            Assert.IsInstanceOf<HeavyPull>(FishBehaviours.Create(FishBehaviour.Heavy));
            Assert.IsInstanceOf<AllPull>(FishBehaviours.Create(FishBehaviour.All));
            Assert.IsInstanceOf<SteadyPull>(FishBehaviours.Create(FishBehaviour.Steady));
        }
    }
}
