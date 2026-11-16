using System.Collections;
using NUnit.Framework;
using Omut.Core;
using Omut.Fishing;
using Omut.UI;
using Omut.Player;
using Omut.NPC;
using Omut.Inventory;
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
            // Автосохранения в тестах не должны попадать в настоящую папку игрока.
            SaveSystem.FolderOverride = System.IO.Path.Combine(Application.temporaryCachePath, "omut_tests");
            GameManager.Instance?.ResetToMenu();
            SceneManager.LoadScene("Main");
            yield return null;
            yield return null;
        }

        [TearDown]
        public void CleanSaves()
        {
            SaveSystem.Delete();
            SaveSystem.FolderOverride = null;
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
            Assert.IsTrue(DialogueUI.Instance.IsOpen);
            DialogueUI.Instance.ShowMenu("тест");
            yield return null;
            DialogueUI.Instance.Close();
            yield return null;
            ShopUI.Instance.OpenFor(taras);
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

        [UnityTest]
        public IEnumerator SaveAndLoad_RestoresProgress()
        {
            try
            {
                GameEvents.RaiseNewGameStarted();
                GameManager.Instance.StartGame();
                yield return new WaitForSeconds(0.3f);

                GameSession session = GameSession.Instance;
                session.Wallet.Earn(500);
                session.Hold.TryAdd(new CatchInfo(session.Database.FindFish("pike"), 6f, true, Omut.World.ZoneId.Pool, 1));
                session.Journal.Register(new CatchInfo(session.Database.FindFish("pike"), 6f, true, Omut.World.ZoneId.Pool, 1));
                session.Tackle.Add(BaitType.Phosphor, 2);
                int money = session.Wallet.Money;
                Assert.IsTrue(session.SaveGame());

                GameEvents.RaiseNewGameStarted();
                yield return null;
                Assert.AreEqual(0, session.Hold.Count);

                GameEvents.RaiseContinueRequested();
                yield return null;
                Assert.AreEqual(money, session.Wallet.Money);
                Assert.AreEqual(1, session.Hold.Count);
                Assert.IsTrue(session.Journal.IsCaught("pike"));
                Assert.AreEqual(2, session.Tackle.Count(BaitType.Phosphor));
            }
            finally
            {
                SaveSystem.Delete();
            }
        }

        [UnityTest]
        public IEnumerator Panic100_Faint_LosesNightCatchOnly()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return new WaitForSeconds(0.3f);

            DockingZone.Instance.Board();
            Omut.World.DayNightCycle.Instance.Clock.SetTime(23, 0);
            GameSession session = GameSession.Instance;
            var fish = session.Database.FindFish("crucian");
            session.Hold.TryAdd(new CatchInfo(fish, 1f, false, Omut.World.ZoneId.Bay, 1));
            session.Hold.TryAdd(new CatchInfo(fish, 1f, true, Omut.World.ZoneId.Bay, 1));
            int day = Omut.World.DayNightCycle.Instance.Clock.Day;

            session.Panic.Set(100f);
            yield return new WaitForSeconds(4.5f);

            Assert.IsTrue(PlayerWalker.OnFoot, "Очнулся на причале");
            Assert.AreEqual(1, session.Hold.Count, "Ночной улов пропал, дневной цел");
            Assert.AreEqual(day + 1, Omut.World.DayNightCycle.Instance.Clock.Day);
            Assert.AreEqual(Omut.World.DayPhase.Morning, Omut.World.DayNightCycle.Instance.Clock.Phase);
            Assert.Less(session.Panic.Value, 1f);
            Assert.AreEqual(1, session.Boat.Durability, "Лодка побита");
        }

        [UnityTest]
        public IEnumerator FiveChests_OpenDockChest_UnlocksBoss()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return new WaitForSeconds(0.3f);

            GameSession session = GameSession.Instance;
            var chest = session.Database.FindFish("grandpa_chest");
            Assert.IsTrue(FishingController.ChestAllowed());
            for (int i = 0; i < 5; i++)
            {
                var info = new CatchInfo(chest, 15f, true, Omut.World.ZoneId.Reef, 1);
                GameEvents.RaiseFishCaught(info);
                Assert.IsNull(CatchCard.TryStore(info), "Сундучок не кладётся в трюм, но и не ошибка");
                yield return null;
                Object.FindAnyObjectByType<CatchCard>().Dismiss();
            }

            Assert.AreEqual(5, session.DiaryPages);
            Assert.AreEqual(0, session.Hold.Count);
            Assert.IsFalse(FishingController.ChestAllowed(), "Больше сундучков нет");
            yield return null;

            var dockChest = Object.FindAnyObjectByType<Omut.World.DockChest>();
            Assert.IsNotNull(dockChest);
            dockChest.Interact();
            yield return null;
            Assert.IsTrue(session.HasFlag(Omut.World.DockChest.BossFlag));
            DialogueUI.Instance.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boss_OnlyAfterLetter_AtNight_OnPhosphor()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return new WaitForSeconds(0.3f);

            GameSession session = GameSession.Instance;
            DockingZone.Instance.Board();
            var boss = Omut.World.BossEncounter.Instance;
            Assert.IsNotNull(boss);
            Omut.World.DayNightCycle.Instance.Clock.SetTime(23, 0);
            yield return null;
            Assert.IsFalse(boss.IsActive, "Без письма деда водоворота нет");

            session.SetFlag(Omut.World.DockChest.BossFlag);
            session.Tackle.Add(BaitType.Phosphor, 3);
            session.Tackle.Select(BaitType.Phosphor);
            yield return null;
            Assert.IsTrue(boss.IsActive);

            FishingController fishing = FishingController.Instance;
            fishing.SetState(fishing.Charging);
            fishing.ShowBobber(boss.transform.position);
            fishing.Landed(boss.transform.position);
            Assert.IsNotNull(fishing.HookedFish);
            Assert.IsTrue(fishing.HookedFish.isBoss, "В водовороте на фосфорную блесну клюёт Хозяин");

            fishing.SetState(fishing.Reeling);
            yield return null;
            fishing.OnCaught();
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(session.HasFlag(Omut.World.BossEncounter.FinaleFlag));
            Assert.IsTrue(session.Journal.IsCaught("lake_master"));
            Assert.AreEqual(0, session.Hold.Count, "Хозяин в трюм не кладётся");
            Assert.IsTrue(DialogueUI.Instance.IsOpen, "Идёт финальная сцена");
            DialogueUI.Instance.Close();
            yield return null;
            CreditsUI.Instance.Show();
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(CreditsUI.Instance.IsOpen);
        }

        [UnityTest]
        public IEnumerator Walker_CanWalkFromHutToPierTip()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return null;

            // Путь пешком: от хаты на дорожку, вниз по мосткам до конца. Стык земли и мостков проходим.
            Vector2 pierTip = DockingZone.Instance.transform.Find("WalkPoint").position;
            for (float y = 26.5f; y >= pierTip.y; y -= 0.1f)
                Assert.IsTrue(PlayerWalker.CanStand(new Vector2(pierTip.x, y)), $"Застрял на y = {y:0.0}");
            for (float x = -45f; x <= pierTip.x; x += 0.1f)
                Assert.IsTrue(PlayerWalker.CanStand(new Vector2(x, 26.5f)), $"Застрял на x = {x:0.0}");
        }

        // Подсказки и панель предметов: после новой игры видно, к кому идти, а удочка в руке.
        [UnityTest]
        public IEnumerator NewGame_ShowsObjective_Hotbar_AndRider()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return new WaitForSeconds(1f);

            StringAssert.Contains("Тарас", Guide.LastText);
            Assert.IsNotNull(Hotbar.Instance);
            Assert.AreEqual(0, Hotbar.Instance.Selected);
            Assert.IsFalse(Hotbar.HoldsLantern);
            Assert.IsNotNull(BoatRider.Instance);

            // Блёсен ещё нет: в руку не берутся, остаётся червь.
            int spoons = GameSession.Instance.Tackle.Count(BaitType.Spoon);
            Hotbar.Instance.Use(1);
            Assert.AreEqual(spoons > 0 ? 1 : 0, Hotbar.Instance.Selected);

            Hotbar.Instance.Use(Hotbar.LanternSlot);
            Assert.IsTrue(Hotbar.HoldsLantern);
            yield return null;
        }

        // Окно не должно "зависать": если игра уже снова в плавании, трюм и лавка открываются.
        [UnityTest]
        public IEnumerator StaleWindow_IsReleased()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return null;

            var hold = Object.FindAnyObjectByType<InventoryUI>();
            Assert.IsTrue(hold.Open());
            GameManager.Instance.ChangeState(GameState.Sailing);
            yield return null;

            Assert.IsNull(GameWindow.Active);
            Assert.IsTrue(Object.FindAnyObjectByType<BagUI>().Open(), "Другое окно открывается");
            Object.FindAnyObjectByType<BagUI>().Close();
            Assert.AreEqual(GameState.Sailing, GameManager.Instance.CurrentState);
        }

        // Фонарь зажигается и на берегу (F), сам оказывается в руке и плавно разгорается.
        [UnityTest]
        public IEnumerator Lantern_WorksOnFoot_AndGoesToHand()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return null;
            Assert.IsTrue(PlayerWalker.OnFoot, "Игра начинается на берегу");

            if (Lantern.Instance.IsOn)
                Lantern.Instance.Toggle();
            Lantern.Instance.Toggle();
            Assert.IsTrue(Lantern.Instance.IsOn);
            Assert.IsTrue(Hotbar.HoldsLantern);
            yield return new WaitForSeconds(0.5f);

            Lantern.Instance.Toggle();
            Assert.IsFalse(Lantern.Instance.IsOn);
            Assert.IsFalse(Hotbar.HoldsLantern, "Погасил: снова удочка");
        }

        // Сдать "Железо со дна": железо уходит из трюма только если задание сдано. Не хватает: ничего не пропадает.
        [UnityTest]
        public IEnumerator DeliverQuest_TakesScrapOnlyWhenDone()
        {
            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return null;

            GameSession session = GameSession.Instance;
            QuestData quest = session.Quests.Find("q3_scrap");
            Assert.IsTrue(session.Quests.Accept(quest));
            FishData scrap = session.Database.FindFish("scrap");
            for (int i = 0; i < 2; i++)
                session.Hold.TryAdd(new CatchInfo(scrap, 2f, false, Omut.World.ZoneId.Pool, 1));

            session.CompleteQuest(quest, null);
            Assert.AreEqual(QuestState.Active, session.Quests.StateOf(quest), "Двух кусков мало");
            Assert.AreEqual(2, session.Hold.CountOf("scrap"), "Железо не пропало");

            session.Hold.TryAdd(new CatchInfo(scrap, 2f, false, Omut.World.ZoneId.Pool, 1));
            int money = session.Wallet.Money;
            session.CompleteQuest(quest, null);
            Assert.AreEqual(QuestState.Done, session.Quests.StateOf(quest));
            Assert.AreEqual(0, session.Hold.CountOf("scrap"));
            Assert.Greater(session.Wallet.Money, money);
        }
    }
}
