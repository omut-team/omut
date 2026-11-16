using System.Collections.Generic;
using System.Text;
using Omut.Fishing;
using Omut.NPC;
using Omut.UI;

namespace Omut.Core
{
    // Дневник деда: страницы из сундучков, которые ловятся ночью на рифе.
    public partial class GameSession
    {
        public const int DiaryPagesTotal = 5;

        public int DiaryPages { get; private set; }

        private void ResetDiary() => DiaryPages = 0;

        public List<NoteData> NotesOf(NoteKind kind)
        {
            List<NoteData> list = database.notes.FindAll(n => n != null && n.kind == kind);
            list.Sort((a, b) => a.order.CompareTo(b.order));
            return list;
        }

        public NoteData FinaleNote(int order) => NotesOf(NoteKind.Finale).Find(n => n.order == order);

        public void SetDiaryPages(int pages) => DiaryPages = UnityEngine.Mathf.Clamp(pages, 0, DiaryPagesTotal);

        // Пойман сундучок: в нём следующая страница дневника.
        private void OnChestCaught(CatchInfo info)
        {
            if (info.fish == null || !info.fish.isChest || DiaryPages >= DiaryPagesTotal)
                return;

            DiaryPages++;
            GameEvents.RaisePlaySound("chest");
            Notifications.Show($"В сундучке страница дневника деда ({DiaryPages} из {DiaryPagesTotal}). Читать: J, вкладка «Дневник деда».", UiFactory.Theme.Gold);
            if (DiaryPages == DiaryPagesTotal)
                Notifications.Show("Все страницы собраны! Сундук деда на причале можно открыть.", UiFactory.Theme.Gold);
        }

        // Текст вкладки "Дневник деда" в журнале.
        private string DiaryText()
        {
            List<NoteData> pages = NotesOf(NoteKind.Diary);
            var sb = new StringBuilder($"<b>Дневник деда Язэпа: {DiaryPages} из {DiaryPagesTotal} страниц</b>\n");
            if (DiaryPages == 0)
                sb.Append("\nСтраниц пока нет. Вырванные страницы лежат в сундучках на дне у рифа. Ловятся ночью на любую наживку.\n");

            for (int i = 0; i < DiaryPages && i < pages.Count; i++)
                sb.Append($"\n<b>{pages[i].title}</b>\n{pages[i].text}\n");

            if (DiaryPages >= DiaryPagesTotal)
                sb.Append(HasFlag(World.DockChest.OpenedFlag)
                    ? "\n<i>Сундук на причале открыт. Письмо деда прочитано.</i>"
                    : "\n<i>Все страницы собраны. Открой сундук деда на причале (E).</i>");
            return sb.ToString();
        }
    }
}
