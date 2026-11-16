using System;
using UnityEngine;

namespace Omut.World
{
    // Четыре зоны озера.
    public enum ZoneId
    {
        Dock,   // Тихий причал
        Bay,    // Заросший залив
        Pool,   // Глубокий омут
        Reef    // Маячный риф и биостанция
    }

    // Набор зон для рыбы: где она водится. Можно несколько: Bay | Pool.
    [Flags]
    public enum ZoneMask
    {
        None = 0,
        Dock = 1,
        Bay = 2,
        Pool = 4,
        Reef = 8,
        All = Dock | Bay | Pool | Reef
    }

    // Участок озера. Прямоугольник area в мировых координатах (клетках).
    // По нему определяем, в какой зоне лодка, и где появляются рыбные места.
    public class Zone : MonoBehaviour
    {
        [SerializeField] private ZoneId id;

        [Tooltip("Название для игрока")]
        [SerializeField] private string displayName = "Зона";

        [Tooltip("Прямоугольник зоны: x, y левого нижнего угла, ширина, высота (в клетках)")]
        [SerializeField] private Rect area = new Rect(-10, -10, 20, 20);

        [Tooltip("Здесь всегда туман, даже днём (риф)")]
        [SerializeField] private bool alwaysFoggy;

        public ZoneId Id => id;
        public string DisplayName => displayName;
        public Rect Area => area;
        public bool AlwaysFoggy => alwaysFoggy;

        public void Setup(ZoneId zoneId, string zoneName, Rect zoneArea, bool foggy)
        {
            id = zoneId;
            displayName = zoneName;
            area = zoneArea;
            alwaysFoggy = foggy;
        }

        public bool Contains(Vector2 point) => area.Contains(point);

        public static ZoneMask ToMask(ZoneId zone) => (ZoneMask)(1 << (int)zone);

        // В какой зоне точка. Если ни в одной, считаем, что у причала.
        public static Zone Find(Vector2 point)
        {
            foreach (Zone zone in All)
            {
                if (zone.Contains(point))
                    return zone;
            }

            return null;
        }

        public static readonly System.Collections.Generic.List<Zone> All = new System.Collections.Generic.List<Zone>();

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(area.center, area.size);
        }
    }
}
