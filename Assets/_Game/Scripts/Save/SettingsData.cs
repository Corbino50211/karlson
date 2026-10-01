using System;
using UnityEngine;

namespace Momentum.SaveSystem
{
    /// <summary>User settings, saved to Application.persistentDataPath/settings.json.</summary>
    [Serializable]
    public class SettingsData
    {
        public int version = 1;

        [Header("Controls")]
        public float mouseSensitivity = 2f;
        public bool invertY;
        public bool sprintEnabled = true;

        [Header("Video")]
        public float fieldOfView = 95f;
        public bool fullscreen = true;
        public int resolutionWidth;
        public int resolutionHeight;
        public bool vsync;
        public int fpsLimit = 144;

        [Header("Audio")]
        public float masterVolume = 0.8f;
        public float musicVolume = 0.5f;
        public float sfxVolume = 0.8f;

        [Header("Effects / Comfort")]
        public bool cameraShake = true;
        public bool headBob = true;
        public bool weaponBob = true;
        public bool motionEffects = true;

        [Header("HUD")]
        public bool showSpeedometer = true;
        public bool showTimer = true;
        public bool showDebugOverlay;

        public SettingsData Clone()
        {
            return (SettingsData)MemberwiseClone();
        }

        /// <summary>Clamps values loaded from disk to sane ranges.</summary>
        public void Validate()
        {
            mouseSensitivity = Mathf.Clamp(mouseSensitivity, 0.05f, 20f);
            fieldOfView = Mathf.Clamp(fieldOfView, 60f, 130f);
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            if (fpsLimit < 0) fpsLimit = 0;
            if (resolutionWidth < 0) resolutionWidth = 0;
            if (resolutionHeight < 0) resolutionHeight = 0;
        }
    }
}
