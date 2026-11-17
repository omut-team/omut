using NUnit.Framework;
using Omut.Core;
using Omut.Fishing;
using Omut.World;
using UnityEditor;

namespace Omut.Tests
{
    // Шансы клёва на настоящих данных игры (Assets/Data/GameDatabase.asset).
    // Баг был такой: в омуте днём на червя ловились одни сапоги и железо, рыбы не было вовсе.
    public class RealCatchChancesTests
    {
        private const int Casts = 3000;

        private static GameDatabase Database() =>
            AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");

        // Доля настоящей рыбы среди всего, что клюнуло. -1: ничего не клюёт.
        private static float FishShare(GameDatabase db, ZoneId zone, DayPhase phase, BaitType bait)
        {
            var selector = new FishSelector(db.fish, 42);
            var c = new CastConditions(zone, phase, bait, 6f);
            int bites = 0, fish = 0;
            for (int i = 0; i < Casts; i++)
            {
                FishData f = selector.Pick(c);
                if (f == null)
                    continue;
                bites++;
                if (FishSelector.IsRealFish(f))
                    fish++;
            }

            return bites == 0 ? -1f : (float)fish / bites;
        }

        [Test]
        public void FishBitesNearDockAndInPool_OnStartingWorm()
        {
            GameDatabase db = Database();
            Assert.IsNotNull(db, "Нет GameDatabase.asset");

            // С червём (наживка в начале игры) рыба клюёт сразу, куда бы ни отплыл днём.
            foreach (ZoneId zone in new[] { ZoneId.Dock, ZoneId.Bay, ZoneId.Pool })
            {
                float share = FishShare(db, zone, DayPhase.Morning, BaitType.Worm);
                Assert.Greater(share, 0.7f, $"{zone}, утро, червь: рыбы мало ({share:P0})");
            }
        }

        [Test]
        public void JunkAndScrap_NeverMoreThanFish()
        {
            GameDatabase db = Database();
            foreach (ZoneId zone in new[] { ZoneId.Dock, ZoneId.Bay, ZoneId.Pool, ZoneId.Reef })
            foreach (DayPhase phase in new[] { DayPhase.Morning, DayPhase.Day, DayPhase.Evening, DayPhase.Night })
            foreach (BaitType bait in new[] { BaitType.Worm, BaitType.Spoon, BaitType.Phosphor })
            {
                float share = FishShare(db, zone, phase, bait);
                if (share < 0f)
                    continue; // тут на эту наживку не клюёт ничего: игрок видит подсказку
                Assert.Greater(share, 0.6f, $"{zone}, {phase}, {bait}: рыбы {share:P0}, остальное мусор и железо");
            }
        }
    }
}
