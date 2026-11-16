using System.Collections;
using System.IO;
using NUnit.Framework;
using Omut.Core;
using Omut.Fishing;
using Omut.Inventory;
using Omut.NPC;
using Omut.Player;
using Omut.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Omut.Tests
{
    // Снимки экрана для проверки интерфейса глазами. Запускаются только если задана
    // переменная окружения OMUT_SCREENSHOTS=1, иначе тест пропускается. Файлы: game/Logs/shot_*.png
    public class ScreenshotTests
    {
        private static bool Enabled => System.Environment.GetEnvironmentVariable("OMUT_SCREENSHOTS") == "1";

        // В batchmode кадры на экран не рисуются, поэтому рисуем камеру вручную в текстуру,
        // а холсты интерфейса на время переключаем в режим Screen Space - Camera.
        private static IEnumerator Shot(string name)
        {
            yield return null;
            Camera cam = Camera.main;
            var rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (!canvases[i].isRootCanvas) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam;
                canvases[i].planeDistance = 1f + (100 - canvases[i].sortingOrder) * 0.01f;
                canvases[i].sortingOrder += 1000; // интерфейс поверх тумана, как в игре
            }

            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            for (int i = 0; i < canvases.Length; i++)
            {
                if (!canvases[i].isRootCanvas) continue;
                canvases[i].renderMode = modes[i];
                canvases[i].sortingOrder -= 1000;
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllBytes($"Logs/shot_{name}.png", tex.EncodeToPNG());
            Object.Destroy(tex);
            Object.Destroy(rt);
        }

        [UnityTest]
        public IEnumerator TakeScreenshots()
        {
            if (!Enabled)
            {
                Assert.Ignore("OMUT_SCREENSHOTS не задан");
                yield break;
            }

            GameManager.Instance?.ResetToMenu();
            SceneManager.LoadScene("Main");
            yield return new WaitForSeconds(0.5f);
            yield return Shot("menu");

            GameEvents.RaiseNewGameStarted();
            GameManager.Instance.StartGame();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("dock");

            GameSession session = GameSession.Instance;
            foreach (string id in new[] { "pike", "crucian", "carp", "bream", "black_catfish" })
                session.Hold.TryAdd(new CatchInfo(session.Database.FindFish(id), 3.3f, id == "black_catfish", Omut.World.ZoneId.Pool, 1));

            Merchant taras = Object.FindAnyObjectByType<Merchant>();
            taras.Interact();
            yield return new WaitForSeconds(0.2f);
            yield return Shot("dialogue");
            DialogueUI.Instance.Close();
            yield return null;
            ShopUI.Instance.OpenFor(taras);
            yield return new WaitForSeconds(0.2f);
            yield return Shot("shop");
            ShopUI.Instance.Close();
            yield return null;

            Object.FindAnyObjectByType<InventoryUI>().Open();
            yield return new WaitForSeconds(0.2f);
            yield return Shot("hold");
            Object.FindAnyObjectByType<InventoryUI>().Close();

            Object.FindAnyObjectByType<BagUI>().Open();
            yield return new WaitForSeconds(0.2f);
            yield return Shot("bag");
            Object.FindAnyObjectByType<BagUI>().Close();
            yield return null;

            session.Journal.Register(new CatchInfo(session.Database.FindFish("pike"), 5f, false, Omut.World.ZoneId.Pool, 1));
            Object.FindAnyObjectByType<JournalUI>().Open();
            yield return new WaitForSeconds(0.2f);
            yield return Shot("journal");
            Object.FindAnyObjectByType<JournalUI>().Close();
            yield return null;

            Object.FindAnyObjectByType<MapUI>().Open();
            yield return new WaitForSeconds(0.2f);
            yield return Shot("map");
            Object.FindAnyObjectByType<MapUI>().Close();
            yield return null;

            DockingZone.Instance.Board();
            yield return new WaitForSeconds(0.3f);
            yield return Shot("boat");
            FishingController fishing = FishingController.Instance;
            fishing.SetState(fishing.Charging);
            fishing.CastPower = 0.6f;
            yield return null;
            fishing.ShowBobber(fishing.transform.position + new Vector3(2f, -3f));
            fishing.Landed(fishing.transform.position + new Vector3(2f, -3f));
            FishingController.ForcedFish = () => fishing.Database.FindFish("pike");
            fishing.RepickFish();
            FishingController.ForcedFish = () => null;
            fishing.SetState(fishing.Reeling);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("reeling");

            fishing.OnCaught();
            yield return new WaitForSeconds(0.2f);
            yield return Shot("catch");

            Object.FindAnyObjectByType<CatchCard>().Dismiss();
            yield return null;

            // Ночь у причала с фонарём.
            DayNightCycle().Clock.SetTime(23, 0);
            Lantern.Instance.Fuel.SetOn(true);
            yield return new WaitForSeconds(2.5f);
            yield return Shot("night_lantern");

            // Ночь без фонаря.
            Lantern.Instance.Fuel.SetOn(false);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("night_dark");

            // Риф днём: вечный туман.
            DayNightCycle().Clock.SetTime(12, 0);
            var body = fishing.GetComponent<Rigidbody2D>();
            fishing.transform.position = new Vector3(30f, 5f, 0f);
            body.position = new Vector2(30f, 5f);
            Object.FindAnyObjectByType<Omut.World.CameraFollow>().SnapToTarget();
            yield return new WaitForSeconds(2.5f);
            yield return Shot("reef_day");
        }

        private static Omut.World.DayNightCycle DayNightCycle() => Omut.World.DayNightCycle.Instance;
    }
}
