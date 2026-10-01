using System;
using System.Collections.Generic;
using UnityEngine;

namespace Momentum.Audio
{
    /// <summary>
    /// Maps sound cues and music tracks to clips. Leave clips empty to use the procedural placeholders.
    /// Drop your own AudioClips in here to replace any sound without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "Momentum/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public class SoundEntry
        {
            public SoundId id;
            public AudioClip[] clips = new AudioClip[0];
            [Range(0f, 2f)] public float volume = 1f;
            [Range(0f, 0.5f)] public float pitchVariance = 0.06f;
            [Tooltip("Minimum seconds between two plays of this cue (prevents stacking).")]
            public float minInterval = 0.02f;
        }

        [Serializable]
        public class MusicEntry
        {
            public MusicTrack track;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        [Tooltip("Generate placeholder sounds for cues without clips.")]
        public bool useProceduralFallback = true;
        public List<SoundEntry> sounds = new List<SoundEntry>();
        public List<MusicEntry> music = new List<MusicEntry>();
        public AudioClip ambientLoop;

        Dictionary<SoundId, SoundEntry> soundLookup;

        void OnEnable()
        {
            soundLookup = null;
        }

        public SoundEntry GetEntry(SoundId id)
        {
            if (soundLookup == null)
            {
                soundLookup = new Dictionary<SoundId, SoundEntry>();
                foreach (var s in sounds)
                {
                    if (s != null) soundLookup[s.id] = s;
                }
            }
            soundLookup.TryGetValue(id, out var entry);
            return entry;
        }

        public MusicEntry GetMusic(MusicTrack track)
        {
            foreach (var m in music)
            {
                if (m != null && m.track == track) return m;
            }
            return null;
        }

        /// <summary>Ensures there is an entry for every SoundId (used by the setup tool).</summary>
        public void EnsureAllEntries()
        {
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                if (id == SoundId.None) continue;
                if (sounds.Exists(s => s != null && s.id == id)) continue;
                sounds.Add(new SoundEntry { id = id });
            }
            foreach (MusicTrack t in Enum.GetValues(typeof(MusicTrack)))
            {
                if (t == MusicTrack.None) continue;
                if (music.Exists(m => m != null && m.track == t)) continue;
                music.Add(new MusicEntry { track = t });
            }
            soundLookup = null;
        }
    }
}
