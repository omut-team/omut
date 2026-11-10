using System;
using System.Collections.Generic;

namespace Omut.Core
{
    // Конечный автомат состояний игры (паттерн State).
    // Знает текущее состояние и решает, в какие состояния из него можно перейти.
    // Это обычный C#-класс без MonoBehaviour, поэтому его легко проверять автотестами.
    public class GameStateMachine
    {
        // Таблица разрешённых переходов: из какого состояния -> в какие можно попасть.
        private static readonly Dictionary<GameState, GameState[]> AllowedTransitions =
            new Dictionary<GameState, GameState[]>
            {
                { GameState.Menu,    new[] { GameState.Sailing } },
                { GameState.Sailing, new[] { GameState.Fishing, GameState.Shop, GameState.Paused } },
                { GameState.Fishing, new[] { GameState.Sailing, GameState.Paused } },
                { GameState.Shop,    new[] { GameState.Sailing, GameState.Paused } },
                // Из паузы можно выйти в меню. Возврат в игру проверяется отдельно (см. CanChangeTo).
                { GameState.Paused,  new[] { GameState.Menu } },
            };

        public GameState Current { get; private set; }

        // Состояние, в которое вернёмся, когда снимем паузу.
        public GameState StateBeforePause { get; private set; }

        public bool IsPaused => Current == GameState.Paused;

        // Сообщает о смене состояния: (было, стало).
        public event Action<GameState, GameState> StateChanged;

        public GameStateMachine(GameState startState = GameState.Menu)
        {
            Current = startState;
            StateBeforePause = startState;
        }

        // Можно ли сейчас перейти в состояние next.
        public bool CanChangeTo(GameState next)
        {
            if (next == Current)
                return false;

            // Снять паузу можно только обратно туда, где были до неё.
            if (Current == GameState.Paused && next == StateBeforePause)
                return true;

            return Array.IndexOf(AllowedTransitions[Current], next) >= 0;
        }

        // Пытается перейти в новое состояние. Возвращает false, если так нельзя.
        public bool TryChangeTo(GameState next)
        {
            if (!CanChangeTo(next))
                return false;

            GameState previous = Current;

            if (next == GameState.Paused)
                StateBeforePause = previous;

            Current = next;
            StateChanged?.Invoke(previous, next);
            return true;
        }

        // Esc: поставить игру на паузу или снять с неё.
        public bool TogglePause()
        {
            if (IsPaused)
                return TryChangeTo(StateBeforePause);

            return TryChangeTo(GameState.Paused);
        }
    }
}
