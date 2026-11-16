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

            root = UiFactory.Rect(canvas.transform, "Panic");
            UiFactory.Place(root, new Vector2(1f, 1f), new Vector2(420f, 70f), new Vector2(-30f, -80f));

            label = UiFactory.HudLabel(root, "Страх", 26, TextAnchor.UpperRight);
            UiFactory.Place(label.rectTransform, new Vector2(1f, 1f), new Vector2(420f, 32f), Vector2.zero);

            fill = UiFactory.Bar(root, "Bar", new Vector2(420f, 22f), new Color(0f, 0f, 0f, 0.6f), UiFactory.Theme.Good);
            UiFactory.Place(fill.transform.parent as RectTransform, new Vector2(1f, 0f), new Vector2(420f, 22f), Vector2.zero);
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
            label.text = $"Страх {Mathf.RoundToInt(panic)}   чай [C]: {thermos}";

            // Виньетка с 40 и пульсирует сильнее к 100.
            float strength = inGame ? Mathf.InverseLerp(40f, 100f, panic) : 0f;
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * (2f + 4f * strength));
            vignette.color = new Color(0.05f, 0f, 0.08f, Mathf.Clamp01(strength * 0.9f * pulse));
        }
    }
}
