using System.Collections;
using NUnit.Framework;
using Omut.Core;
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
    }
}
