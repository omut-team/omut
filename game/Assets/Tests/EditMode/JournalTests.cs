using System.Collections.Generic;
using NUnit.Framework;
using Omut.Core;
using Omut.Fishing;
using Omut.World;
using UnityEngine;

namespace Omut.Tests
{
    // Автотесты журнала: новая запись, счётчик N из 14, рекорд веса, сохранение.
    public class JournalTests
    {
        private readonly List<Object> created = new List<Object>();

        private FishData Fish(string id, int number)
        {
            var fish = ScriptableObject.CreateInstance<FishData>();
            fish.id = id;
            fish.journalNumber = number;
            fish.pricePerKg = 10f;
            created.Add(fish);
            return fish;
        }

        private static CatchInfo Catch(FishData fish, float weight, int day = 1) => new CatchInfo(fish, weight, false, ZoneId.Bay, day);

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in created)
                Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void FirstCatch_IsNewRecord()
        {
            var journal = new Journal();
            FishData crucian = Fish("crucian", 1);

            Assert.IsTrue(journal.Register(Catch(crucian, 1f)));
            Assert.IsFalse(journal.Register(Catch(crucian, 0.5f)));
            Assert.AreEqual(1, journal.CaughtCount);
            Assert.AreEqual(2, journal.Get("crucian").count);
        }

        [Test]
        public void BestWeight_IsKept()
        {
            var journal = new Journal();
            FishData pike = Fish("pike", 6);

            journal.Register(Catch(pike, 4f));
            journal.Register(Catch(pike, 7.5f));
            journal.Register(Catch(pike, 5f));

            Assert.AreEqual(7.5f, journal.Get("pike").bestWeight, 0.001f);
        }

        [Test]
        public void Scrap_NotInJournal()
        {
            var journal = new Journal();

            Assert.IsFalse(journal.Register(Catch(Fish("scrap", 0), 2f)));
            Assert.AreEqual(0, journal.CaughtCount);
        }

        [Test]
        public void Complete_At14()
        {
            var journal = new Journal();
            for (int i = 1; i <= 14; i++)
                journal.Register(Catch(Fish("f" + i, i), 1f));

            Assert.IsTrue(journal.IsComplete);
        }

        [Test]
        public void SaveAndLoad_KeepsEntries()
        {
            var journal = new Journal();
            journal.Register(Catch(Fish("perch", 2), 1.2f, 3));

            var loaded = new Journal();
            loaded.Load(journal.ToList());

            Assert.IsTrue(loaded.IsCaught("perch"));
            Assert.AreEqual(3, loaded.Get("perch").firstDay);
        }

        [Test]
        public void UpdatedEvent_Fires()
        {
            var journal = new Journal();
            bool wasNew = false;
            journal.Updated += (e, isNew) => wasNew = isNew;

            journal.Register(Catch(Fish("bream", 3), 2f));

            Assert.IsTrue(wasNew);
        }
    }
}
