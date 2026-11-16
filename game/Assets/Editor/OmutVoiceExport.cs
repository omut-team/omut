using System.Collections.Generic;
using System.IO;
using System.Text;
using Omut.Core;
using Omut.NPC;
using UnityEditor;
using UnityEngine;

namespace Omut.EditorTools
{
    // Выгружает все реплики игры в tools/voice/lines.txt (код|говорящий|текст).
    // Потом tools/make_voice.ps1 озвучивает их голосом Windows в Assets/Resources/Voice.
    public static class OmutVoiceExport
    {
        private const string Output = "../tools/voice/lines.txt";

        [MenuItem("Omut/Озвучка: выгрузить реплики")]
        public static void Export()
        {
            GameDatabase db = OmutSceneBuilder.Database;
            var lines = new List<(string speaker, string text)>();

            foreach (DialogueData d in db.dialogues)
                if (d != null)
                    foreach (DialogueLine l in d.lines)
                        lines.Add((l.speaker, l.text));

            foreach (QuestData q in db.quests)
            {
                if (q == null) continue;
                string giver = q.giver == NpcId.Taras ? "Тарас" : "Михалыч";
                lines.Add((giver, q.giveText));
                lines.Add((giver, q.completeText));
            }

            foreach (NoteData n in db.notes)
            {
                if (n == null) continue;
                if (n.kind == NoteKind.Lore) lines.Add(("Тарас", $"<b>{n.title}.</b> {n.text}"));
                if (n.kind == NoteKind.Finale && n.order == 1) lines.Add(("Письмо деда", n.text));
                if (n.kind == NoteKind.Finale && n.order == 2) lines.Add(("", n.text));
            }

            foreach (string who in new[] { "Тарас", "Михалыч" })
                foreach (string text in new[] { "Чего тебе, внук Язэпа?", "Что-нибудь ещё?", "Договорились.", "Как знаешь." })
                    lines.Add((who, text));

            // Приветствия жителей лежат в сцене.
            foreach (Npc npc in Object.FindObjectsByType<Npc>(FindObjectsSortMode.None))
            {
                SerializedProperty greetings = new SerializedObject(npc).FindProperty("greetings");
                for (int i = 0; i < greetings.arraySize; i++)
                    lines.Add((npc.DisplayName, greetings.GetArrayElementAtIndex(i).stringValue));
            }

            lines.Add(("Тарас", "Задаром не рассказываю. Принеси редкую рыбу, пойманную ночью: светящегося угря, двухголового окуня, слепыша или чёрного сома. Тогда поговорим."));
            lines.Add(("Сымон", "Пять страниц... На последней цифры. Это код от замка."));
            lines.Add(("Сымон", "Щёлк. Внутри старый компас и письмо."));
            lines.Add(("Сымон", "Ночью. В омут. На фосфорную блесну. Я сделаю это, дед."));
            lines.Add(("Сымон", "Тише, тише... Сейчас сниму. Этот крючок дедов, я его узнаю."));
            lines.Add(("Сымон", "Плыви. Ты свободен."));

            var sb = new StringBuilder();
            var seen = new HashSet<string>();
            foreach ((string speaker, string text) in lines)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;
                string name = VoiceLines.ClipName(speaker, text);
                if (!seen.Add(name)) continue;
                sb.Append(name).Append('|').Append(speaker).Append('|')
                  .Append(VoiceLines.Plain(text).Replace("\n", " ").Replace("|", " ")).Append('\n');
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Output)));
            File.WriteAllText(Output, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[OmutVoice] Реплик выгружено: {seen.Count} -> {Path.GetFullPath(Output)}");
        }
    }
}
