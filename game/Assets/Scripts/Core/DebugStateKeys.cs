using UnityEngine;
using UnityEngine.InputSystem;

namespace Omut.Core
{
    // Временные клавиши, чтобы проверить смену состояний, пока нет меню и рыбалки.
    // Добавляется к GameManager сам, только в редакторе и тестовых сборках.
    // Уберём или оставим как чит-клавиши, когда появится настоящее меню (этап 4).
    public class DebugStateKeys : MonoBehaviour
    {
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            GameManager game = GameManager.Instance;

            if (keyboard == null || game == null)
                return;

            if (keyboard.enterKey.wasPressedThisFrame)      game.StartGame();
            if (keyboard.escapeKey.wasPressedThisFrame)     game.TogglePause();
            if (keyboard.digit1Key.wasPressedThisFrame)     game.ChangeState(GameState.Sailing);
            if (keyboard.digit2Key.wasPressedThisFrame)     game.ChangeState(GameState.Fishing);
            if (keyboard.digit3Key.wasPressedThisFrame)     game.ChangeState(GameState.Shop);
            if (keyboard.backspaceKey.wasPressedThisFrame)  game.BackToMenu();
        }
    }
}
