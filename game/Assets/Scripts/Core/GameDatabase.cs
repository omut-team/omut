using System.Collections.Generic;
using Omut.Fishing;
using Omut.Inventory;
using Omut.NPC;
using Omut.Player;
using UnityEngine;

namespace Omut.Core
{
    // Список всех данных игры в одном ассете (Data/GameDatabase).
    // Сохранения хранят только id ("pike"), а по id отсюда берётся сам FishData.
    // Заполняется меню Omut > Создать данные игры.
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "Omut/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        public List<FishData> fish = new List<FishData>();
        public List<BoatData> boats = new List<BoatData>();
        public List<ItemData> items = new List<ItemData>();
        public List<BaitData> baits = new List<BaitData>();
        public List<QuestData> quests = new List<QuestData>();
        public List<NoteData> notes = new List<NoteData>();
        public List<DialogueData> dialogues = new List<DialogueData>();

        [Tooltip("Материал спрайтов без освещения (туман, метки эхолота): Art/Materials/SpriteUnlit")]
        public Material unlitMaterial;

        public DialogueData FindDialogue(string id) => dialogues.Find(d => d != null && d.id == id);

        public FishData FindFish(string id) => fish.Find(f => f != null && f.id == id);

        public BoatData FindBoat(string id) => boats.Find(b => b != null && b.name == id);

        public BaitData FindBait(BaitType type) => baits.Find(b => b != null && b.type == type);

        public ItemData FindItem(ItemKind kind) => items.Find(i => i != null && i.kind == kind);

        // Записи журнала по порядку (1-14).
        public List<FishData> JournalEntries()
        {
            List<FishData> list = fish.FindAll(f => f != null && f.InJournal);
            list.Sort((a, b) => a.journalNumber.CompareTo(b.journalNumber));
            return list;
        }
    }
}
