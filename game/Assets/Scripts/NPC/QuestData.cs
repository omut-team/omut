using Omut.Fishing;
using UnityEngine;

namespace Omut.NPC
{
    public enum NpcId
    {
        Taras,
        Mikhalych
    }

    public enum QuestGoal
    {
        CatchFish,    // поймать N рыб вида fishId
        CatchAny,     // поймать N любых рыб
        CatchAtNight, // поймать N рыб ночью
        DeliverFish,  // принести N рыб вида fishId в трюме (отдаются NPC)
        EarnMoney     // заработать всего N монет за игру
    }

    // Задание. ScriptableObject, лежит в Data/Quests. Тексты: docs/texts/quests.md.
    [CreateAssetMenu(fileName = "NewQuest", menuName = "Omut/Quest Data")]
    public class QuestData : ScriptableObject
    {
        public string id = "q1";
        public string title = "Задание";
        public NpcId giver = NpcId.Taras;

        [Tooltip("Что сделать, для журнала заданий")]
        [TextArea(2, 3)] public string description = "";

        [Header("Цель")]
        public QuestGoal goal = QuestGoal.CatchFish;
        public string fishId = "";
        [Min(1)] public int count = 1;

        [Header("Награда")]
        [Min(0)] public int rewardMoney;
        public BaitType rewardBait = BaitType.Worm;
        [Min(0)] public int rewardBaitCount;
        [Tooltip("Особая награда: сюжетная отметка (например, free_repair)")]
        public string rewardFlag = "";

        [Header("Условия")]
        [Tooltip("Какое задание надо сдать раньше (пусто = сразу доступно)")]
        public string requiredQuest = "";

        [Header("Тексты")]
        [TextArea(2, 5)] public string giveText = "";
        [TextArea(2, 5)] public string completeText = "";
    }
}
