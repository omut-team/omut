using Omut.Core;
using Omut.NPC;
using Omut.UI;
using Omut.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Omut.Player
{
    // Сымон пешком на причале: ходит по земле и мосткам (WASD), говорит с NPC (E).
    // Пока Сымон в лодке, этот объект спрятан.
    public class PlayerWalker : MonoBehaviour
    {
        public static PlayerWalker Instance { get; private set; }

        // true = Сымон на берегу, лодка стоит у мостков.
        public static bool OnFoot { get; set; }

        [SerializeField] private float speed = 3.5f;
        [SerializeField] private SpriteRenderer sprite;

        private const float FeetRadius = 0.25f;

        private static readonly Vector2[] FeetOffsets =
        {
            new Vector2(0.22f, 0f), new Vector2(-0.22f, 0f), new Vector2(0f, 0.12f), new Vector2(0f, -0.12f)
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => OnFoot = false;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Show(Vector2 position)
        {
            transform.position = position;
            sprite.enabled = true;
        }

        public void Hide() => sprite.enabled = false;

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (!OnFoot || (game != null && game.CurrentState != GameState.Sailing))
                return;

            Move(ReadInput() * speed * Time.deltaTime);

            Interactable target = Interactable.FindNearest(transform.position);
            if (target != null)
            {
                Hud.SetPrompt("E: " + target.Prompt, 10);
                if (Controls.Pressed(Controls.Interact))
                    target.Interact();
            }
        }

        private static Vector2 ReadInput()
        {
            Keyboard k = Keyboard.current;
            if (k == null)
                return Vector2.zero;

            var input = new Vector2(
                (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f),
                (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f));
            return Vector2.ClampMagnitude(input, 1f);
        }

        // Двигаемся по осям отдельно, чтобы скользить вдоль края, а не застревать.
        private void Move(Vector2 delta)
        {
            Vector2 position = transform.position;

            // Если уже стоим внутри чего-то (вышли из лодки рядом с NPC, сундуком), даём выйти,
            // иначе Сымон застрял бы навсегда: проверяем только землю под ногами.
            bool stuck = !CanStand(position);

            Vector2 tryX = position + new Vector2(delta.x, 0f);
            if (stuck ? WalkableGround.IsWalkable(tryX, 0f) : CanStand(tryX)) position = tryX;

            Vector2 tryY = position + new Vector2(0f, delta.y);
            if (stuck ? WalkableGround.IsWalkable(tryY, 0f) : CanStand(tryY)) position = tryY;

            transform.position = position;

            if (delta.x > 0.001f) sprite.flipX = false;
            else if (delta.x < -0.001f) sprite.flipX = true;
        }

        // Можно ли стоять: под ногами земля или мостки, и не упираемся в дом, маяк или NPC.
        public static bool CanStand(Vector2 point)
        {
            // Ноги (центр и 4 точки вокруг) должны стоять на земле или мостках.
            // Каждую точку проверяем отдельно: так можно перейти с мостков на землю через стык.
            if (!WalkableGround.IsWalkable(point, 0f))
                return false;
            foreach (Vector2 offset in FeetOffsets)
            {
                if (!WalkableGround.IsWalkable(point + offset, 0f))
                    return false;
            }

            foreach (Collider2D hit in Physics2D.OverlapCircleAll(point, FeetRadius))
            {
                if (hit.isTrigger || hit.GetComponent<WalkableGround>() != null || hit.GetComponentInParent<BoatController>() != null)
                    continue;
                return false;
            }

            return true;
        }
    }
}
