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
        // Мир = рисованная карта озера Ульяны (Art/Map/world_map.png): 100 x 56.28 клеток.
        // Причал внизу, залив слева, омут справа (водоворот), риф с биостанцией наверху.
        public static readonly Rect Lake = new Rect(-50f, -28.14f, 100f, 56.28f);
        public static readonly Vector2 BoatStart = new Vector2(8.6f, -16.6f);

        public static readonly Rect DockZone = new Rect(-50f, -28.14f, 100f, 16.14f);
        public static readonly Rect BayZone = new Rect(-50f, -12f, 36f, 25f);
        public static readonly Rect PoolZone = new Rect(-14f, -12f, 64f, 25f);
        public static readonly Rect ReefZone = new Rect(-38f, 13f, 88f, 15.14f);

        // Где Сымон может ходить пешком: деревня у причала (суша и мостки на карте).
        public static readonly Rect Village = new Rect(-31f, -28f, 60f, 15.5f);

        [MenuItem("Omut/Собрать сцену Main")]
        public static void BuildMainScene()
        {
            OmutDataBuilder.Build(false);
            ArtImportSettings.FixPivots();
            ArtImportSettings.FixBorders();
            EnsureUiSkin();
            EnsureUnlitMaterial();

            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            RemoveOldPlaceholders();
            BuildWorld();
            SetupBoatAndCamera();
            BuildSystems();

            GameObject ui = EnsureRoot("UI");
            MainMenu menu = EnsureComponent<MainMenu>(ui);
            SetRef(menu, "background", OmutDataBuilder.FindSprite("ui_menu_bg"));
            SetRef(menu, "buttonSprite", OmutDataBuilder.FindSprite("ui_button_plank"));
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
            OmutVoiceExport.Export();
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

            // Картинка озера во весь мир и берег по маске воды (tools/make_world_map.py).
            BuildLakeFromMap(world);

            BuildZone(world, ZoneId.Dock, DockZone, 2);
            BuildZone(world, ZoneId.Bay, BayZone, 6);
            BuildZone(world, ZoneId.Pool, PoolZone, 5);
            BuildZone(world, ZoneId.Reef, ReefZone, 4);

            BuildDock(world);
            BuildDecor(world, random);
            BuildWorldExtras(world, random);
        }

        private static void BuildZone(Transform world, ZoneId id, Rect area, int spots)
        {
            var go = new GameObject("Zone " + id);
            go.transform.SetParent(world, false);
            go.AddComponent<Zone>().Setup(Database.zones.Find(z => z.id == id), area);
            go.AddComponent<FishingSpotSpawner>().Setup(OmutDataBuilder.FindSprite("fx_ripple"), spots, 7f);
        }

        private static void BuildDock(Transform world)
        {
            Transform dock = new GameObject("Dock").transform;
            dock.SetParent(world, false);

            // Земля и мостки, по которым ходит Сымон (из маски карты).
            BuildWalkable(dock);

            // Дома нарисованы на карте: здесь только невидимые стены у их основания, чтобы не ходить сквозь них.
            // Позиция = низ дома на карте, к ней привязан свет окон и маяка.
            Building(dock, "Hut", new Vector2(-23.6f, -17.7f), new Vector2(8.6f, 2.6f));
            Building(dock, "Shed", new Vector2(-17.3f, -17.1f), new Vector2(3.6f, 2.4f));
            Building(dock, "Garage", new Vector2(-6.0f, -17.0f), new Vector2(11f, 4.2f));
            Building(dock, "Lighthouse", new Vector2(24.2f, -15.0f), new Vector2(3.2f, 2.6f));
            Building(dock, "LighthouseHut", new Vector2(20.8f, -15.9f), new Vector2(4f, 3.4f));

            BuildDockPeople(dock);
        }

        // Точки на карте Ульяны: лодка в кармане между мостками, Тарас у маяка, Михалыч у мастерской.
        public static readonly Vector2 MooringPoint = new Vector2(8.6f, -16.6f);
        public static readonly Vector2 PierTip = new Vector2(8.6f, -19.5f);
        public static readonly Vector2 HeroStart = new Vector2(-21.3f, -18.8f);
        public static readonly Vector2 TarasPosition = new Vector2(20.0f, -17.3f);
        public static readonly Vector2 MikhalychPosition = new Vector2(-6.0f, -18.0f);
        public static readonly Vector2 YadvigaPosition = new Vector2(-15.5f, -21.0f);
        public static readonly Vector2 YazepPosition = new Vector2(12.5f, -19.4f);
        public static readonly Vector2 ChestPosition = new Vector2(-17.6f, -18.3f);

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

        // Камыш, кувшинки, коряги и скалы нарисованы на карте. Здесь только биостанция (метка для карты).
        private static void BuildDecor(Transform world, System.Random random)
        {
            Transform hazards = new GameObject("Hazards").transform;
            hazards.SetParent(world, false);
            var station = new GameObject("Biostation");
            station.transform.SetParent(hazards, false);
            station.transform.position = new Vector2(6.7f, 20.5f);
        }

        private static Vector2 RandomIn(Rect area, float margin, System.Random random)
        {
            return new Vector2(
                Mathf.Lerp(area.xMin + margin, area.xMax - margin, (float)random.NextDouble()),
                Mathf.Lerp(area.yMin + margin, area.yMax - margin, (float)random.NextDouble()));
        }

        public static readonly Vector2 BossCenter = new Vector2(34.2f, 6.0f); // нарисованный водоворот

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

            cam.orthographicSize = 7f; // мир по карте Ульяны: видно больше озера
            cam.backgroundColor = new Color(0.1f, 0.16f, 0.12f);
            if (boat != null)
                cam.transform.position = new Vector3(BoatStart.x, BoatStart.y, -10f);

            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow != null)
            {
                var so = new SerializedObject(follow);
                so.FindProperty("boundsMin").vector2Value = Lake.min;
                so.FindProperty("boundsMax").vector2Value = Lake.max;
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
