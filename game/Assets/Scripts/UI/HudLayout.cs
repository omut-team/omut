using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Две колонки HUD: слева (монеты, лодка, наживка, страх) и справа (часы, цель).
    // Панели кладутся друг под другом сами, поэтому ничего не налезает, даже если строк стало больше.
    public static class HudLayout
    {
        private static RectTransform left;
        private static RectTransform right;

        public static RectTransform Left
        {
            get
            {
                if (left == null) Build();
                return left;
            }
        }

        public static RectTransform Right
        {
            get
            {
                if (right == null) Build();
                return right;
            }
        }

        private static void Build()
        {
            Canvas canvas = UiFactory.CreateCanvas("HudCanvas", 20);
            left = Column(canvas.transform, "Left", new Vector2(0f, 1f), new Vector2(24f, -20f), TextAnchor.UpperLeft);
            right = Column(canvas.transform, "Right", new Vector2(1f, 1f), new Vector2(-24f, -20f), TextAnchor.UpperRight);
        }

        private static RectTransform Column(Transform parent, string name, Vector2 anchor, Vector2 position, TextAnchor alignment)
        {
            RectTransform column = UiFactory.Rect(parent, name);
            UiFactory.Place(column, anchor, new Vector2(560f, 900f), position);
            VerticalLayoutGroup layout = UiFactory.Column(column, 10f, alignment);
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            return column;
        }

        // Тёмная панель в колонке HUD высотой height.
        public static Image AddPanel(RectTransform column, string name, float width, float height)
        {
            Image panel = UiFactory.HudPanel(column, name);
            panel.rectTransform.sizeDelta = new Vector2(width, height);
            var element = panel.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.preferredWidth = width;
            return panel;
        }

        public static void SetHeight(Image panel, float height)
        {
            panel.GetComponent<LayoutElement>().preferredHeight = height;
        }
    }
}
