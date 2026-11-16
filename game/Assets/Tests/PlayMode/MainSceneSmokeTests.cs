using System.Collections;
using NUnit.Framework;
using Omut.Core;
using Omut.Fishing;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Omut.Tests
{
    // Дымовые тесты: сцена Main запускается и работает несколько секунд без ошибок в консоли.
    // Если в консоли появится ошибка или исключение, тест упадёт сам.
    // Запуск: Window > General > Test Runner > PlayMode > Run All.
    public class MainSceneSmokeTests
    {
        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            GameManager.Instance?.ResetToMenu();
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartsInMenu_WithoutErrors()
        {
            yield return new WaitForSeconds(0.5f);

            Assert.IsNotNull(GameManager.Instance);
            Assert.AreEqual(GameState.Menu, GameManager.Instance.CurrentState);
        }

        [UnityTest]
        public IEnumerator NewGame_Pause_Resume()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(GameState.Sailing, GameManager.Instance.CurrentState);

            GameManager.Instance.TogglePause();
            yield return null;
            Assert.AreEqual(GameState.Paused, GameManager.Instance.CurrentState);
            Assert.AreEqual(0f, Time.timeScale);

            GameManager.Instance.TogglePause();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(GameState.Sailing, GameManager.Instance.CurrentState);
        }
    
        [UnityTest]
        public IEnumerator Fishing_FullCycle_WithoutErrors()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return new WaitForSeconds(0.5f);

            FishingController fishing = FishingController.Instance;
            Assert.IsNotNull(fishing, "На лодке нет FishingController");

            CatchInfo caught = null;
            System.Action<CatchInfo> onCaught = info => caught = info;
            GameEvents.FishCaught += onCaught;

            fishing.SetState(fishing.Charging);
            Assert.AreEqual(GameState.Fishing, GameManager.Instance.CurrentState);
            yield return null;

            Vector2 water = (Vector2)fishing.transform.position + new Vector2(0f, -3f);
            Assert.IsTrue(fishing.IsWater(water), "Под лодкой должна быть вода");
            fishing.ShowBobber(water);
            fishing.Landed(water);
            fishing.SetState(fishing.Waiting);
            yield return new WaitForSeconds(0.3f);

            FishingController.ForcedFish = () => fishing.Database.FindFish("crucian");
            fishing.RepickFish();
            FishingController.ForcedFish = () => null;
            Assert.IsNotNull(fishing.HookedFish);

            fishing.SetState(fishing.Bite);
            yield return null;
            {
                fishing.SetState(fishing.Reeling);
                Assert.IsNotNull(fishing.Reel);
                yield return new WaitForSeconds(0.5f);
                fishing.OnCaught();
                yield return null;
                Assert.IsNotNull(caught);
                Assert.Greater(caught.weight, 0f);
                fishing.FinishCatch();
            }

            GameEvents.FishCaught -= onCaught;
            yield return null;
            Assert.AreEqual(GameState.Sailing, GameManager.Instance.CurrentState);
        }
}
}
