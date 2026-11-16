using Omut.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Omut.UI
{
    // Основа для всех игровых окон: трюм, журнал, карта, квесты, магазин, диалог.
    // Одновременно открыто только одно окно (Active). Пока окно открыто, игра в состоянии Window
    // (или Shop для разговоров с NPC): лодка стоит, время идёт.
    // Esc или своя клавиша (ToggleKey) закрывают окно.
    public abstract class GameWindow : MonoBehaviour
    {
        public static GameWindow Active { get; private set; }

        protected Canvas Canvas { get; private set; }
        protected RectTransform Root { get; private set; }

        public bool IsOpen => Active == this;

        // Какое состояние игры включать, пока окно открыто.
        protected virtual GameState WindowState => GameState.Window;

        // Клавиша, которая открывает и закрывает окно. Key.None = только кодом.
        protected virtual Key ToggleKey => Key.None;
        protected virtual Key ToggleKeyAlt => Key.None;

        protected virtual int SortOrder => 50;

        // Построить окно внутри холста. Вернуть корневой объект окна (его будем прятать и показывать).
        protected abstract RectTransform Build(Transform canvas);

        // Обновить содержимое при открытии.
        protected virtual void Refresh() { }

        protected virtual void OnClosed() { }

        protected virtual void Awake()
        {
            Canvas = UiFactory.CreateCanvas(GetType().Name + "Canvas", SortOrder, transform);
            Root = Build(Canvas.transform);
            Root.gameObject.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            if (Active == this)
                Active = null;
        }

        public bool Open()
        {
            if (IsOpen)
            {
                Refresh();
                return true;
            }

            if (Active != null)
                return false;

            GameManager game = GameManager.Instance;
            if (game != null && game.CurrentState != WindowState && !game.ChangeState(WindowState))
                return false;

            Active = this;
            Root.gameObject.SetActive(true);
            Refresh();
            return true;
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            Active = null;
            Root.gameObject.SetActive(false);
            OnClosed();

            GameManager game = GameManager.Instance;
            if (game != null && game.CurrentState == WindowState && Active == null)
                game.ChangeState(GameState.Sailing);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        protected virtual void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.CurrentState == GameState.Paused || game.CurrentState == GameState.Menu)
                return;

            if (IsOpen && Controls.EscapePressed)
            {
                Controls.ConsumeEscape();
                Close();
                return;
            }

            bool keyPressed = (ToggleKey != Key.None && Controls.Pressed(ToggleKey))
                              || (ToggleKeyAlt != Key.None && Controls.Pressed(ToggleKeyAlt));
            if (!keyPressed)
                return;

            if (IsOpen)
                Close();
            else if (game.CurrentState == GameState.Sailing && CanOpenByKey())
                Open();
        }

        // Можно ли открыть клавишей прямо сейчас (например, карту нельзя, пока идёт рыбалка).
        protected virtual bool CanOpenByKey() => true;

        // Закрыть любое открытое окно (обморок, финал).
        public static void CloseActive()
        {
            if (Active != null)
                Active.Close();
        }
    }
}
