using NUnit.Framework;
using Omut.Player;
using UnityEngine;

namespace Omut.Tests
{
    // Автотесты движения лодки. Запуск: Window > General > Test Runner > EditMode > Run All.
    // Во всех тестах: макс. скорость 3, разгон 4, торможение 2 (как у баркаса).
    public class BoatMotorTests
    {
        private const float Tolerance = 0.0001f;

        private static BoatMotor CreateMotor() => new BoatMotor(3f, 4f, 2f);

        [Test]
        public void StartsStopped()
        {
            var motor = CreateMotor();

            Assert.AreEqual(0f, motor.Speed, Tolerance);
        }

        [Test]
        public void Accelerates_Gradually()
        {
            var motor = CreateMotor();

            motor.Tick(Vector2.right, 0.25f); // разгон 4 * 0.25 с = 1

            Assert.AreEqual(1f, motor.Velocity.x, Tolerance);
            Assert.Less(motor.Speed, 3f);
        }

        [Test]
        public void ReachesMaxSpeed_AndNotMore()
        {
            var motor = CreateMotor();

            for (int i = 0; i < 100; i++)
                motor.Tick(Vector2.right, 0.1f);

            Assert.AreEqual(3f, motor.Speed, Tolerance);
        }

        [Test]
        public void Diagonal_IsNotFaster()
        {
            var motor = CreateMotor();

            for (int i = 0; i < 100; i++)
                motor.Tick(new Vector2(1f, 1f), 0.1f);

            Assert.AreEqual(3f, motor.Speed, Tolerance);
        }

        [Test]
        public void KeepsMoving_AfterKeysReleased()
        {
            var motor = CreateMotor();
            for (int i = 0; i < 100; i++)
                motor.Tick(Vector2.up, 0.1f);

            motor.Tick(Vector2.zero, 0.5f); // торможение 2 * 0.5 с = 1, остаётся 2

            Assert.AreEqual(2f, motor.Velocity.y, Tolerance);
        }

        [Test]
        public void StopsCompletely_AfterSomeTime()
        {
            var motor = CreateMotor();
            for (int i = 0; i < 100; i++)
                motor.Tick(Vector2.left, 0.1f);

            for (int i = 0; i < 20; i++)
                motor.Tick(Vector2.zero, 0.1f); // 2 секунды, от скорости 3 хватит 1.5 с

            Assert.AreEqual(Vector2.zero, motor.Velocity);
        }

        [Test]
        public void ZeroDeltaTime_ChangesNothing()
        {
            var motor = CreateMotor();
            motor.Tick(Vector2.right, 0.25f);

            motor.Tick(Vector2.down, 0f); // пауза: Time.deltaTime = 0

            Assert.AreEqual(new Vector2(1f, 0f), motor.Velocity);
        }

        [Test]
        public void SetVelocity_IsLimitedByMaxSpeed()
        {
            var motor = CreateMotor();

            motor.SetVelocity(new Vector2(10f, 0f));

            Assert.AreEqual(3f, motor.Speed, Tolerance);
        }

        [Test]
        public void Stop_ResetsSpeed()
        {
            var motor = CreateMotor();
            motor.Tick(Vector2.right, 0.5f);

            motor.Stop();

            Assert.AreEqual(Vector2.zero, motor.Velocity);
        }

        [Test]
        public void CreatedFromBoatData_UsesItsNumbers()
        {
            var data = ScriptableObject.CreateInstance<BoatData>();
            data.maxSpeed = 5f;
            data.acceleration = 10f;
            var motor = new BoatMotor(data);

            motor.Tick(Vector2.right, 1f);

            Assert.AreEqual(5f, motor.Speed, Tolerance);
            Object.DestroyImmediate(data);
        }
    }
}
