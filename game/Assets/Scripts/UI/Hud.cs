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

        private Image panel;
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
            panel = HudLayout.AddPanel(HudLayout.Left, "Stats", 470f, 100f);
            panel.transform.SetAsFirstSibling(); // монеты всегда сверху, страх под ними
            panelText = UiFactory.HudLabel(panel.transform, "", 27, TextAnchor.UpperLeft);
            UiFactory.Stretch(panelText.rectTransform, 22f);
            panelText.supportRichText = true;
            panelText.lineSpacing = 1.1f;

            Canvas canvas = UiFactory.CreateCanvas("PromptCanvas", 22, transform);
            promptText = UiFactory.HudLabel(canvas.transform, "", 36, TextAnchor.MiddleCenter);
            promptText.color = UiFactory.Theme.Gold;
            UiFactory.Place(promptText.rectTransform, new Vector2(0.5f, 0f), new Vector2(1400f, 60f), new Vector2(0f, 150f));
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
                HudLayout.SetHeight(panel, 40f + count * 34f);
                panel.gameObject.SetActive(count > 0);
            }

            promptText.text = visible && game != null && game.CurrentState == GameState.Sailing ? prompt ?? "" : "";
            prompt = null;
            promptPriority = int.MinValue;
        }
    }
}
