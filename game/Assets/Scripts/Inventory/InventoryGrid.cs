using System;
using System.Collections.Generic;
using Omut.Fishing;
using UnityEngine;

namespace Omut.Inventory
{
    // Рыба в трюме: где лежит (левая верхняя клетка), какого размера, повёрнута ли.
    [Serializable]
    public class GridItem
    {
        public CatchInfo catchInfo;
        public Vector2Int position;
        public Vector2Int baseSize = Vector2Int.one;
        public bool rotated;

        // Размер с учётом поворота: повёрнутая рыба 3x1 занимает 1x3.
        public Vector2Int Size => rotated ? new Vector2Int(baseSize.y, baseSize.x) : baseSize;

        public RectInt Rect => new RectInt(position, Size);

        public bool Covers(Vector2Int cell) => Rect.Contains(cell);
    }

    // Трюм-сетка (баркас 4x5). Рыбы-прямоугольники разного размера, их можно двигать и поворачивать.
    // Клетка (0,0) слева сверху, x вправо, y вниз. Обычный C#-класс, проверяется тестами (InventoryGridTests).
    public class InventoryGrid
    {
        private readonly List<GridItem> items = new List<GridItem>();

        public int Width { get; private set; }
        public int Height { get; private set; }
        public IReadOnlyList<GridItem> Items => items;
        public int Count => items.Count;

        public event Action Changed;

        public InventoryGrid(int width, int height)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
        }

        public bool InBounds(Vector2Int position, Vector2Int size)
        {
            return position.x >= 0 && position.y >= 0 && position.x + size.x <= Width && position.y + size.y <= Height;
        }

        // Можно ли положить предмет размера size в клетку position (ignore: сам двигаемый предмет).
        public bool CanPlace(Vector2Int position, Vector2Int size, GridItem ignore = null)
        {
            if (!InBounds(position, size))
                return false;

            var rect = new RectInt(position, size);
            foreach (GridItem item in items)
            {
                if (item != ignore && item.Rect.Overlaps(rect))
                    return false;
            }

            return true;
        }

        public GridItem ItemAt(Vector2Int cell)
        {
            foreach (GridItem item in items)
            {
                if (item.Covers(cell))
                    return item;
            }

            return null;
        }

        // Положить улов в первое свободное место (сверху вниз, слева направо, можно повернуть).
        // null = не помещается.
        public GridItem TryAdd(CatchInfo info)
        {
            if (info == null || info.fish == null || info.fish.isBoss)
                return null;

            var item = new GridItem { catchInfo = info, baseSize = info.fish.holdSize };
            if (!FindFreeSpot(item, out Vector2Int position, out bool rotated))
                return null;

            item.position = position;
            item.rotated = rotated;
            items.Add(item);
            Changed?.Invoke();
            return item;
        }

        public bool FindFreeSpot(GridItem item, out Vector2Int position, out bool rotated)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                rotated = pass == 1;
                Vector2Int size = rotated ? new Vector2Int(item.baseSize.y, item.baseSize.x) : item.baseSize;
                for (int y = 0; y <= Height - size.y; y++)
                {
                    for (int x = 0; x <= Width - size.x; x++)
                    {
                        position = new Vector2Int(x, y);
                        if (CanPlace(position, size, item))
                            return true;
                    }
                }
            }

            position = Vector2Int.zero;
            rotated = false;
            return false;
        }

        // Переложить предмет (перетаскивание мышью). false = туда нельзя, предмет остаётся на месте.
        public bool TryMove(GridItem item, Vector2Int position, bool rotated)
        {
            if (!items.Contains(item))
                return false;

            Vector2Int size = rotated ? new Vector2Int(item.baseSize.y, item.baseSize.x) : item.baseSize;
            if (!CanPlace(position, size, item))
                return false;

            item.position = position;
            item.rotated = rotated;
            Changed?.Invoke();
            return true;
        }

        public bool Remove(GridItem item)
        {
            bool removed = items.Remove(item);
            if (removed)
                Changed?.Invoke();
            return removed;
        }

        // Убрать всё, что подходит под условие (например, весь ночной улов при обмороке).
        public List<GridItem> RemoveWhere(Predicate<GridItem> match)
        {
            List<GridItem> removed = items.FindAll(match);
            if (removed.Count > 0)
            {
                items.RemoveAll(match);
                Changed?.Invoke();
            }

            return removed;
        }

        public void Clear()
        {
            items.Clear();
            Changed?.Invoke();
        }

        // Сменили лодку: новый размер трюма. Что не влезло, возвращается списком.
        public List<GridItem> Resize(int width, int height)
        {
            List<GridItem> old = new List<GridItem>(items);
            items.Clear();
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);

            var overflow = new List<GridItem>();
            foreach (GridItem item in old)
            {
                if (CanPlace(item.position, item.Size))
                {
                    items.Add(item);
                }
                else if (FindFreeSpot(item, out Vector2Int position, out bool rotated))
                {
                    item.position = position;
                    item.rotated = rotated;
                    items.Add(item);
                }
                else
                {
                    overflow.Add(item);
                }
            }

            Changed?.Invoke();
            return overflow;
        }

        // Положить готовый предмет (при загрузке сохранения). false = не влез.
        public bool Restore(GridItem item)
        {
            if (item?.catchInfo == null || !CanPlace(item.position, item.Size))
                return false;

            items.Add(item);
            Changed?.Invoke();
            return true;
        }

        public int TotalPrice
        {
            get
            {
                int sum = 0;
                foreach (GridItem item in items)
                    sum += item.catchInfo.price;
                return sum;
            }
        }

        public int CountOf(string fishId) => items.FindAll(i => i.catchInfo.fishId == fishId).Count;

        public int FreeCells
        {
            get
            {
                int used = 0;
                foreach (GridItem item in items)
                    used += item.Size.x * item.Size.y;
                return Width * Height - used;
            }
        }
    }
}
