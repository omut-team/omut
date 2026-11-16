using System.Collections;
using System.Collections.Generic;
using Omut.Core;
using Omut.Fishing;
using Omut.NPC;
using Omut.Player;
using Omut.UI;
using UnityEngine;

namespace Omut.World
{
    // Финал: после письма деда ночью в центре омута крутится водоворот.
    // Заброс в него на фосфорную блесну: на крючке Хозяин Омута (все виды поведения по очереди).
    // Вытащил: Сымон снимает старые крючки и отпускает его, туман над рифом уходит, титры.
    public class BossEncounter : MonoBehaviour
    {
        public const string FinaleFlag = "finale_done";

        public static BossEncounter Instance { get; private set; }

        [SerializeField] private Sprite rippleSprite;
        [SerializeField] private Material unlitMaterial;

        [Tooltip("Радиус водоворота, клеток")]
        [SerializeField] private float radius = 2.2f;

        private readonly List<SpriteRenderer> rings = new List<SpriteRenderer>();
        private bool warned;

        public bool IsActive { get; private set; }

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("Whirlpool");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = rippleSprite;
                if (unlitMaterial != null)
                    sr.sharedMaterial = unlitMaterial;
                sr.sortingOrder = 210;
                rings.Add(sr);
            }

            FishingController.ForcedFish = BossOnHook;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            FishingController.ForcedFish = () => null;
        }

        private void OnEnable() => GameEvents.FishCaught += OnFishCaught;

        private void OnDisable() => GameEvents.FishCaught -= OnFishCaught;

        private static bool IsNight => DayNightCycle.Instance != null && DayNightCycle.Instance.Clock.Phase == DayPhase.Night;

        private void Update()
        {
            GameSession session = GameSession.Instance;
            IsActive = session != null && session.HasFlag(DockChest.BossFlag) && !session.HasFlag(FinaleFlag) && IsNight;

            for (int i = 0; i < rings.Count; i++)
            {
                SpriteRenderer sr = rings[i];
                sr.enabled = IsActive;
                if (!IsActive)
                    continue;

                float t = Mathf.Repeat(Time.time * 0.35f + i / (float)rings.Count, 1f);
                float size = Mathf.Lerp(radius * 1.6f, 0.3f, t); // круги сходятся к центру
                sr.transform.localScale = new Vector3(size, size * 0.55f, 1f);
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 40f + i * 30f);
                sr.color = new Color(0.55f, 1f, 0.75f, Mathf.Sin(t * Mathf.PI) * 0.9f);
            }

            if (IsActive && !warned && DockingZone.Instance != null &&
                Vector2.Distance(DockingZone.Instance.Boat.transform.position, transform.position) < 12f)
            {
                warned = true;
                Notifications.Show("Вода крутится воронкой... Хозяин здесь. Нужна фосфорная блесна.", new Color(0.6f, 1f, 0.75f));
            }
        }

        // FishingController спрашивает: не клюнет ли сейчас Хозяин?
        private FishData BossOnHook()
        {
            FishingController fishing = FishingController.Instance;
            GameSession session = GameSession.Instance;
            if (!IsActive || fishing == null || session == null)
                return null;

            if (Vector2.Distance(fishing.BobberPosition, transform.position) > radius)
                return null;

            if (FishingController.CurrentBait() != BaitType.Phosphor)
            {
                Notifications.Show("Хозяин даже не смотрит на эту наживку. Только фосфорная блесна (Q).", new Color(0.6f, 1f, 0.75f));
                return null;
            }

            return session.Database.fish.Find(f => f != null && f.isBoss);
        }

        private void OnFishCaught(CatchInfo info)
        {
            if (info.fish != null && info.fish.isBoss)
                StartCoroutine(Finale(info));
        }

        private IEnumerator Finale(CatchInfo info)
        {
            GameSession session = GameSession.Instance;
            session.SetFlag(FinaleFlag);
            yield return null; // карточка трофея для босса не показывается (CatchCard пропускает isBoss)
            FishingController.Instance?.FinishCatch();

            NoteData end = session.FinaleNote(2);
            var lines = new List<DialogueLine>
            {
                new DialogueLine("Сымон", $"Вот он. Огромный, {info.weight:0} кило, весь в старых крючках и лесках."),
                new DialogueLine("Сымон", "Тише, тише... Сейчас сниму. Этот крючок дедов, я его узнаю."),
                new DialogueLine("Сымон", "Плыви. Ты свободен."),
                new DialogueLine("", end != null ? end.text : "")
            };

            bool done = false;
            DialogueUI.Instance.Monologue(lines, () => done = true);
            while (!done)
                yield return null;

            GameEvents.RaiseFinale();
            CreditsUI.Instance?.Show();
        }
    }
}
