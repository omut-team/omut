using System.Collections;
using NUnit.Framework;
using Omut.Core;
using Omut.Fishing;
using Omut.UI;
using Omut.Player;
using Omut.NPC;
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
            DockingZone.Instance.Board();
            yield return null;

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

        [UnityTest]
        public IEnumerator AllWindows_OpenAndClose_WithoutErrors()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return new WaitForSeconds(0.3f);

            GameWindow[] windows = Object.FindObjectsByType<GameWindow>(FindObjectsSortMode.None);
            Assert.Greater(windows.Length, 0);

            foreach (GameWindow window in windows)
            {
                Assert.IsTrue(window.Open(), "Не открылось окно " + window.GetType().Name);
                yield return null;
                yield return null;
                window.Close();
                yield return null;
                Assert.AreEqual(GameState.Sailing, GameManager.Instance.CurrentState, window.GetType().Name);
            }
        }

        [UnityTest]
        public IEnumerator Dock_WalkTalkSell_WithoutErrors()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(PlayerWalker.OnFoot, "Новая игра начинается на причале");
            Assert.IsTrue(PlayerWalker.CanStand(PlayerWalker.Instance.transform.position), "Сымон должен стоять на земле");

            // Положить рыбу в трюм и продать Тарасу.
            GameSession session = GameSession.Instance;
            var fish = session.Database.FindFish("crucian");
            session.Hold.TryAdd(new CatchInfo(fish, 1f, false, Omut.World.ZoneId.Bay, 1));
            int before = session.Wallet.Money;

            Merchant taras = Object.FindAnyObjectByType<Merchant>();
            Assert.IsNotNull(taras);
            taras.Interact();
            yield return null;
            Assert.AreEqual(GameState.Shop, GameManager.Instance.CurrentState);
            yield return null;
            ShopUI.Instance.Close();
            yield return null;

            foreach (var item in new System.Collections.Generic.List<Omut.Inventory.GridItem>(session.Hold.Items))
            {
                session.Hold.Remove(item);
                session.Wallet.Earn(item.catchInfo.price);
            }
            Assert.Greater(session.Wallet.Money, before);

            // Сесть в лодку и сойти обратно.
            DockingZone.Instance.Board();
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(PlayerWalker.OnFoot);
            Assert.IsTrue(DockingZone.Instance.BoatNearDock);
            DockingZone.Instance.Disembark();
            yield return null;
            Assert.IsTrue(PlayerWalker.OnFoot);
        }
}
}
