using Omut.Core;
using Omut.Fishing;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Подсказки рыбалки: "КЛЮЁТ! Space" во время поклёвки и что делать, пока ждём.
    public class BitePrompt : MonoBehaviour
    {
        private Text big;
        private Text hint;

        private void Awake()
        {
            Canvas canvas = UiFactory.CreateCanvas("BitePromptCanvas", 31, transform);

            big = UiFactory.HudLabel(canvas.transform, "КЛЮЁТ! Жми Space!", 64, TextAnchor.MiddleCenter);
            big.color = UiFactory.Theme.Gold;
            big.fontStyle = FontStyle.Bold;
            UiFactory.Place(big.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1200f, 100f), new Vector2(0f, 180f));

            hint = UiFactory.HudLabel(canvas.transform, "", 28, TextAnchor.MiddleCenter);
            UiFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(1400f, 44f), new Vector2(0f, 160f));
        }

        private void Update()
        {
            FishingController fishing = FishingController.Instance;
            GameManager game = GameManager.Instance;
            bool biting = fishing != null && fishing.IsBiting;

            big.enabled = biting;
            if (biting)
                big.transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.time * 18f));

            string text = "";
            if (fishing != null && game != null)
            {
                if (fishing.State == fishing.Waiting)
                    text = "Ждём поклёвку...   Space: вытащить снасть";
                else if (fishing.State == fishing.Idle && game.CurrentState == GameState.Sailing && FishingController.CanFishHere())
                    text = "Зажми Space, чтобы забросить (целься мышью)";
            }

            hint.text = text;
        }
    }
}
