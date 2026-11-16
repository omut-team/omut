using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Omut.UI
{
    // Набор помощников, чтобы собирать интерфейс кодом: холст, панели, тексты, кнопки, ползунки.
    // Оформление (шрифты, бумага, дерево) берётся из UiSkin (Resources/UiSkin), в стиле макета Ульяны.
    // Без UiSkin всё рисуется простыми цветными прямоугольниками.
    public static class UiFactory
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

        private static Font fallbackFont;

        private static UiSkin Skin => UiSkin.Current;

        private static Font Fallback
        {
            get
            {
                if (fallbackFont == null)
                    fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return fallbackFont;
            }
        }

        // Основной шрифт (PT Sans), жирный (PT Sans Bold) и шрифт заголовков (Lora).
        public static Font Font => Skin != null && Skin.bodyFont != null ? Skin.bodyFont : Fallback;
        public static Font BoldFont => Skin != null && Skin.boldFont != null ? Skin.boldFont : Font;
        public static Font TitleFont => Skin != null && Skin.titleFont != null ? Skin.titleFont : Font;

        // Картинка 32 px на клетку, холст 100 px на юнит: множитель, чтобы рамки были нужной толщины.
        private static void Slice(Image image, Sprite sprite, float multiplier)
        {
            if (sprite == null)
                return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = multiplier;
            image.color = Color.white;
        }

        // Бумага в деревянной рамке (окна, диалог, карточки).
        public static Image PaperPanel(Transform parent, string name)
        {
            Image image = Panel(parent, name, Theme.Paper);
            if (Skin != null && Skin.panel != null) Slice(image, Skin.panel, 2.2f);
            else image.gameObject.AddComponent<Outline>().effectColor = Theme.Ink;
            return image;
        }

        // Тёмная деревянная панель для HUD.
        public static Image HudPanel(Transform parent, string name)
        {
            Image image = Panel(parent, name, Theme.Hud);
            if (Skin != null && Skin.hud != null) Slice(image, Skin.hud, 2.4f);
            return image;
        }

        // Ячейка инвентаря (трюм, панель предметов, сумка).
        public static Image SlotPanel(Transform parent, string name)
        {
            Image image = Panel(parent, name, new Color(0.55f, 0.47f, 0.36f, 1f));
            if (Skin != null && Skin.slot != null) Slice(image, Skin.slot, 3f);
            return image;
        }

        // Цвета интерфейса "тетрадь рыбака".
        public static class Theme
        {
            public static readonly Color Paper = new Color(0.93f, 0.89f, 0.78f, 0.97f);
            public static readonly Color PaperDark = new Color(0.82f, 0.76f, 0.62f, 1f);
            public static readonly Color Ink = new Color(0.16f, 0.13f, 0.10f, 1f);
            public static readonly Color InkLight = new Color(0.38f, 0.33f, 0.27f, 1f);
            public static readonly Color Accent = new Color(0.55f, 0.24f, 0.14f, 1f);
            public static readonly Color Button = new Color(0.30f, 0.38f, 0.36f, 1f);
            public static readonly Color ButtonText = new Color(0.96f, 0.93f, 0.85f, 1f);
            public static readonly Color Shade = new Color(0f, 0f, 0f, 0.6f);
            public static readonly Color Hud = new Color(0.05f, 0.09f, 0.11f, 0.75f);
            public static readonly Color Good = new Color(0.30f, 0.65f, 0.30f, 1f);
            public static readonly Color Bad = new Color(0.80f, 0.20f, 0.15f, 1f);
            public static readonly Color Gold = new Color(0.95f, 0.78f, 0.25f, 1f);
        }

        // Холст поверх экрана. sortOrder: чем больше, тем выше (меню выше HUD).
        public static Canvas CreateCanvas(string name, int sortOrder, Transform parent = null)
        {
            EnsureEventSystem();

            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null)
                go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<AdaptiveScaler>(); // ничего не обрезается на любом мониторе
            return canvas;
        }

        // Без EventSystem кнопки не нажимаются. Создаём один на всю игру (новый Input System).
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        // Растянуть на всего родителя с отступом.
        public static RectTransform Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        // Поставить по якорю (0..1) с размером и сдвигом в пикселях (при 1920x1080).
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            RectTransform rect = Rect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        // Затемнение на весь экран + бумажная панель по центру. Возвращает панель.
        public static RectTransform Window(Transform parent, string name, Vector2 size, string title, out Text titleText)
        {
            Image shade = Panel(parent, name, Theme.Shade);
            Stretch(shade.rectTransform);

            Image paper = PaperPanel(shade.transform, "Paper");
            Place(paper.rectTransform, new Vector2(0.5f, 0.5f), size, Vector2.zero);

            titleText = Label(paper.transform, title, 42, TextAnchor.UpperCenter, Theme.Ink);
            titleText.font = TitleFont;
            titleText.fontStyle = FontStyle.Bold;
            Place(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(size.x - 120f, 60f), new Vector2(0f, -30f));

            return paper.rectTransform;
        }

        public static Text Label(Transform parent, string text, int size, TextAnchor alignment, Color color)
        {
            RectTransform rect = Rect(parent, "Text");
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        // Текст с чёрной обводкой: читается и на воде, и ночью.
        public static Text HudLabel(Transform parent, string text, int size, TextAnchor alignment)
        {
            Text label = Label(parent, text, size, alignment, Color.white);
            label.font = BoldFont;
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            return label;
        }

        public static Button Button(Transform parent, string text, UnityAction onClick, Vector2 size, int fontSize = 30)
        {
            Image image = Panel(parent, "Button " + text, Theme.Button);
            image.rectTransform.sizeDelta = size;

            var button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            button.colors = colors;

            button.onClick.AddListener(() => Core.GameEvents.RaisePlaySound("click"));
            if (onClick != null)
                button.onClick.AddListener(onClick);

            Text label = Label(image.transform, text, fontSize, TextAnchor.MiddleCenter, Theme.ButtonText);
            label.font = BoldFont;
            Stretch(label.rectTransform, 4f);

            // Широкая кнопка: дощечка как в меню Ульяны, узкая: деревянная плашка.
            if (Skin != null && size.x >= 260f && Skin.plank != null)
            {
                Slice(image, Skin.plank, 4f);
                label.color = Theme.Ink;
                label.rectTransform.offsetMin = new Vector2(70f, 4f);
                label.rectTransform.offsetMax = new Vector2(-70f, -4f);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 14;
                label.resizeTextMaxSize = fontSize;
            }
            else if (Skin != null && Skin.button != null)
            {
                Slice(image, Skin.button, 2.4f);
                label.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);
                label.rectTransform.offsetMin = new Vector2(16f, 2f);
                label.rectTransform.offsetMax = new Vector2(-16f, -2f);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 12;
                label.resizeTextMaxSize = fontSize;
            }

            if (Skin != null)
            {
                ColorBlock skinned = button.colors;
                skinned.normalColor = new Color(0.94f, 0.92f, 0.88f);
                skinned.highlightedColor = Color.white;
                skinned.selectedColor = new Color(0.94f, 0.92f, 0.88f);
                skinned.pressedColor = new Color(0.72f, 0.66f, 0.58f);
                skinned.disabledColor = new Color(0.55f, 0.52f, 0.5f, 0.6f);
                button.colors = skinned;
            }

            var layout = image.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            return button;
        }

        public static void SetButtonText(Button button, string text)
        {
            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = text;
        }

        // Столбик элементов один под другим.
        public static VerticalLayoutGroup Column(RectTransform rect, float spacing, TextAnchor alignment = TextAnchor.UpperCenter)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup Row(RectTransform rect, float spacing, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        // Полоса-индикатор (шкала): фон и заливка слева направо. Возвращает картинку заливки.
        public static Image Bar(Transform parent, string name, Vector2 size, Color background, Color fill)
        {
            Image back = Panel(parent, name, background);
            back.rectTransform.sizeDelta = size;
            float inset = 0f;
            if (Skin != null && Skin.bar != null)
            {
                Slice(back, Skin.bar, 2.5f); // деревянная рамка шкалы
                inset = 5f;
            }

            RectTransform area = Rect(back.transform, "Area");
            Stretch(area, inset);

            Image fillImage = Panel(area, "Fill", fill);
            fillImage.rectTransform.anchorMin = Vector2.zero;
            fillImage.rectTransform.anchorMax = new Vector2(0f, 1f);
            fillImage.rectTransform.pivot = new Vector2(0f, 0.5f);
            fillImage.rectTransform.offsetMin = Vector2.zero;
            fillImage.rectTransform.offsetMax = Vector2.zero;
            return fillImage;
        }

        // Вся шкала (рамка) по её заливке.
        public static RectTransform BarRoot(Image fill) => (RectTransform)fill.transform.parent.parent;

        // Заполнить шкалу на долю value (0..1).
        public static void SetBar(Image fill, float value)
        {
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        }

        // Ползунок с подписью: "Музыка  [=====|----]  70%".
        public static Slider Slider(Transform parent, string caption, float value, Action<float> onChange, float width = 700f)
        {
            RectTransform row = Rect(parent, "Slider " + caption);
            row.sizeDelta = new Vector2(width, 50f);

            Text label = Label(row, caption, 28, TextAnchor.MiddleLeft, Theme.Ink);
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(0.38f, 1f);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

            Text percent = Label(row, "", 26, TextAnchor.MiddleRight, Theme.InkLight);
            percent.rectTransform.anchorMin = new Vector2(0.86f, 0f);
            percent.rectTransform.anchorMax = new Vector2(1f, 1f);
            percent.rectTransform.offsetMin = percent.rectTransform.offsetMax = Vector2.zero;

            RectTransform sliderRect = Rect(row, "Slider");
            sliderRect.anchorMin = new Vector2(0.4f, 0.3f);
            sliderRect.anchorMax = new Vector2(0.84f, 0.7f);
            sliderRect.offsetMin = sliderRect.offsetMax = Vector2.zero;

            Image background = sliderRect.gameObject.AddComponent<Image>();
            background.color = Theme.PaperDark;

            RectTransform fillArea = Stretch(Rect(sliderRect, "Fill Area"));
            Image fill = Panel(fillArea, "Fill", Theme.Button);
            fill.rectTransform.sizeDelta = Vector2.zero;

            RectTransform handleArea = Stretch(Rect(sliderRect, "Handle Area"));
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);
            Image handle = Panel(handleArea, "Handle", Theme.Accent);
            handle.rectTransform.sizeDelta = new Vector2(20f, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, -0.4f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1.4f);

            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            percent.text = Mathf.RoundToInt(value * 100f) + "%";
            slider.onValueChanged.AddListener(v =>
            {
                percent.text = Mathf.RoundToInt(v * 100f) + "%";
                onChange?.Invoke(v);
            });

            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
            return slider;
        }

        // Галочка с подписью.
        public static Toggle Toggle(Transform parent, string caption, bool value, Action<bool> onChange, float width = 700f)
        {
            RectTransform row = Rect(parent, "Toggle " + caption);
            row.sizeDelta = new Vector2(width, 50f);

            Image box = Panel(row, "Box", Theme.PaperDark);
            box.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            box.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            box.rectTransform.pivot = new Vector2(0f, 0.5f);
            box.rectTransform.sizeDelta = new Vector2(40f, 40f);
            box.gameObject.AddComponent<Outline>().effectColor = Theme.Ink;

            Image check = Panel(box.transform, "Check", Theme.Accent);
            Stretch(check.rectTransform, 8f);

            Text label = Label(row, caption, 28, TextAnchor.MiddleLeft, Theme.Ink);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(56f, 0f);
            label.rectTransform.offsetMax = Vector2.zero;

            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(v => onChange?.Invoke(v));

            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
            return toggle;
        }

        // Строка "Подпись   < значение >" для выбора из списка (разрешение экрана и т.п.).
        public static Text Selector(Transform parent, string caption, string value, UnityAction onPrev, UnityAction onNext, float width = 700f)
        {
            RectTransform row = Rect(parent, "Selector " + caption);
            row.sizeDelta = new Vector2(width, 50f);

            Text label = Label(row, caption, 28, TextAnchor.MiddleLeft, Theme.Ink);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = new Vector2(0.4f, 1f);
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

            Button prev = Button(row, "<", onPrev, new Vector2(50f, 44f));
            Place((RectTransform)prev.transform, new Vector2(0.4f, 0.5f), new Vector2(50f, 44f), Vector2.zero);
            prev.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

            Button next = Button(row, ">", onNext, new Vector2(50f, 44f));
            Place((RectTransform)next.transform, new Vector2(1f, 0.5f), new Vector2(50f, 44f), Vector2.zero);

            Text valueText = Label(row, value, 28, TextAnchor.MiddleCenter, Theme.Ink);
            valueText.rectTransform.anchorMin = new Vector2(0.4f, 0f);
            valueText.rectTransform.anchorMax = new Vector2(1f, 1f);
            valueText.rectTransform.offsetMin = new Vector2(56f, 0f);
            valueText.rectTransform.offsetMax = new Vector2(-56f, 0f);

            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
            return valueText;
        }

        // Область с прокруткой колёсиком. Возвращает content: в него класть элементы.
        public static RectTransform ScrollArea(RectTransform area)
        {
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = Stretch(Rect(area, "Viewport"));
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.001f); // чтобы колёсико ловилось и в пустом месте

            RectTransform content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        // Поле ввода текста (имя, пароль, адрес сервера).
        public static InputField Input(Transform parent, string placeholder, bool password, Vector2 size)
        {
            Image back = Panel(parent, "Input " + placeholder, Color.white);
            back.rectTransform.sizeDelta = size;
            back.gameObject.AddComponent<Outline>().effectColor = Theme.InkLight;

            Text text = Label(back.transform, "", 28, TextAnchor.MiddleLeft, Theme.Ink);
            Stretch(text.rectTransform, 10f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.supportRichText = false;

            Text hint = Label(back.transform, placeholder, 28, TextAnchor.MiddleLeft, new Color(0.5f, 0.5f, 0.5f));
            Stretch(hint.rectTransform, 10f);
            hint.fontStyle = FontStyle.Italic;

            var input = back.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = hint;
            input.targetGraphic = back;
            input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            input.lineType = InputField.LineType.SingleLine;

            var layout = back.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            return input;
        }

        // Удалить все дочерние объекты (перед перестройкой списка).
        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                child.transform.SetParent(null, false);
                UnityEngine.Object.Destroy(child);
            }
        }

        // Картинка спрайта (иконка рыбы, предмета).
        public static Image Icon(Transform parent, Sprite sprite, Vector2 size)
        {
            Image image = Panel(parent, "Icon", Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            if (sprite == null)
                image.color = new Color(1f, 1f, 1f, 0f);
            return image;
        }
    }
}
