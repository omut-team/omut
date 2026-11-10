using UnityEngine;

namespace Omut.World
{
    // Камера плавно едет за лодкой и не выезжает за край карты.
    // Вешается на Main Camera, в поле Target перетащить лодку.
    public class CameraFollow : MonoBehaviour
    {
        [Tooltip("За кем следить (лодка)")]
        [SerializeField] private Transform target;

        [Tooltip("За сколько секунд камера примерно догоняет цель. 0 = сразу, без плавности")]
        [Min(0f)] [SerializeField] private float smoothTime = 0.2f;

        [Header("Границы карты")]
        [Tooltip("Не показывать то, что за краем карты")]
        [SerializeField] private bool useBounds = true;

        [Tooltip("Левый нижний угол карты (в клетках)")]
        [SerializeField] private Vector2 boundsMin = new Vector2(-20f, -12f);

        [Tooltip("Правый верхний угол карты (в клетках)")]
        [SerializeField] private Vector2 boundsMax = new Vector2(20f, 12f);

        private Camera cam;
        private Vector3 currentVelocity; // нужно для SmoothDamp, сами не трогаем

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            // В начале сразу ставим камеру на цель, чтобы не было "подлёта" через всю карту.
            if (target != null)
                transform.position = GetDesiredPosition();
        }

        // LateUpdate вызывается после того, как все объекты подвинулись в этом кадре,
        // поэтому камера не дёргается.
        private void LateUpdate()
        {
            if (target == null)
                return;

            transform.position = Vector3.SmoothDamp(transform.position, GetDesiredPosition(), ref currentVelocity, smoothTime);
        }

        private Vector3 GetDesiredPosition()
        {
            Vector3 position = target.position;
            position.z = transform.position.z; // камера всегда остаётся на своей высоте (z = -10)

            if (useBounds && cam != null && cam.orthographic)
            {
                float halfHeight = cam.orthographicSize;
                float halfWidth = halfHeight * cam.aspect;
                position.x = ClampAxis(position.x, boundsMin.x + halfWidth, boundsMax.x - halfWidth);
                position.y = ClampAxis(position.y, boundsMin.y + halfHeight, boundsMax.y - halfHeight);
            }

            return position;
        }

        // Если карта уже, чем экран, просто ставим камеру по центру карты.
        private static float ClampAxis(float value, float min, float max)
        {
            if (min > max)
                return (min + max) * 0.5f;

            return Mathf.Clamp(value, min, max);
        }

        // Рисуем рамку границ в окне Scene, чтобы было видно, где край карты.
        private void OnDrawGizmosSelected()
        {
            if (!useBounds)
                return;

            Gizmos.color = Color.yellow;
            Vector3 center = (boundsMin + boundsMax) * 0.5f;
            Vector3 size = boundsMax - boundsMin;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
