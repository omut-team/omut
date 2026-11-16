using System.Collections.Generic;
using Omut.World;
using UnityEditor;
using UnityEngine;

namespace Omut.EditorTools
{
    // Мир из рисованной карты Ульяны: картинка на весь мир и берег по маске воды.
    // Маску делает tools/make_world_map.py: ~ вода, # суша, r скалы рифа, x коряги в омуте.
    public static partial class OmutSceneBuilder
    {
        public const string MaskPath = "Assets/Art/Map/world_mask.txt";

        private static char[,] mask;   // [столбец, строка], строка 0 = верх карты
        private static float cell = 0.5f;

        private static void LoadMask()
        {
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(MaskPath);
            string[] lines = text.text.Replace("\r", "").Split('\n');
            string[] head = lines[0].Split(' ');
            int cols = int.Parse(head[0]);
            int rows = int.Parse(head[1]);
            cell = float.Parse(head[2], System.Globalization.CultureInfo.InvariantCulture);
            mask = new char[cols, rows];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    mask[c, r] = lines[r + 1][c];
        }

        private static Vector2 CellCenter(int c, int r) =>
            new Vector2(Lake.xMin + (c + 0.5f) * cell, Lake.yMax - (r + 0.5f) * cell);

        private static bool Walkable(int c, int r) => mask[c, r] == '#' && Village.Contains(CellCenter(c, r));

        // Прямоугольники из клеток, где test = true (жадно: вправо, потом вниз). Так коллайдеров в разы меньше.
        private static List<Rect> Rects(System.Func<int, int, bool> test)
        {
            int cols = mask.GetLength(0), rows = mask.GetLength(1);
            var used = new bool[cols, rows];
            var result = new List<Rect>();
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (used[c, r] || !test(c, r))
                    continue;
                int w = 1;
                while (c + w < cols && !used[c + w, r] && test(c + w, r)) w++;
                int h = 1;
                while (r + h < rows)
                {
                    bool full = true;
                    for (int k = 0; k < w && full; k++)
                        full = !used[c + k, r + h] && test(c + k, r + h);
                    if (!full) break;
                    h++;
                }

                for (int y = r; y < r + h; y++)
                for (int x = c; x < c + w; x++)
                    used[x, y] = true;
                result.Add(new Rect(Lake.xMin + c * cell, Lake.yMax - (r + h) * cell, w * cell, h * cell));
            }

            return result;
        }

        private static void BuildLakeFromMap(Transform world)
        {
            LoadMask();
            Plain(world, "LakeMap", "world_map", Lake.center, -40);

            Composite(world, "Shore", (c, r) => mask[c, r] == '#' && !Walkable(c, r), false);
            Composite(world, "ReefRocks", (c, r) => mask[c, r] == 'r', true);
            Composite(world, "Snags", (c, r) => mask[c, r] == 'x', true);
        }

        // Берег одним составным коллайдером. hazard = удар о него ломает лодку (скалы, коряги).
        private static void Composite(Transform parent, string name, System.Func<int, int, bool> test, bool hazard)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            foreach (Rect rect in Rects(test))
            {
                var box = go.AddComponent<BoxCollider2D>();
                box.offset = rect.center;
                box.size = rect.size;
                box.compositeOperation = Collider2D.CompositeOperation.Merge;
            }

            if (hazard)
                go.AddComponent<Hazard>();
        }

        // Земля и мостки деревни: по ним ходит Сымон (WalkableGround), лодку они не пускают.
        private static void BuildWalkable(Transform dock)
        {
            var root = new GameObject("Walkable").transform;
            root.SetParent(dock, false);
            int i = 0;
            foreach (Rect rect in Rects(Walkable))
            {
                var go = new GameObject("Ground " + i++);
                go.transform.SetParent(root, false);
                go.transform.position = rect.center;
                go.AddComponent<BoxCollider2D>().size = rect.size;
                go.AddComponent<WalkableGround>();
            }
        }

        // Невидимая стена у основания нарисованного дома.
        private static void Building(Transform dock, string name, Vector2 basePosition, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(dock, false);
            go.transform.position = basePosition;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.offset = new Vector2(0f, size.y * 0.5f);
        }
    }
}
