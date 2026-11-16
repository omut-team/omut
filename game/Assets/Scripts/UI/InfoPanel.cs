using System;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Простое окно "заголовок + текст + кнопка Закрыть" (управление, об игре, сообщения).
    public class InfoPanel
    {
        private readonly RectTransform root;
        private readonly Text titleText;
        private readonly Text bodyText;
        private readonly Action onClose;

        public InfoPanel(Transform parent, Vector2 size, Action onClose = null)
        {
            this.onClose = onClose;
            RectTransform paper = UiFactory.Window(parent, "Info", size, "", out titleText);
            root = (RectTransform)paper.parent;

            bodyText = UiFactory.Label(paper, "", 30, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            UiFactory.Stretch(bodyText.rectTransform);
            bodyText.rectTransform.offsetMin = new Vector2(60f, 110f);
            bodyText.rectTransform.offsetMax = new Vector2(-60f, -100f);
            bodyText.lineSpacing = 1.15f;

            Button close = UiFactory.Button(paper, "Закрыть", Close, new Vector2(300f, 64f));
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(300f, 64f), new Vector2(0f, 24f));

            root.gameObject.SetActive(false);
        }

        public bool IsOpen => root.gameObject.activeSelf;

        public void Show(string title, string body)
        {
            titleText.text = title;
            bodyText.text = body;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            root.gameObject.SetActive(false);
            onClose?.Invoke();
        }
    }
}
