using System.Collections.Generic;
using Omut.Core;
using Omut.NPC;
using Omut.Player;
using Omut.World;
using UnityEngine;

namespace Omut.UI
{
    // Цель для игрока: текст и куда идти (точка на карте, может не быть).
    public struct Objective
    {
        public string text;
        public Vector2? target;

        public Objective(string text, Vector2? target)
        {
            this.text = text;
            this.target = target;
        }
    }

    // Что делать дальше. Считается из сюжета и заданий, поэтому всегда совпадает с тем, что в игре.
    // Первая цель главная: к ней показывает стрелка.
    public static class Objectives
    {
        public static List<Objective> Current()
        {
            var list = new List<Objective>();
            GameSession session = GameSession.Instance;
            if (session == null || session.Quests == null)
                return list;

            Npc taras = FindNpc(NpcId.Taras);
            Npc mikhalych = FindNpc(NpcId.Mikhalych);
            Vector2? dock = DockingZone.Instance != null ? DockingZone.Instance.transform.position : (Vector2?)null;

            if (!session.HasFlag("met_" + NpcId.Taras))
            {
                list.Add(new Objective("Поговори с дедом Тарасом на причале", Pos(taras)));
                return list;
            }

            // Вечер: колокол скоро, ночью вдали от света страшно.
            DayNightCycle cycle = DayNightCycle.Instance;
            DayPhase phase = cycle != null ? cycle.Clock.Phase : DayPhase.Day;
            bool atDock = PlayerWalker.OnFoot || (ZoneTracker.Current != null && ZoneTracker.Current.Id == ZoneId.Dock);
            if (phase == DayPhase.Evening && !atDock)
                list.Add(new Objective("Вернись к причалу до колокола", dock));

            foreach (QuestData q in session.Quests.WithState(QuestState.Active))
            {
                Npc giver = q.giver == NpcId.Taras ? taras : mikhalych;
                if (session.Quests.IsComplete(q, session))
                    list.Add(new Objective($"Сдай «{q.title}»: {GiverName(q.giver)}", Pos(giver)));
                else
                    list.Add(new Objective($"{Short(q)} ({session.Quests.Progress(q, session)}/{q.count})", QuestPlace(q)));
            }

            foreach (QuestData q in session.Quests.WithState(QuestState.Available))
            {
                Npc giver = q.giver == NpcId.Taras ? taras : mikhalych;
                list.Add(new Objective($"Возьми задание «{q.title}»: {GiverName(q.giver)}", Pos(giver)));
            }

            if (!session.HasFlag("met_" + NpcId.Mikhalych) && session.Quests.WithState(QuestState.Available, NpcId.Mikhalych).Count == 0)
                list.Add(new Objective("Загляни к Михалычу в гараж", Pos(mikhalych)));

            if (session.DiaryPages < GameSession.DiaryPagesTotal && list.Count < 4)
                list.Add(new Objective($"Найди страницы дневника деда: {session.DiaryPages}/{GameSession.DiaryPagesTotal}", null));

            if (list.Count == 0)
                list.Add(new Objective("Лови рыбу, продавай Тарасу, копи на лодку", null));
            return list;
        }

        private static string GiverName(NpcId id) => id == NpcId.Taras ? "дед Тарас" : "Михалыч";

        // Описание задания короче, чтобы влезло в строку.
        // Берём первое предложение описания: "Поймать 3 карасей в заливе" -> "Поймай 3 карасей в заливе".
        public static string Short(QuestData q)
        {
            string text = string.IsNullOrEmpty(q.description) ? q.title : q.description;
            int dot = text.IndexOf('.');
            if (dot > 0)
                text = text.Substring(0, dot);
            if (text.StartsWith("Поймать")) text = "Поймай" + text.Substring(7);
            else if (text.StartsWith("Принести")) text = "Принеси" + text.Substring(8);
            else if (text.StartsWith("Заработать")) text = "Заработай" + text.Substring(10);
            return text;
        }

        // Где выполнять задание: зона, где водится нужная рыба.
        private static Vector2? QuestPlace(QuestData q)
        {
            switch (q.fishId)
            {
                case "crucian": return ZoneCenter(ZoneId.Bay);
                case "pike": return ZoneCenter(ZoneId.Pool);
                case "scrap": return ZoneCenter(ZoneId.Pool);
                default: return null;
            }
        }

        private static Vector2? ZoneCenter(ZoneId id)
        {
            foreach (Zone zone in Object.FindObjectsByType<Zone>(FindObjectsSortMode.None))
                if (zone.Id == id)
                    return zone.Area.center;
            return null;
        }

        public static Npc FindNpc(NpcId id)
        {
            foreach (Interactable i in Interactable.All)
                if (i is Npc npc && npc.Id == id)
                    return npc;
            return null;
        }

        private static Vector2? Pos(Npc npc) => npc != null ? (Vector2)npc.transform.position : (Vector2?)null;
    }
}
