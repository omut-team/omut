using System;
using System.Collections.Generic;
using Omut.World;

namespace Omut.Core
{
    // Рыба в трюме для сохранения.
    [Serializable]
    public class SavedItem
    {
        public string fishId;
        public float weight;
        public bool night;
        public int price;
        public ZoneId zone;
        public int day;
        public int x;
        public int y;
        public bool rotated;
    }

    // Всё, что пишется в файл сохранения (JSON). Только простые данные: числа, строки, списки.
    // Рыбы, лодки и квесты хранятся по id, сами ассеты берутся из GameDatabase.
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string savedAt = "";

        // Время
        public int day = 1;
        public float minutes = 360f;

        // Деньги и запасы
        public int money;
        public int totalEarned;
        public float kerosene = 100f;
        public int thermos;
        public int spoons;
        public int phosphor;
        public string currentBait = "Worm";
        public int maggots;
        public int dough;
        public int leeches;
        public List<string> rods = new List<string>();
        public string rod = "bamboo";

        // 2 = мир по карте Ульяны. В старых сохранениях координаты лодки от прежнего озера.
        public int mapVersion;

        // Лодка и где стоим
        public string boatId = "Barkas";
        public List<string> ownedBoats = new List<string>();
        public int durability = -1;      // -1 = полная
        public bool onFoot = true;
        public float boatX;
        public float boatY;
        public float walkerX;
        public float walkerY;

        // Прогресс
        public List<JournalEntry> journal = new List<JournalEntry>();
        public List<SavedItem> hold = new List<SavedItem>();
        public List<string> flags = new List<string>();          // сюжетные отметки
        public List<string> activeQuests = new List<string>();
        public List<string> doneQuests = new List<string>();
        public List<string> questProgress = new List<string>();  // "id:число"
        public int lorePieces;
        public int diaryPages;
        public float panic;
        public int totalCaught;
    }
}
