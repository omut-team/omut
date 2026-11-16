using NUnit.Framework;
using Omut.Core;

namespace Omut.Tests
{
    // Автотесты синтезированных звуков: каждый звук есть, не пустой и без перегруза.
    public class ProceduralSoundsTests
    {
        [Test]
        public void EverySound_IsGenerated()
        {
            foreach (string id in ProceduralSounds.Ids)
            {
                float[] data = ProceduralSounds.Generate(id);
                Assert.IsNotNull(data, id);
                Assert.Greater(data.Length, 100, id);
            }
        }

        [Test]
        public void Sounds_DoNotClip()
        {
            foreach (string id in ProceduralSounds.Ids)
            {
                foreach (float sample in ProceduralSounds.Generate(id))
                    Assert.LessOrEqual(System.Math.Abs(sample), 1.0f, id);
            }
        }

        [Test]
        public void Unknown_IsNull()
        {
            Assert.IsNull(ProceduralSounds.Generate("нет такого"));
        }

        [Test]
        public void Bell_IsLong()
        {
            Assert.Greater(ProceduralSounds.Generate("bell").Length, ProceduralSounds.Rate * 3);
        }
    }
}
