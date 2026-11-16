using UnityEngine;

namespace Omut.World
{
    // Описание зоны озера. ScriptableObject, лежит в Data/Zones.
    [CreateAssetMenu(fileName = "NewZone", menuName = "Omut/Zone Data")]
    public class ZoneData : ScriptableObject
    {
        public ZoneId id;
        public string displayName = "Зона";
        [TextArea(2, 4)] public string description = "";

        [Tooltip("Насколько опасно: показывается на карте")]
        public string danger = "безопасно";

        [Tooltip("Цвет зоны на карте (M)")]
        public Color mapColor = new Color(0.3f, 0.6f, 0.7f);

        [Tooltip("Вечный туман (риф)")]
        public bool alwaysFoggy;
    }
}
