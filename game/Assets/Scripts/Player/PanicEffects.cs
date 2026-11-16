using System.Collections;
using System.Collections.Generic;
using Omut.Core;
using Omut.Fishing;
using Omut.Inventory;
using Omut.UI;
using Omut.World;
using UnityEngine;

namespace Omut.Player
{
    // Паника в игре: считает её каждый кадр (PanicMeter) и включает эффекты.
    // 50: виньетка, шёпот, ложные поклёвки, камни-силуэты. 80: руль с задержкой.
    // 100: обморок. Утро на причале, лодка повреждена, ночной улов потерян, дневной цел.
    // C: выпить чай из термоса (паника -35).
    public class PanicEffects : MonoBehaviour
    {
        private static readonly string[] Whispers =
        {
            "...Сымон...", "...вернись к свету...", "...он внизу...", "...ты похож на него...",
            "...колокол уже бил...", "...не смотри в воду...", "...Язэп..."
        };

        [SerializeField] private Sprite silhouetteSprite;
        [SerializeField] private Material unlitMaterial;

        private float whisperTimer = 8f;
        private float silhouetteTimer = 5f;
        private bool fainting;
        private readonly List<SpriteRenderer> silhouettes = new List<SpriteRenderer>();

        private void OnEnable()
        {
            FishingController.FakeBitesPerSecond = FakeBites;
        }

        private void Start()
        {
            GameSession.Instance.Panic.Fainted += OnFainted;
            GameSession.Instance.Panic.LevelChanged += OnLevelChanged;
        }

        private void OnDestroy()
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.Panic.Fainted -= OnFainted;
                GameSession.Instance.Panic.LevelChanged -= OnLevelChanged;
            }

