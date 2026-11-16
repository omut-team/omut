using Omut.World;
using UnityEngine;

namespace Omut.Fishing
{
    // Создаёт рыбные места (круги на воде) в своей зоне: не больше maxSpots одновременно.
    // Вешается на объект зоны (рядом с компонентом Zone).
    [RequireComponent(typeof(Zone))]
    public class FishingSpotSpawner : MonoBehaviour
    {
        [Tooltip("Картинка кругов на воде (Art/Placeholders/fx_ripple)")]
        [SerializeField] private Sprite rippleSprite;

        [Tooltip("Сколько мест одновременно")]
        [Min(0)] [SerializeField] private int maxSpots = 4;

        [Tooltip("Раз в сколько секунд пробовать создать новое место")]
        [Min(0.5f)] [SerializeField] private float spawnInterval = 6f;

        [SerializeField] private Vector2 lifetimeRange = new Vector2(25f, 50f);
        [SerializeField] private float spotRadius = 1.3f;

        [Tooltip("Не ставить места ближе к краю зоны, клеток")]
        [SerializeField] private float margin = 3f;

        private Zone zone;
        private float timer;

        public void Setup(Sprite sprite, int max, float interval)
        {
            rippleSprite = sprite;
            maxSpots = max;
            spawnInterval = interval;
        }

        private void Awake()
        {
            zone = GetComponent<Zone>();
        }

        private void Start()
        {
            // Сразу несколько мест, чтобы озеро не было пустым в начале.
            for (int i = 0; i < maxSpots / 2; i++)
                Spawn(Random.Range(lifetimeRange.x, lifetimeRange.y) * Random.Range(0.3f, 1f));
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < spawnInterval)
                return;

            timer = 0f;
            if (CountMine() < maxSpots)
                Spawn(Random.Range(lifetimeRange.x, lifetimeRange.y));
        }

        private int CountMine()
        {
            int count = 0;
            foreach (FishingSpot spot in FishingSpot.All)
            {
                if (spot.transform.parent == transform)
                    count++;
            }

            return count;
        }

        private void Spawn(float lifetime)
        {
            Rect area = zone.Area;
            var position = new Vector2(
                Random.Range(area.xMin + margin, area.xMax - margin),
                Random.Range(area.yMin + margin, area.yMax - margin));

            // Не ставим круги на камни и берег.
            if (Physics2D.OverlapCircle(position, spotRadius) != null)
                return;

            var go = new GameObject("FishingSpot");
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.AddComponent<FishingSpot>().Setup(rippleSprite, spotRadius, lifetime);
        }
    }
}
