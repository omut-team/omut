using NUnit.Framework;
using Omut.Fishing;
using Omut.Network;
using Omut.World;
using UnityEngine;

namespace Omut.Tests
{
    // Автотесты клиента сервера без сети: что отправляем и как читаем ответ.
    public class NetworkTests
    {
        private static FishData Fish(string id, int number, bool chest = false)
        {
            var fish = ScriptableObject.CreateInstance<FishData>();
            fish.id = id;
            fish.displayName = "Щука";
            fish.journalNumber = number;
            fish.isChest = chest;
            fish.pricePerKg = 10f;
            return fish;
        }

        [Test]
        public void Dto_HasAllFields()
        {
            FishData pike = Fish("pike", 6);
            CatchDto dto = CatchUploader.ToDto(new CatchInfo(pike, 5.556f, true, ZoneId.Pool, 3));

            Assert.AreEqual("pike", dto.fishId);
            Assert.AreEqual("Щука", dto.fishName);
            Assert.AreEqual(5.56, dto.weightKg, 0.001);
            Assert.IsTrue(dto.night);
            Assert.AreEqual("Pool", dto.zone);
            Assert.AreEqual(3, dto.gameDay);
            Object.DestroyImmediate(pike);
        }

        [Test]
        public void ScrapAndChest_NotUploaded()
        {
            FishData scrap = Fish("scrap", 0);
            FishData chest = Fish("grandpa_chest", 13, true);
            FishData pike = Fish("pike", 6);

            Assert.IsFalse(CatchUploader.ShouldUpload(new CatchInfo(scrap, 1f, false, ZoneId.Pool, 1)));
            Assert.IsFalse(CatchUploader.ShouldUpload(new CatchInfo(chest, 1f, true, ZoneId.Reef, 1)));
            Assert.IsTrue(CatchUploader.ShouldUpload(new CatchInfo(pike, 1f, false, ZoneId.Pool, 1)));

            Object.DestroyImmediate(scrap);
            Object.DestroyImmediate(chest);
            Object.DestroyImmediate(pike);
        }

        [Test]
        public void ParseRecords_ReadsServerArray()
        {
            string body = "[{\"id\":1,\"player\":\"Symon\",\"fishId\":\"pike\",\"fishName\":\"Щука\",\"weightKg\":7.25,\"night\":false}," +
                          "{\"id\":2,\"player\":\"Taras\",\"fishId\":\"carp\",\"fishName\":\"Карп\",\"weightKg\":3.1,\"night\":true}]";

            CatchRecordDto[] list = ApiClient.ParseRecords(body);

            Assert.AreEqual(2, list.Length);
            Assert.AreEqual("Symon", list[0].player);
            Assert.AreEqual(7.25, list[0].weightKg, 0.001);
            Assert.IsTrue(list[1].night);
        }

        [Test]
        public void ParseRecords_Empty()
        {
            Assert.AreEqual(0, ApiClient.ParseRecords("[]").Length);
            Assert.AreEqual(0, ApiClient.ParseRecords("").Length);
        }

        [Test]
        public void BaseUrl_TrimsSlash()
        {
            Assert.AreEqual("http://example:5000", new ApiClient("http://example:5000/").BaseUrl);
            Assert.AreEqual(ApiClient.DefaultBaseUrl, new ApiClient("").BaseUrl);
        }
    }
}