            BoatController.InputDelay = 0f;
        }

        private static float FakeBites()
        {
            GameSession session = GameSession.Instance;
            return session != null && session.Panic.Level >= 1 ? 0.06f : 0f;
        }

        private void Update()
        {
            GameSession session = GameSession.Instance;
            GameManager game = GameManager.Instance;
            if (session == null || game == null || fainting)
                return;

            GameState state = game.CurrentState;
            if (state == GameState.Menu || state == GameState.Paused)
                return;

            session.Panic.Tick(Time.deltaTime, CurrentConditions(session));
            BoatController.InputDelay = session.Panic.Level >= 2 ? 0.45f : 0f;

            if ((state == GameState.Sailing || state == GameState.Fishing) && Controls.Pressed(Controls.Thermos))
                DrinkTea(session);

            if (session.Panic.Level >= 1)
                PlayScaryEffects();

            FadeSilhouettes();
        }

        public static PanicConditions CurrentConditions(GameSession session)
        {
            DayNightCycle cycle = DayNightCycle.Instance;
            bool night = cycle != null && cycle.Clock.Phase == DayPhase.Night;
            bool onFoot = PlayerWalker.OnFoot;
            Vector2 position = onFoot && PlayerWalker.Instance != null
                ? (Vector2)PlayerWalker.Instance.transform.position
                : DockingZone.Instance != null ? (Vector2)DockingZone.Instance.Boat.transform.position : Vector2.zero;

            bool safe = onFoot || NightLight.IsInSafeLight(position);
            bool lantern = Lantern.Instance != null && Lantern.Instance.IsOn;
            float multiplier = session.CurrentBoat != null ? session.CurrentBoat.panicMultiplier : 1f;
            return new PanicConditions(night, FogController.InEternalFog, lantern, safe, multiplier);
        }

        private static void DrinkTea(GameSession session)
        {
            if (session.Thermos <= 0)
            {
                Notifications.Show("Термос пуст. Купи чай у Тараса.");
                return;
            }

            session.Thermos--;
            session.Panic.Calm();
            GameEvents.RaisePlaySound("drink");
            Notifications.Show($"Горячий чай. Отпустило. (термосов: {session.Thermos})", new Color(0.8f, 1f, 0.8f));
        }

        private void OnLevelChanged(int from, int to)
        {
            if (to == 1 && from == 0)
                Notifications.Show("Становится жутко... Держись у света, выпей чаю (C).", new Color(0.75f, 0.7f, 1f));
            else if (to == 2)
                Notifications.Show("Руки дрожат, руль плохо слушается!", UiFactory.Theme.Bad);
        }

        // ---------- шёпот и силуэты ----------

        private void PlayScaryEffects()
        {
            whisperTimer -= Time.deltaTime;
            if (whisperTimer <= 0f)
            {
                whisperTimer = Random.Range(6f, 12f);
                Notifications.Show(Whispers[Random.Range(0, Whispers.Length)], new Color(0.65f, 0.6f, 0.85f, 1f));
                GameEvents.RaisePlaySound("whisper");
            }

            silhouetteTimer -= Time.deltaTime;
            if (silhouetteTimer <= 0f && !PlayerWalker.OnFoot && DockingZone.Instance != null)
            {
                silhouetteTimer = Random.Range(4f, 9f);
                SpawnSilhouette(DockingZone.Instance.Boat.transform.position);
            }
        }

        // Камень, который "кажется" в темноте: появляется рядом и тает. Коллайдера нет.
        private void SpawnSilhouette(Vector2 near)
        {
            var go = new GameObject("PanicSilhouette");
            go.transform.position = near + Random.insideUnitCircle.normalized * Random.Range(2f, 3.5f);
            go.transform.localScale = Vector3.one * Random.Range(1.2f, 2f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = silhouetteSprite;
            if (unlitMaterial != null)
                sr.sharedMaterial = unlitMaterial;
            sr.sortingOrder = 199;
            sr.color = new Color(0.02f, 0.02f, 0.05f, 0f);
            silhouettes.Add(sr);
            StartCoroutine(Silhouette(sr));
        }

        private IEnumerator Silhouette(SpriteRenderer sr)
        {
            for (float t = 0f; t < 3f && sr != null; t += Time.deltaTime)
            {
                Color c = sr.color;
                c.a = Mathf.Sin(t / 3f * Mathf.PI) * 0.8f;
                sr.color = c;
                yield return null;
            }

            if (sr != null)
            {
                silhouettes.Remove(sr);
                Destroy(sr.gameObject);
            }
        }

        private void FadeSilhouettes()
        {
            if (GameSession.Instance.Panic.Level >= 1)
                return;

            foreach (SpriteRenderer sr in silhouettes)
                if (sr != null) Destroy(sr.gameObject);
            silhouettes.Clear();
        }

        // ---------- обморок ----------

        private void OnFainted()
        {
            if (!fainting)
                StartCoroutine(Faint());
        }

        private IEnumerator Faint()
        {
            fainting = true;
            GameEvents.RaisePlaySound("faint");
            yield return ScreenFader.Instance != null ? ScreenFader.Instance.FadeTo(1f, 1.5f) : null;

            GameSession session = GameSession.Instance;
            FindAnyObjectByType<CatchCard>()?.Dismiss();
            FishingController.Instance?.StopFishing();
            GameWindow.CloseActive();

            List<GridItem> lost = session.Hold.RemoveWhere(item => item.catchInfo.caughtAtNight);
            if (session.Boat.Durability > 1)
                session.Boat.Hit(1);

            DockingZone.Instance?.WakeUpAtDock();
            DayNightCycle.Instance?.Clock.AdvanceToMorning();
            session.Panic.Reset();
            Lantern.Instance?.Fuel?.SetOn(false);
            GameEvents.RaiseFainted(lost.Count);

            yield return new WaitForSeconds(0.5f);
            yield return ScreenFader.Instance != null ? ScreenFader.Instance.FadeTo(0f, 1.5f) : null;

            Notifications.Show(lost.Count > 0
                ? $"Ты очнулся на причале. Ночной улов пропал ({lost.Count} шт.), лодка побита. Дневной улов цел."
                : "Ты очнулся на причале. Лодка побита. Больше не гаси фонарь ночью.", UiFactory.Theme.Bad);
            fainting = false;
        }
    }
}
