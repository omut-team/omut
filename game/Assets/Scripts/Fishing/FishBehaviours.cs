using UnityEngine;

namespace Omut.Fishing
{
    // Поведение рыбы при вываживании (паттерн Strategy).
    // Каждый класс по-своему отвечает на вопрос "с какой силой рыба тянет сейчас".
    // TensionModel не знает, какая рыба на крючке, он просто спрашивает стратегию.
    public interface IFishBehaviour
    {
        string Name { get; }

        // Сила, с которой рыба тянет леску (натяжение растёт), примерно 0..2.
        float Force(float time, float strength);

        // true = управление наоборот: ЛКМ отпускает, ПКМ тянет (обратная шкала).
        bool InvertsControls(float time);

        // Множитель скорости прогресса (тяжёлая рыба выводится дольше).
        float ProgressMultiplier { get; }
    }

    // Ровно: тянет спокойно, чуть покачиваясь.
    public class SteadyPull : IFishBehaviour
    {
        public string Name => "ровно";
        public float ProgressMultiplier => 1f;
        public float Force(float time, float strength) => 0.35f + strength * 0.4f + 0.08f * Mathf.Sin(time * 2f);
        public bool InvertsControls(float time) => false;
    }

    // Рывки: в обычное время слабо, но раз в пару секунд резкий рывок.
    public class JerkPull : IFishBehaviour
    {
        private readonly float period;
        private readonly float jerkLength;

        public JerkPull(float period = 2.2f, float jerkLength = 0.4f)
        {
            this.period = period;
            this.jerkLength = jerkLength;
        }

        public string Name => "рывки";
        public float ProgressMultiplier => 0.9f;

        public bool IsJerking(float time) => time > 1f && (time % period) < jerkLength;

        public float Force(float time, float strength)
        {
            float calm = 0.3f + strength * 0.25f;
            return IsJerking(time) ? calm + 1.3f + strength * 0.6f : calm;
        }

        public bool InvertsControls(float time) => false;
    }

    // Тяжело: тянет сильно и постоянно, прогресс медленный.
    public class HeavyPull : IFishBehaviour
    {
        public string Name => "тяжело";
        public float ProgressMultiplier => 0.7f;
        public float Force(float time, float strength) => 0.55f + strength * 0.5f + 0.05f * Mathf.Sin(time * 0.8f);
        public bool InvertsControls(float time) => false;
    }

    // Обратная шкала: тянет ровно, но кнопки работают наоборот.
    public class ReversePull : IFishBehaviour
    {
        private readonly SteadyPull steady = new SteadyPull();

        public string Name => "обратная шкала";
        public float ProgressMultiplier => 0.9f;
        public float Force(float time, float strength) => steady.Force(time, strength);
        public bool InvertsControls(float time) => true;
    }

    // Хозяин Омута: все поведения по очереди, каждое по phaseLength секунд.
    public class AllPull : IFishBehaviour
    {
        private readonly IFishBehaviour[] phases = { new SteadyPull(), new JerkPull(1.6f, 0.45f), new HeavyPull(), new ReversePull() };
        private readonly float phaseLength;

        public AllPull(float phaseLength = 6f)
        {
            this.phaseLength = phaseLength;
        }

        public IFishBehaviour Current(float time) => phases[(int)(time / phaseLength) % phases.Length];

        public string Name => "все виды по очереди";
        public float ProgressMultiplier => 0.45f;
        public float Force(float time, float strength) => Current(time).Force(time, strength);
        public bool InvertsControls(float time) => Current(time).InvertsControls(time);
    }

    public static class FishBehaviours
    {
        // Фабрика: по значению из FishData получить стратегию.
        public static IFishBehaviour Create(FishBehaviour behaviour)
        {
            switch (behaviour)
            {
                case FishBehaviour.Jerk: return new JerkPull();
                case FishBehaviour.Heavy: return new HeavyPull();
                case FishBehaviour.Reverse: return new ReversePull();
                case FishBehaviour.All: return new AllPull();
                default: return new SteadyPull();
            }
        }
    }
}
