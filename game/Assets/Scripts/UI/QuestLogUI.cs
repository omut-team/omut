using System.Collections.Generic;
using System.Text;
using Omut.Core;
using Omut.NPC;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Omut.UI
{
    // Журнал заданий (L): текущие задания с прогрессом, сданные, и тайны озера от Тараса.
    public class QuestLogUI : GameWindow
    {
        private Text questsText;
        private Text loreText;

        protected override Key ToggleKey => Controls.Quests;

        protected override RectTransform Build(Transform canvas)
        {
            RectTransform paper = UiFactory.Window(canvas, "Quests", new Vector2(1500f, 860f), "Задания и тайны озера", out _);

            RectTransform left = UiFactory.Rect(paper, "QuestsArea");
            UiFactory.Place(left, new Vector2(0f, 1f), new Vector2(680f, 720f), new Vector2(50f, -100f));
            RectTransform leftContent = UiFactory.ScrollArea(left);
            UiFactory.Column(leftContent, 0f, TextAnchor.UpperLeft).childControlHeight = true;
            questsText = UiFactory.Label(leftContent, "", 28, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            questsText.supportRichText = true;
            questsText.rectTransform.sizeDelta = new Vector2(660f, 0f);
            questsText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform right = UiFactory.Rect(paper, "LoreArea");
            UiFactory.Place(right, new Vector2(1f, 1f), new Vector2(680f, 720f), new Vector2(-50f, -100f));
            RectTransform rightContent = UiFactory.ScrollArea(right);
            UiFactory.Column(rightContent, 0f, TextAnchor.UpperLeft).childControlHeight = true;
            loreText = UiFactory.Label(rightContent, "", 26, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            loreText.supportRichText = true;
            loreText.rectTransform.sizeDelta = new Vector2(660f, 0f);
            loreText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return (RectTransform)paper.parent;
        }

        protected override void Refresh()
        {
            GameSession session = GameSession.Instance;
            if (session == null)
                return;

            QuestManager quests = session.Quests;
            var sb = new StringBuilder();
            sb.Append("<b>Сейчас</b>\n");

            List<QuestData> active = quests.WithState(QuestState.Active);
            if (active.Count == 0)
                sb.Append("Заданий нет. Поговори с Тарасом или Михалычем.\n");

            foreach (QuestData q in active)
            {
                bool complete = quests.IsComplete(q, session);
                sb.Append($"\n<b>{q.title}</b> ({GiverName(q.giver)})\n{q.description}\n");
                sb.Append(complete
                    ? $"<color=#2e7d32>Готово! Сдай: {GiverName(q.giver)}.</color>\n"
                    : $"Сделано: {quests.Progress(q, session)} из {q.count}\n");
            }

            List<QuestData> done = quests.WithState(QuestState.Done);
            if (done.Count > 0)
            {
                sb.Append("\n<b>Сделано</b>\n");
                foreach (QuestData q in done)
                    sb.Append($"<color=#6b6256>✓ {q.title}</color>\n");
            }

            questsText.text = sb.ToString();

            List<NoteData> lore = session.LoreNotes();
            var lb = new StringBuilder($"<b>Тайны озера: {session.LorePieces} из {lore.Count}</b>\n");
            lb.Append("<color=#6b6256>Тарас рассказывает их за редкую рыбу, пойманную ночью.</color>\n");
            for (int i = 0; i < session.LorePieces && i < lore.Count; i++)
                lb.Append($"\n<b>{lore[i].title}</b>\n{lore[i].text}\n");
            loreText.text = lb.ToString();
        }

        public static string GiverName(NpcId id) => id == NpcId.Taras ? "Тарас" : "Михалыч";
    }
}
