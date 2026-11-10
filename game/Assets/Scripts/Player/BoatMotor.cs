using UnityEngine;

namespace Omut.Player
{
    // Расчёт движения лодки с инерцией. Обычный C#-класс без MonoBehaviour,
    // поэтому его легко проверить автотестами (Tests/EditMode/BoatMotorTests.cs).
    //
    // Идея: есть "желаемая" скорость (куда жмём WASD * максимальная скорость).
    // Текущая скорость не прыгает к ней сразу, а плавно подтягивается:
    // пока жмём, на acceleration в секунду, когда отпустили, на deceleration в секунду.
    // Отпустил клавиши: лодка ещё немного плывёт сама. Это и есть инерция.
    public class BoatMotor
    {
        private readonly float maxSpeed;
        private readonly float acceleration;
        private readonly float deceleration;

        public Vector2 Velocity { get; private set; }

        public float Speed => Velocity.magnitude;

        public BoatMotor(float maxSpeed, float acceleration, float deceleration)
        {
            this.maxSpeed = Mathf.Max(0f, maxSpeed);
            this.acceleration = Mathf.Max(0f, acceleration);
            this.deceleration = Mathf.Max(0f, deceleration);
        }

        public BoatMotor(BoatData data)
            : this(data.maxSpeed, data.acceleration, data.deceleration)
        {
        }

        // Один шаг расчёта. input: направление от WASD (x и y от -1 до 1), deltaTime: сколько секунд прошло.
        // Возвращает новую скорость в клетках в секунду.
        public Vector2 Tick(Vector2 input, float deltaTime)
        {
            // По диагонали (W + D) вектор длиннее 1. Обрезаем, иначе по диагонали лодка плыла бы быстрее.
            input = Vector2.ClampMagnitude(input, 1f);

            Vector2 targetVelocity = input * maxSpeed;
            float rate = input == Vector2.zero ? deceleration : acceleration;

            Velocity = Vector2.MoveTowards(Velocity, targetVelocity, rate * deltaTime);
            return Velocity;
        }

        // Подставить скорость снаружи. Нужно после столкновения: физика остановила лодку о камень,
        // и мотор должен об этом узнать, а не продолжать "толкать" в камень на полной скорости.
        public void SetVelocity(Vector2 velocity)
        {
            Velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
        }

        public void Stop()
        {
            Velocity = Vector2.zero;
        }
    }
}
