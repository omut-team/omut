using System.Collections;
using NUnit.Framework;
using Omut.Network;
using UnityEngine.TestTools;

namespace Omut.Tests
{
    // Проверка игры с настоящим сервером. Нужен запущенный docker compose up и переменная OMUT_SERVER=1,
    // иначе тест пропускается (чтобы Test Runner не падал без сервера).
    public class ServerIntegrationTests
    {
        private static bool Enabled => System.Environment.GetEnvironmentVariable("OMUT_SERVER") == "1";

        [UnityTest]
        public IEnumerator Register_Login_SendCatch_SeeInLeaderboard()
        {
            if (!Enabled)
            {
                Assert.Ignore("OMUT_SERVER не задан");
                yield break;
            }

            var api = new ApiClient(ApiClient.DefaultBaseUrl);
            string name = "test_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);

            ApiResult health = default;
            yield return api.Health(r => health = r);
            Assert.IsTrue(health.ok, "Сервер не отвечает: " + health.error);

            ApiResult reg = default;
            yield return api.Register(name, "secret123", r => reg = r);
            Assert.IsTrue(reg.ok, reg.error);
            string token = UnityEngine.JsonUtility.FromJson<AuthResponseDto>(reg.body).token;

            ApiResult login = default;
            yield return api.Login(name, "wrongpass", r => login = r);
            Assert.IsFalse(login.ok);
            Assert.AreEqual("Неверное имя или пароль", login.error);

            var dto = new CatchDto { fishId = "black_catfish", fishName = "Чёрный сом", weightKg = 99.5, night = true, zone = "Pool", gameDay = 1, price = 1 };
            ApiResult sent = default;
            yield return api.PostCatch(dto, token, r => sent = r);
            Assert.IsTrue(sent.ok, sent.error);

            ApiResult board = default;
            yield return api.Leaderboard(1, r => board = r);
            CatchRecordDto[] top = ApiClient.ParseRecords(board.body);
            Assert.AreEqual(name, top[0].player);
        }
    }
}
