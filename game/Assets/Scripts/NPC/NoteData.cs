using UnityEngine;

namespace Omut.NPC
{
    public enum NoteKind
    {
        Lore,    // тайна озера от Тараса за редкую рыбу
        Diary,   // страница дневника деда из сундучка
        Finale   // правда о деде в сундуке на причале
    }

    // Текстовая запись: кусок тайны, страница дневника, финал. Лежит в Data/Notes.
    [CreateAssetMenu(fileName = "NewNote", menuName = "Omut/Note Data")]
    public class NoteData : ScriptableObject
    {
        public string id = "note";
        public NoteKind kind = NoteKind.Lore;
        [Tooltip("Порядковый номер внутри своего вида (1, 2, 3...)")]
        public int order = 1;
        public string title = "Запись";
        [TextArea(4, 12)] public string text = "";
    }
}
