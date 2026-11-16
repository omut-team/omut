using System.Collections.Generic;
using UnityEngine;

namespace Omut.Fishing
{
    // Рыбное место: круги на воде. Появляется, живёт какое-то время и пропадает.
    // Если поплавок упал в круги, клюёт быстрее и чаще попадается редкая рыба.
    public class FishingSpot : MonoBehaviour
    {
        public static readonly List<FishingSpot> All = new List<FishingSpot>();

        [SerializeField] private float radius = 1.3f;
        [SerializeField] private float lifetime = 30f;

        private SpriteRenderer ripple;
        private SpriteRenderer innerRipple;
        private float age;
        private Color baseColor = new Color(0.85f, 0.95f, 1f, 0.9f);

        public float Radius => radius;
        public bool IsAlive => age < lifetime;

        // Видна ли метка поверх тумана (эхолот у «Охотника»).
        public static bool ShowThroughFog { get; set; }

        // Материал без освещения: метка эхолота видна и в темноте. Ставит FogController.
        public static Material UnlitMaterial { get; set; }

        private Material litMaterial;

        public void Setup(Sprite sprite, float spotRadius, float spotLifetime)
        {
            radius = spotRadius;
            lifetime = spotLifetime;

            ripple = CreateRipple("Ripple", sprite, -5);
            innerRipple = CreateRipple("RippleInner", sprite, -5);
            litMaterial = ripple.sharedMaterial;
        }

        private SpriteRenderer CreateRipple(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = baseColor;
            return sr;
        }

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Плавно появляется первые 2 секунды и плавно гаснет последние 3.
            float fade = Mathf.Min(1f, age / 2f, (lifetime - age) / 3f);

            // Два круга расходятся по очереди, как от рыбы под водой.
            AnimateRipple(ripple, (age * 0.6f) % 1f, fade);
            AnimateRipple(innerRipple, (age * 0.6f + 0.5f) % 1f, fade);

            int order = ShowThroughFog ? 220 : -5;
            Material material = ShowThroughFog && UnlitMaterial != null ? UnlitMaterial : litMaterial;
            ripple.sortingOrder = order;
            innerRipple.sortingOrder = order;
            ripple.sharedMaterial = material;
            innerRipple.sharedMaterial = material;
        }

        private void AnimateRipple(SpriteRenderer sr, float t, float fade)
        {
            float size = Mathf.Lerp(0.4f, 1.2f, t) * radius;
            sr.transform.localScale = new Vector3(size, size * 0.5f, 1f); // овал 2:1: вид под 45°
            Color c = ShowThroughFog ? new Color(0.4f, 1f, 0.6f, 1f) : baseColor;
            c.a *= (1f - t) * fade;
            sr.color = c;
        }

        public bool Contains(Vector2 point) => Vector2.Distance(point, transform.position) <= radius;

        // Есть ли круги в этой точке.
        public static FishingSpot FindAt(Vector2 point)
        {
            foreach (FishingSpot spot in All)
            {
                if (spot.IsAlive && spot.Contains(point))
                    return spot;
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
