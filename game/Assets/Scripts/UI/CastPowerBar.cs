using Omut.Fishing;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Шкала силы заброса внизу экрана + метка на воде, куда упадёт поплавок.
    public class CastPowerBar : MonoBehaviour
    {
        [SerializeField] private Sprite aimSprite;

        private RectTransform root;
        private Image fill;
        private Text caption;
        private SpriteRenderer aim;

        private void Awake()
        {
            Canvas canvas = UiFactory.CreateCanvas("CastPowerCanvas", 30, transform);
            root = UiFactory.Rect(canvas.transform, "CastPower");
            UiFactory.Place(root, new Vector2(0.5f, 0f), new Vector2(700f, 90f), new Vector2(0f, 60f));

            caption = UiFactory.HudLabel(root, "Сила заброса", 30, TextAnchor.UpperCenter);
            UiFactory.Place(caption.rectTransform, new Vector2(0.5f, 1f), new Vector2(700f, 40f), Vector2.zero);

            fill = UiFactory.Bar(root, "Bar", new Vector2(700f, 36f), new Color(0f, 0f, 0f, 0.6f), UiFactory.Theme.Gold);
            UiFactory.Place(fill.transform.parent as RectTransform, new Vector2(0.5f, 0f), new Vector2(700f, 36f), Vector2.zero);

            var aimObject = new GameObject("CastAim");
            aimObject.transform.SetParent(transform, false);
            aim = aimObject.AddComponent<SpriteRenderer>();
            aim.sprite = aimSprite;
            aim.color = new Color(1f, 0.9f, 0.4f, 0.8f);
            aim.sortingOrder = 225;
            aim.transform.localScale = new Vector3(0.35f, 0.25f, 1f);

            root.gameObject.SetActive(false);
            aim.enabled = false;
        }

        private void Update()
        {
            FishingController fishing = FishingController.Instance;
            bool charging = fishing != null && fishing.State == fishing.Charging;

            root.gameObject.SetActive(charging);
            aim.enabled = charging;
            if (!charging)
                return;

            UiFactory.SetBar(fill, fishing.CastPower);
            float distance = Vector2.Distance(fishing.transform.position, fishing.AimTarget);
            caption.text = $"Сила заброса: {Mathf.RoundToInt(fishing.CastPower * 100f)}%   ({distance:0.0} кл.)  Отпусти Space";
            aim.transform.position = fishing.AimTarget;
        }
    }
}
