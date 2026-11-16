using System;
using System.Collections.Generic;
using Omut.Fishing;

namespace Omut.Core
{
    // Одна запись журнала: сколько раз поймана рыба и самый большой вес.
    [Serializable]
    public class JournalEntry
    {
        public string fishId;
        public int count;
        public float bestWeight;
        public int firstDay;
    }

    // Журнал рыбака: какие виды уже пойманы. Цель игры: 14 из 14.
    // Обычный C#-класс, проверяется тестами (JournalTests).
    public class Journal
    {
        public const int TotalEntries = 14;

        private readonly Dictionary<string, JournalEntry> entries = new Dictionary<string, JournalEntry>();

        public event Action<JournalEntry, bool> Updated; // (запись, новая ли)

        public int CaughtCount => entries.Count;
        public bool IsComplete => CaughtCount >= TotalEntries;

        public bool IsCaught(string fishId) => entries.ContainsKey(fishId);

        public JournalEntry Get(string fishId) => entries.TryGetValue(fishId, out JournalEntry e) ? e : null;

        // Записать улов. Возвращает true, если это новая запись (первый раз поймали этот вид).
        public bool Register(CatchInfo info)
        {
            if (info == null || info.fish == null || !info.fish.InJournal)
                return false;

            bool isNew = !entries.TryGetValue(info.fishId, out JournalEntry entry);
            if (isNew)
            {
                entry = new JournalEntry { fishId = info.fishId, firstDay = info.day };
                entries.Add(info.fishId, entry);
            }

            entry.count++;
            if (info.weight > entry.bestWeight)
                entry.bestWeight = info.weight;

            Updated?.Invoke(entry, isNew);
            return isNew;
        }

        // Для сохранения: список записей.
        public List<JournalEntry> ToList() => new List<JournalEntry>(entries.Values);

        public void Load(IEnumerable<JournalEntry> list)
        {
            entries.Clear();
            if (list == null)
                return;

            foreach (JournalEntry e in list)
            {
                if (e != null && !string.IsNullOrEmpty(e.fishId))
                    entries[e.fishId] = e;
            }
        }

        public void Clear() => entries.Clear();
    }
}
