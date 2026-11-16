using System.Collections.Generic;
using System.Text;
using Omut.Core;
using Omut.NPC;
using Omut.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Подсказки, куда идти:
    // 1) справа сверху список целей ("Поговори с дедом Тарасом", "Поймай 3 карасей"...);
    // 2) стрелка у края экрана показывает на главную цель, если её не видно;
    // 3) над жителями табличка с именем и значок: "!" есть задание, "?" можно сдать.
    public class Guide : MonoBehaviour
    {
        private const float RefreshEvery = 0.3f;

        private Canvas canvas;
        private RectTransform canvasRect;
        private Image trackerPanel;
        private Text trackerText;
        private RectTransform arrow;
        private Text arrowDistance;
        private readonly Dictionary<Npc, (RectTransform tag, Image mark)> tags = new Dictionary<Npc, (RectTransform, Image)>();

        private List<Objective> objectives = new List<Objective>();
        private float timer;

        public static string LastText { get; private set; } = "";

        private void Awake()
        {
            canvas = UiFactory.CreateCanvas("GuideCanvas", 15, transform);
            canvasRect = (RectTransform)canvas.transform;

            trackerPanel = HudLayout.AddPanel(HudLayout.Right, "Objectives", 470f, 120f);
            Text title = UiFactory.HudLabel(trackerPanel.transform, "Что делать", 24, TextAnchor.UpperLeft);
            title.color = UiFactory.Theme.Gold;
            title.font = UiFactory.TitleFont;
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(420f, 32f), new Vector2(22f, -14f));
            trackerText = UiFactory.HudLabel(trackerPanel.transform, "", 23, TextAnchor.UpperLeft);
            trackerText.supportRichText = true;
            trackerText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Place(trackerText.rectTransform, new Vector2(0f, 1f), new Vector2(430f, 400f), new Vector2(22f, -48f));

            UiSkin skin = UiSkin.Current;
            Image arrowImage = UiFactory.Icon(canvas.transform, skin != null ? skin.arrow : null, new Vector2(64f, 64f));
            arrow = arrowImage.rectTransform;
            arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
            arrowDistance = UiFactory.HudLabel(canvas.transform, "", 22, TextAnchor.MiddleCenter);
            arrowDistance.rectTransform.sizeDelta = new Vector2(160f, 30f);
            arrowDistance.rectTransform.anchorMin = arrowDistance.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        }

        private void LateUpdate()
        {
            GameManager game = GameManager.Instance;
            GameState state = game != null ? game.CurrentState : GameState.Sailing;
            bool visible = GameSession.Instance != null && state != GameState.Menu && state != GameState.Shop
                           && state != GameState.Paused && GameWindow.Active == null;

            canvas.enabled = visible;
            trackerPanel.gameObject.SetActive(state != GameState.Menu && GameSession.Instance != null);
            if (!visible)
                return;

            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
            {
                timer = RefreshEvery;
                objectives = Objectives.Current();
                UpdateTracker();
            }

            UpdateArrow();
            UpdateTags();
        }

        private void UpdateTracker()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < objectives.Count && i < 4; i++)
            {
                string text = objectives[i].text;
                sb.AppendLine(i == 0 ? $"<color=#f2c94c>► {text}</color>" : "• " + text);
            }

            LastText = sb.ToString().TrimEnd();
            trackerText.text = LastText;
            HudLayout.SetHeight(trackerPanel, trackerText.preferredHeight + 70f);
        }

        // ---------- стрелка к главной цели ----------

        private static Vector2 PlayerPosition()
        {
            if (PlayerWalker.OnFoot && PlayerWalker.Instance != null)
                return PlayerWalker.Instance.transform.position;
            return DockingZone.Instance != null && DockingZone.Instance.Boat != null
                ? (Vector2)DockingZone.Instance.Boat.transform.position
                : Vector2.zero;
        }

        private void UpdateArrow()
        {
            Camera cam = Camera.main;
            Vector2? target = objectives.Count > 0 ? objectives[0].target : null;
            bool show = cam != null && target.HasValue;
            Vector2 local = Vector2.zero;
            bool onScreen = false;

            if (show)
            {
                Vector3 viewport = cam.WorldToViewportPoint(target.Value);
                onScreen = viewport.x > 0.05f && viewport.x < 0.95f && viewport.y > 0.08f && viewport.y < 0.92f;
                Vector2 size = canvasRect.rect.size;
                local = new Vector2((viewport.x - 0.5f) * size.x, (viewport.y - 0.5f) * size.y);
            }

            // Цель видно на экране: стрелка не нужна (над жителем и так есть значок).
            show = show && !onScreen;
            arrow.gameObject.SetActive(show);
            arrowDistance.gameObject.SetActive(show);
            if (!show)
                return;

            Vector2 half = canvasRect.rect.size * 0.5f - new Vector2(90f, 110f);
            Vector2 dir = local.normalized;
            float scale = Mathf.Min(Mathf.Abs(half.x / Mathf.Max(0.001f, Mathf.Abs(dir.x))), Mathf.Abs(half.y / Mathf.Max(0.001f, Mathf.Abs(dir.y))));
            Vector2 edge = dir * scale;
            // Внизу панель предметов: стрелку держим выше неё.
            edge.y = Mathf.Max(edge.y, -canvasRect.rect.size.y * 0.5f + 190f);
            float bob = Mathf.Sin(Time.unscaledTime * 5f) * 6f;

            arrow.anchoredPosition = edge + dir * bob;
            arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            arrowDistance.rectTransform.anchoredPosition = edge - dir * 56f;
            arrowDistance.text = Mathf.RoundToInt(Vector2.Distance(PlayerPosition(), target.Value)) + " м";
        }

        // ---------- таблички над жителями ----------

        private void UpdateTags()
        {
            Camera cam = Camera.main;
            GameSession session = GameSession.Instance;
            foreach (Interactable i in Interactable.All)
            {
                if (!(i is Npc npc))
                    continue;
                if (!tags.ContainsKey(npc))
                    tags[npc] = CreateTag(npc);

                (RectTransform tag, Image mark) = tags[npc];
                if (npc.Hidden)
                {
                    tag.gameObject.SetActive(false);
                    continue;
                }
                Vector3 viewport = cam != null ? cam.WorldToViewportPoint(npc.transform.position + new Vector3(0f, 1.75f, 0f)) : new Vector3(-1f, -1f, 0f);
                bool visible = viewport.x > -0.1f && viewport.x < 1.1f && viewport.y > -0.1f && viewport.y < 1.1f && viewport.z > 0f;
                tag.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                Vector2 size = canvasRect.rect.size;
                tag.anchoredPosition = new Vector2((viewport.x - 0.5f) * size.x, (viewport.y - 0.5f) * size.y);

                UiSkin skin = UiSkin.Current;
                Sprite markSprite = null;
                if (CanTurnIn(session, npc.Id)) markSprite = skin != null ? skin.markTurnIn : null;
                else if (HasNewQuest(session, npc.Id)) markSprite = skin != null ? skin.markQuest : null;
                mark.enabled = markSprite != null;
                mark.sprite = markSprite;
                mark.rectTransform.anchoredPosition = new Vector2(0f, 58f + Mathf.Sin(Time.unscaledTime * 4f) * 5f);
            }
        }

        private static bool HasNewQuest(GameSession session, NpcId id) =>
            session != null && (session.Quests.WithState(QuestState.Available, id).Count > 0 || !session.HasFlag("met_" + id));

        private static bool CanTurnIn(GameSession session, NpcId id)
        {
            if (session == null)
                return false;
            foreach (QuestData q in session.Quests.WithState(QuestState.Active, id))
                if (session.Quests.IsComplete(q, session))
                    return true;
            return false;
        }

        // Тараса все зовут дедом: так и подписан.
        public static string TagName(Npc npc) => npc.Id == NpcId.Taras ? "Дед Тарас" : npc.DisplayName;

        private (RectTransform, Image) CreateTag(Npc npc)
        {
            UiSkin skin = UiSkin.Current;
            string name = TagName(npc);
            Image back = UiFactory.Panel(canvas.transform, "Tag " + name, new Color(1f, 1f, 1f, 0.95f));
            if (skin != null && skin.tag != null)
            {
                back.sprite = skin.tag;
                back.type = Image.Type.Sliced;
                back.pixelsPerUnitMultiplier = 2.5f;
            }
            else
            {
                back.color = UiFactory.Theme.Hud;
            }

            RectTransform rect = back.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(30f + name.Length * 15f, 40f);
            back.raycastTarget = false;

            Text label = UiFactory.Label(back.transform, name, 24, TextAnchor.MiddleCenter, UiFactory.Theme.Ink);
            label.font = UiFactory.BoldFont;
            UiFactory.Stretch(label.rectTransform);
            label.raycastTarget = false;

            Image mark = UiFactory.Icon(back.transform, null, new Vector2(40f, 40f));
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            mark.raycastTarget = false;
            mark.color = Color.white;
            mark.enabled = false;
            return (rect, mark);
        }
    }
}
