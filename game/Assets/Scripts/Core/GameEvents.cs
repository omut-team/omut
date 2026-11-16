using System;
using Omut.Fishing;
using Omut.World;
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

        // Сменилась фаза суток: (было, стало). Например, Evening -> Night.
        public static event Action<DayPhase, DayPhase> DayPhaseChanged;

        // 20:00, бьёт колокол маяка. Дальше ночь: безопасно только у света.
        public static event Action BellRang;

        // 06:00, начался новый день (номер дня).
        public static event Action<int> NewDayStarted;

        public static void RaiseDayPhaseChanged(DayPhase from, DayPhase to) => DayPhaseChanged?.Invoke(from, to);

        public static void RaiseBellRang() => BellRang?.Invoke();

        public static void RaiseNewDayStarted(int day) => NewDayStarted?.Invoke(day);

        // Игрок нажал "Новая игра" в меню (сбросить прогресс).
        public static event Action NewGameStarted;

        // Игрок нажал "Продолжить" (загрузить сохранение).
        public static event Action ContinueRequested;

        public static void RaiseNewGameStarted() => NewGameStarted?.Invoke();

        public static void RaiseContinueRequested() => ContinueRequested?.Invoke();

        // ---------- рыбалка ----------

        // Подсёк: рыба на крючке, началось вываживание.
        public static event Action<FishData> FishHooked;

        // Рыба вытащена (карточка трофея).
        public static event Action<CatchInfo> FishCaught;

        // Рыба сорвалась (lineBroke = леска оборвалась).
        public static event Action<FishData, bool> CatchLost;

        // Игрок решил оставить рыбу: положить в трюм.
        public static event Action<CatchInfo> CatchKept;

        // Наживка потрачена (съедена рыбой или ушла с оборванной леской).
        public static event Action<BaitType> BaitUsed;

        public static void RaiseFishHooked(FishData fish) => FishHooked?.Invoke(fish);
        public static void RaiseFishCaught(CatchInfo info) => FishCaught?.Invoke(info);
        public static void RaiseCatchLost(FishData fish, bool lineBroke) => CatchLost?.Invoke(fish, lineBroke);
        public static void RaiseCatchKept(CatchInfo info) => CatchKept?.Invoke(info);
        public static void RaiseBaitUsed(BaitType bait) => BaitUsed?.Invoke(bait);

        // ---------- звук ----------

        // Сыграть звук по имени ("cast", "bite", "bell"...). Слушает AudioManager (этап 21).
        public static event Action<string> PlaySound;

        public static void RaisePlaySound(string id) => PlaySound?.Invoke(id);

        // В проекте выключена перезагрузка скриптов при нажатии Play (так Play запускается быстрее).
        // Из-за этого подписки от прошлого запуска остаются в памяти. Чистим их перед каждым запуском.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscribers()
        {
            GameStateChanged = null;
            DayPhaseChanged = null;
            BellRang = null;
            NewDayStarted = null;
            NewGameStarted = null;
            ContinueRequested = null;
            FishHooked = null;
            FishCaught = null;
            CatchLost = null;
            CatchKept = null;
            BaitUsed = null;
            PlaySound = null;
        }
    }
}
