using NUnit.Framework;
using Omut.Fishing;

namespace Omut.Tests
{
    // Автотесты коробки со снастями: червь бесконечный, блёсны кончаются, Q листает только то, что есть.
    public class TackleBoxTests
    {
        [Test]
        public void StartsWithWorm()
        {
            var box = new TackleBox();

            Assert.AreEqual(BaitType.Worm, box.Current);
            Assert.IsTrue(box.Has(BaitType.Worm));
            Assert.IsFalse(box.Has(BaitType.Spoon));
        }

        [Test]
        public void Worm_NeverRunsOut()
        {
            var box = new TackleBox();

            for (int i = 0; i < 100; i++)
                box.Use(BaitType.Worm);

            Assert.IsTrue(box.Has(BaitType.Worm));
        }

        [Test]
        public void Next_SkipsMissingBaits()
        {
            var box = new TackleBox();
            box.Add(BaitType.Phosphor, 2);

            Assert.AreEqual(BaitType.Phosphor, box.Next());
            Assert.AreEqual(BaitType.Worm, box.Next());
        }

        [Test]
        public void Next_CyclesAllThree()
        {
            var box = new TackleBox();
            box.Add(BaitType.Spoon, 1);
            box.Add(BaitType.Phosphor, 1);

            Assert.AreEqual(BaitType.Spoon, box.Next());
            Assert.AreEqual(BaitType.Phosphor, box.Next());
            Assert.AreEqual(BaitType.Worm, box.Next());
        }

        [Test]
        public void Use_LastSpoon_SwitchesToWorm()
        {
            var box = new TackleBox();
            box.Add(BaitType.Spoon, 1);
            box.Select(BaitType.Spoon);

            Assert.IsTrue(box.Use(BaitType.Spoon));
            Assert.AreEqual(0, box.Count(BaitType.Spoon));
            Assert.AreEqual(BaitType.Worm, box.Current);
        }

        [Test]
        public void Select_Missing_Fails()
        {
            var box = new TackleBox();

            Assert.IsFalse(box.Select(BaitType.Phosphor));
            Assert.AreEqual(BaitType.Worm, box.Current);
        }

        [Test]
        public void Set_FromSave()
        {
            var box = new TackleBox();
            box.Set(BaitType.Spoon, 7);

            Assert.AreEqual(7, box.Count(BaitType.Spoon));
        }
    }
}
