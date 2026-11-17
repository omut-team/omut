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

        // Заборы и перила с карты Ульяны: линии по низу столбиков. Сымон через них не ходит.
        // Координаты сняты с картинки (наложение линий проверено на world_map.png).
        private static readonly Vector2[][] Fences =
        {
            // Длинный забор вдоль дороги от хаты, за ним луг и ручей.
            new[] { V(-31.2f, -18.4f), V(-30.1f, -18.9f), V(-28.7f, -19.5f), V(-27.1f, -20.1f), V(-25.6f, -20.5f), V(-24.0f, -21.05f), V(-22.5f, -21.5f),
                    V(-21.5f, -22.35f), V(-19.9f, -22.9f), V(-18.0f, -23.4f), V(-16.05f, -23.85f), V(-13.4f, -24.2f), V(-10.9f, -24.5f),
                    V(-8.3f, -24.8f), V(-5.9f, -25.0f), V(-5.9f, -28.3f) },
            // Забор у клумбы с ёлками.
            new[] { V(-13.9f, -21.7f), V(-12.5f, -22.1f), V(-11.4f, -22.7f), V(-8.3f, -22.8f), V(-5.9f, -22.8f), V(-4.7f, -22.3f), V(-4.65f, -21.2f) },
            // Забор за сараем.
            new[] { V(-15.1f, -14.2f), V(-12.0f, -14.1f) },
            // Заборчик на травке у мостков.
            new[] { V(-1.5f, -19.7f), V(0.2f, -19.7f), V(0.7f, -20.5f) },
            // Перила от мостков вниз к лугу и дальше вдоль дороги.
            new[] { V(3.4f, -20.65f), V(2.3f, -21.6f), V(0.8f, -22.5f), V(-1.0f, -23.4f), V(-1.4f, -25.8f), V(-1.4f, -28.3f) },
            // Южный край нижних мостков и перила дорожки к маяку.
            new[] { V(3.4f, -20.7f), V(13.6f, -20.85f), V(15.3f, -20.6f), V(17.0f, -19.0f), V(19.1f, -18.0f), V(21.0f, -17.55f), V(22.7f, -17.1f),
                    V(24.8f, -16.6f), V(27.0f, -16.1f), V(28.6f, -15.9f) },
            // Перила верхних мостков, карман с лодкой, северные перила дорожки к маяку, угол с фонарём.
            new[] { V(-0.6f, -13.6f), V(11.0f, -13.6f), V(11.0f, -15.15f), V(4.6f, -15.15f), V(3.2f, -16.0f), V(2.6f, -17.3f), V(2.8f, -18.7f),
                    V(3.3f, -19.4f), V(11.8f, -19.35f), V(12.6f, -18.65f), V(13.85f, -17.85f), V(15.3f, -17.35f), V(17.2f, -16.7f),
                    V(16.4f, -15.9f), V(16.6f, -15.2f), V(18.8f, -15.2f) },
        };

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static void BuildFences(Transform dock)
        {
            var root = new GameObject("Fences").transform;
            root.SetParent(dock, false);
            for (int i = 0; i < Fences.Length; i++)
            {
                var go = new GameObject("Fence " + i);
                go.transform.SetParent(root, false);
                go.AddComponent<EdgeCollider2D>().points = Fences[i];
            }

            // Стол с ящиками и фонарь у клумбы.
            Building(dock, "Table", new Vector2(-8.0f, -20.5f), new Vector2(4.0f, 1.6f));
            Building(dock, "StreetLamp", new Vector2(-5.2f, -20.5f), new Vector2(0.5f, 0.3f));
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
