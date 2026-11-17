using Omut.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Omut.Player
{
    // Предмет в руке Сымона, когда он ходит по берегу: удочка или фонарь (что выбрано на панели предметов).
    // Предмет держится в правой руке и поворачивается вместе с Сымоном:
    // лицом к нам рука слева, спиной к нам рука справа и удочка за спиной, боком рука по центру и удочка вперёд.
    public class HeldItemView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;

        // Где ладонь на кадрах Ульяны (от точки опоры у ног, в клетках). Кадр 32x48, 32 пикселя = 1 клетка.
        private static readonly Vector2 HandFront = new Vector2(-0.25f, 0.5f);
        private static readonly Vector2 HandBack = new Vector2(0.22f, 0.5f);
        private static readonly Vector2 HandSide = new Vector2(0.02f, 0.5f);

        private const float RodScale = 0.6f;
        private const float LanternScale = 0.42f;
        // У картинки удочки ручка в левом нижнем углу: сдвиг от центра картинки до места хвата.
        private static readonly Vector2 RodGrip = new Vector2(0.19f, 0.19f);
        // Фонарь висит под ладонью.
        private static readonly Vector2 LanternGrip = new Vector2(0f, -0.16f);

        private SpriteRenderer held;
        private PlayerWalker walker;

        public void Setup(SpriteRenderer bodySprite) => body = bodySprite;

        private void Awake()
        {
            if (body == null)
                body = GetComponent<SpriteRenderer>();
            walker = GetComponent<PlayerWalker>();

            var group = GetComponent<SortingGroup>();
            if (group == null)
                group = gameObject.AddComponent<SortingGroup>();
            group.sortingOrder = body.sortingOrder;
            body.sortingOrder = 0;

            var go = new GameObject("Held");
            go.transform.SetParent(transform, false);
            held = go.AddComponent<SpriteRenderer>();
            held.sortingOrder = 1;
        }

        private void LateUpdate()
        {
            Sprite sprite = Hotbar.HeldSprite;
            held.enabled = body.enabled && sprite != null;
            held.sprite = sprite;
            if (!held.enabled)
                return;

            Vector2 face = walker != null ? walker.Facing : Vector2.down;
            Place(face, Hotbar.HoldsLantern, out Vector2 position, out bool flip, out bool behind);
            held.transform.localPosition = position;
            held.transform.localScale = Vector3.one * (Hotbar.HoldsLantern ? LanternScale : RodScale);
            held.flipX = flip;
            held.sortingOrder = behind ? -1 : 1; // за спиной: под Сымоном
        }

        // Где и как рисовать предмет для этого направления взгляда.
        public static void Place(Vector2 face, bool lantern, out Vector2 position, out bool flip, out bool behind)
        {
            Vector2 hand;
            if (face.y > 0.5f)
            {
                // Спиной к нам: правая рука справа, удочка смотрит вверх-вправо из-за спины.
                hand = HandBack;
                flip = false;
                behind = true;
            }
            else if (face.y < -0.5f)
            {
                // Лицом к нам: правая рука слева на экране, удочка вверх-влево.
                hand = HandFront;
                flip = true;
                behind = false;
            }
            else
            {
                // Боком: удочка вперёд, по ходу.
                flip = face.x < 0f;
                hand = new Vector2(flip ? -HandSide.x : HandSide.x, HandSide.y);
                behind = false;
            }

            if (lantern)
            {
                flip = false;
                position = hand + LanternGrip;
            }
            else
            {
                position = hand + new Vector2(flip ? -RodGrip.x : RodGrip.x, RodGrip.y);
            }
        }
    }
}
