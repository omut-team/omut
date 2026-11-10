using Omut.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Omut.Player
{
    // Управление лодкой: читает WASD (или стрелки, или стик геймпада),
    // считает скорость через BoatMotor и двигает Rigidbody2D.
    // Rigidbody2D нужен, чтобы лодка упиралась в камни и берег (коллайдеры).
    [RequireComponent(typeof(Rigidbody2D))]
    public class BoatController : MonoBehaviour
    {
        [Tooltip("Характеристики лодки (Data/Boats/Barkas)")]
        [SerializeField] private BoatData data;

        [Tooltip("Картинка лодки. Повернём её влево или вправо по ходу движения")]
        [SerializeField] private SpriteRenderer sprite;

        private Rigidbody2D body;
        private BoatMotor motor;
        private InputAction moveAction;

        public BoatData Data => data;
        public Vector2 Velocity => motor != null ? motor.Velocity : Vector2.zero;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;      // вид сверху, падать некуда
            body.freezeRotation = true;  // от ударов о камни лодка не крутится

            if (data == null)
            {
                Debug.LogError("[BoatController] Не указан BoatData. Перетащи Data/Boats/Barkas в поле Data.", this);
                enabled = false;
                return;
            }

            motor = new BoatMotor(data);
            moveAction = CreateMoveAction();
        }

        private void OnEnable()
        {
            moveAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
        }

        private void OnDestroy()
        {
            moveAction?.Dispose();
        }

        private void FixedUpdate()
        {
            // Если лодка упёрлась в камень, физика уже уменьшила скорость. Сообщаем мотору.
            motor.SetVelocity(body.linearVelocity);

            Vector2 velocity = motor.Tick(ReadInput(), Time.fixedDeltaTime);
            body.linearVelocity = velocity;

            TurnSprite(velocity);
        }

        // Плывём только в состоянии Sailing. В меню, в магазине и во время рыбалки
        // клавиши не действуют, и лодка по инерции плавно останавливается.
        // На паузе время стоит (Time.timeScale = 0), FixedUpdate не вызывается.
        private Vector2 ReadInput()
        {
            GameManager game = GameManager.Instance;
            if (game != null && game.CurrentState != GameState.Sailing)
                return Vector2.zero;

            return moveAction.ReadValue<Vector2>();
        }

        // Лодка нарисована носом вправо. Плывём влево: отражаем картинку.
        private void TurnSprite(Vector2 velocity)
        {
            if (sprite == null)
                return;

            if (velocity.x < -0.05f) sprite.flipX = true;
            else if (velocity.x > 0.05f) sprite.flipX = false;
        }

        // Клавиши задаём прямо в коде, отдельный файл .inputactions не нужен.
        private static InputAction CreateMoveAction()
        {
            var action = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");

            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            action.AddBinding("<Gamepad>/leftStick");

            return action;
        }
    }
}
