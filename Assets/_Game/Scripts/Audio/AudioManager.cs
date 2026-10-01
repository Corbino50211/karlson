using System.Collections.Generic;
using Momentum.SaveSystem;
using UnityEngine;

namespace Momentum.Audio
{
    /// <summary>
    /// Central audio playback: pooled 3D/2D sound effects, UI sounds, crossfaded music and ambience.
    /// Works with no audio files at all thanks to ProceduralAudio placeholders.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] int sfxVoices = 28;
        [SerializeField] float musicFadeSpeed = 1.2f;

        AudioSource[] sfxSources;
        AudioSource uiSource;
        AudioSource musicA;
        AudioSource musicB;
        AudioSource ambientSource;
        bool musicAActive = true;
        float musicTargetVolume;
        int nextVoice;
        MusicTrack currentTrack = MusicTrack.None;

        readonly Dictionary<SoundId, float> lastPlayTimes = new Dictionary<SoundId, float>();

        AudioLibrary Library => GameConfig.Instance != null ? GameConfig.Instance.audioLibrary : null;

        public MusicTrack CurrentTrack => currentTrack;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            sfxSources = new AudioSource[Mathf.Max(4, sfxVoices)];
            for (int i = 0; i < sfxSources.Length; i++)
            {
                var src = CreateSource("SFX " + i);
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = 3f;
                src.maxDistance = 120f;
                src.dopplerLevel = 0f;
                sfxSources[i] = src;
            }

            uiSource = CreateSource("UI");
            uiSource.spatialBlend = 0f;
            uiSource.ignoreListenerPause = true;

            musicA = CreateSource("Music A");
            musicB = CreateSource("Music B");
            foreach (var m in new[] { musicA, musicB })
            {
                m.loop = true;
                m.spatialBlend = 0f;
                m.ignoreListenerPause = true;
                m.priority = 0;
                m.volume = 0f;
            }

