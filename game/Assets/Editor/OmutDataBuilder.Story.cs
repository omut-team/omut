using System.Collections.Generic;
using Omut.Core;
using Omut.Fishing;
using Omut.NPC;
using UnityEditor;

namespace Omut.EditorTools
{
    // Сюжет: диалоги, задания, тайны озера, дневник деда, финал.
    // Черновые тексты (docs/texts/*.md). Сарина правит там, а сюда переносим.
    public static partial class OmutDataBuilder
    {
        static partial void BuildMore(GameDatabase db, bool overwrite)
        {
            db.dialogues = new List<DialogueData>
            {
                Dialogue("taras_first", overwrite,
                    ("Тарас", "Ты внук Язэпа? Похож. Такой же упрямый взгляд."),
                    ("Сымон", "Сымон. Приехал разобраться с хатой деда."),
                    ("Тарас", "Хата стоит, баркас на плаву. А вот Язэп три года как пропал на озере."),
                    ("Тарас", "Запомни правило: в 20:00 бьёт колокол маяка. После него держись у света. Маяк, фонарь, причал."),
                    ("Тарас", "Рыбу приноси мне, куплю. Наживку, керосин и чай тоже у меня. А для начала есть дело.")),
                Dialogue("mikhalych_first", overwrite,
                    ("Михалыч", "О, наследник! Баркас Язэпа я сто раз латал."),
                    ("Михалыч", "Ударишься о корягу в омуте или о скалу на рифе, тащи сюда. Чиню за монеты и железо со дна."),
                    ("Михалыч", "А надоест корыто, есть у меня катера: «Следопыт» и «Охотник за тенями». Недёшево, но оно того стоит.")),
                Dialogue("dock_chest_locked", overwrite,
                    ("Сымон", "Сундук деда. Замок тяжёлый, а на крышке выцарапано: «пять страниц».")),
            };

            db.quests = new List<QuestData>
            {
                Quest("q1_crucian", "Первый улов", NpcId.Taras, "Поймать 3 карасей в заливе. Червь, днём.",
                    QuestGoal.CatchFish, "crucian", 3, 40, BaitType.Spoon, 3, "", "",
                    "Начни с простого. Залив к югу от причала, там карась на червя берёт. Поймай трёх, а я посмотрю, будет ли из тебя рыбак.",
                    "Три карася! Язэп бы одобрил. Держи монеты и пару блёсен, в омуте пригодятся.", overwrite),
                Quest("q2_pike", "Щука к ужину", NpcId.Taras, "Принести Тарасу щуку из омута. Блесна, днём.",
                    QuestGoal.DeliverFish, "pike", 1, 90, BaitType.Worm, 0, "", "q1_crucian",
                    "Омут к востоку, вода там тёмная. Щука на блесну бьёт резко, рывками. Привези мне одну, давно ухи не ел.",
                    "Вот это зверь! Спасибо, Сымон. Только в омут после колокола не суйся.", overwrite),
                Quest("q3_scrap", "Железо со дна", NpcId.Mikhalych, "Принести Михалычу 3 куска металлолома. Ловится в омуте и на рифе.",
                    QuestGoal.DeliverFish, "scrap", 3, 60, BaitType.Worm, 0, "free_repair", "",
                    "Мне на ремонт железо нужно. В омуте и у рифа его полно, цепляется на любую наживку. Привези три куска.",
                    "Отличное железо! Держи монеты, а лодку один раз починю бесплатно.", overwrite),
                Quest("q4_night", "Ночной улов", NpcId.Taras, "Поймать 2 рыбы ночью, после колокола. Держись у света.",
                    QuestGoal.CatchAtNight, "", 2, 50, BaitType.Phosphor, 3, "", "q2_pike",
                    "Хочешь понять озеро, выйди ночью. Только фонарь не гаси (F) и держи чай под рукой. Поймай две рыбы после колокола.",
                    "Живой вернулся, и с уловом. Вот тебе фосфорные блёсны, на них ночные уроды клюют.", overwrite),
                Quest("q5_money", "На новую лодку", NpcId.Mikhalych, "Заработать за игру 1000 монет.",
                    QuestGoal.EarnMoney, "", 1000, 150, BaitType.Worm, 0, "", "q3_scrap",
                    "Баркас для рифа слабоват. Наберёшь тысячу монет, поговорим про «Следопыта». Скину тебе немного.",
                    "Тысяча! Ты серьёзный рыбак. Держи премию, на катер пригодится.", overwrite),
            };

            db.notes = new List<NoteData>
            {
                Note("lore1", NoteKind.Lore, 1, "Биостанция",
                    "В восьмидесятых на рифе построили биостанцию. Учёные что-то делали с рыбой. Потом станцию затопили, а всем велели забыть.", overwrite),
                Note("lore2", NoteKind.Lore, 2, "Первые уроды",
                    "Через год в сетях пошли рыбы с двумя головами. По ночам они светились. В записках учёных было про опыты, а старики говорили: это Вадзянік злится.", overwrite),
                Note("lore3", NoteKind.Lore, 3, "Колокол",
                    "Колокол на маяк повесил ещё мой отец, когда трое рыбаков не вернулись ночью. С тех пор правило: после колокола держись света.", overwrite),
                Note("lore4", NoteKind.Lore, 4, "Язэп",
                    "Твой дед не верил ни в Вадзяніка, ни в учёных. Ходил ночью к рифу, искал что-то на дне. Говорил, что станцию надо закрыть до конца.", overwrite),
                Note("lore5", NoteKind.Lore, 5, "Хозяин",
                    "В ночь, когда Язэп пропал, я видел с маяка тень под водой, больше лодки. Старики зовут её Хозяином Омута. Если он жив, он помнит твоего деда.", overwrite),

                Note("diary1", NoteKind.Diary, 1, "Страница 1",
                    "Третий год рыба в омуте странная. Светится по ночам, глаза белые. Тарас говорит: Вадзянік. Я думаю, это станция. Надо проверить самому.", overwrite),
                Note("diary2", NoteKind.Diary, 2, "Страница 2",
                    "Был у рифа ночью. Под водой огни, ровные, как окна. Станция не умерла, там что-то работает. Лодку едва не перевернуло о скалу.", overwrite),
                Note("diary3", NoteKind.Diary, 3, "Страница 3",
                    "Нашёл в тине ящик с бумагами станции. Опыт назывался «Омут»: рыбу кормили, чтобы росла быстрее. Самую первую назвали Хозяином.", overwrite),
                Note("diary4", NoteKind.Diary, 4, "Страница 4",
                    "Хозяин стар, как я. Он не злой, он больной: в нём крючки всех, кто его ловил. Это он держит мутантов у рифа и не пускает их в залив.", overwrite),
                Note("diary5", NoteKind.Diary, 5, "Страница 5",
                    "Я решил. Пойду ночью в омут на фосфорную блесну и сниму с него крючки. Если не вернусь, письмо оставлю в сундуке на причале. Страницы спрячу, чтобы прочитал только тот, кто дойдёт до конца. Сымон, ты поймёшь.", overwrite),

                Note("finale_letter", NoteKind.Finale, 1, "Письмо деда",
                    "Сымон, если ты читаешь это, я не вернулся. Хозяин Омута — первая рыба станции. В нём старые крючки, и от боли он мутит всё озеро. " +
                    "Выйди ночью в омут на фосфорную блесну, вытащи его и отпусти. Тогда туман над рифом уйдёт. Не бойся. Я рядом, на дне, у самой станции. Твой дед Язэп.", overwrite),
                Note("finale_end", NoteKind.Finale, 2, "Рассвет",
                    "Хозяин медленно ушёл в глубину. В его плавнике блеснул старый дедов компас. Над рифом впервые за много лет рассвело без тумана. " +
                    "Колокол в ту ночь так и не понадобился.", overwrite),
            };
        }

