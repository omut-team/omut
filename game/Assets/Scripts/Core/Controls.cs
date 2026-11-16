using UnityEngine;
using UnityEngine.InputSystem;

namespace Omut.Core
{
    // Все клавиши игры в одном месте. Если надо поменять клавишу, меняем только здесь.
    // Движение лодки (WASD) настроено в BoatController.
    public static class Controls
    {
        public const Key Interact = Key.E;        // сойти на причал, сесть в лодку, говорить
        public const Key Cast = Key.Space;        // заброс (зажать), подсечка
        public const Key Lantern = Key.F;         // фонарь
        public const Key Bait = Key.Q;            // сменить наживку
        public const Key Hold = Key.Tab;          // трюм
        public const Key HoldAlt = Key.I;
        public const Key Journal = Key.J;         // журнал рыб и дневник
        public const Key Map = Key.M;             // карта
        public const Key Quests = Key.L;          // задания
        public const Key Thermos = Key.C;         // выпить чай из термоса
        public const Key Rotate = Key.R;          // повернуть рыбу в трюме
        public const Key Bag = Key.B;             // сумка со всеми вещами
        public const Key Pause = Key.Escape;

        // Последний кадр, в котором Esc уже кто-то обработал (закрыл окно).
        // Нужно, чтобы одним нажатием Esc не закрыть окно и сразу не открыть паузу.
        private static int escConsumedFrame = -1;

        public static bool Pressed(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasPressedThisFrame;
        }

        public static bool Held(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].isPressed;
        }

        public static bool Released(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard[key].wasReleasedThisFrame;
        }

        public static bool LeftMouseHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;
        public static bool RightMouseHeld => Mouse.current != null && Mouse.current.rightButton.isPressed;

        public static Vector2 MouseScreenPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        // Esc нажат и ещё никем не обработан в этом кадре.
        public static bool EscapePressed => Pressed(Pause) && escConsumedFrame != Time.frameCount;

        public static void ConsumeEscape() => escConsumedFrame = Time.frameCount;

        // Подсказка по клавишам для окна "Управление".
        public const string Help =
            "WASD / стрелки: плыть или идти\n" +
            "E: причалить, сойти на берег, сесть в лодку, поговорить\n" +
            "Space (зажать и отпустить): заброс. Space при поклёвке: подсечка\n" +
            "ЛКМ: тянуть леску, ПКМ: стравить\n" +
            "1-9 или колесо мыши: 1-6 удочка с наживкой, 7 фонарь, 8 чай, 9 карта\n" +
            "Q: сменить наживку\n" +
            "F: фонарь, C: чай из термоса\n" +
            "Tab или I: трюм с рыбой (перетаскивать мышью, R: повернуть)\n" +
            "B: сумка со всеми вещами\n" +
            "J: журнал и дневник, M: карта, L: задания\n" +
            "Esc: пауза или закрыть окно";
    }
}
