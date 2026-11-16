using System.Collections.Generic;
using NUnit.Framework;
using Omut.Fishing;
using Omut.NPC;
using Omut.World;
using UnityEngine;

namespace Omut.Tests
{
    // Автотесты заданий: доступность по цепочке, прогресс, «принести», «заработать», сохранение.
    public class QuestManagerTests
    {
        private class Context : IQuestContext
        {
            public readonly Dictionary<string, int> hold = new Dictionary<string, int>();
            public int earned;
            public int HoldCount(string fishId) => hold.TryGetValue(fishId, out int n) ? n : 0;
            public int TotalEarned => earned;
        }

        private readonly List<Object> created = new List<Object>();
        private QuestData q1, q2, deliver, earn;
        private FishData crucian, pike;

        private T Make<T>() where T : ScriptableObject
        {
            T o = ScriptableObject.CreateInstance<T>();
            created.Add(o);
            return o;
        }

        [SetUp]
        public void SetUp()
        {
            crucian = Make<FishData>();
            crucian.id = "crucian";
            crucian.journalNumber = 1;
            pike = Make<FishData>();
            pike.id = "pike";
            pike.journalNumber = 6;

            q1 = Make<QuestData>();
            q1.id = "q1";
            q1.goal = QuestGoal.CatchFish;
            q1.fishId = "crucian";
            q1.count = 3;

            q2 = Make<QuestData>();
            q2.id = "q2";
            q2.goal = QuestGoal.CatchAtNight;
            q2.count = 1;
            q2.requiredQuest = "q1";

            deliver = Make<QuestData>();
            deliver.id = "d";
            deliver.goal = QuestGoal.DeliverFish;
            deliver.fishId = "pike";
            deliver.count = 2;

            earn = Make<QuestData>();
            earn.id = "e";
            earn.goal = QuestGoal.EarnMoney;
            earn.count = 100;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in created)
                Object.DestroyImmediate(o);
            created.Clear();
        }

        private QuestManager Manager() => new QuestManager(new List<QuestData> { q1, q2, deliver, earn });

        private static CatchInfo Catch(FishData f, bool night = false) => new CatchInfo(f, 1f, night, ZoneId.Bay, 1);

        [Test]
        public void Chain_LockedUntilPreviousDone()
        {
            QuestManager m = Manager();

            Assert.AreEqual(QuestState.Available, m.StateOf(q1));
            Assert.AreEqual(QuestState.Locked, m.StateOf(q2));
            Assert.IsFalse(m.Accept(q2));
        }

        [Test]
        public void CatchQuest_CountsOnlyRightFish()
        {
            QuestManager m = Manager();
            var ctx = new Context();
            m.Accept(q1);

            m.OnFishCaught(Catch(crucian));
            m.OnFishCaught(Catch(pike));
            m.OnFishCaught(Catch(crucian));

            Assert.AreEqual(2, m.Progress(q1, ctx));
            Assert.IsFalse(m.IsComplete(q1, ctx));

            m.OnFishCaught(Catch(crucian));
            Assert.IsTrue(m.TryComplete(q1, ctx));
            Assert.AreEqual(QuestState.Done, m.StateOf(q1));
            Assert.AreEqual(QuestState.Available, m.StateOf(q2));
        }

        [Test]
        public void CatchBeforeAccept_DoesNotCount()
        {
            QuestManager m = Manager();
            m.OnFishCaught(Catch(crucian));
            m.Accept(q1);

            Assert.AreEqual(0, m.Progress(q1, new Context()));
        }

        [Test]
        public void NightQuest_OnlyNightCatches()
        {
            QuestManager m = Manager();
            var ctx = new Context();
            m.Accept(q1);
            for (int i = 0; i < 3; i++)
                m.OnFishCaught(Catch(crucian));
            m.TryComplete(q1, ctx);
            m.Accept(q2);

            m.OnFishCaught(Catch(pike, false));
            Assert.AreEqual(0, m.Progress(q2, ctx));
            m.OnFishCaught(Catch(pike, true));
            Assert.IsTrue(m.IsComplete(q2, ctx));
        }

        [Test]
        public void Deliver_UsesHold()
        {
            QuestManager m = Manager();
            var ctx = new Context();
            m.Accept(deliver);

            ctx.hold["pike"] = 1;
            Assert.IsFalse(m.TryComplete(deliver, ctx));
            ctx.hold["pike"] = 2;
            Assert.IsTrue(m.TryComplete(deliver, ctx));
        }

        [Test]
        public void Earn_UsesTotalEarned()
        {
            QuestManager m = Manager();
            var ctx = new Context { earned = 99 };
            m.Accept(earn);

            Assert.IsFalse(m.IsComplete(earn, ctx));
            ctx.earned = 150;
            Assert.IsTrue(m.IsComplete(earn, ctx));
            Assert.AreEqual(100, m.Progress(earn, ctx));
        }

        [Test]
        public void SaveLoad_KeepsState()
        {
            QuestManager m = Manager();
            m.Accept(q1);
            m.OnFishCaught(Catch(crucian));
            m.Accept(deliver);

            QuestManager loaded = Manager();
            loaded.Load(m.ActiveIds(), m.DoneIds(), m.ProgressList());

            Assert.AreEqual(QuestState.Active, loaded.StateOf(q1));
            Assert.AreEqual(1, loaded.Progress(q1, new Context()));
            Assert.AreEqual(QuestState.Active, loaded.StateOf(deliver));
        }
    }
}
