using System;
using Omut.Core;
using Omut.Fishing;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Карточка трофея: картинка, название, вес, редкость, цена. Кнопки "В трюм" и "Отпустить".
    public class CatchCard : MonoBehaviour
    {
        // Положить улов в трюм. Возвращает текст ошибки или null, если положили. Подставляет трюм (этап 9).
        public static Func<CatchInfo, string> TryStore = info => null;

        private RectTransform root;
        private Image icon;
        private Text title;
        private Text details;
        private Text note;
        private Text error;
        private CatchInfo current;
        private bool needsRender;

        private void Awake()
        {
            Canvas canvas = UiFactory.CreateCanvas("CatchCardCanvas", 60, transform);
            RectTransform paper = UiFactory.Window(canvas.transform, "CatchCard", new Vector2(820f, 640f), "Трофей!", out _);
            root = (RectTransform)paper.parent;

            icon = UiFactory.Icon(paper, null, new Vector2(300f, 200f));
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(300f, 200f), new Vector2(0f, -80f));

            title = UiFactory.Label(paper, "", 44, TextAnchor.MiddleCenter, UiFactory.Theme.Ink);
            title.fontStyle = FontStyle.Bold;
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(760f, 60f), new Vector2(0f, -290f));

            details = UiFactory.Label(paper, "", 30, TextAnchor.UpperCenter, UiFactory.Theme.Ink);
            UiFactory.Place(details.rectTransform, new Vector2(0.5f, 1f), new Vector2(760f, 110f), new Vector2(0f, -350f));

            note = UiFactory.Label(paper, "", 28, TextAnchor.MiddleCenter, UiFactory.Theme.Accent);
            note.fontStyle = FontStyle.Bold;
            UiFactory.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(760f, 40f), new Vector2(0f, -462f));

            error = UiFactory.Label(paper, "", 26, TextAnchor.MiddleCenter, UiFactory.Theme.Bad);
            UiFactory.Place(error.rectTransform, new Vector2(0.5f, 0f), new Vector2(760f, 36f), new Vector2(0f, 100f));

            RectTransform buttons = UiFactory.Rect(paper, "Buttons");
            UiFactory.Place(buttons, new Vector2(0.5f, 0f), new Vector2(760f, 70f), new Vector2(0f, 24f));
            UiFactory.Row(buttons, 30f);
            UiFactory.Button(buttons, "В трюм (Space)", Keep, new Vector2(330f, 64f));
            UiFactory.Button(buttons, "Отпустить", Release, new Vector2(330f, 64f));

            root.gameObject.SetActive(false);
        }

        private void OnEnable() => GameEvents.FishCaught += Show;

        private void OnDisable() => GameEvents.FishCaught -= Show;

        // Показываем в LateUpdate, чтобы журнал успел отметить "новая запись" (он тоже слушает FishCaught).
        private void Show(CatchInfo info)
        {
            current = info;
            needsRender = true;
        }

        private void LateUpdate()
        {
            if (needsRender && current != null)
                Render(current);
            needsRender = false;
        }

        private void Render(CatchInfo info)
        {
            FishData fish = info.fish;

            icon.sprite = fish.sprite;
            icon.color = fish.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            title.text = fish.displayName;

            string price = fish.isChest ? "внутри страница дневника деда" : $"цена у Тараса: {info.price} мон.";
            if (info.caughtAtNight && !fish.isChest)
                price += " (ночной улов x3)";
            details.text = $"Вес: {info.weight:0.00} кг\n" +
                           $"Редкость: <color=#{ColorUtility.ToHtmlStringRGB(FishData.RarityColor(fish.rarity))}>{FishData.RarityName(fish.rarity)}</color>\n" +
                           price;
            details.supportRichText = true;

            note.text = info.isNewRecord ? "Новая запись в журнале!" : "";
            error.text = "";
            root.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (current != null && !needsRender && root.gameObject.activeSelf && Controls.Pressed(Controls.Cast))
                Keep();
        }

        private void Keep()
        {
            if (current == null)
                return;

            string problem = TryStore(current);
            if (problem != null)
            {
                error.text = problem;
                return;
            }

            GameEvents.RaiseCatchKept(current);
            Close();
        }

        private void Release()
        {
            if (current == null)
                return;

            Notifications.Show($"{current.Title} отпущен(а) обратно в озеро.");
            Close();
        }

        private void Close()
        {
            current = null;
            root.gameObject.SetActive(false);
            FishingController.Instance?.FinishCatch();
        }
    }
}
