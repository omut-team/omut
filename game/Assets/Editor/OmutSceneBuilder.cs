using Omut.Fishing;
using Omut.Player;
using Omut.UI;
using Omut.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Omut.EditorTools
{
    // Сборщик сцены Main. Меню Unity: Omut > Собрать сцену Main.
    // 1) создаёт недостающие данные (Omut > Создать данные игры);
    // 2) заново строит мир озера (объект World): вода четырёх зон, берег, причал, камни, коряги;
    // 3) добавляет системные объекты и интерфейс, если их ещё нет.
    // Можно запускать сколько угодно раз. Всё, что лежит внутри World, пересоздаётся,
    // поэтому руками внутри World ничего не меняйте (меняйте этот файл или ставьте объекты рядом).
    // Из командной строки:
    //   Unity.exe -batchmode -quit -projectPath game -executeMethod Omut.EditorTools.OmutSceneBuilder.BuildFromCommandLine
    public static partial class OmutSceneBuilder
    {
        public const string MainScenePath = "Assets/Scenes/Main.unity";

        // Размеры озера в клетках.
        public static readonly Rect Lake = new Rect(-50f, -32f, 100f, 64f);
        public static readonly Rect DockLand = new Rect(-50f, 24f, 16f, 8f);
        public static readonly Rect Pier = new Rect(-42f, 16f, 3f, 8f);
        public static readonly Vector2 BoatStart = new Vector2(-40.5f, 14.6f);

        public static readonly Rect DockZone = new Rect(-50f, 12f, 40f, 20f);
        public static readonly Rect BayZone = new Rect(-50f, -32f, 40f, 44f);
        public static readonly Rect PoolZone = new Rect(-10f, -32f, 32f, 64f);
        public static readonly Rect ReefZone = new Rect(22f, -32f, 28f, 64f);

        [MenuItem("Omut/Собрать сцену Main")]
        public static void BuildMainScene()
        {
            OmutDataBuilder.Build(false);
            ArtImportSettings.FixPivots();
            EnsureUnlitMaterial();

            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            RemoveOldPlaceholders();
            BuildWorld();
            SetupBoatAndCamera();
            BuildSystems();

            GameObject ui = EnsureRoot("UI");
            EnsureComponent<MainMenu>(ui);
            EnsureComponent<PauseMenu>(ui);
            EnsureComponent<Notifications>(ui);
            SetRef(EnsureComponent<CastPowerBar>(ui), "aimSprite", OmutDataBuilder.FindSprite("fx_soft_circle"));
            EnsureComponent<BitePrompt>(ui);
            EnsureComponent<TensionBar>(ui);
            EnsureComponent<CatchCard>(ui);
            BuildUi(ui);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[OmutSceneBuilder] Сцена Main собрана");
        }

        public static void BuildFromCommandLine()
        {
            OmutDataBuilder.Build(true);
            BuildMainScene();
        }

        public static Omut.Core.GameDatabase Database =>
            AssetDatabase.LoadAssetAtPath<Omut.Core.GameDatabase>(OmutDataBuilder.DataRoot + "/GameDatabase.asset");

        // Хуки для следующих этапов.
        static partial void SetupBoat(BoatController boat);
        static partial void BuildSystems();
        static partial void BuildUi(GameObject ui);
        static partial void BuildWorldExtras(Transform world, System.Random random);

        // Старое тестовое озеро с этапа 2 больше не нужно.
        private static void RemoveOldPlaceholders()
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name.StartsWith("_Placeholder_Lake"))
                    Object.DestroyImmediate(root);
            }
        }

        // ---------------- мир ----------------

        private static void BuildWorld()
        {
            GameObject old = GameObject.Find("World");
            if (old != null)
                Object.DestroyImmediate(old);

            Transform world = new GameObject("World").transform;
            var random = new System.Random(2409);

            // Земля вокруг озера (под водой), берег-граница для лодки.
            Tiled(world, "Ground", "tile_grass", Lake.center, Lake.size + new Vector2(40f, 30f), -40);
            var shore = new GameObject("Shore");
            shore.transform.SetParent(world, false);
            var edge = shore.AddComponent<EdgeCollider2D>();
            edge.points = new[]
            {
                new Vector2(Lake.xMin, Lake.yMin), new Vector2(Lake.xMin, Lake.yMax), new Vector2(Lake.xMax, Lake.yMax),
                new Vector2(Lake.xMax, Lake.yMin), new Vector2(Lake.xMin, Lake.yMin)
            };
            Tiled(world, "Sand", "tile_sand", Lake.center, Lake.size + new Vector2(2f, 2f), -35);

            BuildZone(world, ZoneId.Dock, DockZone, "tile_water_dock", 2);
            BuildZone(world, ZoneId.Bay, BayZone, "tile_water_bay", 6);
            BuildZone(world, ZoneId.Pool, PoolZone, "tile_water_deep", 5);
            BuildZone(world, ZoneId.Reef, ReefZone, "tile_water_reef", 4);

            BuildDock(world);
            BuildDecor(world, random);
            BuildWorldExtras(world, random);
        }

        private static void BuildZone(Transform world, ZoneId id, Rect area, string tile, int spots)
        {
            var go = new GameObject("Zone " + id);
            go.transform.SetParent(world, false);
            go.AddComponent<Zone>().Setup(Database.zones.Find(z => z.id == id), area);
            go.AddComponent<FishingSpotSpawner>().Setup(OmutDataBuilder.FindSprite("fx_ripple"), spots, 7f);

            Tiled(go.transform, "Water", tile, area.center, area.size, -30);
        }

        private static void BuildDock(Transform world)
        {
            Transform dock = new GameObject("Dock").transform;
            dock.SetParent(world, false);

            SpriteRenderer land = Tiled(dock, "Land", "tile_grass", DockLand.center, DockLand.size, -20);
            land.gameObject.AddComponent<BoxCollider2D>().size = DockLand.size;
            land.gameObject.AddComponent<WalkableGround>();

            SpriteRenderer pier = Tiled(dock, "Pier", "tile_dock", Pier.center, Pier.size, -19);
            pier.gameObject.AddComponent<BoxCollider2D>().size = Pier.size;
            pier.gameObject.AddComponent<WalkableGround>();

            Tiled(dock, "Path", "tile_dock", new Vector2(-40.5f, 26f), new Vector2(3f, 4f), -18);

            // Позиция = точка, где объект стоит на земле (низ картинки), вид 45°.
            Solid(dock, "Hut", "obj_hut", new Vector2(-46.5f, 28.25f), new Vector2(2.6f, 1.2f), new Vector2(0f, 0.6f));
            Solid(dock, "Garage", "obj_garage", new Vector2(-37f, 28.55f), new Vector2(2.6f, 1.2f), new Vector2(0f, 0.6f));
            Solid(dock, "Lighthouse", "obj_lighthouse", new Vector2(-33.5f, 24.5f), new Vector2(1f, 1f), new Vector2(0f, 0.5f));

            BuildDockPeople(dock);
        }

        public static readonly Vector2 MooringPoint = new Vector2(-40.5f, 14.6f);
        public static readonly Vector2 PierTip = new Vector2(-40.5f, 16.7f);
        public static readonly Vector2 HeroStart = new Vector2(-46.5f, 26.8f);
        public static readonly Vector2 TarasPosition = new Vector2(-35.4f, 24.6f);
        public static readonly Vector2 MikhalychPosition = new Vector2(-38.6f, 27.5f);

        // Все "стоящие" объекты на одном слое: сортировка по Y (вид 45°, Renderer2D: Custom Axis 0,1,0).
        public const int StandingOrder = 5;

        // NPC на причале с маленьким коллайдером у ног (герой их обходит).
        public static GameObject Npc(Transform parent, string name, string sprite, Vector2 position)
        {
            SpriteRenderer sr = Plain(parent, name, sprite, position, StandingOrder);
            var box = sr.gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 0.3f);
            box.offset = new Vector2(0f, 0.15f);
            return sr.gameObject;
        }

        private static void BuildDockPeople(Transform dock)
        {
            GameObject taras = Npc(dock, "Taras", "npc_taras", TarasPosition);
            taras.AddComponent<Omut.NPC.Merchant>().Setup(Omut.NPC.NpcId.Taras, "Тарас", OmutDataBuilder.FindSprite("npc_taras_portrait"),
                Database.FindDialogue("taras_first"),
                new System.Collections.Generic.List<string> { "Ну что, клюёт?", "Колокол сегодня в восемь, как всегда.", "Опять ты. Ладно, говори.", "Маяк горит, значит, всё в порядке." });
            BuildMoreDockPeople(dock);

            // Сымон пешком.
            SpriteRenderer hero = Plain(dock, "Hero", "hero_symon", HeroStart, StandingOrder);
            var walker = hero.gameObject.AddComponent<PlayerWalker>();
            SetRef(walker, "sprite", hero);

            // Место швартовки у конца мостков.
            var mooring = new GameObject("DockingZone");
            mooring.transform.SetParent(dock, false);
            mooring.transform.position = MooringPoint;
            var walkPoint = new GameObject("WalkPoint").transform;
            walkPoint.SetParent(mooring.transform, false);
            walkPoint.position = PierTip;
            var startPoint = new GameObject("StartPoint").transform;
            startPoint.SetParent(mooring.transform, false);
            startPoint.position = HeroStart;

            var zone = mooring.AddComponent<DockingZone>();
            SetRef(zone, "boat", Object.FindAnyObjectByType<BoatController>());
            SetRef(zone, "walker", walker);
            SetRef(zone, "cameraFollow", Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null);
            SetRef(zone, "walkPoint", walkPoint);
            SetRef(zone, "startPoint", startPoint);
        }

        static partial void BuildMoreDockPeople(Transform dock);

        // Камыш и кувшинки в заливе, коряги в омуте, скалы и биостанция на рифе.
        private static void BuildDecor(Transform world, System.Random random)
        {
            Transform decor = new GameObject("Decor").transform;
            decor.SetParent(world, false);

            for (int i = 0; i < 70; i++)
            {
                Vector2 p = RandomNearEdge(BayZone, 4f, random);
                Plain(decor, "Reed", "env_reed", p, StandingOrder);
            }

            for (int i = 0; i < 45; i++)
                Plain(decor, "Lily", "env_lily", RandomIn(BayZone, 3f, random), -10);

            Transform hazards = new GameObject("Hazards").transform;
            hazards.SetParent(world, false);

            for (int i = 0; i < 18; i++)
            {
                SpriteRenderer snag = Solid(hazards, "Snag", "obj_snag", RandomInPool(random), new Vector2(1.1f, 0.5f), new Vector2(0f, 0.25f));
                snag.gameObject.AddComponent<Hazard>();
            }

            for (int i = 0; i < 6; i++)
                Solid(hazards, "Rock", "obj_reef_rock", RandomInPool(random), new Vector2(0.8f, 0.6f), new Vector2(0f, 0.3f));

            for (int i = 0; i < 45; i++)
            {
                SpriteRenderer rock = Solid(hazards, "ReefRock", "obj_reef_rock", RandomIn(ReefZone, 2f, random), new Vector2(0.8f, 0.6f), new Vector2(0f, 0.3f));
                rock.gameObject.AddComponent<Hazard>();
            }

            Solid(hazards, "Biostation", "obj_biostation", new Vector2(40f, -3.5f), new Vector2(3.4f, 1.6f), new Vector2(0f, 0.8f));
        }

        private static Vector2 RandomIn(Rect area, float margin, System.Random random)
        {
            return new Vector2(
                Mathf.Lerp(area.xMin + margin, area.xMax - margin, (float)random.NextDouble()),
                Mathf.Lerp(area.yMin + margin, area.yMax - margin, (float)random.NextDouble()));
        }

        public static readonly Vector2 BossCenter = new Vector2(6f, 0f);

        // Точка в омуте, но не в водовороте Хозяина (там должна быть чистая вода).
        private static Vector2 RandomInPool(System.Random random)
        {
            Vector2 p;
            do p = RandomIn(PoolZone, 4f, random);
            while (Vector2.Distance(p, BossCenter) < 5f);
            return p;
        }

        private static Vector2 RandomNearEdge(Rect area, float depth, System.Random random)
        {
            Vector2 p = RandomIn(area, 0.5f, random);
            if (random.Next(2) == 0) p.x = area.xMin + 0.5f + (float)random.NextDouble() * depth;
            else p.y = area.yMin + 0.5f + (float)random.NextDouble() * depth;
            return p;
        }

        // ---------------- лодка и камера ----------------

        private static void SetupBoatAndCamera()
        {
            BoatController boat = Object.FindAnyObjectByType<BoatController>();
            if (boat != null)
            {
                boat.transform.position = BoatStart;
                boat.name = "Boat";

                FishingController fishing = EnsureComponent<FishingController>(boat.gameObject);
                SetRef(fishing, "database", Database);
                SetRef(fishing, "bobberSprite", OmutDataBuilder.FindSprite("obj_bobber"));
                SetRef(fishing, "biteSprite", OmutDataBuilder.FindSprite("fx_bite"));
                SetRef(fishing, "lineSprite", OmutDataBuilder.FindSprite("fx_white"));
                foreach (SpriteRenderer boatSprite in boat.GetComponentsInChildren<SpriteRenderer>())
                    boatSprite.sortingOrder = StandingOrder;
                var damage = EnsureComponent<BoatDamage>(boat.gameObject);
                SetRef(damage, "sprite", boat.GetComponentInChildren<SpriteRenderer>());
                SetupBoat(boat);
            }

            Camera cam = Camera.main;
            if (cam == null)
                return;

            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.1f, 0.16f, 0.12f);
            if (boat != null)
                cam.transform.position = new Vector3(BoatStart.x, BoatStart.y, -10f);

            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow != null)
            {
                var so = new SerializedObject(follow);
                so.FindProperty("boundsMin").vector2Value = Lake.min - new Vector2(4f, 4f);
                so.FindProperty("boundsMax").vector2Value = Lake.max + new Vector2(4f, 4f);
                if (boat != null)
                    so.FindProperty("target").objectReferenceValue = boat.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------------- помощники ----------------

        public static SpriteRenderer Tiled(Transform parent, string name, string sprite, Vector2 center, Vector2 size, int order)
        {
            SpriteRenderer sr = Plain(parent, name, sprite, center, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            return sr;
        }

        public static SpriteRenderer Plain(Transform parent, string name, string sprite, Vector2 position, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = OmutDataBuilder.FindSprite(sprite);
            sr.sortingOrder = order;
            return sr;
        }

        // Объект, в который упирается лодка (коллайдер у основания картинки).
        public static SpriteRenderer Solid(Transform parent, string name, string sprite, Vector2 position, Vector2 colliderSize, Vector2 colliderOffset)
        {
            SpriteRenderer sr = Plain(parent, name, sprite, position, StandingOrder);
            var box = sr.gameObject.AddComponent<BoxCollider2D>();
            box.size = colliderSize;
            box.offset = colliderOffset;
            return sr;
        }

        public static GameObject EnsureRoot(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
                go = new GameObject(name);
            return go;
        }

        public static GameObject EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
                return child.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        public static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        public static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[OmutSceneBuilder] Нет поля {field} у {target.GetType().Name}");
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
