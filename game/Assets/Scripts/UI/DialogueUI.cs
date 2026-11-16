using System;
using System.Collections.Generic;
using Omut.Core;
using Omut.NPC;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Разговор с NPC внизу экрана: портрет, имя, реплика, кнопки.
    // Два режима: реплики по очереди ("Дальше", Space или E) и меню выбора.
    public class DialogueUI : GameWindow
    {
        public static DialogueUI Instance { get; private set; }

        [SerializeField] private Sprite heroPortrait;

        private Image portrait;
        private Text speakerText;
        private Text bodyText;
        private RectTransform options;

        private Npc npc;
        private List<DialogueLine> queue;
        private int lineIndex;
        private Action afterLines;

        protected override GameState WindowState => GameState.Shop;

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
        }

        protected override RectTransform Build(Transform canvas)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.Rect(canvas, "Dialogue"));

            Image panel = UiFactory.PaperPanel(root, "Panel");
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(1760f, 410f), new Vector2(0f, 24f));

            // Портрет в своей деревянной рамке.
            Image portraitFrame = UiFactory.SlotPanel(panel.transform, "PortraitFrame");
            UiFactory.Place(portraitFrame.rectTransform, new Vector2(0f, 1f), new Vector2(260f, 260f), new Vector2(56f, -56f));
            portrait = UiFactory.Icon(portraitFrame.transform, null, new Vector2(224f, 224f));
            UiFactory.Place(portrait.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(224f, 224f), Vector2.zero);

            speakerText = UiFactory.Label(panel.transform, "", 36, TextAnchor.UpperLeft, UiFactory.Theme.Accent);
            speakerText.font = UiFactory.TitleFont;
            speakerText.fontStyle = FontStyle.Bold;
            UiFactory.Place(speakerText.rectTransform, new Vector2(0f, 1f), new Vector2(860f, 50f), new Vector2(350f, -48f));

            bodyText = UiFactory.Label(panel.transform, "", 31, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            bodyText.font = UiFactory.TitleFont;
            bodyText.lineSpacing = 1.1f;
            UiFactory.Place(bodyText.rectTransform, new Vector2(0f, 1f), new Vector2(860f, 290f), new Vector2(350f, -104f));
            bodyText.supportRichText = true;

            options = UiFactory.Rect(panel.transform, "Options");
            UiFactory.Place(options, new Vector2(1f, 1f), new Vector2(500f, 330f), new Vector2(-50f, -48f));
            UiFactory.Column(options, 8f, TextAnchor.UpperRight);

            return root;
        }

        // ---------- вход ----------

        public void Talk(Npc who)
        {
            npc = who;
            if (!Open())
                return;

            GameSession session = GameSession.Instance;
            string flag = "met_" + who.Id;
            if (session != null && !session.HasFlag(flag) && who.FirstMeeting != null)
            {
                session.SetFlag(flag);
                PlayLines(who.FirstMeeting.lines, () => ShowMenu(null));
            }
            else
            {
                ShowMenu(who.RandomGreeting());
            }
        }

        // Реплики без NPC (сундук, финал): показать и закрыть окно.
        public void Monologue(List<DialogueLine> lines, Action then = null)
        {
            npc = null;
            if (!Open())
                return;

            PlayLines(lines, () =>
            {
                Close();
                then?.Invoke();
            });
        }

        // ---------- реплики по очереди ----------

        public void PlayLines(List<DialogueLine> lines, Action then)
        {
            queue = lines;
            lineIndex = 0;
            afterLines = then;
            ShowLine();
        }

        private void ShowLine()
        {
            if (queue == null || lineIndex >= queue.Count)
            {
                queue = null;
                Action next = afterLines;
                afterLines = null;
                next?.Invoke();
                return;
            }

            DialogueLine line = queue[lineIndex];
            Show(line.speaker, line.text);

            ClearOptions();
            AddOption(lineIndex == queue.Count - 1 ? "Дальше" : "Дальше  (Space)", NextLine);
        }

        private void NextLine()
        {
            lineIndex++;
            ShowLine();
        }

        protected override void Update()
        {
            base.Update();
            if (IsOpen && queue != null && (Controls.Pressed(Controls.Cast) || Controls.Pressed(Controls.Interact)))
                NextLine();
        }

        // ---------- меню ----------

        public void ShowMenu(string greeting)
        {
            if (npc == null)
            {
                Close();
                return;
            }

            GameSession session = GameSession.Instance;
            Show(npc.DisplayName, greeting ?? "Чего тебе, внук Язэпа?");
            ClearOptions();

            if (session != null)
            {
                foreach ((string label, Action action) in session.QuestOptions(npc, this))
                    AddOption(label, action);

                if (npc.Id == NpcId.Taras)
                {
                    (string label, Action action)? lore = session.LoreOption(this);
                    if (lore.HasValue)
                        AddOption(lore.Value.label, lore.Value.action);
                }
            }

            foreach ((string label, Action action) in npc.ExtraOptions())
            {
                Action captured = action;
                AddOption(label, () =>
                {
                    Close();
                    captured();
                });
            }

            AddOption("До встречи", Close);
        }

        // Короткий ответ NPC и кнопка "Назад" в меню.
        public void Say(string text, Action back = null)
        {
            Show(npc != null ? npc.DisplayName : "", text);
            ClearOptions();
            AddOption("Назад", back ?? (() => ShowMenu("Что-нибудь ещё?")));
        }

        // Вопрос с ответами.
        public void Ask(string text, params (string label, Action action)[] answers)
        {
            Show(npc != null ? npc.DisplayName : "", text);
            ClearOptions();
            foreach ((string label, Action action) in answers)
                AddOption(label, action);
        }

        // ---------- помощники ----------

        // Показать реплику и озвучить её.
        private void Show(string speaker, string text)
        {
            SetSpeaker(speaker);
            bodyText.text = text;
            AudioManager.Instance?.PlayVoice(speaker, text);
        }

        private void SetSpeaker(string speaker)
        {
            speakerText.text = speaker;
            bool hero = speaker == "Сымон";
            portrait.sprite = hero ? heroPortrait : (npc != null ? npc.Portrait : null);
            portrait.color = portrait.sprite != null ? Color.white : Color.clear;
        }

        private void AddOption(string label, Action action)
        {
            Button button = UiFactory.Button(options, label, () => action(), new Vector2(490f, 60f), 25);
            button.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;
        }

        private void ClearOptions() => UiFactory.ClearChildren(options);

        protected override void OnClosed()
        {
            AudioManager.Instance?.StopVoice();
            queue = null;
            afterLines = null;
        }
    }
}
