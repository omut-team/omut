using System.Collections.Generic;
using Omut.Core;
using Omut.Fishing;
using Omut.Inventory;
using Omut.Player;
using Omut.World;
using UnityEditor;
using UnityEngine;

namespace Omut.EditorTools
{
    // Создаёт ассеты данных игры (рыбы, наживки, лодки, предметы...) из таблиц в этом файле.
    // Меню Unity:
    //   Omut > Создать данные игры                  — создаёт только то, чего ещё нет (правки в инспекторе не трогает);
    //   Omut > Обновить данные игры (перезаписать)  — перезаписывает значения из этого файла.
    // Таблицы повторяют docs/PLAN.md. Тексты: docs/texts/.
    public static partial class OmutDataBuilder
    {
        public const string DataRoot = "Assets/Data";

        [MenuItem("Omut/Создать данные игры")]
        public static void CreateMissing() => Build(false);

        [MenuItem("Omut/Обновить данные игры (перезаписать)")]
        public static void Overwrite() => Build(true);

        public static void Build(bool overwrite)
        {
            GameDatabase db = LoadOrCreate<GameDatabase>(DataRoot + "/GameDatabase.asset", out _);

            db.fish = BuildFish(overwrite);
            db.boats = new List<BoatData>
            {
                Boat("Barkas", "Старый баркас", 3f, 4f, 2f, 2, 4, 5, 2.5f, 1f, false, 0, 0, "boat_barkas", overwrite),
                Boat("Sledopyt", "Следопыт", 3.75f, 5f, 2.5f, 4, 5, 6, 3.5f, 1f, false, 1200, 3, "boat_sledopyt", overwrite),
                Boat("Okhotnik", "Охотник за тенями", 4.2f, 6f, 3f, 5, 6, 7, 4f, 0.6f, true, 3500, 6, "boat_okhotnik", overwrite),
            };
            db.items = new List<ItemData>
            {
                Item("kerosene", "Керосин", "Заправка фонаря на половину бака.", ItemKind.Kerosene, 12, 50, "item_kerosene", overwrite),
                Item("thermos", "Термос с чаем", "Горячий чай. Выпить (C): паника -35.", ItemKind.Thermos, 20, 1, "item_thermos", overwrite),
            };
            db.baits = new List<BaitData>
            {
                Bait(BaitType.Worm, "Червь", "Мирная рыба: карась, лещ, линь, карп, налим. Бесплатно, копаешь сам.", true, 0, 1, "item_worm", overwrite),
                Bait(BaitType.Spoon, "Блесна", "Хищники: окунь, щука, судак.", false, 25, 5, "item_spoon", overwrite),
                Bait(BaitType.Phosphor, "Фосфорная блесна", "Светится в темноте. Только ночные мутанты, днём бесполезна.", false, 60, 3, "item_phosphor_spoon", overwrite),
            };
            db.zones = new List<ZoneData>
            {
                ZoneAsset(ZoneId.Dock, "Тихий причал", "Хата деда, маяк, Тарас и Михалыч. В свете маяка безопасно даже ночью.", "безопасно", new Color(0.35f, 0.62f, 0.68f), false, overwrite),
                ZoneAsset(ZoneId.Bay, "Заросший залив", "Мелко, камыш и кувшинки, камней нет. Мирная рыба на червя.", "спокойно", new Color(0.4f, 0.68f, 0.66f), false, overwrite),
                ZoneAsset(ZoneId.Pool, "Глубокий омут", "Тёмная вода и коряги. Хищники на блесну, ночью мутанты.", "тревожно, коряги", new Color(0.18f, 0.36f, 0.48f), false, overwrite),
                ZoneAsset(ZoneId.Reef, "Маячный риф", "Скалы, затопленная биостанция и вечный туман. Сундучки деда и самые редкие рыбы.", "жутко, скалы, туман", new Color(0.25f, 0.32f, 0.36f), true, overwrite),
            };
            BuildMore(db, overwrite);

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("[OmutDataBuilder] Данные игры готовы: рыб " + db.fish.Count);
        }

        // Остальные данные добавляются на следующих этапах (partial-методы в других файлах).
        static partial void BuildMore(GameDatabase db, bool overwrite);

        // ---------- рыбы (таблица из docs/PLAN.md) ----------
        private struct FishRow
        {
            public string id, name, sprite, text;
            public int number;
            public float min, max, price, minCast, strength;
            public Rarity rarity;
            public FishTime time;
            public ZoneMask zones;
            public BaitType bait;
            public FishBehaviour behaviour;
            public int w, h;
            public bool chest, boss;
        }

        private static FishRow Row(int number, string id, string name, float min, float max, float price, Rarity rarity,
            FishTime time, ZoneMask zones, BaitType bait, FishBehaviour behaviour, float strength, int w, int h, string text)
        {
            return new FishRow
            {
                number = number, id = id, name = name, min = min, max = max, price = price, rarity = rarity, time = time,
                zones = zones, bait = bait, behaviour = behaviour, strength = strength, w = w, h = h, text = text,
                sprite = "fish_" + id
            };
        }

