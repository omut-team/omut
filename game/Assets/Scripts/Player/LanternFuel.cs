using System;

namespace Omut.Player
{
    // Керосин в фонаре. Горящий фонарь тратит керосин, пустой гаснет сам.
    // Обычный C#-класс, проверяется тестами (LanternFuelTests).
    public class LanternFuel
    {
        // Полного бака (100 %) хватает на столько секунд горения (ночь длится 240 с).
        public const float SecondsPerTank = 300f;

        public float Fuel { get; private set; }  // 0..100
        public bool IsOn { get; private set; }
        public bool IsEmpty => Fuel <= 0f;

        public event Action<bool> Toggled;

        public LanternFuel(float fuel)
        {
            Fuel = Clamp(fuel);
        }

        // Включить или выключить (F). Пустой не включится. Возвращает новое состояние.
        public bool Toggle()
        {
            SetOn(!IsOn);
            return IsOn;
        }

        public void SetOn(bool on)
        {
            bool next = on && !IsEmpty;
            if (next == IsOn)
                return;

            IsOn = next;
            Toggled?.Invoke(IsOn);
        }

        // Прошло dt секунд. Возвращает true, если керосин только что кончился.
        public bool Burn(float dt)
        {
            if (!IsOn || dt <= 0f)
                return false;

            Fuel = Clamp(Fuel - dt * 100f / SecondsPerTank);
            if (!IsEmpty)
                return false;

            SetOn(false);
            return true;
        }

        public void Refill(float amount) => Fuel = Clamp(Fuel + amount);

        public void Set(float fuel) => Fuel = Clamp(fuel);

        private static float Clamp(float v) => v < 0f ? 0f : (v > 100f ? 100f : v);
    }
}
