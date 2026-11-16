using Omut.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Титры после финала: текст едет снизу вверх, внизу итог игры. Потом "Рыбачить дальше" или "В главное меню".
    public class CreditsUI : MonoBehaviour
    {
        public static CreditsUI Instance { get; private set; }

        private const float Speed = 60f;

        private Canvas canvas;
        private RectTransform scroll;
        private Text body;
        private RectTransform buttons;
        private bool rolling;

        private void Awake()
        {
            Instance = this;
            canvas = UiFactory.CreateCanvas("CreditsCanvas", 140, transform);
            Image back = UiFactory.Panel(canvas.transform, "Credits", new Color(0.02f, 0.04f, 0.06f, 0.96f));
            UiFactory.Stretch(back.rectTransform);

            scroll = UiFactory.Rect(back.transform, "Scroll");
            scroll.anchorMin = scroll.anchorMax = new Vector2(0.5f, 0f);
            scroll.pivot = new Vector2(0.5f, 1f);
            scroll.sizeDelta = new Vector2(1200f, 2200f);

            body = UiFactory.HudLabel(scroll, "", 34, TextAnchor.UpperCenter);
            UiFactory.Stretch(body.rectTransform);
            body.supportRichText = true;
            body.lineSpacing = 1.2f;

            buttons = UiFactory.Rect(back.transform, "Buttons");
            UiFactory.Place(buttons, new Vector2(0.5f, 0f), new Vector2(900f, 80f), new Vector2(0f, 40f));
            UiFactory.Row(buttons, 30f);
            UiFactory.Button(buttons, "Рыбачить дальше", Continue, new Vector2(420f, 70f), 30);
            UiFactory.Button(buttons, "В главное меню", ToMenu, new Vector2(420f, 70f), 30);

            canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool IsOpen => canvas.gameObject.activeSelf;

        public void Show()
        {
            GameManager game = GameManager.Instance;
            if (game != null && game.CurrentState == GameState.Sailing)
                game.ChangeState(GameState.Window);

            body.text = CreditsText();
            scroll.anchoredPosition = new Vector2(0f, 0f);
            canvas.gameObject.SetActive(true);
            rolling = true;
            GameEvents.RaisePlaySound("quest");
        }

        private void Update()
        {
            if (!rolling)
                return;

            scroll.anchoredPosition += new Vector2(0f, Speed * Time.unscaledDeltaTime);
            if (scroll.anchoredPosition.y > 1080f + 1500f)
                scroll.anchoredPosition = new Vector2(0f, 1080f + 1500f);
        }

        private static string CreditsText()
        {
            GameSession session = GameSession.Instance;
            string stats = "";
            if (session != null)
            {
                int day = World.DayNightCycle.Instance != null ? World.DayNightCycle.Instance.Clock.Day : 1;
                stats = $"<b>Твой итог</b>\nДней на озере: {day}\nРыбы поймано: {session.TotalCaught}\n" +
                        $"Журнал: {session.Journal.CaughtCount} из 14\nЗаработано: {session.Wallet.TotalEarned} мон.\n" +
                        $"Тайн озера: {session.LorePieces} из 5\nЛодка: {session.CurrentBoat?.displayName}\n\n\n";
            }

            return "<size=90><b>ОМУТ</b></size>\n\n" +
                   "Хозяин ушёл на дно. Туман над рифом рассеялся.\nКолокол маяка теперь бьёт просто по привычке.\n\n\n" +
                   stats +
                   "<b>Команда</b>\n\n" +
                   "Ярослав\nкод игры, сервер, сайт, Docker\n\n" +
                   "Ульяна\nперсонажи, лодки, рыбы, интерфейс\n\n" +
                   "Лиза\nтайлы, окружение, звук\n\n" +
                   "Сарина\nтексты, диаграммы, тест-кейсы, документы\n\n" +
                   "Илья\nтимлид\n\n\n" +
                   "Групповой проект по ТРПО\nгруппа ПО-2409, 2026\n\n" +
                   "Unity 6, ASP.NET Core, PostgreSQL\n\n\n" +
                   "<size=44>Спасибо за игру!</size>";
        }

        private void Continue()
        {
            rolling = false;
            canvas.gameObject.SetActive(false);
            GameManager game = GameManager.Instance;
            if (game != null && game.CurrentState == GameState.Window)
                game.ChangeState(GameState.Sailing);
            GameSession.Instance?.SaveGame();
        }

        private void ToMenu()
        {
            GameSession.Instance?.SaveGame();
            rolling = false;
            canvas.gameObject.SetActive(false);
            GameManager.Instance?.ResetToMenu();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
