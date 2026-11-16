using UnityEngine;

namespace Omut.UI
{
    // Оформление интерфейса: шрифты и картинки панелей, кнопок, ячеек.
    // Лежит в Assets/Resources/UiSkin.asset (его создаёт сборщик сцены), UiFactory берёт его сам.
    // Поменять стиль всей игры = поменять картинки здесь.
    [CreateAssetMenu(fileName = "UiSkin", menuName = "Omut/UI Skin")]
    public class UiSkin : ScriptableObject
    {
        [Header("Шрифты (Art/Fonts, лицензия OFL)")]
        [Tooltip("Основной текст: PT Sans")]
        public Font bodyFont;

        [Tooltip("Жирный: PT Sans Bold (кнопки, HUD)")]
        public Font boldFont;

        [Tooltip("Заголовки и текст на бумаге: Lora")]
        public Font titleFont;

        [Header("Картинки (Art/UI, края для растяжения уже настроены)")]
        public Sprite panel;          // бумага в деревянной рамке
        public Sprite plank;          // широкая дощечка (кнопки меню)
        public Sprite button;         // деревянная кнопка
        public Sprite slot;           // ячейка инвентаря
        public Sprite slotSelected;   // рамка выбранной ячейки
        public Sprite hud;            // тёмная деревянная панель HUD
        public Sprite bar;            // рамка шкалы
        public Sprite tag;            // табличка с именем над NPC
        public Sprite markQuest;      // "!"
        public Sprite markTurnIn;     // "?"
        public Sprite arrow;          // стрелка к цели

        private static UiSkin current;
        private static bool loaded;

        public static UiSkin Current
        {
            get
            {
                if (!loaded)
                {
                    current = Resources.Load<UiSkin>("UiSkin");
                    loaded = true;
                }

                return current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => loaded = false;
    }
}
