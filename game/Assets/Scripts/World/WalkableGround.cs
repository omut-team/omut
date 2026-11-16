using UnityEngine;

namespace Omut.World
{
    // Метка: по этой земле можно ходить пешком (земля у причала и мостки).
    // Её коллайдер не пускает лодку, но не мешает герою.
    [RequireComponent(typeof(BoxCollider2D))]
    public class WalkableGround : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<WalkableGround> All = new System.Collections.Generic.List<WalkableGround>();

        private BoxCollider2D box;

        private void OnEnable()
        {
            box = GetComponent<BoxCollider2D>();
            All.Add(this);
        }

        private void OnDisable() => All.Remove(this);

        // Стоит ли точка на этой земле (с отступом от края).
        public bool Contains(Vector2 point, float margin)
        {
            Bounds b = box.bounds;
            return point.x >= b.min.x + margin && point.x <= b.max.x - margin &&
                   point.y >= b.min.y + margin && point.y <= b.max.y - margin;
        }

        public static bool IsWalkable(Vector2 point, float margin = 0.25f)
        {
            foreach (WalkableGround ground in All)
            {
                if (ground.Contains(point, margin))
                    return true;
            }

            return false;
        }
    }
}
