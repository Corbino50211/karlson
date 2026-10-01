using System;
using System.Collections.Generic;
using UnityEngine;

namespace Momentum.Audio
{
    /// <summary>
    /// Tiny synthesizer that generates original placeholder sound effects and music loops at runtime.
    /// This guarantees the game has audio feedback without shipping any audio files.
    /// Replace any cue by assigning a clip in the AudioLibrary asset.
    /// </summary>
    public static class ProceduralAudio
    {
        const int SampleRate = 44100;
        const int MusicSampleRate = 22050;
        const float TwoPi = Mathf.PI * 2f;

        enum Wave { Sine, Square, Saw, Triangle }

        static readonly Dictionary<SoundId, AudioClip> sfxCache = new Dictionary<SoundId, AudioClip>();
        static readonly Dictionary<MusicTrack, AudioClip> musicCache = new Dictionary<MusicTrack, AudioClip>();
        static AudioClip ambientClip;
        static System.Random rng = new System.Random(1337);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            sfxCache.Clear();
            musicCache.Clear();
            ambientClip = null;
            rng = new System.Random(1337);
        }

        public static AudioClip GetSfx(SoundId id)
        {
            if (id == SoundId.None) return null;
            if (sfxCache.TryGetValue(id, out var clip) && clip != null) return clip;
            clip = GenerateSfx(id);
            sfxCache[id] = clip;
            return clip;
        }

        public static AudioClip GetMusic(MusicTrack track)
        {
            if (track == MusicTrack.None) return null;
            if (musicCache.TryGetValue(track, out var clip) && clip != null) return clip;
            clip = GenerateMusic(track);
            musicCache[track] = clip;
            return clip;
        }

