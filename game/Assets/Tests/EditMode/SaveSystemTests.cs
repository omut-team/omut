using System.IO;
using NUnit.Framework;
using Omut.Core;
using UnityEngine;

namespace Omut.Tests
{
    // Автотесты сохранений: JSON туда и обратно, битый файл, запись в папку.
    public class SaveSystemTests
    {
        private string folder;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Application.temporaryCachePath, "omut_test_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.FolderOverride = folder;
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.FolderOverride = null;
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        private static SaveData Sample()
        {
            var data = new SaveData { day = 4, minutes = 1250f, money = 777, spoons = 5, onFoot = false, boatX = 3.5f };
            data.journal.Add(new JournalEntry { fishId = "pike", count = 2, bestWeight = 6.1f, firstDay = 2 });
            data.hold.Add(new SavedItem { fishId = "carp", weight = 3f, x = 1, y = 2, rotated = true });
            data.flags.Add("met_taras");
            return data;
        }

        [Test]
        public void Json_RoundTrip()
        {
            SaveData loaded = SaveSystem.FromJson(SaveSystem.ToJson(Sample()));

            Assert.AreEqual(4, loaded.day);
            Assert.AreEqual(777, loaded.money);
            Assert.AreEqual("pike", loaded.journal[0].fishId);
            Assert.IsTrue(loaded.hold[0].rotated);
            Assert.Contains("met_taras", loaded.flags);
        }

        [Test]
        public void BrokenJson_IsNull()
        {
            Assert.IsNull(SaveSystem.FromJson("{ сломано"));
            Assert.IsNull(SaveSystem.FromJson(""));
        }

        [Test]
        public void File_SaveLoadDelete()
        {
            Assert.IsFalse(SaveSystem.Exists());

            Assert.IsTrue(SaveSystem.Save(Sample()));
            Assert.IsTrue(SaveSystem.Exists());
            Assert.AreEqual(1250f, SaveSystem.Load().minutes, 0.01f);

            SaveSystem.Delete();
            Assert.IsFalse(SaveSystem.Exists());
            Assert.IsNull(SaveSystem.Load());
        }

        [Test]
        public void Save_Twice_Overwrites()
        {
            SaveSystem.Save(Sample());
            var second = Sample();
            second.money = 1;
            SaveSystem.Save(second);

            Assert.AreEqual(1, SaveSystem.Load().money);
        }
    }
}
