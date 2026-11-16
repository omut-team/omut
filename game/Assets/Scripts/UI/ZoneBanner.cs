using Omut.Core;
using Omut.World;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Крупное название зоны вверху экрана при въезде: "Глубокий омут", ниже "тревожно".
    public class ZoneBanner : MonoBehaviour
    {
        private const float ShowSeconds = 3f;

        private Text title;
        private Text subtitle;
        private float timer;

        private void Awake()
        {
            Canvas canvas = UiFactory.CreateCanvas("ZoneBannerCanvas", 25, transform);
            title = UiFactory.HudLabel(canvas.transform, "", 64, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(1400f, 90f), new Vector2(0f, -130f));
            subtitle = UiFactory.HudLabel(canvas.transform, "", 30, TextAnchor.MiddleCenter);
            UiFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(1400f, 50f), new Vector2(0f, -210f));
        }

        private void OnEnable() => GameEvents.ZoneEntered += Show;

        private void OnDisable() => GameEvents.ZoneEntered -= Show;

        private void Show(Zone zone)
        {
            title.text = zone.DisplayName;
            subtitle.text = zone.Data != null ? zone.Data.danger : "";
            timer = ShowSeconds;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            float a = Mathf.Clamp01(Mathf.Min(timer, ShowSeconds - timer) / 0.5f);
            title.color = new Color(1f, 0.95f, 0.85f, a);
            subtitle.color = new Color(0.85f, 0.85f, 0.85f, a);
        }
    }
}
