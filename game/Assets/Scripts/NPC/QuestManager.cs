using System;
using System.Collections.Generic;
using Omut.Fishing;

namespace Omut.NPC
{
    public enum QuestState
    {
        Locked,     // ещё нельзя взять (не сдано предыдущее)
        Available,  // можно взять у NPC
        Active,     // взято, выполняется
        Done        // сдано
    }

    // Что нужно знать квестам об игре (трюм и деньги). Так QuestManager не зависит от Unity.
    public interface IQuestContext
    {
        int HoldCount(string fishId);
        int TotalEarned { get; }
    }

    // Задания: какие взяты, сколько сделано, какие сданы. Обычный C#-класс, проверяется тестами (QuestManagerTests).
    public class QuestManager
    {
        private readonly List<QuestData> quests;
        private readonly HashSet<string> active = new HashSet<string>();
        private readonly HashSet<string> done = new HashSet<string>();
        private readonly Dictionary<string, int> progress = new Dictionary<string, int>();

        public event Action<QuestData> Accepted;
        public event Action<QuestData> Completed;
        public event Action<QuestData> ProgressChanged;

        public QuestManager(List<QuestData> quests)
        {
            this.quests = quests ?? new List<QuestData>();
        }

        public IReadOnlyList<QuestData> All => quests;

        public QuestData Find(string id) => quests.Find(q => q != null && q.id == id);

        public QuestState StateOf(QuestData q)
        {
            if (done.Contains(q.id)) return QuestState.Done;
            if (active.Contains(q.id)) return QuestState.Active;
            if (!string.IsNullOrEmpty(q.requiredQuest) && !done.Contains(q.requiredQuest)) return QuestState.Locked;
            return QuestState.Available;
        }

        public List<QuestData> WithState(QuestState state, NpcId? giver = null)
        {
            return quests.FindAll(q => q != null && StateOf(q) == state && (giver == null || q.giver == giver));
        }

        public int Progress(QuestData q, IQuestContext context)
        {
            switch (q.goal)
            {
                case QuestGoal.DeliverFish: return Math.Min(q.count, context?.HoldCount(q.fishId) ?? 0);
                case QuestGoal.EarnMoney: return Math.Min(q.count, context?.TotalEarned ?? 0);
                default: return Math.Min(q.count, progress.TryGetValue(q.id, out int n) ? n : 0);
            }
        }

        public bool IsComplete(QuestData q, IQuestContext context) => StateOf(q) == QuestState.Active && Progress(q, context) >= q.count;

        public bool Accept(QuestData q)
        {
            if (q == null || StateOf(q) != QuestState.Available)
                return false;

            active.Add(q.id);
            progress[q.id] = 0;
            Accepted?.Invoke(q);
            return true;
        }

        // Рыба поймана: обновить задания "поймать".
        public void OnFishCaught(CatchInfo info)
        {
            foreach (QuestData q in quests)
            {
                if (q == null || !active.Contains(q.id))
                    continue;

                bool counts = (q.goal == QuestGoal.CatchFish && info.fishId == q.fishId)
                              || (q.goal == QuestGoal.CatchAny && info.fish != null && info.fish.InJournal)
                              || (q.goal == QuestGoal.CatchAtNight && info.caughtAtNight);
                if (!counts)
                    continue;

                progress[q.id] = (progress.TryGetValue(q.id, out int n) ? n : 0) + 1;
                ProgressChanged?.Invoke(q);
            }
        }

        // Сдать. false = ещё не выполнено. Награду выдаёт вызывающий (GameSession).
        public bool TryComplete(QuestData q, IQuestContext context)
        {
            if (!IsComplete(q, context))
                return false;

            active.Remove(q.id);
            done.Add(q.id);
            progress.Remove(q.id);
            Completed?.Invoke(q);
            return true;
        }

        // ---------- сохранение ----------

        public List<string> ActiveIds() => new List<string>(active);
        public List<string> DoneIds() => new List<string>(done);

        public List<string> ProgressList()
        {
            var list = new List<string>();
            foreach (KeyValuePair<string, int> p in progress)
                list.Add(p.Key + ":" + p.Value);
            return list;
        }

        public void Load(List<string> activeIds, List<string> doneIds, List<string> progressList)
        {
            active.Clear();
            done.Clear();
            progress.Clear();
            if (activeIds != null) active.UnionWith(activeIds);
            if (doneIds != null) done.UnionWith(doneIds);
            if (progressList == null)
                return;

            foreach (string entry in progressList)
            {
                string[] parts = entry.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int n))
                    progress[parts[0]] = n;
            }
        }

        public void Clear() => Load(null, null, null);
    }
}
