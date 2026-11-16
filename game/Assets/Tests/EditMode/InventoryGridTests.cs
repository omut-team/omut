using System.Collections.Generic;
using NUnit.Framework;
using Omut.Fishing;
using Omut.Inventory;
using Omut.World;
using UnityEngine;

namespace Omut.Tests
{
    // Автотесты трюма: сетка 4x5, рыбы разного размера, поворот, перемещение, выброс, смена лодки.
    public class InventoryGridTests
    {
        private readonly List<Object> created = new List<Object>();

        private CatchInfo Catch(int w, int h, bool night = false, string id = "fish")
        {
            var fish = ScriptableObject.CreateInstance<FishData>();
            fish.id = id;
            fish.holdSize = new Vector2Int(w, h);
            fish.pricePerKg = 10f;
            created.Add(fish);
            return new CatchInfo(fish, 1f, night, ZoneId.Bay, 1);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in created)
                Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void FirstFish_GoesToTopLeft()
        {
            var grid = new InventoryGrid(4, 5);

            GridItem item = grid.TryAdd(Catch(1, 1));

            Assert.AreEqual(Vector2Int.zero, item.position);
            Assert.AreEqual(1, grid.Count);
        }

        [Test]
        public void Items_DoNotOverlap()
        {
            var grid = new InventoryGrid(4, 5);

            GridItem a = grid.TryAdd(Catch(2, 2));
            GridItem b = grid.TryAdd(Catch(2, 2));

            Assert.IsFalse(a.Rect.Overlaps(b.Rect));
        }

        [Test]
        public void TooLong_IsRotatedToFit()
        {
            var grid = new InventoryGrid(2, 5);

            GridItem pike = grid.TryAdd(Catch(3, 1)); // 3 в ширину не влезает в 2, поворачиваем

            Assert.IsNotNull(pike);
            Assert.IsTrue(pike.rotated);
            Assert.AreEqual(new Vector2Int(1, 3), pike.Size);
        }

        [Test]
        public void FullHold_RejectsFish()
        {
            var grid = new InventoryGrid(4, 5);
            for (int i = 0; i < 20; i++)
                Assert.IsNotNull(grid.TryAdd(Catch(1, 1)));

            Assert.IsNull(grid.TryAdd(Catch(1, 1)));
            Assert.AreEqual(0, grid.FreeCells);
        }

        [Test]
        public void Move_ToFreePlace_Works()
        {
            var grid = new InventoryGrid(4, 5);
            GridItem item = grid.TryAdd(Catch(2, 1));

            Assert.IsTrue(grid.TryMove(item, new Vector2Int(2, 4), false));
            Assert.AreEqual(new Vector2Int(2, 4), item.position);
        }

        [Test]
        public void Move_OutOfBoundsOrOverlap_Fails()
        {
            var grid = new InventoryGrid(4, 5);
            GridItem a = grid.TryAdd(Catch(2, 1));
            GridItem b = grid.TryAdd(Catch(2, 1));

            Assert.IsFalse(grid.TryMove(a, new Vector2Int(3, 0), false)); // вылезает за край
            Assert.IsFalse(grid.TryMove(a, b.position, false));          // на другую рыбу
            Assert.AreEqual(Vector2Int.zero, a.position);
        }

        [Test]
        public void Rotate_InPlace()
        {
            var grid = new InventoryGrid(4, 5);
            GridItem pike = grid.TryAdd(Catch(3, 1));

            Assert.IsTrue(grid.TryMove(pike, Vector2Int.zero, true));
            Assert.AreEqual(new Vector2Int(1, 3), pike.Size);
        }

        [Test]
        public void ItemAt_FindsCoveringItem()
        {
            var grid = new InventoryGrid(4, 5);
            GridItem carp = grid.TryAdd(Catch(2, 2));

            Assert.AreEqual(carp, grid.ItemAt(new Vector2Int(1, 1)));
            Assert.IsNull(grid.ItemAt(new Vector2Int(3, 3)));
        }

        [Test]
        public void RemoveNightCatch_KeepsDayCatch()
        {
            var grid = new InventoryGrid(4, 5);
            grid.TryAdd(Catch(1, 1, false));
            grid.TryAdd(Catch(1, 1, true));
            grid.TryAdd(Catch(1, 1, true));

            List<GridItem> lost = grid.RemoveWhere(i => i.catchInfo.caughtAtNight);

            Assert.AreEqual(2, lost.Count);
            Assert.AreEqual(1, grid.Count);
        }

        [Test]
        public void Resize_Bigger_KeepsEverything()
        {
            var grid = new InventoryGrid(4, 5);
            for (int i = 0; i < 5; i++)
                grid.TryAdd(Catch(2, 1));

            List<GridItem> overflow = grid.Resize(5, 6);

            Assert.AreEqual(0, overflow.Count);
            Assert.AreEqual(5, grid.Count);
        }

        [Test]
        public void Resize_Smaller_ReturnsOverflow()
        {
            var grid = new InventoryGrid(4, 5);
            for (int i = 0; i < 20; i++)
                grid.TryAdd(Catch(1, 1));

            List<GridItem> overflow = grid.Resize(2, 2);

            Assert.AreEqual(4, grid.Count);
            Assert.AreEqual(16, overflow.Count);
        }

        [Test]
        public void BossDoesNotFit()
        {
            var grid = new InventoryGrid(6, 7);
            CatchInfo boss = Catch(6, 3);
            boss.fish.isBoss = true;

            Assert.IsNull(grid.TryAdd(boss));
        }

        [Test]
        public void TotalPrice_Sums()
        {
            var grid = new InventoryGrid(4, 5);
            grid.TryAdd(Catch(1, 1));        // 10
            grid.TryAdd(Catch(1, 1, true));  // 30

            Assert.AreEqual(40, grid.TotalPrice);
        }
    }
}
