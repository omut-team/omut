using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Затемнение всего экрана (обморок, финал). FadeTo(1) = чёрный экран, FadeTo(0) = снова видно.
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        private Image black;

        public float Alpha => black.color.a;

        private void Awake()
        {
            Instance = this;
            Canvas canvas = UiFactory.CreateCanvas("FaderCanvas", 150, transform);
            black = UiFactory.Panel(canvas.transform, "Black", new Color(0f, 0f, 0f, 0f));
            UiFactory.Stretch(black.rectTransform);
            black.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public IEnumerator FadeTo(float target, float seconds)
        {
            black.raycastTarget = target > 0.5f;
            float start = black.color.a;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(start, target, t / seconds));
                yield return null;
            }

            SetAlpha(target);
        }

        public void SetAlpha(float a) => black.color = new Color(0f, 0f, 0f, Mathf.Clamp01(a));
    }
}
