using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Интерфейс нарисован под 1920x1080. Чтобы на любом мониторе (16:9, 16:10, 21:9, 4:3, маленькое окно)
    // ничего не обрезалось, подгоняем масштаб: экран уже 16:9 = вписываем по ширине, шире = по высоте.
    [RequireComponent(typeof(CanvasScaler))]
    public class AdaptiveScaler : MonoBehaviour
    {
        private const float ReferenceAspect = 16f / 9f;

        private CanvasScaler scaler;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
                Apply();
        }

        private void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            float aspect = lastHeight > 0 ? (float)lastWidth / lastHeight : ReferenceAspect;
            scaler.matchWidthOrHeight = MatchFor(aspect);
        }

        // 0 = по ширине, 1 = по высоте.
        public static float MatchFor(float aspect) => aspect < ReferenceAspect ? 0f : 1f;
    }
}
