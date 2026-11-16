using System;
using System.Collections.Generic;
using UnityEngine;

namespace Omut.NPC
{
    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Кто говорит: Тарас, Михалыч, Сымон")]
        public string speaker;

        [TextArea(2, 4)] public string text;

        public DialogueLine() { }

        public DialogueLine(string speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }
    }

    // Разговор: реплики по очереди. ScriptableObject, лежит в Data/Dialogues. Тексты: docs/texts/dialogues.md.
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Omut/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        public string id = "dialogue";
        public List<DialogueLine> lines = new List<DialogueLine>();
    }
}
