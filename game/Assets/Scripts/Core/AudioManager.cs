using System;
using System.Collections.Generic;
using Omut.World;
using UnityEngine;

namespace Omut.Core
{
    // Звук игры (паттерн Singleton): музыка дня и ночи с плавной сменой и короткие звуки по имени.
    // Любой скрипт: GameEvents.RaisePlaySound("bell"). Громкость из настроек.
    // Если в поле clips есть файл с таким id (Лиза кладёт в Assets/Audio), играет он,
    // иначе звук синтезируется кодом (ProceduralSounds).
    public class AudioManager : MonoBehaviour
    {
        [Serializable]
        public class NamedClip
        {
            public string id;
            public AudioClip clip;
        }

        public static AudioManager Instance { get; private set; }

        [Tooltip("Звуки из файлов: id (bell, splash...) и клип. Заполняет сборщик сцены из Assets/Audio/SFX (sfx_<id>.wav)")]
        [SerializeField] private List<NamedClip> clips = new List<NamedClip>();

        [SerializeField] private AudioClip dayMusic;
        [SerializeField] private AudioClip nightMusic;

        [Tooltip("Сколько секунд длится переход музыки")]
        [SerializeField] private float crossfadeSeconds = 4f;

        private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource sfx;
        private bool nightPlaying;
        private float crossfade = 1f;

        private float MusicVolume => SettingsManager.Instance != null ? SettingsManager.Instance.Data.musicVolume : 0.7f;
        private float SfxVolume => SettingsManager.Instance != null ? SettingsManager.Instance.Data.sfxVolume : 1f;

        private void Awake()
        {
            Instance = this;
            musicA = CreateSource(true);
            musicB = CreateSource(true);
            sfx = CreateSource(false);

            foreach (NamedClip named in clips)
            {
                if (named != null && named.clip != null && !string.IsNullOrEmpty(named.id))
                    cache[named.id] = named.clip;
            }

            if (dayMusic == null) dayMusic = ProceduralSounds.DayMusic();
            if (nightMusic == null) nightMusic = ProceduralSounds.NightMusic();

            musicA.clip = dayMusic;
            musicA.volume = MusicVolume;
            musicA.Play();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private AudioSource CreateSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void OnEnable()
        {
            GameEvents.PlaySound += Play;
            GameEvents.BellRang += OnBell;
        }

        private void OnDisable()
        {
            GameEvents.PlaySound -= Play;
            GameEvents.BellRang -= OnBell;
        }

        private void OnBell() => Play("bell");

        public void Play(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            if (!cache.TryGetValue(id, out AudioClip clip))
            {
                clip = ProceduralSounds.Create(id);
                cache[id] = clip;
            }

            if (clip != null)
                sfx.PlayOneShot(clip, SfxVolume);
        }

        public bool HasSound(string id) => cache.ContainsKey(id) || Array.IndexOf(ProceduralSounds.Ids, id) >= 0;

        private void Update()
        {
            DayNightCycle cycle = DayNightCycle.Instance;
            bool wantNight = (cycle != null && cycle.Clock.Phase == DayPhase.Night) || FogController.InEternalFog;

            if (wantNight != nightPlaying)
            {
                nightPlaying = wantNight;
                // Новая музыка в свободный источник, старая плавно гаснет.
                AudioSource next = musicA.isPlaying && musicA.volume > 0.01f ? musicB : musicA;
                next.clip = wantNight ? nightMusic : dayMusic;
                next.volume = 0f;
                next.Play();
                crossfade = 0f;
            }

            crossfade = Mathf.Min(1f, crossfade + Time.unscaledDeltaTime / Mathf.Max(0.1f, crossfadeSeconds));
            AudioClip target = nightPlaying ? nightMusic : dayMusic;
            SetMusicVolume(musicA, musicA.clip == target);
            SetMusicVolume(musicB, musicB.clip == target);
        }

        private void SetMusicVolume(AudioSource source, bool isTarget)
        {
            float v = MusicVolume * (isTarget ? crossfade : 1f - crossfade);
            source.volume = v;
            if (!isTarget && v <= 0.001f && source.isPlaying)
                source.Stop();
        }
    }
}
