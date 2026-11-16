using Omut.Fishing;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Шкала натяжения при вываживании: зелёный коридор, красная зона, стрелка натяжения,
    // под ней шкала прогресса до 100% и таймер обрыва лески.
    public class TensionBar : MonoBehaviour
    {
        private const float Width = 900f;

        private RectTransform root;
        private RectTransform needle;
        private Image progressFill;
        private Text title;
        private Text warning;
        private Text controls;

        private void Awake()
        {
            Canvas canvas = UiFactory.CreateCanvas("TensionCanvas", 32, transform);
            root = UiFactory.Rect(canvas.transform, "Tension");
            UiFactory.Place(root, new Vector2(0.5f, 0f), new Vector2(Width, 230f), new Vector2(0f, 70f));

            Image back = UiFactory.Panel(root, "Back", UiFactory.Theme.Hud);
            UiFactory.Stretch(back.rectTransform, -16f);

            title = UiFactory.HudLabel(root, "", 30, TextAnchor.UpperCenter);
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(Width, 40f), new Vector2(0f, 0f));

            // Шкала натяжения: серая основа, зелёный коридор, красная зона.
            Image bar = UiFactory.Panel(root, "TensionBar", new Color(0.25f, 0.25f, 0.25f, 1f));
            UiFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(Width, 44f), new Vector2(0f, -48f));
            Zone(bar.rectTransform, TensionModel.GreenMin, TensionModel.GreenMax, new Color(0.25f, 0.65f, 0.3f));
            Zone(bar.rectTransform, TensionModel.RedStart, 1f, new Color(0.75f, 0.18f, 0.12f));

            Image needleImage = UiFactory.Panel(bar.transform, "Needle", Color.white);
            needle = needleImage.rectTransform;
            needle.anchorMin = new Vector2(0f, -0.25f);
            needle.anchorMax = new Vector2(0f, 1.25f);
            needle.sizeDelta = new Vector2(8f, 0f);
            needleImage.gameObject.AddComponent<Outline>().effectColor = Color.black;

            progressFill = UiFactory.Bar(root, "Progress", new Vector2(Width, 22f), new Color(0f, 0f, 0f, 0.6f), UiFactory.Theme.Gold);
            UiFactory.Place(progressFill.transform.parent as RectTransform, new Vector2(0.5f, 1f), new Vector2(Width, 22f), new Vector2(0f, -112f));

            warning = UiFactory.HudLabel(root, "", 30, TextAnchor.MiddleCenter);
            warning.color = new Color(1f, 0.45f, 0.35f);
            UiFactory.Place(warning.rectTransform, new Vector2(0.5f, 1f), new Vector2(Width, 40f), new Vector2(0f, -140f));

            controls = UiFactory.HudLabel(root, "", 26, TextAnchor.MiddleCenter);
            UiFactory.Place(controls.rectTransform, new Vector2(0.5f, 0f), new Vector2(Width, 36f), new Vector2(0f, 0f));

            root.gameObject.SetActive(false);
        }

        private static void Zone(RectTransform bar, float from, float to, Color color)
        {
            Image zone = UiFactory.Panel(bar, "Zone", color);
            zone.rectTransform.anchorMin = new Vector2(from, 0f);
            zone.rectTransform.anchorMax = new Vector2(to, 1f);
            zone.rectTransform.offsetMin = zone.rectTransform.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            FishingController fishing = FishingController.Instance;
            TensionModel reel = fishing != null ? fishing.Reel : null;

            root.gameObject.SetActive(reel != null);
            if (reel == null)
                return;

            needle.anchorMin = new Vector2(reel.Tension, -0.25f);
            needle.anchorMax = new Vector2(reel.Tension, 1.25f);
            needle.GetComponent<Image>().color = reel.InRed ? new Color(1f, 0.4f, 0.3f) : (reel.InGreen ? Color.white : new Color(0.7f, 0.85f, 1f));

            UiFactory.SetBar(progressFill, reel.Progress);
            title.text = $"Вываживание: {Mathf.RoundToInt(reel.Progress * 100f)}%   Рыба тянет: {reel.Behaviour.Name}";

            if (reel.InRed)
                warning.text = $"Леска трещит! Обрыв через {Mathf.Max(0f, TensionModel.BreakSeconds - reel.RedTimer):0.0} с. Стравливай!";
            else if (reel.Tension < TensionModel.GreenMin)
                warning.text = "Леска провисла, рыба уходит. Подтягивай!";
            else
                warning.text = "";

            controls.text = reel.ControlsInverted
                ? "ОБРАТНАЯ ШКАЛА!  ПКМ: тянуть   ЛКМ: стравить"
                : "ЛКМ: тянуть   ПКМ: стравить   держи стрелку в зелёном";
            controls.color = reel.ControlsInverted ? new Color(0.6f, 1f, 0.8f) : Color.white;
        }
    }
}
