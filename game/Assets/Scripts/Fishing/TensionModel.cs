using UnityEngine;

namespace Omut.Fishing
{
    public enum ReelResult
    {
        InProgress,
        Caught,     // прогресс дошёл до 100%
        LineBroke,  // больше 3 секунд подряд в красной зоне: леска рвётся, пропадают рыба и наживка
        Escaped     // натяжение упало до нуля: рыба сошла
    }

    // Расчёт вываживания. Обычный C#-класс, проверяется тестами (TensionModelTests).
    //
    // Натяжение 0..1. Рыба всё время тянет (сила зависит от поведения), леска сама провисает.
    // ЛКМ подтягивает (натяжение растёт), ПКМ стравливает (падает).
    // Держишь натяжение в зелёном коридоре: прогресс растёт. Ниже коридора: прогресс медленно падает.
    // В красной зоне прогресс растёт вдвое медленнее и тикает таймер обрыва.
    public class TensionModel
    {
        public const float GreenMin = 0.3f;
        public const float GreenMax = 0.7f;
        public const float RedStart = 0.85f;
        public const float BreakSeconds = 3f;

        public const float PullRate = 1.2f;    // ЛКМ, в секунду
        public const float SlackRate = 1.3f;   // ПКМ, в секунду
        public const float SagRate = 0.75f;    // леска сама провисает без рыбы
        public const float BaseProgress = 0.17f;
        public const float GraceSeconds = 1.2f; // первые секунды рыба не может сойти

        private readonly IFishBehaviour behaviour;
        private readonly float strength;
        private readonly float progressMultiplier;

        public float Tension { get; private set; } = 0.5f;
        public float Progress { get; private set; }
        public float RedTimer { get; private set; }
        public float Time { get; private set; }
        public ReelResult Result { get; private set; } = ReelResult.InProgress;

        public bool InGreen => Tension >= GreenMin && Tension <= GreenMax;
        public bool InRed => Tension >= RedStart;
        public bool ControlsInverted => behaviour.InvertsControls(Time);
        public IFishBehaviour Behaviour => behaviour;

        // extraProgressMultiplier: например, 0.5 для босса (дольше тянуть).
        public TensionModel(IFishBehaviour behaviour, float strength, float extraProgressMultiplier = 1f)
        {
            this.behaviour = behaviour;
            this.strength = Mathf.Clamp01(strength);
            progressMultiplier = behaviour.ProgressMultiplier * extraProgressMultiplier * (1f - 0.5f * this.strength);
        }

        // pull: зажата ЛКМ, slack: зажата ПКМ.
        public ReelResult Tick(float deltaTime, bool pull, bool slack)
        {
            if (Result != ReelResult.InProgress || deltaTime <= 0f)
                return Result;

            if (ControlsInverted)
            {
                bool t = pull;
                pull = slack;
                slack = t;
            }

            float change = behaviour.Force(Time, strength) - SagRate;
            if (pull) change += PullRate;
            if (slack) change -= SlackRate;

            Time += deltaTime;
            Tension = Mathf.Clamp01(Tension + change * deltaTime);

            float rate = BaseProgress * progressMultiplier;
            if (InRed) Progress += rate * 0.5f * deltaTime;
            else if (InGreen) Progress += rate * deltaTime;
            else if (Tension < GreenMin) Progress -= 0.04f * deltaTime;
            Progress = Mathf.Clamp01(Progress);

            RedTimer = InRed ? RedTimer + deltaTime : 0f;

            if (Progress >= 1f) Result = ReelResult.Caught;
            else if (RedTimer > BreakSeconds) Result = ReelResult.LineBroke;
            else if (Tension <= 0f && Time > GraceSeconds) Result = ReelResult.Escaped;

            return Result;
        }
    }
}
