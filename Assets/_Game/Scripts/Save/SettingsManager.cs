using System.Collections.Generic;
using UnityEngine;

namespace Momentum.SaveSystem
{
    /// <summary>
    /// Loads, applies and saves user settings (settings.json). Other systems read SettingsManager.Settings
    /// and listen to GameEvents.SettingsChanged.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        /// <summary>Frame-rate limit options shown in the settings menu (0 = unlimited).</summary>
        public static readonly int[] FpsOptions = { 30, 60, 75, 120, 144, 165, 240, 0 };

        static SettingsData fallback;

        public SettingsData Current { get; private set; } = new SettingsData();

        /// <summary>Current settings (never null; returns defaults when no manager exists).</summary>
        public static SettingsData Settings
        {
            get
            {
                if (Instance != null) return Instance.Current;
                if (fallback == null) fallback = new SettingsData();
                return fallback;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            Load();
            Apply(true);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Load()
        {
            if (JsonFileUtility.TryRead(SaveManager.SettingsFilePath, out SettingsData loaded))
            {
                loaded.Validate();
                Current = loaded;
            }
            else
            {
                Current = new SettingsData();
            }
        }

        public void Save()
        {
            Current.Validate();
            JsonFileUtility.Write(SaveManager.SettingsFilePath, Current);
        }

        /// <summary>Applies the current settings to the engine. Display changes are optional (they can flicker).</summary>
        public void Apply(bool applyDisplay)
        {
            Current.Validate();
            QualitySettings.vSyncCount = Current.vsync ? 1 : 0;
            Application.targetFrameRate = Current.vsync || Current.fpsLimit <= 0 ? -1 : Current.fpsLimit;
            AudioListener.volume = Current.masterVolume;

            if (applyDisplay && !Application.isEditor)
            {
                int w = Current.resolutionWidth;
                int h = Current.resolutionHeight;
                if (w <= 0 || h <= 0)
                {
                    w = Screen.currentResolution.width;
                    h = Screen.currentResolution.height;
                }
                var mode = Current.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                Screen.SetResolution(w, h, mode);
            }

            GameEvents.RaiseSettingsChanged();
        }

        /// <summary>Applies and saves in one call (used by the settings menu).</summary>
        public void ApplyAndSave(bool applyDisplay)
        {
            Apply(applyDisplay);
            Save();
        }

        public void ResetToDefaults()
        {
            Current = new SettingsData();
            ApplyAndSave(true);
        }

        /// <summary>Distinct resolutions supported by the display (largest last).</summary>
        public static List<Vector2Int> GetResolutionOptions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (!list.Contains(v)) list.Add(v);
            }
            if (list.Count == 0) list.Add(new Vector2Int(Screen.width, Screen.height));
            list.Sort((a, b) => (a.x * a.y).CompareTo(b.x * b.y));
            return list;
        }
    }
}