            ambientSource = CreateSource("Ambient");
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEvents.SettingsChanged += OnSettingsChanged;
        }

        void OnDisable()
        {
            GameEvents.SettingsChanged -= OnSettingsChanged;
        }

        AudioSource CreateSource(string sourceName)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return src;
        }

        void OnSettingsChanged()
        {
            UpdateMusicTargetVolume();
            if (ambientSource != null) ambientSource.volume = SettingsManager.Settings.sfxVolume * 0.35f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime * musicFadeSpeed;
            var active = musicAActive ? musicA : musicB;
            var inactive = musicAActive ? musicB : musicA;
            active.volume = Mathf.MoveTowards(active.volume, musicTargetVolume, dt);
            inactive.volume = Mathf.MoveTowards(inactive.volume, 0f, dt);
            if (inactive.isPlaying && inactive.volume <= 0.001f) inactive.Stop();
        }

        // ------------------------------------------------------------------ Static API

        /// <summary>Plays a positional sound effect.</summary>
        public static void Play(SoundId id, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (Instance != null) Instance.PlayInternal(id, position, true, volume, pitch);
        }

        /// <summary>Plays a non-positional sound effect (pauses with the game).</summary>
        public static void Play2D(SoundId id, float volume = 1f, float pitch = 1f)
        {
            if (Instance != null) Instance.PlayInternal(id, Vector3.zero, false, volume, pitch);
        }

        /// <summary>Plays a UI sound that keeps working while the game is paused.</summary>
        public static void PlayUI(SoundId id, float volume = 1f)
        {
            if (Instance == null) return;
            var clip = Instance.ResolveClip(id, out var entry);
            if (clip == null) return;
            float v = volume * (entry != null ? entry.volume : 1f) * SettingsManager.Settings.sfxVolume;
            Instance.uiSource.PlayOneShot(clip, v);
        }

        /// <summary>Plays an arbitrary clip at a position.</summary>
        public static void PlayClip(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, bool spatial = true)
        {
            if (Instance == null || clip == null) return;
            var src = Instance.NextVoice();
            Instance.Configure(src, clip, position, spatial, volume * SettingsManager.Settings.sfxVolume, pitch);
        }

        public static void PlayMusic(MusicTrack track)
        {
            if (Instance != null) Instance.PlayMusicInternal(track);
        }

        public static void StopMusic()
        {
            if (Instance != null) Instance.PlayMusicInternal(MusicTrack.None);
        }

        /// <summary>Starts or stops the looping ambient bed (library clip or procedural room tone).</summary>
        public static void SetAmbient(bool enabled)
        {
            if (Instance != null) Instance.SetAmbientInternal(enabled);
        }

        void SetAmbientInternal(bool enabled)
        {
            if (!enabled)
            {
                ambientSource.Stop();
                return;
            }
            var lib = Library;
            AudioClip clip = lib != null && lib.ambientLoop != null ? lib.ambientLoop : null;
            if (clip == null && (lib == null || lib.useProceduralFallback)) clip = ProceduralAudio.GetAmbient();
            if (clip == null) return;
            if (ambientSource.clip == clip && ambientSource.isPlaying) return;
            ambientSource.clip = clip;
            ambientSource.volume = SettingsManager.Settings.sfxVolume * 0.35f;
            ambientSource.Play();
        }

        // ------------------------------------------------------------------ Internals

        AudioClip ResolveClip(SoundId id, out AudioLibrary.SoundEntry entry)
        {
            entry = null;
            if (id == SoundId.None) return null;
            var lib = Library;
            if (lib != null)
            {
                entry = lib.GetEntry(id);
                if (entry != null && entry.clips != null && entry.clips.Length > 0)
                {
                    int start = Random.Range(0, entry.clips.Length);
                    for (int i = 0; i < entry.clips.Length; i++)
                    {
                        var c = entry.clips[(start + i) % entry.clips.Length];
                        if (c != null) return c;
                    }
                }
                if (!lib.useProceduralFallback) return null;
            }
            return ProceduralAudio.GetSfx(id);
        }

        void PlayInternal(SoundId id, Vector3 position, bool spatial, float volume, float pitch)
        {
            var clip = ResolveClip(id, out var entry);
            if (clip == null) return;

            float minInterval = entry != null ? entry.minInterval : 0.02f;
            float now = Time.unscaledTime;
            if (lastPlayTimes.TryGetValue(id, out float last) && now - last < minInterval) return;
            lastPlayTimes[id] = now;

            float variance = entry != null ? entry.pitchVariance : 0.06f;
            float finalPitch = pitch * (1f + Random.Range(-variance, variance));
            float finalVolume = volume * (entry != null ? entry.volume : 1f) * SettingsManager.Settings.sfxVolume;
            Configure(NextVoice(), clip, position, spatial, finalVolume, finalPitch);
        }

        AudioSource NextVoice()
        {
            for (int i = 0; i < sfxSources.Length; i++)
            {
                int index = (nextVoice + i) % sfxSources.Length;
                if (!sfxSources[index].isPlaying)
                {
                    nextVoice = (index + 1) % sfxSources.Length;
                    return sfxSources[index];
                }
            }
            var src = sfxSources[nextVoice];
            nextVoice = (nextVoice + 1) % sfxSources.Length;
            return src;
        }

        void Configure(AudioSource src, AudioClip clip, Vector3 position, bool spatial, float volume, float pitch)
        {
            src.Stop();
            src.transform.position = position;
            src.spatialBlend = spatial ? 1f : 0f;
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            src.Play();
        }

        void PlayMusicInternal(MusicTrack track)
        {
            if (track == currentTrack) return;
            currentTrack = track;
            musicAActive = !musicAActive;
            var active = musicAActive ? musicA : musicB;
            active.Stop();
            if (track == MusicTrack.None)
            {
                musicTargetVolume = 0f;
                return;
            }

            AudioClip clip = null;
            var lib = Library;
            var entry = lib != null ? lib.GetMusic(track) : null;
            if (entry != null && entry.clip != null) clip = entry.clip;
            if (clip == null && (lib == null || lib.useProceduralFallback)) clip = ProceduralAudio.GetMusic(track);
            if (clip == null) return;

            active.clip = clip;
            active.volume = 0f;
            active.Play();
            UpdateMusicTargetVolume();
        }

        void UpdateMusicTargetVolume()
        {
            float entryVolume = 1f;
            var lib = Library;
            var entry = lib != null ? lib.GetMusic(currentTrack) : null;
            if (entry != null) entryVolume = entry.volume;
            musicTargetVolume = currentTrack == MusicTrack.None ? 0f : SettingsManager.Settings.musicVolume * entryVolume * 0.6f;
        }
    }
}