        private static readonly ZoneMask Everywhere = ZoneMask.All;

        private static List<FishRow> FishTable()
        {
            var rows = new List<FishRow>
            {
                Row(1, "crucian", "Карась", 0.4f, 1.2f, 12, Rarity.Common, FishTime.Day, ZoneMask.Bay, BaitType.Worm, FishBehaviour.Steady, 0.15f, 1, 1,
                    "Золотистый и упрямый. Дед говорил: карась в заливе есть всегда, даже когда больше никого нет."),
                Row(2, "perch", "Окунь", 0.5f, 1.8f, 15, Rarity.Common, FishTime.Day, Everywhere, BaitType.Spoon, FishBehaviour.Steady, 0.25f, 1, 1,
                    "Полосатый разбойник. Хватает блесну где угодно, лишь бы светло."),
                Row(3, "bream", "Лещ", 1.0f, 3.2f, 14, Rarity.Common, FishTime.Day, ZoneMask.Bay, BaitType.Worm, FishBehaviour.Steady, 0.3f, 2, 1,
                    "Широкий, как лопата. Ходит стаями по мелководью и любит червя."),
                Row(4, "tench", "Линь", 1.5f, 4.0f, 22, Rarity.Uncommon, FishTime.Day, ZoneMask.Bay, BaitType.Worm, FishBehaviour.Heavy, 0.45f, 2, 1,
                    "Скользкий, в зелёной слизи. Тянет тяжело и молча, как бревно."),
                Row(5, "carp", "Карп", 2.0f, 6.5f, 20, Rarity.Uncommon, FishTime.Day, ZoneMask.Bay, BaitType.Worm, FishBehaviour.Heavy, 0.55f, 2, 2,
                    "Толстый и сильный. Если повело катушку, держи крепче."),
                Row(6, "pike", "Щука", 3.0f, 8.5f, 25, Rarity.Uncommon, FishTime.Day, ZoneMask.Pool, BaitType.Spoon, FishBehaviour.Jerk, 0.55f, 3, 1,
                    "Хищница омута. Бьёт резко, рывками, может перекусить леску."),
                Row(7, "zander", "Судак", 2.5f, 7.0f, 30, Rarity.Uncommon, FishTime.Day | FishTime.Dusk, ZoneMask.Pool, BaitType.Spoon, FishBehaviour.Jerk, 0.5f, 3, 1,
                    "Стоит глубоко и далеко от лодки. Берёт только на дальний заброс, днём и в сумерках."),
                Row(8, "burbot", "Налим", 2.0f, 5.5f, 18, Rarity.Uncommon, FishTime.Night, ZoneMask.Pool, BaitType.Worm, FishBehaviour.Steady, 0.4f, 2, 1,
                    "Ночная рыба, холодная, как камень со дна. Днём его не найти."),
                Row(9, "glow_eel", "Светящийся угорь", 3.0f, 7.2f, 40, Rarity.Rare, FishTime.Night, ZoneMask.Pool, BaitType.Phosphor, FishBehaviour.Reverse, 0.6f, 3, 1,
                    "Светится зелёным изнутри. Тянет наоборот: чем сильнее держишь, тем легче ему уйти."),
                Row(10, "twohead_perch", "Двухголовый окунь", 1.0f, 2.5f, 60, Rarity.Rare, FishTime.Night, ZoneMask.Reef, BaitType.Phosphor, FishBehaviour.Jerk, 0.5f, 1, 1,
                    "Две головы, и обе голодные. Такого в Полесье не было, пока не построили станцию."),
                Row(11, "pale_blindfish", "Бледный слепыш", 4.0f, 9.0f, 35, Rarity.Rare, FishTime.Night, ZoneMask.Reef, BaitType.Phosphor, FishBehaviour.Reverse, 0.65f, 2, 2,
                    "Белый, без глаз. Живёт у затопленной биостанции и чувствует свет."),
                Row(12, "black_catfish", "Чёрный сом", 5.0f, 14.0f, 30, Rarity.Rare, FishTime.Night, ZoneMask.Pool, BaitType.Phosphor, FishBehaviour.Heavy, 0.75f, 3, 2,
                    "Огромный и чёрный, как ночная вода. Старики говорят, что это слуга Вадзяніка."),
                Row(13, "grandpa_chest", "Сундучок деда", 10f, 25f, 0, Rarity.Epic, FishTime.Night, ZoneMask.Reef, BaitType.Any, FishBehaviour.Heavy, 0.6f, 2, 2,
                    "Железный сундучок Язэпа, весь в тине. Внутри страница из его дневника."),
                Row(14, "lake_master", "Дед-сом, Хозяин Омута", 25f, 45f, 0, Rarity.Legendary, FishTime.Night, ZoneMask.Pool, BaitType.Phosphor, FishBehaviour.All, 0.95f, 6, 3,
                    "Тот, кого боится всё озеро. В трюм не помещается. Говорят, он помнит деда Язэпа."),
                Row(0, "scrap", "Металлолом", 1f, 4f, 3, Rarity.Uncommon, FishTime.Always, ZoneMask.Pool | ZoneMask.Reef, BaitType.Any, FishBehaviour.Heavy, 0.3f, 1, 1,
                    "Ржавое железо со дна. Михалыч берёт его для ремонта лодки."),
            };

            FishRow chest = rows[12];
            chest.chest = true;
            rows[12] = chest;

            FishRow boss = rows[13];
            boss.boss = true;
            rows[13] = boss;

            FishRow scrap = rows[14];
            scrap.sprite = "item_scrap";
            rows[14] = scrap;

            FishRow zander = rows[6];
            zander.minCast = 5f;
            rows[6] = zander;

            return rows;
        }

