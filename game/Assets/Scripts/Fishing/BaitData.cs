using UnityEngine;

namespace Omut.Fishing
{
    // Наживка. ScriptableObject, лежит в Data/Baits. Три штуки: червь, блесна, фосфорная блесна.
    [CreateAssetMenu(fileName = "NewBait", menuName = "Omut/Bait Data")]
    public class BaitData : ScriptableObject
    {
        public BaitType type = BaitType.Worm;
        public string displayName = "Червь";
        [TextArea(2, 4)] public string description = "";
        public Sprite sprite;

        [Tooltip("Бесконечная (червей можно накопать сколько угодно)")]
        public bool infinite;

        [Tooltip("Цена одной упаковки у Тараса")]
        [Min(0)] public int packPrice = 25;

        [Tooltip("Сколько штук в упаковке")]
        [Min(1)] public int packSize = 5;

        [Tooltip("Множитель ожидания поклёвки: 0.6 = клюёт быстрее")]
        public float biteSpeed = 1f;

        [Tooltip("Любимая рыба (id) и шанс, что клюнет именно она")]
        public string favoriteFish = "";
        [Range(0f, 1f)] public float favoriteChance;
    }
}
