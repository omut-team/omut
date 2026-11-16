using System.Collections.Generic;
using System.Text;
using Omut.Core;
using Omut.Fishing;
using Omut.Player;
using Omut.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Omut.UI
{
    // Карта озера (M): рисунок Ульяны, на нём зоны, маяк, биостанция, где сейчас лодка и Сымон.
    // Справа описание зон и какая рыба там уже поймана.
    public class MapUI : GameWindow
    {
        private const float MapWidth = 1000f;

        [Tooltip("Картинка карты (Art/Map/world_map). Совпадает с миром игры")]
        [SerializeField] private Sprite mapSprite;

        private RectTransform map;
        private RectTransform boatMarker;
        private RectTransform heroMarker;
        private Text zonesText;
        [Tooltip("Какую часть мира показывает картинка")]
        [SerializeField] private Rect world = new Rect(-50f, -28.14f, 100f, 56.28f);

        public void Setup(Sprite sprite, Rect worldRect)
        {
            mapSprite = sprite;
            world = worldRect;
        }
        private bool zonesBuilt;

        protected override Key ToggleKey => Controls.Map;

        protected override RectTransform Build(Transform canvas)
        {
            RectTransform paper = UiFactory.Window(canvas, "Map", new Vector2(1600f, 860f), "Карта озера", out _);

            Image frame = UiFactory.Panel(paper, "Map", mapSprite != null ? Color.white : new Color(0.35f, 0.45f, 0.3f));
            frame.sprite = mapSprite;
            map = frame.rectTransform;
            map.anchorMin = map.anchorMax = new Vector2(0f, 1f);
            map.pivot = new Vector2(0f, 1f);
            map.anchoredPosition = new Vector2(50f, -100f);
            map.sizeDelta = new Vector2(MapWidth, MapWidth * world.height / world.width);
            frame.gameObject.AddComponent<Outline>().effectColor = UiFactory.Theme.Ink;

            zonesText = UiFactory.Label(paper, "", 24, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            zonesText.supportRichText = true;
            UiFactory.Place(zonesText.rectTransform, new Vector2(1f, 1f), new Vector2(480f, 720f), new Vector2(-40f, -100f));

            return (RectTransform)paper.parent;
        }

        protected override bool CanOpenByKey() => true;

        // Перевести клетки мира в точку на карте.
        private Vector2 ToMap(Vector2 worldPoint)
        {
            float x = (worldPoint.x - world.xMin) / world.width * map.sizeDelta.x;
            float y = (worldPoint.y - world.yMin) / world.height * map.sizeDelta.y;
            return new Vector2(x, y - map.sizeDelta.y);
        }

        private RectTransform Box(string name, Rect area, Color color)
        {
            Image image = UiFactory.Panel(map, name, color);
            RectTransform r = image.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 0f);
            r.anchoredPosition = ToMap(area.min);
            r.sizeDelta = new Vector2(area.width / world.width * map.sizeDelta.x, area.height / world.height * map.sizeDelta.y);
            image.raycastTarget = false;
            return r;
        }

        private RectTransform Marker(string name, Vector2 worldPoint, Color color, float size, string label = null)
        {
            Image dot = UiFactory.Panel(map, name, color);
            RectTransform r = dot.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(size, size);
            r.anchoredPosition = ToMap(worldPoint);
            dot.gameObject.AddComponent<Outline>().effectColor = Color.black;
            if (label != null)
            {
                Text text = UiFactory.HudLabel(r, label, 20, TextAnchor.MiddleLeft);
                UiFactory.Place(text.rectTransform, new Vector2(1f, 0.5f), new Vector2(240f, 30f), new Vector2(240f + 8f, 0f));
            }

            return r;
        }

        private void BuildZones()
        {
            // Названия зон поверх рисунка (сами зоны на нём видны: залив, омут, риф, причал).
            foreach (Zone zone in Zone.All)
            {
                Vector2 center = zone.Id == ZoneId.Dock ? new Vector2(zone.Area.center.x, zone.Area.yMax - 2.5f)
                    : zone.Id == ZoneId.Reef ? new Vector2(-18f, zone.Area.center.y + 2f)
                    : zone.Area.center;
                Text name = UiFactory.HudLabel(map, zone.DisplayName, 26, TextAnchor.MiddleCenter);
                RectTransform r = name.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                r.sizeDelta = new Vector2(320f, 40f);
                r.anchoredPosition = ToMap(center);
                name.color = UiFactory.Theme.Gold;
                name.raycastTarget = false;
            }

            foreach (NightLight light in NightLight.All)
            {
                if (!light.IsSafeLight)
                    continue;
                Marker("Lighthouse", light.transform.position, UiFactory.Theme.Gold, 18f, "маяк: безопасный свет");
            }

            GameObject station = GameObject.Find("World/Hazards/Biostation");
            if (station != null)
                Marker("Biostation", station.transform.position, new Color(0.5f, 0.6f, 0.55f), 18f, "биостанция");

            boatMarker = Marker("Boat", Vector2.zero, UiFactory.Theme.Bad, 16f);
            heroMarker = Marker("Hero", Vector2.zero, new Color(0.3f, 0.8f, 0.3f), 14f);
            zonesBuilt = true;
        }

        protected override void Refresh()
        {
            if (!zonesBuilt)
                BuildZones();

            DockingZone dock = DockingZone.Instance;
            if (dock != null)
            {
                boatMarker.anchoredPosition = ToMap(dock.Boat.transform.position);
                heroMarker.gameObject.SetActive(PlayerWalker.OnFoot);
                heroMarker.anchoredPosition = ToMap(dock.Walker.transform.position);
            }

            zonesText.text = Describe();
        }

        protected override void Update()
        {
            base.Update();
            if (IsOpen)
            {
                // Маркер лодки мигает.
                boatMarker.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f));
            }
        }

        private static string Describe()
        {
            GameSession session = GameSession.Instance;
            var sb = new StringBuilder("<b>Красная точка: лодка. Зелёная: Сымон.</b>\n");

            var zones = new List<Zone>(Zone.All);
            zones.Sort((a, b) => a.Id.CompareTo(b.Id));
            foreach (Zone zone in zones)
            {
                sb.Append($"\n<b>{zone.DisplayName}</b>");
                if (zone.Data != null)
                    sb.Append($" ({zone.Data.danger})\n{zone.Data.description}");
                sb.Append('\n');

                if (session == null)
                    continue;

                var known = new List<string>();
                int unknown = 0;
                foreach (FishData fish in session.Database.JournalEntries())
                {
                    if (fish.isBoss || (fish.zones & Zone.ToMask(zone.Id)) == 0)
                        continue;
                    if (session.Journal.IsCaught(fish.id)) known.Add(fish.displayName);
                    else unknown++;
                }

                sb.Append("Рыба: ").Append(known.Count > 0 ? string.Join(", ", known) : "пока никого");
                if (unknown > 0)
                    sb.Append($", ещё ??? ({unknown})");
                sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}
