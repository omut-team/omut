using Omut.Core;
using Omut.Player;
using Omut.World;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Шкала страха под часами и виньетка по краям экрана (с 50 и выше).
    public class PanicBar : MonoBehaviour
    {
        [SerializeField] private Sprite vignetteSprite;

        private RectTransform root;
        private Image fill;
        private Text label;
        private Image vignette;

        private void Awake()
        {
            Canvas canvas = UiFactory.CreateCanvas("PanicCanvas", 21, transform);

            vignette = UiFactory.Panel(canvas.transform, "Vignette", new Color(0.05f, 0f, 0.08f, 0f));
            vignette.sprite = vignetteSprite;
            vignette.raycastTarget = false;
            UiFactory.Stretch(vignette.rectTransform, -60f);
            vignette.transform.SetAsFirstSibling();

            // Панель в левой колонке HUD под монетами: "Страх 35  чай [C]: 1" и шкала.
            Image panel = HudLayout.AddPanel(HudLayout.Left, "Panic", 470f, 96f);
            root = panel.rectTransform;

            label = UiFactory.HudLabel(root, "Страх", 25, TextAnchor.MiddleLeft);
            UiFactory.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(430f, 34f), new Vector2(22f, -12f));

            fill = UiFactory.Bar(root, "Bar", new Vector2(426f, 26f), new Color(0.1f, 0.08f, 0.06f, 1f), UiFactory.Theme.Good);
            UiFactory.Place(UiFactory.BarRoot(fill), new Vector2(0f, 0f), new Vector2(426f, 26f), new Vector2(22f, 16f));
        }

        private void Update()
        {
            GameSession session = GameSession.Instance;
            GameManager game = GameManager.Instance;
            bool inGame = game != null && game.CurrentState != GameState.Menu;
            float panic = session != null ? session.Panic.Value : 0f;
            bool night = DayNightCycle.Instance != null && DayNightCycle.Instance.Clock.Phase == DayPhase.Night;

            root.gameObject.SetActive(inGame && (panic > 0.5f || night || FogController.InEternalFog));
            float k = panic / PanicMeter.Max;
            UiFactory.SetBar(fill, k);
            fill.color = Color.Lerp(UiFactory.Theme.Good, UiFactory.Theme.Bad, k);
            int thermos = session != null ? session.Thermos : 0;
            string level = panic >= PanicMeter.SteeringLevel ? "руки дрожат" : panic >= PanicMeter.WhisperLevel ? "жутко" : "спокойно";
            label.text = $"Страх: {Mathf.RoundToInt(panic)} ({level})     чай [C]: {thermos}";

            // Виньетка с 40 и пульсирует сильнее к 100.
            float strength = inGame ? Mathf.InverseLerp(40f, 100f, panic) : 0f;
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * (2f + 4f * strength));
            vignette.color = new Color(0.05f, 0f, 0.08f, Mathf.Clamp01(strength * 0.9f * pulse));
        }
    }
}
