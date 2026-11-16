using Omut.Core;
using Omut.Player;
using Omut.World;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using Omut.Inventory;
using Omut.UI;
using UnityEngine;

namespace Omut.EditorTools
{
    // Системные объекты и окна интерфейса, которые добавляются в сцену по этапам.
    public static partial class OmutSceneBuilder
    {
        static partial void BuildSystems()
        {
            GameObject systems = EnsureRoot("Systems");
            SetRef(EnsureComponent<GameSession>(systems), "database", Database);

            FogController fog = EnsureComponent<FogController>(systems);
            SetRef(fog, "puffSprite", OmutDataBuilder.FindSprite("fx_fog_puff"));
            SetRef(fog, "holeSprite", OmutDataBuilder.FindSprite("fx_fog_hole"));
            SetRef(fog, "whiteSprite", OmutDataBuilder.FindSprite("fx_white"));
            SetRef(fog, "unlitMaterial", Database.unlitMaterial);

            EnsureComponent<ZoneTracker>(systems);
            SetupAudio(EnsureComponent<AudioManager>(systems));

            PanicEffects panic = EnsureComponent<PanicEffects>(systems);
            SetRef(panic, "silhouetteSprite", OmutDataBuilder.FindSprite("obj_reef_rock"));
            SetRef(panic, "unlitMaterial", Database.unlitMaterial);

            // Свет героя на причале ночью.
            Lantern lantern = Object.FindAnyObjectByType<Lantern>();
            GameObject heroLight = GameObject.Find("World/Dock/Hero/WalkerLight");
            if (lantern != null && heroLight != null)
                SetRef(lantern, "walkerLight", heroLight.GetComponent<Light2D>());

            // Свет суток всегда из кода (DayNightCycle.CreateDefault...), чтобы правки цветов попадали в сцену.
            var cycle = Object.FindAnyObjectByType<Omut.World.DayNightCycle>();
            if (cycle != null)
            {
                var so = new UnityEditor.SerializedObject(cycle);
                so.FindProperty("lightColor").gradientValue = Omut.World.DayNightCycle.CreateDefaultColors();
                so.FindProperty("lightIntensity").animationCurveValue = Omut.World.DayNightCycle.CreateDefaultIntensity();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // Звуки Лизы из Assets/Audio: sfx_<id>.wav и music_day/music_night. Чего нет, синтезируется кодом.
        private static void SetupAudio(AudioManager audio)
        {
            var so = new SerializedObject(audio);
            SerializedProperty list = so.FindProperty("clips");
            list.ClearArray();
            foreach (string id in ProceduralSounds.Ids)
            {
                AudioClip clip = FindClip("sfx_" + id);
                if (clip == null)
                    continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                SerializedProperty item = list.GetArrayElementAtIndex(list.arraySize - 1);
                item.FindPropertyRelative("id").stringValue = id;
                item.FindPropertyRelative("clip").objectReferenceValue = clip;
            }

            so.FindProperty("dayMusic").objectReferenceValue = FindClip("music_day");
            so.FindProperty("nightMusic").objectReferenceValue = FindClip("music_night");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioClip FindClip(string name)
        {
            foreach (string guid in AssetDatabase.FindAssets(name + " t:AudioClip", new[] { "Assets/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }

            return null;
        }

        public const string UnlitMaterialPath = "Assets/Art/Materials/SpriteUnlit.mat";

        // Материал спрайтов без освещения (туман, метки эхолота, значок поклёвки видны в темноте).
        private static void EnsureUnlitMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (material == null)
            {
                OmutDataBuilder.EnsureFolder("Assets/Art/Materials");
                material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(material, UnlitMaterialPath);
            }

            GameDatabase db = Database;
            db.unlitMaterial = material;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        public static Light2D PointLight(Transform parent, string name, Vector2 localPosition, float radius, float intensity, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.2f;
            light.intensity = intensity;
            light.color = color;
            light.falloffIntensity = 0.6f;
            return light;
        }

        // Свет на причале: маяк (безопасная зона), окна хаты, лампа гаража.
        static partial void BuildWorldExtras(Transform world, System.Random random)
        {
            Transform dock = world.Find("Dock");
            Transform lighthouse = dock.Find("Lighthouse");
            Light2D beacon = PointLight(lighthouse, "Beacon", new Vector2(0f, 3.3f), 11f, 1.3f, new Color(1f, 0.9f, 0.65f));
            beacon.gameObject.AddComponent<NightLight>().Setup(true, 9f, 0f);

            Light2D hut = PointLight(dock.Find("Hut"), "Windows", new Vector2(0f, 0.9f), 3f, 0.9f, new Color(1f, 0.75f, 0.45f));
            hut.gameObject.AddComponent<NightLight>().Setup(false, 0f, 0.2f);

            Light2D garage = PointLight(dock.Find("Garage"), "Lamp", new Vector2(0f, 1.6f), 2.5f, 0.8f, new Color(0.9f, 0.9f, 1f));
            garage.gameObject.AddComponent<NightLight>().Setup(false, 0f, 0.1f);

            // Водоворот Хозяина Омута в центре омута (появляется ночью после письма деда).
            var boss = new GameObject("BossWhirlpool");
            boss.transform.SetParent(world, false);
            boss.transform.position = new Vector3(6f, 0f, 0f);
            BossEncounter encounter = boss.AddComponent<BossEncounter>();
            SetRef(encounter, "rippleSprite", OmutDataBuilder.FindSprite("fx_ripple"));
            SetRef(encounter, "unlitMaterial", Database.unlitMaterial);

            SpriteRenderer chest = Solid(dock, "DockChest", "obj_dock_chest", new Vector2(-43.3f, 24.3f), new Vector2(1.3f, 0.5f), new Vector2(0f, 0.25f));
            SetRef(chest.gameObject.AddComponent<DockChest>(), "sprite", chest);

            Transform hero = dock.Find("Hero");
            Light2D heroLight = PointLight(hero, "WalkerLight", new Vector2(0f, 0.6f), 2.2f, 0.7f, new Color(1f, 0.85f, 0.6f));
            heroLight.enabled = false;
        }

        static partial void SetupBoat(BoatController boat)
        {
            Transform old = boat.transform.Find("LanternLight");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            // Опора лодки снизу (вид 45°): коллайдер по корпусу у воды, фонарь на палубе.
            var capsule = boat.GetComponent<CapsuleCollider2D>();
            if (capsule != null)
            {
                capsule.size = new Vector2(1.8f, 0.6f);
                capsule.offset = new Vector2(0f, 0.35f);
            }

            Light2D light = PointLight(boat.transform, "LanternLight", new Vector2(0.3f, 0.7f), 2.5f, 1.1f, new Color(1f, 0.85f, 0.55f));
            Lantern lantern = EnsureComponent<Lantern>(boat.gameObject);
            SetRef(lantern, "lanternLight", light);
        }

        static partial void BuildMoreDockPeople(Transform dock)
        {
            GameObject mikhalych = Npc(dock, "Mikhalych", "npc_mikhalych", MikhalychPosition);
            AddMikhalych(mikhalych).Setup(Omut.NPC.NpcId.Mikhalych, "Михалыч", OmutDataBuilder.FindSprite("npc_mikhalych_portrait"),
                Database.FindDialogue("mikhalych_first"),
                new System.Collections.Generic.List<string> { "Ну что, лодка цела?", "Железо привёз?", "Мотор слышу издалека. Заходи." });
        }

        private static Omut.NPC.Npc AddMikhalych(GameObject go) => go.AddComponent<Omut.NPC.Shipyard>();

        static partial void BuildUi(GameObject ui)
        {
            EnsureComponent<Hud>(ui);
            EnsureComponent<ShopUI>(ui);
            EnsureComponent<JournalUI>(ui);
            EnsureComponent<QuestLogUI>(ui);
            EnsureComponent<ShipyardUI>(ui);
            EnsureComponent<LoginUI>(ui);
            SetRef(EnsureComponent<PanicBar>(ui), "vignetteSprite", OmutDataBuilder.FindSprite("fx_vignette"));
            EnsureComponent<ScreenFader>(ui);
            EnsureComponent<MapUI>(ui);
            EnsureComponent<ZoneBanner>(ui);
            EnsureComponent<CreditsUI>(ui);
            SetRef(EnsureComponent<DialogueUI>(ui), "heroPortrait", OmutDataBuilder.FindSprite("hero_symon_portrait"));
            EnsureComponent<InventoryUI>(ui);
        }
    }
}
