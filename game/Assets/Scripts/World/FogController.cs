using System.Collections.Generic;
using Omut.Core;
using Omut.Fishing;
using Omut.Player;
using UnityEngine;

namespace Omut.World
{
    // Туман. Ночью над озером клубится туман (темноту даёт свет: см. DayNightCycle и Lantern).
    // На рифе туман вечный: даже днём видно только несколько клеток вокруг лодки.
    // Туман рисуется поверх мира картинками без освещения (Sprite-Unlit), поэтому он светлый в темноте.
    public class FogController : MonoBehaviour
    {
        public static FogController Instance { get; private set; }

        // Лодка или Сымон сейчас в зоне вечного тумана (риф).
        public static bool InEternalFog { get; private set; }

        [SerializeField] private Sprite puffSprite;
        [SerializeField] private Sprite holeSprite;
        [SerializeField] private Sprite whiteSprite;
        [SerializeField] private Material unlitMaterial;

        [Tooltip("Сколько клубов тумана вокруг камеры")]
        [SerializeField] private int puffCount = 18;

        [SerializeField] private Color nightFogColor = new Color(0.55f, 0.62f, 0.7f, 1f);
        [SerializeField] private Color reefFogColor = new Color(0.78f, 0.8f, 0.82f, 1f);

        private readonly List<(SpriteRenderer sr, Vector2 velocity)> puffs = new List<(SpriteRenderer, Vector2)>();
        private SpriteRenderer hole;
        private readonly SpriteRenderer[] borders = new SpriteRenderer[4];
        private float density;
        private Camera cam;

        public float Density => density;

        private void Awake()
        {
            Instance = this;
            InEternalFog = false;
            cam = Camera.main;

            for (int i = 0; i < puffCount; i++)
            {
                SpriteRenderer sr = Create("FogPuff", puffSprite, 200);
                sr.transform.localScale = Vector3.one * Random.Range(2.5f, 5f);
                puffs.Add((sr, new Vector2(Random.Range(0.15f, 0.45f), Random.Range(-0.08f, 0.08f))));
            }

            hole = Create("FogHole", holeSprite, 201);
            for (int i = 0; i < 4; i++)
                borders[i] = Create("FogBorder", whiteSprite, 201);

            FishingSpot.UnlitMaterial = unlitMaterial;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private SpriteRenderer Create(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (unlitMaterial != null)
                sr.sharedMaterial = unlitMaterial;
            sr.color = Color.clear;
            return sr;
        }

        private void LateUpdate()
        {
            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            Vector2 focus = FocusPoint();
            Zone zone = Zone.Find(focus);
            InEternalFog = zone != null && zone.AlwaysFoggy;

            DayNightCycle cycle = DayNightCycle.Instance;
            DayPhase phase = cycle != null ? cycle.Clock.Phase : DayPhase.Day;
            float nightFog = phase == DayPhase.Night ? 0.75f : (phase == DayPhase.Evening ? 0.25f : 0f);
            float target = Mathf.Max(nightFog, InEternalFog ? 1f : 0f);
            density = Mathf.MoveTowards(density, target, Time.deltaTime * 0.5f);

            Color color = InEternalFog && phase != DayPhase.Night ? reefFogColor : nightFogColor;
            UpdatePuffs(focus, color);
            UpdateHole(focus, color, phase);

            // Эхолот «Охотника»: рыбные места видно сквозь туман и темноту.
            GameSession session = GameSession.Instance;
            bool sonar = session != null && session.CurrentBoat != null && session.CurrentBoat.hasSonar && !PlayerWalker.OnFoot;
            FishingSpot.ShowThroughFog = sonar && density > 0.2f;
        }

        private static Vector2 FocusPoint()
        {
            if (PlayerWalker.OnFoot && PlayerWalker.Instance != null)
                return PlayerWalker.Instance.transform.position;
            DockingZone dock = DockingZone.Instance;
            return dock != null ? (Vector2)dock.Boat.transform.position : Vector2.zero;
        }

        private void UpdatePuffs(Vector2 focus, Color color)
        {
            Vector2 center = cam.transform.position;
            float halfH = cam.orthographicSize + 3f;
            float halfW = halfH * cam.aspect + 3f;

            for (int i = 0; i < puffs.Count; i++)
            {
                (SpriteRenderer sr, Vector2 v) = puffs[i];
                Vector2 p = (Vector2)sr.transform.position + v * Time.deltaTime;

                // Клуб уплыл за экран: переносим на другую сторону.
                if (p.x > center.x + halfW) p.x -= halfW * 2f;
                if (p.x < center.x - halfW) p.x += halfW * 2f;
                if (p.y > center.y + halfH) p.y -= halfH * 2f;
                if (p.y < center.y - halfH) p.y += halfH * 2f;
                sr.transform.position = p;

                // Рядом с лодкой туман реже (свет фонаря).
                float near = Mathf.Clamp01(Vector2.Distance(p, focus) / 4f);
                Color c = color;
                c.a = density * 0.3f * (0.4f + 0.6f * near);
                sr.color = c;
            }
        }

        // На рифе днём: плотная стена тумана с "окном" вокруг лодки.
        private void UpdateHole(Vector2 focus, Color color, DayPhase phase)
        {
            float alpha = InEternalFog ? density * (phase == DayPhase.Night ? 0.55f : 0.9f) : 0f;
            Color c = color;
            c.a = alpha;

            // fx_fog_hole 256 px = 8 клеток, прозрачная середина. Масштаб: окно около 3 клеток.
            float scale = 1.6f;
            float size = 8f * scale;
            hole.transform.position = focus;
            hole.transform.localScale = new Vector3(scale, scale, 1f);
            hole.color = c;

            // Четыре полосы вокруг окна закрывают остальной экран.
            float far = 60f;
            Place(borders[0], focus + new Vector2(0f, size / 2f + far / 2f), new Vector2(size + far * 2f, far), c);
            Place(borders[1], focus - new Vector2(0f, size / 2f + far / 2f), new Vector2(size + far * 2f, far), c);
            Place(borders[2], focus + new Vector2(size / 2f + far / 2f, 0f), new Vector2(far, size), c);
            Place(borders[3], focus - new Vector2(size / 2f + far / 2f, 0f), new Vector2(far, size), c);
        }

        private static void Place(SpriteRenderer sr, Vector2 position, Vector2 size, Color color)
        {
            sr.transform.position = position;
            // fx_white 4x4 px = 0.125 клетки.
            sr.transform.localScale = new Vector3(size.x / 0.125f, size.y / 0.125f, 1f);
            sr.color = color;
        }
    }
}
