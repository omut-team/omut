using Omut.Core;
using Omut.UI;
using UnityEngine;

namespace Omut.Fishing
{
    // Состояния рыбалки (паттерн State). Каждое состояние знает, что делать в своём кадре
    // и куда переходить дальше. FishingController только хранит текущее и вызывает Tick.
    //
    // Idle -> Charging (зажал Space) -> Flying (отпустил) -> Waiting (поплавок на воде)
    //      -> Bite (клюёт!) -> Reeling (подсёк Space) -> Caught (карточка трофея) -> Idle
    public abstract class FishingState
    {
        protected readonly FishingController c;

        protected FishingState(FishingController controller)
        {
            c = controller;
        }

        public abstract string Name { get; }
        public virtual void Enter() { }
        public virtual void Tick(float dt) { }
        public virtual void Exit() { }
    }

    // Ничего не делаем. Зажал Space: начинаем заброс.
    public class IdleState : FishingState
    {
        private int enteredFrame;

        public IdleState(FishingController c) : base(c) { }
        public override string Name => "нет";

        public override void Enter()
        {
            c.HideTackle();
            enteredFrame = Time.frameCount; // тот же Space, которым закончили, не должен начать новый заброс
        }

        public override void Tick(float dt)
        {
            if (Time.frameCount != enteredFrame && Controls.Pressed(Controls.Cast) && c.CanStartFishing())
                c.SetState(c.Charging);
        }
    }

    // Space зажат: сила заброса ходит туда-сюда 0..1..0. Отпустил: бросаем.
    public class ChargingState : FishingState
    {
        private float t;

        public ChargingState(FishingController c) : base(c) { }
        public override string Name => "заброс";

        public override void Enter()
        {
            t = 0f;
            c.CastPower = 0f;
            GameManager.Instance?.ChangeState(GameState.Fishing);
        }

        public override void Tick(float dt)
        {
            t += dt * c.ChargeSpeed;
            c.CastPower = Mathf.PingPong(t, 1f);
            c.AimTarget = c.ComputeTarget(c.CastPower);

            if (!Controls.Held(Controls.Cast))
                c.SetState(c.Flying);
        }
    }

    // Поплавок летит по дуге к цели.
    public class FlyingState : FishingState
    {
        private const float Duration = 0.6f;
        private Vector2 from;
        private Vector2 to;
        private float t;

        public FlyingState(FishingController c) : base(c) { }
        public override string Name => "полёт";

        public override void Enter()
        {
            from = c.RodTip;
            to = c.ComputeTarget(c.CastPower);
            t = 0f;
            c.ShowBobber(from);
            c.Sfx("cast");
        }

        public override void Tick(float dt)
        {
            t += dt / Duration;
            Vector2 p = Vector2.Lerp(from, to, t);
            p.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 1.2f; // дуга
            c.MoveBobber(p);

            if (t < 1f)
                return;

            c.MoveBobber(to);
            if (!c.IsWater(to))
            {
                Notifications.Show("Поплавок упал на берег. Забрось ещё раз.");
                c.StopFishing();
                return;
            }

            c.Sfx("splash");
            c.Landed(to);
            c.SetState(c.Waiting);
        }
    }

    // Ждём поклёвку. Space раньше времени: вытащить снасть.
    public class WaitingState : FishingState
    {
        private float timer;
        private float noBiteTimer;

        public WaitingState(FishingController c) : base(c) { }
        public override string Name => "ждём";

        public override void Enter()
        {
            timer = c.NextBiteDelay();
            noBiteTimer = 0f;
        }

        public override void Tick(float dt)
        {
            c.BobBobber(0.04f);

            if (Controls.Pressed(Controls.Cast))
            {
                c.StopFishing();
                return;
            }

            if (c.HookedFish == null)
            {
                noBiteTimer += dt;
                if (noBiteTimer > 12f)
                {
                    noBiteTimer = -999f;
                    Notifications.Show(c.NoBiteHint());
                }

                // Ложная поклёвка от страха (паника 50+).
                if (c.FakeBiteNow(dt))
                    c.SetState(c.Bite);
                return;
            }

            timer -= dt;
            if (timer <= 0f || c.FakeBiteNow(dt))
                c.SetState(c.Bite);
        }
    }

    // Клюёт! Поплавок дёргается, есть меньше секунды, чтобы подсечь.
    public class BiteState : FishingState
    {
        private float timer;
        private bool fake;

        public BiteState(FishingController c) : base(c) { }
        public override string Name => "клюёт";

        public override void Enter()
        {
            timer = c.BiteWindow;
            fake = c.HookedFish == null || c.FakeBitePending;
            c.FakeBitePending = false;
            c.IsBiting = true;
            c.Sfx("bite");
        }

        public override void Exit() => c.IsBiting = false;

        public override void Tick(float dt)
        {
            c.BobBobber(0.25f);
            timer -= dt;

            if (Controls.Pressed(Controls.Cast))
            {
                if (fake)
                {
                    Notifications.Show("Показалось... Это была не рыба.", new Color(0.7f, 0.8f, 1f));
                    c.SetState(c.Waiting);
                    return;
                }

                c.SetState(c.Reeling);
                return;
            }

            if (timer <= 0f)
            {
                if (!fake)
                    Notifications.Show("Не успел подсечь. Рыба ушла, ждём дальше.");
                c.RepickFish();
                c.SetState(c.Waiting);
            }
        }
    }

    // Вываживание: шкала натяжения, ЛКМ тянет, ПКМ стравливает.
    public class ReelingState : FishingState
    {
        public ReelingState(FishingController c) : base(c) { }
        public override string Name => "вываживание";

        public override void Enter()
        {
            FishData fish = c.HookedFish;
            float extra = fish.isBoss ? 0.6f : 1f;
            c.Reel = new TensionModel(FishBehaviours.Create(fish.behaviour), fish.strength, extra);
            c.Sfx("hook");
            GameEvents.RaiseFishHooked(fish);
        }

        public override void Exit() => c.Reel = null;

        public override void Tick(float dt)
        {
            ReelResult result = c.Reel.Tick(dt, Controls.LeftMouseHeld, Controls.RightMouseHeld);
            c.BobBobber(0.1f + c.Reel.Tension * 0.2f);
            c.PullBobberTowardsBoat(c.Reel.Progress);

            switch (result)
            {
                case ReelResult.Caught:
                    c.OnCaught();
                    break;
                case ReelResult.LineBroke:
                    c.OnLost(true);
                    break;
                case ReelResult.Escaped:
                    c.OnLost(false);
                    break;
            }
        }
    }

    // Рыба поймана, открыта карточка трофея. Ждём, пока игрок решит: в трюм или отпустить.
    public class CaughtState : FishingState
    {
        public CaughtState(FishingController c) : base(c) { }
        public override string Name => "трофей";

        public override void Enter() => c.HideTackle();
    }
}
