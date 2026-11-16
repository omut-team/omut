using NUnit.Framework;
using Omut.Core;

namespace Omut.Tests
{
    // Автотесты настроек: значения по умолчанию, пределы, сохранение в JSON и обратно.
    public class SettingsDataTests
    {
        [Test]
        public void Defaults_AreSensible()
        {
            var data = new SettingsData();

            Assert.AreEqual(1f, data.masterVolume);
            Assert.AreEqual(0.7f, data.musicVolume, 0.0001f);
            Assert.IsTrue(data.fullscreen);
        }

        [Test]
        public void Clamp_KeepsVolumeInRange()
        {
            var data = new SettingsData { masterVolume = 5f, musicVolume = -1f, sfxVolume = 0.5f };

            data.Clamp();

            Assert.AreEqual(1f, data.masterVolume);
            Assert.AreEqual(0f, data.musicVolume);
            Assert.AreEqual(0.5f, data.sfxVolume);
        }

        [Test]
        public void Json_RoundTrip_KeepsValues()
        {
            var data = new SettingsData { masterVolume = 0.3f, musicVolume = 0.4f, fullscreen = false, resolutionWidth = 1280, resolutionHeight = 720 };

            SettingsData loaded = SettingsData.FromJson(data.ToJson());

            Assert.AreEqual(0.3f, loaded.masterVolume, 0.0001f);
            Assert.AreEqual(0.4f, loaded.musicVolume, 0.0001f);
            Assert.IsFalse(loaded.fullscreen);
            Assert.AreEqual(1280, loaded.resolutionWidth);
        }

        [Test]
        public void EmptyOrBrokenJson_GivesDefaults()
        {
            Assert.AreEqual(1f, SettingsData.FromJson("").masterVolume);
            Assert.AreEqual(1f, SettingsData.FromJson("{это не json").masterVolume);
        }

        [Test]
        public void MusicLevel_DependsOnMaster()
        {
            var data = new SettingsData { masterVolume = 0.5f, musicVolume = 0.5f };

            Assert.AreEqual(0.25f, data.MusicLevel, 0.0001f);
        }

        [Test]
        public void Clone_IsIndependent()
        {
            var data = new SettingsData();
            SettingsData copy = data.Clone();

            copy.masterVolume = 0.1f;

            Assert.AreEqual(1f, data.masterVolume);
        }
    }
}
