using UnityEngine;

namespace Omut.Fishing
{
    // Удочка (рисунки Лизы, design/export/items). ScriptableObject, лежит в Data/Rods.
    // Удочка меняет рыбалку: дальше заброс, быстрее клюёт, легче тянуть, любимая добыча.
    [CreateAssetMenu(fileName = "NewRod", menuName = "Omut/Rod Data")]
    public class RodData : ScriptableObject
    {
        public string id = "bamboo";
        public string displayName = "Бамбуковая удочка";
        [TextArea(2, 4)] public string description = "";
        public Sprite sprite;

        [Tooltip("Цена у Тараса. 0 и inShop = false: не продаётся (стартовая или награда)")]
        [Min(0)] public int price;
        public bool inShop = true;

        [Tooltip("На сколько клеток дальше заброс")]
        public float castBonus;

        [Tooltip("Множитель ожидания поклёвки: 0.8 = на 20% быстрее")]
        public float biteSpeed = 1f;

        [Tooltip("Множитель силы рыбы при вываживании: 0.8 = тянуть на 20% легче")]
        public float pull = 1f;

        [Tooltip("Кто чаще клюёт на эту удочку (id рыбы) и с какой вероятностью")]
        public string favoriteFish = "";
        [Range(0f, 1f)] public float favoriteChance;
    }
}
