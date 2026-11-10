using UnityEngine;

namespace Omut.Player
{
    // Характеристики лодки. Это ScriptableObject: данные лежат отдельным файлом-ассетом
    // (Data/Boats/Barkas.asset), и числа можно менять в инспекторе, не трогая код.
    // Три лодки игры (баркас, «Следопыт», «Охотник за тенями») = три таких ассета.
    [CreateAssetMenu(fileName = "NewBoat", menuName = "Omut/Boat Data")]
    public class BoatData : ScriptableObject
    {
        [Header("Название")]
        public string displayName = "Старый баркас";

        [Header("Движение (1 юнит = 1 клетка = 32 px)")]
        [Tooltip("Максимальная скорость, клеток в секунду")]
        [Min(0f)] public float maxSpeed = 3f;

        [Tooltip("Разгон: на сколько растёт скорость за секунду, пока жмём WASD")]
        [Min(0f)] public float acceleration = 4f;

        [Tooltip("Торможение: на сколько падает скорость за секунду, когда отпустили WASD (инерция)")]
        [Min(0f)] public float deceleration = 2f;

        [Header("Прочность (этап 14)")]
        [Tooltip("Сколько ударов о коряги выдержит лодка")]
        [Min(1)] public int durability = 2;

        [Header("Трюм (этап 9)")]
        [Min(1)] public int holdWidth = 4;
        [Min(1)] public int holdHeight = 5;

        [Header("Ночь (этапы 17-19)")]
        [Tooltip("Радиус света фонаря, клеток")]
        [Min(0f)] public float lightRadius = 2f;

        [Tooltip("Множитель роста паники: 1 = обычно, меньше 1 = паника растёт медленнее")]
        [Min(0f)] public float panicMultiplier = 1f;

        [Tooltip("Эхолот: видно рыбные места в тумане")]
        public bool hasSonar = false;
    }
}
