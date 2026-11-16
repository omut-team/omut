using Omut.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Omut.Player
{
    // Предмет в руке Сымона, когда он ходит по берегу: удочка или фонарь (что выбрано на панели предметов).
    public class HeldItemView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;

        private static readonly Vector2 Hand = new Vector2(0.3f, 0.55f);
        private SpriteRenderer held;

        public void Setup(SpriteRenderer bodySprite) => body = bodySprite;

        private void Awake()
        {
            if (body == null)
                body = GetComponent<SpriteRenderer>();

            var group = GetComponent<SortingGroup>();
            if (group == null)
                group = gameObject.AddComponent<SortingGroup>();
            group.sortingOrder = body.sortingOrder;
            body.sortingOrder = 0;

            var go = new GameObject("Held");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Hand;
            go.transform.localScale = Vector3.one * 0.6f;
            held = go.AddComponent<SpriteRenderer>();
            held.sortingOrder = 1;
        }

        private void LateUpdate()
        {
            Sprite sprite = Hotbar.HeldSprite;
            held.enabled = body.enabled && sprite != null;
            held.sprite = sprite;
            held.flipX = body.flipX;
            held.transform.localPosition = new Vector2(body.flipX ? -Hand.x : Hand.x, Hand.y);
        }
    }
}