        public static AudioClip GetAmbient()
        {
            if (ambientClip != null) return ambientClip;
            float len = 8f;
            int n = (int)(len * MusicSampleRate);
            var data = new float[n];
            float brown = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / MusicSampleRate;
                brown = Mathf.Clamp(brown + Noise() * 0.02f, -1f, 1f);
                lp += 0.02f * (brown - lp);
                float hum = Mathf.Sin(TwoPi * 55f * t) * 0.05f + Mathf.Sin(TwoPi * 110f * t) * 0.02f;
                data[i] = lp * 0.6f + hum;
            }
            LoopCrossfade(data, MusicSampleRate / 4);
            ambientClip = Create("Ambient", data, MusicSampleRate);
            return ambientClip;
        }

        // ------------------------------------------------------------------ SFX

        static AudioClip GenerateSfx(SoundId id)
        {
            string n = id.ToString();
            switch (id)
            {
                case SoundId.UIClick: return Sweep(n, 0.05f, 1600f, 1100f, Wave.Sine, 0.001f, 70f, 0.35f);
                case SoundId.UIHover: return Sweep(n, 0.035f, 2200f, 2000f, Wave.Sine, 0.001f, 110f, 0.15f);
                case SoundId.UIBack: return Sweep(n, 0.07f, 900f, 600f, Wave.Sine, 0.001f, 55f, 0.35f);

                case SoundId.Jump: return Sweep(n, 0.14f, 260f, 520f, Wave.Triangle, 0.004f, 22f, 0.35f, 0.15f);
                case SoundId.Land: return Shot(n, 0.22f, 0.6f, 35f, 900f, 200f, 110f, 0.9f, 18f, 1.2f);
                case SoundId.Footstep: return Shot(n, 0.07f, 0.8f, 70f, 2500f, 600f, 140f, 0.3f, 60f, 0.8f);
                case SoundId.WallRunStep: return Shot(n, 0.06f, 0.8f, 80f, 3500f, 1200f, 220f, 0.2f, 70f, 0.7f);
                case SoundId.SlideStart: return NoiseSweep(n, 0.55f, 5000f, 900f, 6f, 0.45f);
                case SoundId.WallJump: return Sweep(n, 0.16f, 330f, 760f, Wave.Triangle, 0.003f, 20f, 0.38f, 0.25f);
                case SoundId.Vault: return Sweep(n, 0.12f, 200f, 420f, Wave.Triangle, 0.003f, 25f, 0.3f, 0.3f);
                case SoundId.GrappleFire: return Zip(n, 0.22f, 400f, 1800f, 0.4f);
                case SoundId.GrappleAttach: return Metallic(n, 0.18f, 1250f, 0.45f);
                case SoundId.GrappleRelease: return Sweep(n, 0.12f, 900f, 300f, Wave.Saw, 0.002f, 25f, 0.2f, 0.2f);
                case SoundId.LaunchPad: return Boing(n, 0.6f, 160f, 900f, 0.5f);
                case SoundId.SpeedPad: return Zip(n, 0.35f, 300f, 1400f, 0.45f);

                case SoundId.Pistol: return Shot(n, 0.28f, 1f, 22f, 7000f, 900f, 140f, 0.8f, 30f, 1.4f);
                case SoundId.SMG: return Shot(n, 0.14f, 1f, 40f, 8000f, 1500f, 160f, 0.5f, 45f, 1.1f);
                case SoundId.Shotgun: return Shot(n, 0.55f, 1f, 9f, 5000f, 300f, 75f, 1.3f, 9f, 1.8f);
                case SoundId.Rifle: return Shot(n, 0.22f, 1f, 26f, 7500f, 1000f, 120f, 0.8f, 30f, 1.3f);
                case SoundId.Revolver: return Shot(n, 0.6f, 1f, 10f, 6500f, 500f, 90f, 1.2f, 10f, 1.8f);
                case SoundId.RocketLaunch: return NoiseSweep(n, 0.7f, 600f, 3000f, 4f, 0.8f, 80f);
                case SoundId.GrenadeLaunch: return Shot(n, 0.3f, 0.5f, 25f, 1800f, 400f, 180f, 1.2f, 14f, 1.3f);
                case SoundId.Railgun: return Rail(n);
                case SoundId.Reload: return Clicks(n, new[] { 0f, 0.16f, 0.26f }, 0.4f);
                case SoundId.DryFire: return Clicks(n, new[] { 0f }, 0.1f);
                case SoundId.WeaponSwitch: return Clicks(n, new[] { 0f, 0.08f }, 0.2f);

                case SoundId.WeaponPickup: return Arp(n, new[] { 523f, 659f, 784f, 1047f }, 0.06f, Wave.Square, 0.18f);
                case SoundId.AmmoPickup: return Arp(n, new[] { 660f, 990f }, 0.07f, Wave.Square, 0.16f);
                case SoundId.HealthPickup: return Arp(n, new[] { 392f, 523f, 659f }, 0.09f, Wave.Sine, 0.3f);

                case SoundId.HitMarker: return Sweep(n, 0.05f, 2400f, 2200f, Wave.Square, 0.0005f, 90f, 0.12f);
                case SoundId.KillConfirm: return Arp(n, new[] { 1800f, 2600f }, 0.05f, Wave.Square, 0.14f);
                case SoundId.Explosion: return Explosion(n, 1.6f, 1f);
                case SoundId.BulletImpact: return Shot(n, 0.08f, 0.9f, 60f, 6000f, 2000f, 400f, 0.15f, 80f, 0.6f);
                case SoundId.PlayerHurt: return Sweep(n, 0.22f, 160f, 90f, Wave.Square, 0.002f, 14f, 0.3f, 0.3f);
                case SoundId.PlayerDeath: return Sweep(n, 0.9f, 300f, 50f, Wave.Saw, 0.005f, 3.5f, 0.35f, 0.3f);

                case SoundId.EnemyShot: return Sweep(n, 0.2f, 1100f, 280f, Wave.Square, 0.001f, 16f, 0.2f);
                case SoundId.EnemyAlert: return Arp(n, new[] { 880f, 1320f }, 0.08f, Wave.Square, 0.16f);
                case SoundId.EnemyDeath: return Crunch(n, 0.55f, 0.7f);
                case SoundId.LaserCharge: return Sweep(n, 0.9f, 200f, 1400f, Wave.Sine, 0.3f, 0.8f, 0.25f);
                case SoundId.LaserFire: return Buzz(n, 0.5f, 120f, 0.4f);
                case SoundId.ChargerRoar: return Roar(n, 0.8f, 95f, 0.45f);

                case SoundId.BossRoar: return Roar(n, 1.6f, 60f, 0.6f);
                case SoundId.BossSlam: return Explosion(n, 1.2f, 1.3f);
                case SoundId.Shockwave: return NoiseSweep(n, 0.9f, 2500f, 150f, 3f, 0.6f, 50f);
                case SoundId.BossDeath: return Explosion(n, 2.6f, 1.4f);
                case SoundId.ShieldDown: return Sweep(n, 0.7f, 1200f, 150f, Wave.Saw, 0.005f, 4f, 0.3f, 0.2f);

                case SoundId.Checkpoint: return Arp(n, new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.07f, Wave.Triangle, 0.35f);
                case SoundId.Finish: return Arp(n, new[] { 523f, 523f, 659f, 784f, 1047f, 784f, 1047f }, 0.11f, Wave.Square, 0.22f);
                case SoundId.GlassBreak: return Glass(n, 0.7f);
                case SoundId.CrateBreak: return Crunch(n, 0.35f, 0.6f);
                case SoundId.DoorOpen: return Sweep(n, 0.7f, 80f, 140f, Wave.Saw, 0.05f, 2.5f, 0.25f, 0.15f);
                case SoundId.Secret: return Arp(n, new[] { 784f, 988f, 1175f, 1568f, 1976f }, 0.09f, Wave.Sine, 0.3f);
                default: return Sweep(n, 0.1f, 600f, 400f, Wave.Sine, 0.002f, 30f, 0.2f);
            }
        }

        static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        static float Osc(Wave wave, float phase)
        {
            float p = phase - Mathf.Floor(phase);
            switch (wave)
            {
                case Wave.Square: return p < 0.5f ? 1f : -1f;
                case Wave.Saw: return p * 2f - 1f;
                case Wave.Triangle: return 1f - 4f * Mathf.Abs(p - 0.5f);
                default: return Mathf.Sin(p * TwoPi);
            }
        }

        static float LowpassCoef(float cutoff, int sr) => 1f - Mathf.Exp(-TwoPi * Mathf.Max(10f, cutoff) / sr);

        static float SoftClip(float x) => (float)Math.Tanh(x);

        static AudioClip Create(string clipName, float[] data, int sr)
        {
            int fade = Mathf.Min(data.Length / 4, sr / 400);
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                data[i] *= k;
                data[data.Length - 1 - i] *= k;
            }
            var clip = AudioClip.Create("Proc_" + clipName, data.Length, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Noise burst plus pitched thump: gunshots, footsteps, landings, impacts.</summary>
        static AudioClip Shot(string clipName, float length, float noiseAmt, float noiseDecay, float lpStart, float lpEnd,
            float thumpFreq, float thumpAmt, float thumpDecay, float gain)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float k = t / length;
                float a = LowpassCoef(Mathf.Lerp(lpStart, lpEnd, k), SampleRate);
                lp += a * (Noise() * noiseAmt * Mathf.Exp(-t * noiseDecay) - lp);
                phase += thumpFreq * (1f - 0.55f * k) / SampleRate;
                float thump = Mathf.Sin(phase * TwoPi) * thumpAmt * Mathf.Exp(-t * thumpDecay);
                d[i] = SoftClip((lp + thump) * gain) * 0.8f;
            }
            return Create(clipName, d, SampleRate);
        }

        /// <summary>Pitch sweep with an exponential decay envelope.</summary>
        static AudioClip Sweep(string clipName, float length, float f0, float f1, Wave wave, float attack, float decay, float gain, float noiseMix = 0f)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float k = t / length;
                float f = Mathf.Lerp(f0, f1, k);
                phase += f / SampleRate;
                float env = (attack > 0f ? Mathf.Clamp01(t / attack) : 1f) * Mathf.Exp(-t * decay);
                float s = Osc(wave, phase) * (1f - noiseMix) + Noise() * noiseMix;
                d[i] = s * env * gain;
            }
            return Create(clipName, d, SampleRate);
        }

        static AudioClip NoiseSweep(string clipName, float length, float lpStart, float lpEnd, float decay, float gain, float rumbleFreq = 0f)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float k = t / length;
                lp += LowpassCoef(Mathf.Lerp(lpStart, lpEnd, k), SampleRate) * (Noise() - lp);
                float env = Mathf.Clamp01(t / 0.02f) * Mathf.Exp(-t * decay);
                float rumble = 0f;
                if (rumbleFreq > 0f)
                {
                    phase += rumbleFreq / SampleRate;
                    rumble = Mathf.Sin(phase * TwoPi) * 0.4f;
                }
                d[i] = SoftClip((lp * 1.6f + rumble) * env * gain * 1.5f);
            }
            return Create(clipName, d, SampleRate);
        }

        static AudioClip Zip(string clipName, float length, float f0, float f1, float gain)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float k = t / length;
                float f = Mathf.Lerp(f0, f1, k * k);
                phase += f / SampleRate;
                lp += LowpassCoef(f * 3f, SampleRate) * (Noise() - lp);
                float env = Mathf.Clamp01(t / 0.01f) * (1f - k);
                d[i] = (Osc(Wave.Saw, phase) * 0.4f + lp * 0.6f) * env * gain;
            }
            return Create(clipName, d, SampleRate);
        }

        static AudioClip Metallic(string clipName, float length, float f, float gain)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 22f);
                float s = Mathf.Sin(TwoPi * f * t) + 0.6f * Mathf.Sin(TwoPi * f * 1.47f * t) + 0.4f * Mathf.Sin(TwoPi * f * 2.13f * t);
                d[i] = (s * 0.4f + Noise() * 0.3f * Mathf.Exp(-t * 90f)) * env * gain;
            }
            return Create(clipName, d, SampleRate);
        }

        static AudioClip Boing(string clipName, float length, float f0, float f1, float gain)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float k = t / length;
                float f = Mathf.Lerp(f0, f1, Mathf.Sqrt(k));
                phase += f / SampleRate;
                lp += LowpassCoef(2000f * (1f - k) + 200f, SampleRate) * (Noise() - lp);
                float env = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t * 4f);
                d[i] = (Osc(Wave.Triangle, phase) * 0.6f + lp * 0.5f) * env * gain;
            }
            return Create(clipName, d, SampleRate);
        }

        static AudioClip Arp(string clipName, float[] freqs, float noteLength, Wave wave, float gain)
        {
            float length = freqs.Length * noteLength + 0.25f;
            int n = (int)(length * SampleRate);
            var d = new float[n];
            for (int note = 0; note < freqs.Length; note++)
            {
                int start = (int)(note * noteLength * SampleRate);
                float phase = 0f;
                for (int i = start; i < n; i++)
                {
                    float t = (float)(i - start) / SampleRate;
                    float env = Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t * 9f);
                    if (env < 0.001f && t > 0.05f) break;
                    phase += freqs[note] / SampleRate;
                    d[i] += Osc(wave, phase) * env * gain;
                }
            }
            for (int i = 0; i < n; i++) d[i] = SoftClip(d[i]);
            return Create(clipName, d, SampleRate);
        }

        static AudioClip Clicks(string clipName, float[] times, float length)
        {
            int n = (int)((length + 0.05f) * SampleRate);
            var d = new float[n];
            foreach (float time in times)
            {
                int start = (int)(time * SampleRate);
                for (int i = start; i < n; i++)
                {
                    float t = (float)(i - start) / SampleRate;
                    float env = Mathf.Exp(-t * 120f);
                    if (env < 0.001f) break;
                    d[i] += (Noise() * 0.5f + Mathf.Sin(TwoPi * 2200f * t) * 0.4f) * env * 0.5f;
                }
            }
            return Create(clipName, d, SampleRate);
        }

        static AudioClip Explosion(string clipName, float length, float gain)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float lp = 0f, lp2 = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float k = t / length;
                lp += LowpassCoef(Mathf.Lerp(3500f, 120f, Mathf.Sqrt(k)), SampleRate) * (Noise() - lp);
                lp2 += LowpassCoef(300f, SampleRate) * (lp - lp2);
                phase += Mathf.Lerp(70f, 30f, k) / SampleRate;
                float env = Mathf.Clamp01(t / 0.005f) * Mathf.Exp(-t * 3.2f);
                float s = lp * 1.3f + lp2 * 1.5f + Mathf.Sin(phase * TwoPi) * 0.7f * Mathf.Exp(-t * 5f);
                d[i] = SoftClip(s * env * gain * 1.6f) * 0.9f;
            }
            return Create(clipName, d, SampleRate);
        }

        static AudioClip Rail(string clipName)
        {
            float length = 0.9f;
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float k = t / length;
                phase += Mathf.Lerp(2400f, 90f, Mathf.Sqrt(k)) / SampleRate;
                lp += LowpassCoef(9000f * (1f - k) + 300f, SampleRate) * (Noise() - lp);
                float crack = lp * Mathf.Exp(-t * 18f) * 1.4f;
                float zap = Osc(Wave.Saw, phase) * Mathf.Exp(-t * 3.5f) * 0.5f;
                d[i] = SoftClip((crack + zap) * 1.3f) * 0.8f;
            }
            return Create(clipName, d, SampleRate);
        }

        static AudioClip Crunch(string clipName, float length, float gain)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float k = t / length;
                lp += LowpassCoef(2500f * (1f - k) + 200f, SampleRate) * (Noise() - lp);
                phase += Mathf.Lerp(220f, 60f, k) / SampleRate;
                float grain = (rng.NextDouble() < 0.02) ? Noise() * 2f : 0f;
                float env = Mathf.Exp(-t * 6f);
                d[i] = SoftClip((lp + grain * 0.3f + Osc(Wave.Square, phase) * 0.25f) * env * gain * 1.5f);
            }
            return Create(clipName, d, SampleRate);
        }

        static AudioClip Buzz(string clipName, float length, float f, float gain)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                phase += (f + Mathf.Sin(t * 60f) * 8f) / SampleRate;
                float env = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t * 4f);
                d[i] = SoftClip((Osc(Wave.Saw, phase) + Noise() * 0.3f) * env * gain * 1.5f);
            }
            return Create(clipName, d, SampleRate);
        }

        static AudioClip Roar(string clipName, float length, float f, float gain)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float k = t / length;
                phase += (f * (1f + 0.15f * Mathf.Sin(t * 30f)) * (1f - 0.3f * k)) / SampleRate;
                lp += LowpassCoef(900f, SampleRate) * (Noise() - lp);
                float env = Mathf.Clamp01(t / 0.08f) * Mathf.Clamp01((length - t) / 0.3f);
                d[i] = SoftClip((Osc(Wave.Saw, phase) * 0.7f + lp * 0.8f) * env * gain * 2f) * 0.8f;
            }
            return Create(clipName, d, SampleRate);
        }

        static AudioClip Glass(string clipName, float length)
        {
            int n = (int)(length * SampleRate);
            var d = new float[n];
            float hp = 0f, prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float x = Noise();
                hp = 0.95f * (hp + x - prev);
                prev = x;
                float sparkle = 0f;
                if (rng.NextDouble() < 0.004) sparkle = 1f;
                float env = Mathf.Exp(-t * 7f);
                d[i] = (hp * 0.5f * env + sparkle * Mathf.Exp(-t * 2f) * 0.6f) * 0.7f;
            }
            return Create(clipName, d, SampleRate);
        }

        // ------------------------------------------------------------------ Music

        static float MidiToFreq(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        struct TrackStyle
        {
            public float bpm;
            public int[][] chords;      // per bar
            public bool fourOnFloor;
            public bool snare;
            public float bassGain;
            public float arpGain;
            public float padGain;
            public float hatGain;
            public float kickGain;
            public bool distortedBass;
        }

        static AudioClip GenerateMusic(MusicTrack track)
        {
            var am = new[] { 57, 60, 64 };
            var f = new[] { 53, 57, 60 };
            var c = new[] { 55, 60, 64 };
            var g = new[] { 55, 59, 62 };
            var dm = new[] { 57, 62, 65 };
            var e = new[] { 56, 59, 64 };
            var bb = new[] { 58, 62, 65 };

            TrackStyle style;
            switch (track)
            {
                case MusicTrack.Menu:
                    style = new TrackStyle { bpm = 92f, chords = new[] { am, am, f, f, c, c, g, g }, fourOnFloor = false, snare = false, bassGain = 0.18f, arpGain = 0.05f, padGain = 0.12f, hatGain = 0.02f, kickGain = 0.25f };
                    break;
                case MusicTrack.Editor:
                    style = new TrackStyle { bpm = 84f, chords = new[] { f, f, c, c, am, am, g, g }, fourOnFloor = false, snare = false, bassGain = 0.12f, arpGain = 0.04f, padGain = 0.12f, hatGain = 0.015f, kickGain = 0.15f };
                    break;
                case MusicTrack.Boss:
                    style = new TrackStyle { bpm = 152f, chords = new[] { am, am, bb, bb, dm, dm, e, e }, fourOnFloor = true, snare = true, bassGain = 0.22f, arpGain = 0.06f, padGain = 0.04f, hatGain = 0.05f, kickGain = 0.5f, distortedBass = true };
                    break;
                case MusicTrack.Intense:
                    style = new TrackStyle { bpm = 140f, chords = new[] { am, am, f, f, dm, dm, e, e }, fourOnFloor = true, snare = true, bassGain = 0.2f, arpGain = 0.06f, padGain = 0.03f, hatGain = 0.045f, kickGain = 0.45f, distortedBass = true };
                    break;
                default:
                    style = new TrackStyle { bpm = 128f, chords = new[] { am, am, f, f, c, c, g, g }, fourOnFloor = true, snare = false, bassGain = 0.2f, arpGain = 0.06f, padGain = 0.05f, hatGain = 0.04f, kickGain = 0.42f };
                    break;
            }
            return RenderTrack(track.ToString(), style);
        }

        static AudioClip RenderTrack(string trackName, TrackStyle s)
        {
            int bars = s.chords.Length;
            float stepDur = 60f / s.bpm / 4f;
            int stepSamples = Mathf.Max(1, (int)(stepDur * MusicSampleRate));
            int totalSteps = bars * 16;
            int n = stepSamples * totalSteps;
            var d = new float[n];
            int sr = MusicSampleRate;
            var arpPattern = new[] { 0, 1, 2, 1, 0, 2, 1, 2 };

            // Kick, snare, hats
            for (int step = 0; step < totalSteps; step++)
            {
                int start = step * stepSamples;
                int inBar = step % 16;
                bool kick = s.fourOnFloor ? inBar % 4 == 0 : (inBar == 0 || inBar == 10);
                if (kick && s.kickGain > 0f)
                {
                    float phase = 0f;
                    for (int i = 0; i < stepSamples * 3 && start + i < n; i++)
                    {
                        float t = (float)i / sr;
                        phase += (50f + 120f * Mathf.Exp(-t * 30f)) / sr;
                        d[start + i] += Mathf.Sin(phase * TwoPi) * Mathf.Exp(-t * 9f) * s.kickGain;
                    }
                }
                if (s.snare && (inBar == 4 || inBar == 12))
                {
                    float lp = 0f;
                    for (int i = 0; i < stepSamples * 2 && start + i < n; i++)
                    {
                        float t = (float)i / sr;
                        lp += 0.5f * (Noise() - lp);
                        d[start + i] += (lp * 0.8f + Mathf.Sin(TwoPi * 190f * t) * 0.3f) * Mathf.Exp(-t * 16f) * 0.25f;
                    }
                }
                if (s.hatGain > 0f && inBar % 2 == 1)
                {
                    float prev = 0f, hp = 0f;
                    for (int i = 0; i < stepSamples && start + i < n; i++)
                    {
                        float t = (float)i / sr;
                        float x = Noise();
                        hp = 0.7f * (hp + x - prev);
                        prev = x;
                        d[start + i] += hp * Mathf.Exp(-t * 60f) * s.hatGain;
                    }
                }
            }

            // Bass (8th notes), arp (16ths), pad (sustained)
            float bassPhase = 0f, arpPhase = 0f, bassLp = 0f;
            float[] padPhases = new float[3];
            for (int i = 0; i < n; i++)
            {
                int step = i / stepSamples;
                int bar = step / 16;
                int inBar = step % 16;
                var chord = s.chords[bar % s.chords.Length];
                float tStep = (float)(i % stepSamples) / sr;

                // bass
                int bassNote = chord[0] - 24 + ((inBar % 4 == 2) ? 12 : 0);
                bassPhase += MidiToFreq(bassNote) / sr;
                float bassEnvT = (float)(i % (stepSamples * 2)) / sr;
                float bassEnv = Mathf.Exp(-bassEnvT * 5f);
                float bassRaw = Osc(Wave.Saw, bassPhase);
                bassLp += LowpassCoef(s.distortedBass ? 1400f : 700f, sr) * (bassRaw - bassLp);
                float bass = s.distortedBass ? SoftClip(bassLp * 3f) * 0.6f : bassLp;
                d[i] += bass * bassEnv * s.bassGain;

                // arp
                if (s.arpGain > 0f)
                {
                    int arpNote = chord[arpPattern[step % arpPattern.Length]] + 12;
                    arpPhase += MidiToFreq(arpNote) / sr;
                    float arpEnv = Mathf.Exp(-tStep * 18f);
                    d[i] += Osc(Wave.Square, arpPhase) * arpEnv * s.arpGain;
                }

                // pad
                if (s.padGain > 0f)
                {
                    float barT = (float)(i % (stepSamples * 16)) / sr;
                    float padEnv = Mathf.Clamp01(barT / 0.6f);
                    float pad = 0f;
                    for (int v = 0; v < 3; v++)
                    {
                        padPhases[v] += MidiToFreq(chord[v]) * (1f + 0.002f * v) / sr;
                        pad += Osc(Wave.Triangle, padPhases[v]);
                    }
                    d[i] += pad * padEnv * s.padGain / 3f;
                }
            }

            for (int i = 0; i < n; i++) d[i] = SoftClip(d[i] * 1.2f) * 0.85f;
            LoopCrossfade(d, sr / 50);
            var clip = AudioClip.Create("ProcMusic_" + trackName, n, 1, sr, false);
            clip.SetData(d, 0);
            return clip;
        }

        /// <summary>Blends the end of a loop into its start to avoid clicks at the loop point.</summary>
        static void LoopCrossfade(float[] data, int length)
        {
            length = Mathf.Min(length, data.Length / 2);
            for (int i = 0; i < length; i++)
            {
                float k = (float)i / length;
                int end = data.Length - length + i;
                data[end] = Mathf.Lerp(data[end], data[i], k);
            }
        }
    }
}
