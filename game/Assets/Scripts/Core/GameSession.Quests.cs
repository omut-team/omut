using System;
using System.Collections.Generic;
using Omut.Fishing;
using Omut.Inventory;
using Omut.NPC;
using Omut.UI;
using UnityEngine;

namespace Omut.Core
{
    // Задания, сюжетные отметки и "тайна за редкую рыбу".
    public partial class GameSession : IQuestContext
    {
        private readonly HashSet<string> flags = new HashSet<string>();

        public QuestManager Quests { get; private set; }

        // Сколько кусков тайны озера рассказал Тарас.
        public int LorePieces { get; private set; }

        public bool HasFlag(string flag) => flags.Contains(flag);

        public void SetFlag(string flag) => flags.Add(flag);

        public void ClearFlag(string flag) => flags.Remove(flag);

        // IQuestContext
        public int HoldCount(string fishId) => Hold.CountOf(fishId);
        int IQuestContext.TotalEarned => Wallet.TotalEarned;

        private void ResetQuests()
        {
            flags.Clear();
            LorePieces = 0;
            Quests = new QuestManager(database != null ? database.quests : new List<QuestData>());
        }

        private void CaptureQuests(SaveData data)
        {
            data.flags = new List<string>(flags);
            data.activeQuests = Quests.ActiveIds();
            data.doneQuests = Quests.DoneIds();
            data.questProgress = Quests.ProgressList();
            data.lorePieces = LorePieces;
        }

        private void ApplyQuests(SaveData data)
        {
            flags.Clear();
            if (data.flags != null)
                flags.UnionWith(data.flags);
            Quests.Load(data.activeQuests, data.doneQuests, data.questProgress);
            LorePieces = Mathf.Clamp(data.lorePieces, 0, LoreNotes().Count);
        }

        // Строка задания на HUD.
        private string QuestHudLine()
        {
            List<QuestData> active = Quests.WithState(QuestState.Active);
            if (active.Count == 0)
                return null;

            QuestData q = active[0];
            string done = Quests.IsComplete(q, this) ? " <color=#f2c94c>сдать!</color>" : "";
            return $"Задание: {q.title} ({Quests.Progress(q, this)}/{q.count}){done}";
        }

        // Пункты меню разговора про задания этого NPC.
        public List<(string, Action)> QuestOptions(Npc npc, DialogueUI ui)
        {
            var list = new List<(string, Action)>();

            foreach (QuestData q in Quests.WithState(QuestState.Active, npc.Id))
            {
                QuestData quest = q;
                if (Quests.IsComplete(quest, this))
                    list.Add(("Сдать: " + quest.title, () => CompleteQuest(quest, ui)));
                else
                    list.Add(("Про задание: " + quest.title, () =>
                        ui.Say($"{quest.description}\nСделано: {Quests.Progress(quest, this)} из {quest.count}.")));
            }

            foreach (QuestData q in Quests.WithState(QuestState.Available, npc.Id))
            {
                QuestData quest = q;
                list.Add(("Задание: " + quest.title, () => ui.Ask(quest.giveText,
                    ("Возьмусь", () =>
                    {
                        Quests.Accept(quest);
                        Notifications.Show("Новое задание: " + quest.title, UiFactory.Theme.Gold);
                        ui.ShowMenu("Договорились.");
                    }),
                    ("Не сейчас", () => ui.ShowMenu("Как знаешь.")))));
            }

            return list;
        }

        private void CompleteQuest(QuestData q, DialogueUI ui)
        {
            // Задание "принести": рыба уходит из трюма.
            if (q.goal == QuestGoal.DeliverFish)
            {
                int left = q.count;
                foreach (GridItem item in new List<GridItem>(Hold.Items))
                {
                    if (left > 0 && item.catchInfo.fishId == q.fishId)
                    {
                        Hold.Remove(item);
                        left--;
                    }
                }
            }

            if (!Quests.TryComplete(q, this))
                return;

            var reward = new List<string>();
            if (q.rewardMoney > 0)
            {
                Wallet.Earn(q.rewardMoney);
                reward.Add(q.rewardMoney + " мон.");
            }

            if (q.rewardBaitCount > 0)
            {
                Tackle.Add(q.rewardBait, q.rewardBaitCount);
                reward.Add($"{BaitTitle(q.rewardBait)} x{q.rewardBaitCount}");
            }

            if (!string.IsNullOrEmpty(q.rewardFlag))
            {
                SetFlag(q.rewardFlag);
                if (q.rewardFlag == "free_repair")
                    reward.Add("бесплатный ремонт у Михалыча");
            }

            GameEvents.RaisePlaySound("quest");
            Notifications.Show("Задание выполнено: " + q.title, UiFactory.Theme.Gold);
            ui.Say(q.completeText + (reward.Count > 0 ? "\n<b>Награда:</b> " + string.Join(", ", reward) : ""));
        }

        // ---------- тайна за редкую рыбу ----------

        public List<NoteData> LoreNotes()
        {
            List<NoteData> list = database.notes.FindAll(n => n != null && n.kind == NoteKind.Lore);
            list.Sort((a, b) => a.order.CompareTo(b.order));
            return list;
        }

        // Редкая ночная рыба в трюме, которую можно отдать Тарасу за кусок тайны.
        private GridItem FindRareNightFish()
        {
            foreach (GridItem item in Hold.Items)
            {
                FishData fish = item.catchInfo.fish;
                if (fish != null && item.catchInfo.caughtAtNight && fish.rarity >= Rarity.Rare && !fish.isChest)
                    return item;
            }

            return null;
        }

        public (string, Action)? LoreOption(DialogueUI ui)
        {
            List<NoteData> notes = LoreNotes();
            if (LorePieces >= notes.Count)
                return null;

            return ("Расскажи про озеро (отдать редкую ночную рыбу)", () =>
            {
                GridItem fish = FindRareNightFish();
                if (fish == null)
                {
                    ui.Say("Задаром не рассказываю. Принеси редкую рыбу, пойманную ночью: светящегося угря, двухголового окуня, слепыша или чёрного сома. Тогда поговорим.");
                    return;
                }

                Hold.Remove(fish);
                NoteData note = notes[LorePieces];
                LorePieces++;
                Notifications.Show($"Тайна озера: {LorePieces} из {notes.Count}", UiFactory.Theme.Gold);
                GameEvents.RaisePlaySound("quest");
                ui.PlayLines(new List<DialogueLine>
                {
                    new DialogueLine("Тарас", $"{fish.catchInfo.Title}... Ну, раз принёс, слушай."),
                    new DialogueLine("Тарас", $"<b>{note.title}.</b> {note.text}")
                }, () => ui.ShowMenu("Остальное в другой раз."));
            });
        }
    }
}
