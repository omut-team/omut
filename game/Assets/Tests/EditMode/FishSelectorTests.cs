using System.Collections.Generic;
using NUnit.Framework;
using Omut.Fishing;
using Omut.World;
using UnityEngine;

namespace Omut.Tests
{
    // Автотесты выбора рыбы: кто клюёт в какой зоне, в какое время и на какую наживку.
    public class FishSelectorTests
    {
        private readonly List<Object> created = new List<Object>();
        private List<FishData> all;
        private FishData crucian, perch, pike, zander, burbot, chest, boss;

        private FishData Make(string id, Rarity rarity, FishTime time, ZoneMask zones, BaitType bait, float minCast = 0f)
        {
            var fish = ScriptableObject.CreateInstance<FishData>();
            fish.id = id;
            fish.rarity = rarity;
            fish.time = time;
            fish.zones = zones;
            fish.bait = bait;
            fish.minCastDistance = minCast;
            fish.minWeight = 1f;
            fish.maxWeight = 3f;
            fish.pricePerKg = 10f;
            created.Add(fish);
            return fish;
        }

        [SetUp]
        public void SetUp()
        {
            crucian = Make("crucian", Rarity.Common, FishTime.Day, ZoneMask.Bay, BaitType.Worm);
            perch = Make("perch", Rarity.Common, FishTime.Day, ZoneMask.All, BaitType.Spoon);
            pike = Make("pike", Rarity.Uncommon, FishTime.Day, ZoneMask.Pool, BaitType.Spoon);
            zander = Make("zander", Rarity.Uncommon, FishTime.Day | FishTime.Dusk, ZoneMask.Pool, BaitType.Spoon, 5f);
            burbot = Make("burbot", Rarity.Uncommon, FishTime.Night, ZoneMask.Pool, BaitType.Worm);
            chest = Make("chest", Rarity.Epic, FishTime.Night, ZoneMask.Reef, BaitType.Any);
            chest.isChest = true;
            boss = Make("boss", Rarity.Legendary, FishTime.Night, ZoneMask.Pool, BaitType.Phosphor);
            boss.isBoss = true;
            all = new List<FishData> { crucian, perch, pike, zander, burbot, chest, boss };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in created)
                Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Bay_Day_Worm_OnlyCrucian()
        {
            var selector = new FishSelector(all, 1);

            List<FishData> list = selector.Candidates(new CastConditions(ZoneId.Bay, DayPhase.Day, BaitType.Worm, 3f));

            CollectionAssert.AreEquivalent(new[] { crucian }, list);
        }

        [Test]
        public void Pool_Day_Spoon_ShortCast_NoZander()
        {
            var selector = new FishSelector(all, 1);

            List<FishData> list = selector.Candidates(new CastConditions(ZoneId.Pool, DayPhase.Day, BaitType.Spoon, 3f));

            CollectionAssert.AreEquivalent(new[] { perch, pike }, list);
        }

        [Test]
        public void Pool_Dusk_Spoon_LongCast_Zander()
        {
            var selector = new FishSelector(all, 1);

            List<FishData> list = selector.Candidates(new CastConditions(ZoneId.Pool, DayPhase.Evening, BaitType.Spoon, 6f));

            CollectionAssert.AreEquivalent(new[] { zander }, list);
        }

        [Test]
        public void Night_Pool_Worm_Burbot()
        {
            var selector = new FishSelector(all, 1);

            Assert.AreEqual(burbot, selector.Pick(new CastConditions(ZoneId.Pool, DayPhase.Night, BaitType.Worm, 3f)));
        }

        [Test]
        public void Boss_NeverPickedRandomly()
        {
            var selector = new FishSelector(all, 1);

            List<FishData> list = selector.Candidates(new CastConditions(ZoneId.Pool, DayPhase.Night, BaitType.Phosphor, 5f));

            CollectionAssert.DoesNotContain(list, boss);
        }

        [Test]
        public void Chest_OnlyWhilePagesLeft()
        {
            var selector = new FishSelector(all, 1);

            Assert.AreEqual(chest, selector.Pick(new CastConditions(ZoneId.Reef, DayPhase.Night, BaitType.Worm, 3f, false, true)));
            Assert.IsNull(selector.Pick(new CastConditions(ZoneId.Reef, DayPhase.Night, BaitType.Worm, 3f, false, false)));
        }

        [Test]
        public void NothingBites_ReturnsNull()
        {
            var selector = new FishSelector(all, 1);

            Assert.IsNull(selector.Pick(new CastConditions(ZoneId.Bay, DayPhase.Night, BaitType.Spoon, 3f)));
        }

        [Test]
        public void CommonFish_BitesMoreOften()
        {
            var selector = new FishSelector(all, 42);
            int perchCount = 0, pikeCount = 0;
            var c = new CastConditions(ZoneId.Pool, DayPhase.Day, BaitType.Spoon, 3f);

            for (int i = 0; i < 2000; i++)
            {
                FishData f = selector.Pick(c);
                if (f == perch) perchCount++;
                else if (f == pike) pikeCount++;
            }

            Assert.Greater(perchCount, pikeCount);
            Assert.Greater(pikeCount, 0);
        }

        [Test]
        public void InSpot_RareMoreOften()
        {
            Assert.Greater(FishSelector.RarityWeight(Rarity.Rare, true), FishSelector.RarityWeight(Rarity.Rare, false));
            Assert.AreEqual(FishSelector.RarityWeight(Rarity.Common, true), FishSelector.RarityWeight(Rarity.Common, false));
        }

        // Мусор (сапог, шина) клюёт, но заметно реже обычной рыбы, и круги на воде его не приманивают.
        [Test]
        public void Junk_BitesRarelyThanFish()
        {
            FishData boot = Make("boot", Rarity.Common, FishTime.Always, ZoneMask.Bay, BaitType.Any);
            boot.isJunk = true;
            var selector = new FishSelector(new List<FishData> { crucian, boot }, 11);
            var c = new CastConditions(ZoneId.Bay, DayPhase.Day, BaitType.Worm, 3f);

            int junk = 0;
            for (int i = 0; i < 1000; i++)
                if (selector.Pick(c) == boot)
                    junk++;

            Assert.Greater(junk, 0, "Мусор вообще клюёт");
            Assert.Less(junk, 200, "Мусор клюёт реже карася");
            Assert.AreEqual(FishSelector.Weight(boot, true), FishSelector.Weight(boot, false));
        }

        [Test]
        public void Weight_InRange()
        {
            var selector = new FishSelector(all, 5);

            for (int i = 0; i < 200; i++)
            {
                float w = selector.RollWeight(pike);
                Assert.That(w, Is.InRange(1f, 3f));
            }
        }

        [Test]
        public void Price_NightIsTripled()
        {
            Assert.AreEqual(20, FishSelector.Price(pike, 2f, false));
            Assert.AreEqual(60, FishSelector.Price(pike, 2f, true));
        }

        [Test]
        public void PhaseToFishTime()
        {
            Assert.AreEqual(FishTime.Day, FishData.ToFishTime(DayPhase.Morning));
            Assert.AreEqual(FishTime.Dusk, FishData.ToFishTime(DayPhase.Evening));
            Assert.AreEqual(FishTime.Night, FishData.ToFishTime(DayPhase.Night));
        }
    }
}
