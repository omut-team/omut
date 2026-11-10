using NUnit.Framework;
using Omut.Core;

namespace Omut.Tests
{
    // Автотесты автомата состояний. Запуск: Window > General > Test Runner > EditMode > Run All.
    public class GameStateMachineTests
    {
        [Test]
        public void StartsInMenu()
        {
            var machine = new GameStateMachine();

            Assert.AreEqual(GameState.Menu, machine.Current);
        }

        [Test]
        public void MenuToSailing_IsAllowed()
        {
            var machine = new GameStateMachine();

            Assert.IsTrue(machine.TryChangeTo(GameState.Sailing));
            Assert.AreEqual(GameState.Sailing, machine.Current);
        }

        [Test]
        public void MenuToFishing_IsForbidden()
        {
            var machine = new GameStateMachine();

            Assert.IsFalse(machine.TryChangeTo(GameState.Fishing));
            Assert.AreEqual(GameState.Menu, machine.Current);
        }

        [Test]
        public void ChangeToSameState_ReturnsFalse()
        {
            var machine = new GameStateMachine(GameState.Sailing);

            Assert.IsFalse(machine.TryChangeTo(GameState.Sailing));
        }

        [Test]
        public void SailingFishingShop_RoundTrip()
        {
            var machine = new GameStateMachine(GameState.Sailing);

            Assert.IsTrue(machine.TryChangeTo(GameState.Fishing));
            Assert.IsTrue(machine.TryChangeTo(GameState.Sailing));
            Assert.IsTrue(machine.TryChangeTo(GameState.Shop));
            Assert.IsTrue(machine.TryChangeTo(GameState.Sailing));
        }

        [Test]
        public void FishingToShop_IsForbidden()
        {
            var machine = new GameStateMachine(GameState.Fishing);

            Assert.IsFalse(machine.TryChangeTo(GameState.Shop));
        }

        [Test]
        public void MenuCannotBePaused()
        {
            var machine = new GameStateMachine();

            Assert.IsFalse(machine.TogglePause());
            Assert.AreEqual(GameState.Menu, machine.Current);
        }

        [Test]
        public void PauseAndResume_ReturnsToPreviousState()
        {
            var machine = new GameStateMachine(GameState.Fishing);

            Assert.IsTrue(machine.TogglePause());
            Assert.AreEqual(GameState.Paused, machine.Current);

            Assert.IsTrue(machine.TogglePause());
            Assert.AreEqual(GameState.Fishing, machine.Current);
        }

        [Test]
        public void FromPause_CanGoToMenu_ButNotToOtherState()
        {
            var machine = new GameStateMachine(GameState.Sailing);
            machine.TogglePause();

            Assert.IsFalse(machine.TryChangeTo(GameState.Shop));
            Assert.IsTrue(machine.TryChangeTo(GameState.Menu));
        }

        [Test]
        public void StateChanged_SendsOldAndNewState()
        {
            var machine = new GameStateMachine();
            GameState from = GameState.Paused;
            GameState to = GameState.Paused;
            int calls = 0;
            machine.StateChanged += (oldState, newState) =>
            {
                from = oldState;
                to = newState;
                calls++;
            };

            machine.TryChangeTo(GameState.Sailing);

            Assert.AreEqual(1, calls);
            Assert.AreEqual(GameState.Menu, from);
            Assert.AreEqual(GameState.Sailing, to);
        }

        [Test]
        public void StateChanged_NotSentWhenTransitionForbidden()
        {
            var machine = new GameStateMachine();
            int calls = 0;
            machine.StateChanged += (oldState, newState) => calls++;

            machine.TryChangeTo(GameState.Shop);

            Assert.AreEqual(0, calls);
        }
    }
}
