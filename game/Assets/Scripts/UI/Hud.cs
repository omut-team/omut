using System;
using System.Collections.Generic;
using System.Text;
using Omut.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Панель слева сверху (монеты, наживка, прочность...) и подсказка "E: ..." внизу экрана.
    // Системы сами добавляют свои строки: Hud.AddLine(() => "Монеты: 120").
    // Подсказку ставят каждый кадр: Hud.SetPrompt("E: поговорить"), она гаснет сама, если её не ставить.
    public class Hud : MonoBehaviour
    {
        private static readonly List<Func<string>> lines = new List<Func<string>>();
        private static string prompt;
        private static int promptPriority = int.MinValue;

        private RectTransform panel;
        private Text panelText;
        private Text promptText;

        // Добавить строку на панель. Возвращать null или "", если строку сейчас не показывать.
        public static void AddLine(Func<string> line)
        {
            if (!lines.Contains(line))
                lines.Add(line);
        }

        public static void RemoveLine(Func<string> line) => lines.Remove(line);

        // Подсказка внизу. Если за кадр поставили несколько, показывается с большим priority.
        public static void SetPrompt(string text, int priority = 0)
        {
            if (priority >= promptPriority)
            {
                prompt = text;
                promptPriority = priority;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            lines.Clear();
            prompt = null;
            promptPriority = int.MinValue;
        }

        private void Awake()
        {
            Canvas canvas = UiFactory.CreateCanvas("HudCanvas", 20, transform);

            Image back = UiFactory.Panel(canvas.transform, "Panel", UiFactory.Theme.Hud);
            panel = back.rectTransform;
            UiFactory.Place(panel, new Vector2(0f, 1f), new Vector2(470f, 100f), new Vector2(20f, -20f));
            panelText = UiFactory.HudLabel(panel, "", 28, TextAnchor.UpperLeft);
            UiFactory.Stretch(panelText.rectTransform, 14f);
            panelText.supportRichText = true;

            promptText = UiFactory.HudLabel(canvas.transform, "", 38, TextAnchor.MiddleCenter);
            promptText.color = UiFactory.Theme.Gold;
            UiFactory.Place(promptText.rectTransform, new Vector2(0.5f, 0f), new Vector2(1400f, 60f), new Vector2(0f, 90f));
        }

        private void LateUpdate()
        {
            GameManager game = GameManager.Instance;
            bool visible = game == null || (game.CurrentState != GameState.Menu);
            panel.gameObject.SetActive(visible);

            if (visible)
            {
                var sb = new StringBuilder();
                foreach (Func<string> line in lines)
                {
                    string text = line();
                    if (!string.IsNullOrEmpty(text))
                        sb.AppendLine(text);
                }

                string result = sb.ToString().TrimEnd();
                panelText.text = result;
                int count = result.Length == 0 ? 0 : result.Split('\n').Length;
                panel.sizeDelta = new Vector2(470f, 28f + count * 36f);
                panel.gameObject.SetActive(count > 0);
            }

            promptText.text = visible && game != null && game.CurrentState == GameState.Sailing ? prompt ?? "" : "";
            prompt = null;
            promptPriority = int.MinValue;
        }
    }
}
