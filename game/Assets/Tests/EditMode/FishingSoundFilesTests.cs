using NUnit.Framework;
using Omut.Core;
using UnityEditor;
using UnityEngine;

namespace Omut.Tests
{
    // Звуки рыбалки из файлов (design/audio -> tools/make_sfx.py -> Assets/Audio/SFX).
    // Сборщик сцены подключает sfx_<id> только для id из ProceduralSounds.Ids, поэтому проверяем и это.
    public class FishingSoundFilesTests
    {
        [TestCase("cast")]
        [TestCase("fish_splash")]
        [TestCase("reel")]
        public void SoundFile_IsInGame(string id)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/SFX/sfx_{id}.wav");
            Assert.IsNotNull(clip, $"нет файла sfx_{id}.wav");
            Assert.Greater(clip.length, 0.5f, id);
            Assert.Less(clip.length, 5f, id);
            CollectionAssert.Contains(ProceduralSounds.Ids, id, "сборщик сцены не подключит этот звук");
        }
    }
}
