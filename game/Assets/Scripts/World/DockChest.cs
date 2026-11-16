using System.Collections.Generic;
using Omut.Core;
using Omut.NPC;
using Omut.UI;
using UnityEngine;

namespace Omut.World
{
    // Запертый сундук деда на причале. Открывается, когда собраны все 5 страниц дневника.
    // Внутри письмо Язэпа: после него ночью в омуте можно встретить Хозяина Омута (этап 22).
    public class DockChest : Interactable
    {
        public const string OpenedFlag = "chest_opened";
        public const string BossFlag = "boss_unlocked";

        [SerializeField] private SpriteRenderer sprite;

        public override string Prompt
        {
            get
            {
                GameSession session = GameSession.Instance;
                if (session != null && session.HasFlag(OpenedFlag))
                    return "перечитать письмо деда";
                int pages = session != null ? session.DiaryPages : 0;
                return pages >= GameSession.DiaryPagesTotal ? "открыть сундук деда" : $"сундук деда (страниц {pages} из {GameSession.DiaryPagesTotal})";
            }
        }

        public override void Interact()
        {
            GameSession session = GameSession.Instance;
            DialogueUI ui = DialogueUI.Instance;
            if (session == null || ui == null)
                return;

            NoteData letter = session.FinaleNote(1);
            if (session.HasFlag(OpenedFlag))
            {
                ui.Monologue(new List<DialogueLine> { new DialogueLine("Письмо деда", letter != null ? letter.text : "") });
                return;
            }

            if (session.DiaryPages < GameSession.DiaryPagesTotal)
            {
                DialogueData locked = session.Database.FindDialogue("dock_chest_locked");
                var lines = new List<DialogueLine>(locked != null ? locked.lines : new List<DialogueLine>());
                lines.Add(new DialogueLine("Сымон", $"Страниц у меня {session.DiaryPages} из {GameSession.DiaryPagesTotal}. Остальные в сундучках на дне у рифа. Ловить ночью, на любую наживку."));
                ui.Monologue(lines);
                return;
            }

            session.SetFlag(OpenedFlag);
            session.SetFlag(BossFlag);
            GameEvents.RaisePlaySound("chest");
            ui.Monologue(new List<DialogueLine>
            {
                new DialogueLine("Сымон", "Пять страниц... На последней цифры. Это код от замка."),
                new DialogueLine("Сымон", "Щёлк. Внутри старый компас и письмо."),
                new DialogueLine("Письмо деда", letter != null ? letter.text : ""),
                new DialogueLine("Сымон", "Ночью. В омут. На фосфорную блесну. Я сделаю это, дед.")
            }, () => Notifications.Show("Ночью в центре омута появится огромный водоворот. Это он.", UiFactory.Theme.Gold));
        }

        private void Update()
        {
            if (sprite == null || GameSession.Instance == null)
                return;
            // Открытый сундук чуть светлее.
            sprite.color = GameSession.Instance.HasFlag(OpenedFlag) ? new Color(1f, 0.95f, 0.8f) : Color.white;
        }
    }
}
