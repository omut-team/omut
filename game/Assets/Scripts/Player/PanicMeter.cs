using System;

namespace Omut.Player
{
    // Что сейчас вокруг Сымона (для расчёта паники).
    public struct PanicConditions
    {
        public bool isNight;       // после колокола
        public bool eternalFog;    // риф: туман и днём
        public bool lanternOn;
        public bool inSafeLight;   // у маяка или на причале
        public float multiplier;   // от лодки: «Охотник» 0.6

        public PanicConditions(bool isNight, bool eternalFog, bool lanternOn, bool inSafeLight, float multiplier = 1f)
        {
            this.isNight = isNight;
            this.eternalFog = eternalFog;
            this.lanternOn = lanternOn;
            this.inSafeLight = inSafeLight;
            this.multiplier = multiplier;
        }
    }

    // Паника 0..100. С фонарём растёт медленно, без света быстро, у маяка падает, днём проходит.
    // Пороги: 50 (виньетка, шёпот, ложные поклёвки), 80 (руль с задержкой), 100 (обморок).
    // Обычный C#-класс, проверяется тестами (PanicMeterTests).
    public class PanicMeter
    {
        public const float Max = 100f;
        public const float WhisperLevel = 50f;
        public const float SteeringLevel = 80f;

        public const float RiseWithLantern = 0.3f;   // в секунду: за ночь (240 с) около 70
        public const float RiseInDark = 2.5f;        // без света: 40 секунд до обморока
        public const float RiseInFog = 0.4f;         // риф днём
        public const float DropInSafeLight = 8f;     // у маяка
        public const float DropByDay = 5f;           // днём
        public const float ThermosEffect = 35f;

        public float Value { get; private set; }

        public int Level => Value >= Max ? 3 : Value >= SteeringLevel ? 2 : Value >= WhisperLevel ? 1 : 0;

        // Сменился порог: (было, стало).
        public event Action<int, int> LevelChanged;

        // Паника дошла до 100: обморок.
        public event Action Fainted;

        public static float Rate(PanicConditions c)
        {
            if (c.inSafeLight)
                return -DropInSafeLight;

            if (c.isNight)
                return (c.lanternOn ? RiseWithLantern : RiseInDark) * c.multiplier;

            if (c.eternalFog)
                return (c.lanternOn ? RiseWithLantern : RiseInFog) * c.multiplier;

            return -DropByDay;
        }

        public void Tick(float dt, PanicConditions c)
        {
            if (dt <= 0f)
                return;

            Set(Value + Rate(c) * dt);
        }

        // Выпить чай из термоса.
        public void Calm(float amount = ThermosEffect) => Set(Value - amount);

        public void Set(float value)
        {
            int before = Level;
            Value = value < 0f ? 0f : (value > Max ? Max : value);
            int after = Level;
            if (after == before)
                return;

            LevelChanged?.Invoke(before, after);
            if (after == 3)
                Fainted?.Invoke();
        }

        public void Reset() => Set(0f);
    }
}
