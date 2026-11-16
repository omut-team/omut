using Omut.Core;
using Omut.Player;
using UnityEngine;

namespace Omut.NPC
{
    // Анимация жителя по кадрам Ульяны (вниз, вверх, вбок; покой и два шага).
    // Стоит и поворачивается к Сымону, когда тот рядом. Если задан маршрут, ходит туда-сюда (пани Ядвига).
    // Если задан флаг, житель появляется только после него (дед Язэп после финала).
    public class NpcAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite[] down = new Sprite[3];
        [SerializeField] private Sprite[] up = new Sprite[3];
        [SerializeField] private Sprite[] side = new Sprite[3];

        [Tooltip("Куда ходит (смещение от начальной точки). Ноль: стоит на месте")]
        [SerializeField] private Vector2 walkOffset;
        [SerializeField] private float walkSpeed = 0.8f;

        [Tooltip("Появляется только когда в игре есть этот флаг")]
        [SerializeField] private string requiredFlag = "";

        private Vector2 home;
        private float pause;
        private bool goingOut = true;
        private Collider2D[] colliders;
        private Npc npc;

        public void Setup(SpriteRenderer sprite, Sprite[] downFrames, Sprite[] upFrames, Sprite[] sideFrames, Vector2 offset, string flag)
        {
            body = sprite;
            down = downFrames;
            up = upFrames;
            side = sideFrames;
            walkOffset = offset;
            requiredFlag = flag;
        }

        public bool Present => string.IsNullOrEmpty(requiredFlag) || (GameSession.Instance != null && GameSession.Instance.HasFlag(requiredFlag));

        private void Awake()
        {
            if (body == null)
                body = GetComponent<SpriteRenderer>();
            home = transform.position;
            colliders = GetComponents<Collider2D>();
            npc = GetComponent<Npc>();
        }

        private void Update()
        {
            bool present = Present;
            body.enabled = present;
            foreach (Collider2D c in colliders)
                c.enabled = present;
            if (npc != null)
                npc.Hidden = !present;
            if (!present)
                return;

            Vector2 move = Vector2.zero;
            GameManager game = GameManager.Instance;
            bool talking = game != null && game.CurrentState == GameState.Shop;
            if (walkOffset != Vector2.zero && !talking && !PlayerNear(1.6f))
                move = Walk();

            Vector2 look = move;
            if (look == Vector2.zero && PlayerNear(4f))
                look = (Vector2)PlayerWalker.Instance.transform.position - (Vector2)transform.position;

            Face(look, move != Vector2.zero);
        }

        private bool PlayerNear(float distance) =>
            PlayerWalker.OnFoot && PlayerWalker.Instance != null &&
            Vector2.Distance(PlayerWalker.Instance.transform.position, transform.position) <= distance;

        // Шаг к точке маршрута, пауза на концах.
        private Vector2 Walk()
        {
            if (pause > 0f)
            {
                pause -= Time.deltaTime;
                return Vector2.zero;
            }

            Vector2 target = goingOut ? home + walkOffset : home;
            Vector2 position = transform.position;
            Vector2 step = Vector2.MoveTowards(position, target, walkSpeed * Time.deltaTime) - position;
            transform.position = position + step;
            if (Vector2.Distance(transform.position, target) < 0.05f)
            {
                goingOut = !goingOut;
                pause = Random.Range(2f, 5f);
            }

            return step;
        }

        private void Face(Vector2 direction, bool walking)
        {
            Sprite[] frames = down;
            body.flipX = false;
            if (direction.sqrMagnitude > 0.0001f)
            {
                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                {
                    frames = side;
                    body.flipX = direction.x < 0f; // кадры Ульяны смотрят вправо
                }
                else
                {
                    frames = direction.y > 0f ? up : down;
                }
            }

            int index = walking ? 1 + Mathf.FloorToInt(Time.time * 6f) % 2 : 0;
            if (frames != null && frames.Length > index && frames[index] != null)
                body.sprite = frames[index];
        }
    }
}
