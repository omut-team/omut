using System;
using UnityEngine;

namespace Omut.Core
{
    // Общая "доска объявлений" игры (паттерн Observer).
    // Один скрипт сообщает о событии (Raise...), а все, кто подписался (+=), узнают о нём.
    // Отправитель не знает, кто его слушает, поэтому скрипты не зависят друг от друга.
    // Сюда же потом добавятся события поклёвки, смены дня и ночи, паники и т.д.
    public static class GameEvents
    {
        // Сменилось состояние игры: (было, стало).
        public static event Action<GameState, GameState> GameStateChanged;

        public static void RaiseGameStateChanged(GameState from, GameState to)
        {
            GameStateChanged?.Invoke(from, to);
        }

        // В проекте выключена перезагрузка скриптов при нажатии Play (так Play запускается быстрее).
        // Из-за этого подписки от прошлого запуска остаются в памяти. Чистим их перед каждым запуском.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscribers()
        {
            GameStateChanged = null;
        }
    }
}
