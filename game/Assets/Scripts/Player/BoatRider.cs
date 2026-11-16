using Omut.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Omut.Player
{
    // Сымон сидит в лодке и держит удочку (или фонарь, если взял его в руку на панели предметов).
    // Лодка и всё, что на ней, рисуется одной группой (SortingGroup), чтобы сортировка по высоте не ломалась.
    public class BoatRider : MonoBehaviour
    {
        public static BoatRider Instance { get; private set; }

        [SerializeField] private SpriteRenderer boatSprite;
        [SerializeField] private Sprite riderSprite;
        [SerializeField] private Sprite rodSprite;

        private static readonly Vector2 RiderPosition = new Vector2(0.05f, 0.45f);
        private static readonly Vector2 HandPosition = new Vector2(0.38f, 0.62f);
        // Кончик удочки внутри картинки item_rod (32x32, опора в центре): правый верхний угол.
        private static readonly Vector2 RodTipInSprite = new Vector2(0.34f, 0.37f);

        private SpriteRenderer rider;
        private SpriteRenderer held;

        public void Setup(SpriteRenderer boat, Sprite riderPicture, Sprite rod)
        {
            boatSprite = boat;
            riderSprite = riderPicture;
            rodSprite = rod;
        }

        private void Awake()
        {
            Instance = this;
            if (boatSprite == null)
                boatSprite = GetComponentInChildren<SpriteRenderer>();

            var group = GetComponent<SortingGroup>();
            if (group == null)
                group = gameObject.AddComponent<SortingGroup>();
            group.sortingOrder = boatSprite != null ? boatSprite.sortingOrder : 5;
            if (boatSprite != null)
                boatSprite.sortingOrder = 0;

            rider = Child("Rider", riderSprite, 1, RiderPosition);
            held = Child("Held", rodSprite, 2, HandPosition);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private SpriteRenderer Child(string name, Sprite sprite, int order, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private bool Flipped => boatSprite != null && boatSprite.flipX;

        // Откуда идёт леска: кончик удочки в руке Сымона.
        public Vector2 RodTip
        {
            get
            {
                float side = Flipped ? -1f : 1f;
                Vector2 local = new Vector2(side * (HandPosition.x + RodTipInSprite.x), HandPosition.y + RodTipInSprite.y);
                return (Vector2)transform.position + local;
            }
        }

        private void LateUpdate()
        {
            bool visible = !PlayerWalker.OnFoot && (boatSprite == null || boatSprite.enabled);
            float side = Flipped ? -1f : 1f;

            rider.enabled = visible;
            rider.flipX = Flipped;
            rider.transform.localPosition = new Vector2(side * RiderPosition.x, RiderPosition.y);

            Sprite inHand = Hotbar.HeldSprite != null ? Hotbar.HeldSprite : rodSprite;
            held.enabled = visible && inHand != null;
            held.sprite = inHand;
            held.flipX = Flipped;
            held.transform.localPosition = new Vector2(side * HandPosition.x, HandPosition.y);
            held.transform.localScale = Vector3.one * (Hotbar.HoldsLantern ? 0.6f : 1f);
        }
    }
}