        private static DialogueData Dialogue(string id, bool overwrite, params (string speaker, string text)[] lines)
        {
            DialogueData d = LoadOrCreate<DialogueData>($"{DataRoot}/Dialogues/{id}.asset", out bool created);
            if (created || overwrite)
            {
                d.id = id;
                d.lines = new List<DialogueLine>();
                foreach ((string speaker, string text) in lines)
                    d.lines.Add(new DialogueLine(speaker, text));
                EditorUtility.SetDirty(d);
            }

            return d;
        }

        private static QuestData Quest(string id, string title, NpcId giver, string description, QuestGoal goal, string fishId, int count,
            int money, BaitType bait, int baitCount, string flag, string required, string give, string complete, bool overwrite)
        {
            QuestData q = LoadOrCreate<QuestData>($"{DataRoot}/Quests/{id}.asset", out bool created);
            if (created || overwrite)
            {
                q.id = id;
                q.title = title;
                q.giver = giver;
                q.description = description;
                q.goal = goal;
                q.fishId = fishId;
                q.count = count;
                q.rewardMoney = money;
                q.rewardBait = bait;
                q.rewardBaitCount = baitCount;
                q.rewardFlag = flag;
                q.requiredQuest = required;
                q.giveText = give;
                q.completeText = complete;
                EditorUtility.SetDirty(q);
            }

            return q;
        }

        private static NoteData Note(string id, NoteKind kind, int order, string title, string text, bool overwrite)
        {
            NoteData n = LoadOrCreate<NoteData>($"{DataRoot}/Notes/{id}.asset", out bool created);
            if (created || overwrite)
            {
                n.id = id;
                n.kind = kind;
                n.order = order;
                n.title = title;
                n.text = text;
                EditorUtility.SetDirty(n);
            }

            return n;
        }
    }
}
