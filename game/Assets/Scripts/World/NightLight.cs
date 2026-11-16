using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Omut.World
{
    // Свет, который горит только вечером и ночью: маяк, окна хаты, лампа в гараже.
    // Маяк ещё и делает безопасную зону: рядом с ним падает паника (этап 18).
    [RequireComponent(typeof(Light2D))]
    public class NightLight : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<NightLight> All = new System.Collections.Generic.List<NightLight>();

        [Tooltip("Это маяк: в его свете безопасно")]
        [SerializeField] private bool isSafeLight;

        [Tooltip("Насколько далеко безопасно, клеток")]
        [SerializeField] private float safeRadius = 8f;

        [Tooltip("Мигание (окна) 0 = ровный свет")]
        [SerializeField] private float flicker;

        private Light2D lightSource;
        private float baseIntensity;

        public bool IsSafeLight => isSafeLight;
        public float SafeRadius => safeRadius;

        public void Setup(bool safe, float radius, float flickerAmount)
        {
            isSafeLight = safe;
            safeRadius = radius;
            flicker = flickerAmount;
        }

        private void Awake()
        {
            lightSource = GetComponent<Light2D>();
            baseIntensity = lightSource.intensity;
        }

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        private void Update()
        {
            DayNightCycle cycle = DayNightCycle.Instance;
            DayPhase phase = cycle != null ? cycle.Clock.Phase : DayPhase.Day;
            bool on = phase == DayPhase.Night || phase == DayPhase.Evening;
            lightSource.enabled = on;
            if (on && flicker > 0f)
                lightSource.intensity = baseIntensity * (1f - flicker + flicker * Mathf.PerlinNoise(Time.time * 3f, transform.position.x));
        }

        // Стоит ли точка в безопасном свете маяка.
        public static bool IsInSafeLight(Vector2 point)
        {
            foreach (NightLight l in All)
            {
                if (l.isSafeLight && Vector2.Distance(point, l.transform.position) <= l.safeRadius)
                    return true;
            }

            return false;
        }
    }
}
