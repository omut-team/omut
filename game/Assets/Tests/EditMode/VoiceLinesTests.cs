using NUnit.Framework;
using Omut.Core;

namespace Omut.Tests
{
    // Код файла озвучки: одинаковый для одинаковой реплики, разный для разных говорящих.
    public class VoiceLinesTests
    {
        [Test]
        public void ClipName_SameLine_SameName()
        {
            Assert.AreEqual(VoiceLines.ClipName("Тарас", "Ну что, клюёт?"), VoiceLines.ClipName("Тарас", "Ну что, клюёт?"));
            StringAssert.StartsWith("v", VoiceLines.ClipName("Тарас", "Привет"));
            Assert.AreEqual(9, VoiceLines.ClipName("Тарас", "Привет").Length);
        }

        [Test]
        public void ClipName_DependsOnSpeaker()
        {
            Assert.AreNotEqual(VoiceLines.ClipName("Тарас", "Договорились."), VoiceLines.ClipName("Михалыч", "Договорились."));
        }

        [Test]
        public void Plain_RemovesTags_AndSyllablesCountVowels()
        {
            Assert.AreEqual("Биостанция. Текст", VoiceLines.Plain("<b>Биостанция.</b> Текст"));
            Assert.AreEqual(3, VoiceLines.Syllables("молоко"));
        }
    }
}
