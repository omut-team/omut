using UnityEngine;

namespace Omut.Inventory
{
    public enum ItemKind
    {
        Kerosene,   // керосин для фонаря
        Thermos,    // термос с чаем: снижает панику
        Other
    }

    // Товар, который не рыба: керосин, термос. ScriptableObject, лежит в Data/Items.
    // Наживки описаны отдельно (BaitData), рыба и металлолом в FishData.
    [CreateAssetMenu(fileName = "NewItem", menuName = "Omut/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string id = "item";
        public string displayName = "Предмет";
        [TextArea(2, 4)] public string description = "";
        public ItemKind kind = ItemKind.Other;
        [Min(0)] public int price = 10;
        [Tooltip("Сколько даётся за одну покупку (керосин: процентов бака)")]
        [Min(1)] public int amount = 1;
        public Sprite sprite;
    }
}
