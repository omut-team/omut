using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Короткие сообщения внизу экрана: "Сорвалась!", "Леска оборвалась", "+120 монет".
    // Из любого скрипта: Notifications.Show("текст").
    public class Notifications : MonoBehaviour
    {
        private const int MaxLines = 4;
        private const float LineSeconds = 3.5f;

        private static Notifications instance;
        private static readonly Queue<(string, Color)> pending = new Queue<(string, Color)>();

        private readonly List<(Text text, float time)> lines = new List<(Text, float)>();
        private RectTransform column;

        public static void Show(string message) => Show(message, Color.white);

        public static void Show(string message, Color color)
        {
            if (instance != null) instance.Add(message, color);
            else pending.Enqueue((message, color));
        }

        private void Awake()
        {
            instance = this;
            Canvas canvas = UiFactory.CreateCanvas("NotificationsCanvas", 80, transform);
            column = UiFactory.Rect(canvas.transform, "Lines");
            UiFactory.Place(column, new Vector2(0.5f, 0f), new Vector2(1400f, 260f), new Vector2(0f, 260f));
            VerticalLayoutGroup layout = UiFactory.Column(column, 4f, TextAnchor.LowerCenter);
            layout.childControlHeight = false;

            while (pending.Count > 0)
            {
                (string m, Color c) = pending.Dequeue();
                Add(m, c);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Add(string message, Color color)
        {
            Text text = UiFactory.HudLabel(column, message, 34, TextAnchor.MiddleCenter);
            text.color = color;
            text.rectTransform.sizeDelta = new Vector2(1400f, 48f);
            lines.Add((text, Time.unscaledTime));

            if (lines.Count > MaxLines)
            {
                Destroy(lines[0].text.gameObject);
                lines.RemoveAt(0);
            }
        }

        private void Update()
        {
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                float age = Time.unscaledTime - lines[i].time;
                if (age > LineSeconds)
                {
                    Destroy(lines[i].text.gameObject);
                    lines.RemoveAt(i);
                    continue;
                }

                Color c = lines[i].text.color;
                c.a = Mathf.Clamp01((LineSeconds - age) / 0.6f);
                lines[i].text.color = c;
            }
        }
    }
}