        private static List<FishData> BuildFish(bool overwrite)
        {
            var list = new List<FishData>();
            foreach (FishRow row in FishTable())
            {
                FishData fish = LoadOrCreate<FishData>($"{DataRoot}/Fish/{row.id}.asset", out bool created);
                if (created || overwrite)
                {
                    fish.id = row.id;
                    fish.journalNumber = row.number;
                    fish.displayName = row.name;
                    fish.description = row.text;
                    fish.sprite = FindSprite(row.sprite);
                    fish.minWeight = row.min;
                    fish.maxWeight = row.max;
                    fish.pricePerKg = row.price;
                    fish.rarity = row.rarity;
                    fish.time = row.time;
                    fish.zones = row.zones;
                    fish.bait = row.bait;
                    fish.minCastDistance = row.minCast;
                    fish.behaviour = row.behaviour;
                    fish.strength = row.strength;
                    fish.holdSize = new Vector2Int(row.w, row.h);
                    fish.isChest = row.chest;
                    fish.isBoss = row.boss;
                    EditorUtility.SetDirty(fish);
                }

                list.Add(fish);
            }

            return list;
        }

        private static ZoneData ZoneAsset(ZoneId id, string title, string text, string danger, Color color, bool fog, bool overwrite)
        {
            ZoneData zone = LoadOrCreate<ZoneData>($"{DataRoot}/Zones/{id}.asset", out bool created);
            if (created || overwrite)
            {
                zone.id = id;
                zone.displayName = title;
                zone.description = text;
                zone.danger = danger;
                zone.mapColor = color;
                zone.alwaysFoggy = fog;
                EditorUtility.SetDirty(zone);
            }

            return zone;
        }

        private static BoatData Boat(string id, string title, float speed, float accel, float decel, int durability, int w, int h,
            float light, float panic, bool sonar, int price, int scrap, string sprite, bool overwrite)
        {
            BoatData boat = LoadOrCreate<BoatData>($"{DataRoot}/Boats/{id}.asset", out bool created);
            if (created || overwrite)
            {
                boat.displayName = title;
                boat.maxSpeed = speed;
                boat.acceleration = accel;
                boat.deceleration = decel;
                boat.durability = durability;
                boat.holdWidth = w;
                boat.holdHeight = h;
                boat.lightRadius = light;
                boat.panicMultiplier = panic;
                boat.hasSonar = sonar;
                boat.price = price;
                boat.scrapPrice = scrap;
                boat.sprite = FindSprite(sprite);
                EditorUtility.SetDirty(boat);
            }

            return boat;
        }

        private static BaitData Bait(BaitType type, string title, string text, bool infinite, int price, int pack, string sprite, bool overwrite)
        {
            BaitData bait = LoadOrCreate<BaitData>($"{DataRoot}/Baits/{type.ToString().ToLower()}.asset", out bool created);
            if (created || overwrite)
            {
                bait.type = type;
                bait.displayName = title;
                bait.description = text;
                bait.infinite = infinite;
                bait.packPrice = price;
                bait.packSize = pack;
                bait.sprite = FindSprite(sprite);
                EditorUtility.SetDirty(bait);
            }

            return bait;
        }

        private static ItemData Item(string id, string title, string text, ItemKind kind, int price, int amount, string sprite, bool overwrite)
        {
            ItemData item = LoadOrCreate<ItemData>($"{DataRoot}/Items/{id}.asset", out bool created);
            if (created || overwrite)
            {
                item.id = id;
                item.displayName = title;
                item.description = text;
                item.kind = kind;
                item.price = price;
                item.amount = amount;
                item.sprite = FindSprite(sprite);
                EditorUtility.SetDirty(item);
            }

            return item;
        }

        // ---------- помощники ----------

        public static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (asset != null)
                return asset;

            string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(folder);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        // Найти спрайт по имени файла. Настоящий арт (не из Placeholders) важнее заглушки.
        public static Sprite FindSprite(string fileName)
        {
            Sprite placeholder = null;
            foreach (string guid in AssetDatabase.FindAssets(fileName + " t:Sprite", new[] { "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != fileName)
                    continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (!path.Contains("/Placeholders/"))
                    return sprite;
                placeholder = sprite;
            }

            if (placeholder == null)
                Debug.LogWarning("[OmutDataBuilder] Нет картинки " + fileName);
            return placeholder;
        }
    }
}
