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
        private AudioSource voice;
        private AudioSource loop;
        private string loopId;
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
            voice = CreateSource(false);
            loop = CreateSource(true);

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

            AudioClip clip = Clip(id);
            if (clip != null)
                sfx.PlayOneShot(clip, SfxVolume);
        }

        private AudioClip Clip(string id)
        {
            if (!cache.TryGetValue(id, out AudioClip clip))
            {
                clip = ProceduralSounds.Create(id);
                cache[id] = clip;
            }

            return clip;
        }

        // Звук по кругу, пока его не выключат (катушка, пока игрок тянет рыбу).
        // volume и pitch можно менять каждый кадр: катушка трещит быстрее, когда леска натянута.
        public void PlayLoop(string id, float volume = 1f, float pitch = 1f)
        {
            if (loopId != id)
            {
                loopId = id;
                loop.clip = Clip(id);
                if (loop.clip != null)
                    loop.Play();
            }

            loop.volume = SfxVolume * volume;
            loop.pitch = pitch;
        }

        public void StopLoop()
        {
            loopId = null;
            loop.Stop();
        }

        public bool LoopPlaying => loop != null && loop.isPlaying;

        private float VoiceVolume => SettingsManager.Instance != null ? SettingsManager.Instance.Data.voiceVolume : 1f;

        // Реплика вслух: записанный голос (Resources/Voice) или "бормотание", если файла нет.
        public void PlayVoice(string speaker, string text)
        {
            CancelInvoke(nameof(StopVoice));
            voice.Stop();
            if (string.IsNullOrEmpty(text) || VoiceVolume <= 0.001f)
                return;

            AudioClip clip = Resources.Load<AudioClip>(VoiceLines.Folder + "/" + VoiceLines.ClipName(speaker, text));
            voice.pitch = 1f;
            if (clip == null)
            {
                clip = Babble(VoiceLines.Syllables(text));
                voice.pitch = VoiceLines.Pitch(speaker);
            }

            voice.clip = clip;
            voice.volume = VoiceVolume;
            voice.Play();
        }

        public void StopVoice() => voice.Stop();

        public bool VoicePlaying => voice != null && voice.isPlaying;

        private AudioClip babble;

        // Короткие "слоги" разной высоты, как в Animal Crossing. Не дольше 4 секунд.
        private AudioClip Babble(int syllables)
        {
            const int rate = 22050;
            const float syllable = 0.09f;
            int count = Mathf.Clamp(syllables, 2, 40);
            if (babble == null)
            {
                var data = new float[(int)(rate * syllable * 40)];
                var random = new System.Random(7);
                for (int s = 0; s < 40; s++)
                {
                    float freq = 180f + (float)random.NextDouble() * 140f;
                    int start = (int)(s * syllable * rate);
                    int length = (int)(syllable * rate * 0.8f);
                    for (int i = 0; i < length; i++)
                    {
                        float t = i / (float)rate;
                        float env = Mathf.Sin(Mathf.PI * i / length);
                        data[start + i] = env * 0.35f * (Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * freq * t));
                    }
                }

                babble = AudioClip.Create("babble", data.Length, 1, rate, false);
                babble.SetData(data, 0);
            }

            // Обрезать до нужного числа слогов: отдельный клип на каждую длину не нужен, хватит остановить вовремя.
            Invoke(nameof(StopVoice), count * syllable);
            return babble;
        }

        public bool HasSound(string id) => cache.ContainsKey(id) || Array.IndexOf(ProceduralSounds.Ids, id) >= 0;

        private void Update()
        {
            // На паузе время стоит, катушка тоже замолкает.
            if (loopId != null)
            {
                if (Time.timeScale <= 0f && loop.isPlaying) loop.Pause();
                else if (Time.timeScale > 0f && !loop.isPlaying) loop.UnPause();
            }

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
