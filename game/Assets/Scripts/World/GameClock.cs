using System;

namespace Omut.World
{
    // Фазы суток. Колокол маяка бьёт на переходе Evening -> Night (20:00).
    public enum DayPhase
    {
        Morning, // 06:00-10:00
        Day,     // 10:00-17:00
        Evening, // 17:00-20:00 (сумерки)
        Night    // 20:00-06:00, после колокола безопасно только у света
    }

    // Игровые часы. Обычный C#-класс без MonoBehaviour, поэтому проверяется автотестами
    // (Tests/EditMode/GameClockTests.cs). В сцене его крутит DayNightCycle.
    //
    // Время хранится в игровых минутах от полуночи: 0 = 00:00, 360 = 06:00, 1200 = 20:00.
    // День (06:00-20:00, 840 игровых минут) длится 8 реальных минут,
    // ночь (20:00-06:00, 600 игровых минут) 4 реальные минуты. Сутки около 12 минут.
    // Поэтому днём и ночью часы идут с разной скоростью.
    public class GameClock
    {
        public const float MinutesPerDay = 24 * 60;  // 1440
        public const float MorningStart = 6 * 60;    // 06:00
        public const float DayStart = 10 * 60;       // 10:00
        public const float EveningStart = 17 * 60;   // 17:00
        public const float NightStart = 20 * 60;     // 20:00, колокол

        private const float BoundaryEpsilon = 0.0001f; // в реальных секундах

        // Сколько реальных секунд длятся день и ночь.
        private readonly float realDaySeconds;
        private readonly float realNightSeconds;

        // Время в игровых минутах от полуночи (от 0 до 1440).
        public float Minutes { get; private set; }

        // Номер дня. Новый день начинается в 06:00.
        public int Day { get; private set; }

        public DayPhase Phase => GetPhase(Minutes);
        public bool IsNight => Phase == DayPhase.Night;
        public int Hour => (int)Minutes / 60;
        public int Minute => (int)Minutes % 60;

        // Доля суток от 0 до 1 (0 = полночь, 0.5 = полдень). Нужна, чтобы брать цвет света из градиента.
        public float DayFraction => Minutes / MinutesPerDay;

        // События (паттерн Observer). DayNightCycle передаёт их дальше в GameEvents.
        public event Action<DayPhase, DayPhase> PhaseChanged; // (было, стало)
        public event Action BellRang;                         // 20:00
        public event Action<int> NewDay;                      // 06:00, номер нового дня

        public GameClock(float realDayMinutes = 8f, float realNightMinutes = 4f, float startMinutes = MorningStart, int startDay = 1)
        {
            realDaySeconds = Math.Max(1f, realDayMinutes * 60f);
            realNightSeconds = Math.Max(1f, realNightMinutes * 60f);
            Minutes = Wrap(startMinutes);
            Day = Math.Max(1, startDay);
        }

        // Сколько игровых минут проходит за 1 реальную секунду сейчас.
        public float MinutesPerSecond => IsNight
            ? (MinutesPerDay - NightStart + MorningStart) / realNightSeconds  // 600 мин за ночь
            : (NightStart - MorningStart) / realDaySeconds;                   // 840 мин за день

        // Сдвинуть часы на deltaSeconds реальных секунд.
        // Идём по кускам от одной границы (06:00, 10:00, 17:00, 20:00) до следующей,
        // чтобы даже при большом deltaTime не пропустить колокол и смену фазы.
        public void Tick(float deltaSeconds)
        {
            float seconds = Math.Max(0f, deltaSeconds);

            while (seconds > 0f)
            {
                float boundary = NextBoundary(Minutes);
                float minutesToBoundary = boundary - Minutes;
                float rate = MinutesPerSecond;
                float secondsToBoundary = minutesToBoundary / rate;

                // Маленький запас (BoundaryEpsilon), чтобы из-за округления float
                // часы не застряли на 05:59.9999 вместо 06:00.
                if (seconds < secondsToBoundary - BoundaryEpsilon)
                {
                    Minutes = Wrap(Minutes + seconds * rate); // после 23:59 снова 00:00
                    return;
                }

                seconds = Math.Max(0f, seconds - secondsToBoundary);
                SetMinutesAndNotify(boundary);
            }
        }

        // Поставить время вручную (для отладки и загрузки сохранения). Колокол при этом не бьёт,
        // а смена фазы сообщается, если фаза поменялась.
        public void SetTime(int hour, int minute)
        {
            DayPhase before = Phase;
            Minutes = Wrap(hour * 60 + minute);
            if (Phase != before)
                PhaseChanged?.Invoke(before, Phase);
        }

        // Перемотать на утро следующего дня (обморок): 06:00, новый день, события фазы и нового дня.
        public void AdvanceToMorning()
        {
            DayPhase before = Phase;
            Day++;
            Minutes = MorningStart;
            if (before != DayPhase.Morning)
                PhaseChanged?.Invoke(before, DayPhase.Morning);
            NewDay?.Invoke(Day);
        }

        // Номер дня (из сохранения).
        public void SetDay(int day) => Day = Math.Max(1, day);

        public static DayPhase GetPhase(float minutes)
        {
            minutes = Wrap(minutes);

            if (minutes >= NightStart || minutes < MorningStart) return DayPhase.Night;
            if (minutes >= EveningStart) return DayPhase.Evening;
            if (minutes >= DayStart) return DayPhase.Day;
            return DayPhase.Morning;
        }

        // 375 -> "06:15"
        public static string Format(float minutes)
        {
            int total = (int)Wrap(minutes);
            return $"{total / 60:00}:{total % 60:00}";
        }

        public override string ToString() => Format(Minutes);

        // Ближайшая граница фазы после текущего времени (может быть 1440 + 360, если сейчас ночь до полуночи).
        private static float NextBoundary(float minutes)
        {
            if (minutes < MorningStart) return MorningStart;
            if (minutes < DayStart) return DayStart;
            if (minutes < EveningStart) return EveningStart;
            if (minutes < NightStart) return NightStart;
            return MinutesPerDay + MorningStart;
        }

        private void SetMinutesAndNotify(float boundary)
        {
            DayPhase before = Phase;
            Minutes = Wrap(boundary);
            DayPhase after = Phase;

            if (after != before)
                PhaseChanged?.Invoke(before, after);

            if (Minutes == NightStart)
                BellRang?.Invoke();

            if (Minutes == MorningStart)
            {
                Day++;
                NewDay?.Invoke(Day);
            }
        }

        private static float Wrap(float minutes)
        {
            minutes %= MinutesPerDay;
            return minutes < 0f ? minutes + MinutesPerDay : minutes;
        }
    }
}
