using System;
using Omut.World;
using UnityEngine;

namespace Omut.Fishing
{
    public enum Rarity
    {
        Common,     // обычная
        Uncommon,   // необычная
        Rare,       // редкая
        Epic,       // эпическая
        Legendary   // легендарная
    }

    // Какая наживка нужна. Any = клюёт на любую.
    public enum BaitType
    {
        Worm,       // червь: мирная рыба
        Spoon,      // блесна: хищники
        Phosphor,   // фосфорная блесна: только ночные мутанты
        Any,
        Maggot,     // опарыш (Лиза): как червь, но клюёт быстрее
        Dough,      // тесто с анисом: как червь, любимое у карпа
        Leech       // светящаяся пиявка: как фосфорная блесна, любимая у чёрного сома
    }

    // Когда клюёт. Можно несколько сразу: Day | Dusk.
    [Flags]
    public enum FishTime
    {
        Day = 1,    // утро и день, 06:00-17:00
        Dusk = 2,   // вечер (сумерки), 17:00-20:00
        Night = 4,  // ночь, 20:00-06:00
        Always = Day | Dusk | Night
    }

    // Как рыба сопротивляется при вываживании (паттерн Strategy, см. FishBehaviours.cs).
    public enum FishBehaviour
    {
        Steady,     // ровно
        Jerk,       // рывки
        Heavy,      // тяжело
        Reverse,    // обратная шкала: ЛКМ отпускает, ПКМ тянет
        All         // все виды по очереди (Хозяин Омута)
    }

    // Одна запись журнала: вид рыбы или находка. ScriptableObject, лежит в Data/Fish.
    [CreateAssetMenu(fileName = "NewFish", menuName = "Omut/Fish Data")]
    public class FishData : ScriptableObject
    {
        [Header("Журнал")]
        [Tooltip("Уникальный id латиницей, по нему работают сохранения: crucian, pike")]
        public string id = "fish";

        [Tooltip("Номер в журнале 1-14. 0 = не входит в журнал (металлолом)")]
        public int journalNumber;

        public string displayName = "Рыба";

        [TextArea(2, 5)] public string description = "";

        public Sprite sprite;

        [Header("Вес и цена")]
        [Min(0f)] public float minWeight = 0.5f;
        [Min(0f)] public float maxWeight = 1.5f;

        [Tooltip("Цена за 1 кг у Тараса. Ночной улов стоит x3")]
        [Min(0f)] public float pricePerKg = 10f;

        public Rarity rarity = Rarity.Common;

        [Header("Где и когда клюёт")]
        public FishTime time = FishTime.Day;
        public ZoneMask zones = ZoneMask.Bay;
        public BaitType bait = BaitType.Worm;

        [Tooltip("Минимальная дальность заброса в клетках (судак только на дальний заброс)")]
        [Min(0f)] public float minCastDistance;

        [Header("Вываживание")]
        public FishBehaviour behaviour = FishBehaviour.Steady;

        [Tooltip("Сила рыбы 0..1: насколько сильно тянет и как медленно растёт прогресс")]
        [Range(0f, 1f)] public float strength = 0.3f;

        [Header("Трюм")]
        [Tooltip("Размер в клетках трюма (ширина x высота)")]
        public Vector2Int holdSize = Vector2Int.one;

        [Header("Особые")]
        [Tooltip("Сундучок деда: внутри страница дневника")]
        public bool isChest;

        [Tooltip("Хозяин Омута: ловится только в финале и в трюм не кладётся")]
        public bool isBoss;

        public bool InJournal => journalNumber > 0;

        // Подходит ли время суток.
        public bool BitesAt(DayPhase phase) => (time & ToFishTime(phase)) != 0;

        public bool BitesOn(BaitType usedBait) => bait == BaitType.Any || bait == Kind(usedBait);

        // На что похожа наживка для рыбы: опарыш и тесто как червь, пиявка как фосфорная блесна.
        public static BaitType Kind(BaitType b)
        {
            switch (b)
            {
                case BaitType.Maggot:
                case BaitType.Dough: return BaitType.Worm;
                case BaitType.Leech: return BaitType.Phosphor;
                default: return b;
            }
        }

        public static FishTime ToFishTime(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Evening: return FishTime.Dusk;
                case DayPhase.Night: return FishTime.Night;
                default: return FishTime.Day;
            }
        }

        public static string RarityName(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return "обычная";
                case Rarity.Uncommon: return "необычная";
                case Rarity.Rare: return "редкая";
                case Rarity.Epic: return "эпическая";
                default: return "легендарная";
            }
        }

        public static Color RarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return new Color(0.35f, 0.35f, 0.32f);
                case Rarity.Uncommon: return new Color(0.2f, 0.5f, 0.25f);
                case Rarity.Rare: return new Color(0.2f, 0.35f, 0.75f);
                case Rarity.Epic: return new Color(0.55f, 0.25f, 0.65f);
                default: return new Color(0.8f, 0.5f, 0.1f);
            }
        }
    }
}
