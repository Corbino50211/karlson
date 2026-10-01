using System;
using System.Collections.Generic;
using System.Globalization;
using Momentum.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>
    /// Settings screen (used by the main menu and the pause menu). Changes apply immediately and are saved
    /// to settings.json when the panel closes.
    /// </summary>
    public class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] Slider sensitivity;
        [SerializeField] Text sensitivityValue;
        [SerializeField] Toggle invertY;
        [SerializeField] Toggle sprint;
        [SerializeField] Slider fov;
        [SerializeField] Text fovValue;
        [SerializeField] Toggle fullscreen;
        [SerializeField] ValueCycler resolution;
        [SerializeField] Toggle vsync;
        [SerializeField] ValueCycler fpsLimit;
        [SerializeField] Slider masterVolume;
        [SerializeField] Text masterValue;
        [SerializeField] Slider musicVolume;
        [SerializeField] Text musicValue;
        [SerializeField] Slider sfxVolume;
        [SerializeField] Text sfxValue;
        [SerializeField] Toggle cameraShake;
        [SerializeField] Toggle headBob;
        [SerializeField] Toggle weaponBob;
        [SerializeField] Toggle motionEffects;
        [SerializeField] Toggle speedometer;
        [SerializeField] Toggle timerToggle;
        [SerializeField] Toggle debugOverlay;
        [SerializeField] Button resetButton;
        [SerializeField] Button backButton;

        List<Vector2Int> resolutions = new List<Vector2Int>();
        bool dirtyDisplay;

        public Button BackButton => backButton;
        public event Action Closed;

        static SettingsData S => SettingsManager.Settings;

        void Awake()
        {
            Hook(sensitivity, v => { S.mouseSensitivity = v; SetLabel(sensitivityValue, v.ToString("0.00", CultureInfo.InvariantCulture)); });
            Hook(fov, v => { S.fieldOfView = v; SetLabel(fovValue, Mathf.RoundToInt(v).ToString()); });
            Hook(masterVolume, v => { S.masterVolume = v / 100f; SetLabel(masterValue, Mathf.RoundToInt(v) + "%"); });
            Hook(musicVolume, v => { S.musicVolume = v / 100f; SetLabel(musicValue, Mathf.RoundToInt(v) + "%"); });
            Hook(sfxVolume, v => { S.sfxVolume = v / 100f; SetLabel(sfxValue, Mathf.RoundToInt(v) + "%"); });
            Hook(invertY, v => S.invertY = v);
            Hook(sprint, v => S.sprintEnabled = v);
            Hook(fullscreen, v => { S.fullscreen = v; dirtyDisplay = true; });
            Hook(vsync, v => S.vsync = v);
            Hook(cameraShake, v => S.cameraShake = v);
            Hook(headBob, v => S.headBob = v);
            Hook(weaponBob, v => S.weaponBob = v);
            Hook(motionEffects, v => S.motionEffects = v);
            Hook(speedometer, v => S.showSpeedometer = v);
            Hook(timerToggle, v => S.showTimer = v);
            Hook(debugOverlay, v => S.showDebugOverlay = v);

            if (resolution != null)
            {
                resolution.Changed += i =>
                {
                    if (i >= 0 && i < resolutions.Count)
                    {
                        S.resolutionWidth = resolutions[i].x;
                        S.resolutionHeight = resolutions[i].y;
                        dirtyDisplay = true;
                        Apply();
                    }
                };
            }
            if (fpsLimit != null)
            {
                fpsLimit.Changed += i =>
                {
                    S.fpsLimit = SettingsManager.FpsOptions[Mathf.Clamp(i, 0, SettingsManager.FpsOptions.Length - 1)];
                    Apply();
                };
            }
            if (resetButton != null) resetButton.onClick.AddListener(() =>
            {
                if (SettingsManager.Instance != null) SettingsManager.Instance.ResetToDefaults();
                Refresh();
            });
            if (backButton != null)
            {
                backButton.onClick.AddListener(Close);
                var sounds = backButton.GetComponent<UIButtonSounds>();
                if (sounds != null) sounds.SetBackButton(true);
            }
        }

        void OnEnable()
        {
            Refresh();
        }

        void OnDisable()
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.Save();
        }

        void Hook(Slider slider, Action<float> onChange)
        {
            if (slider == null) return;
            slider.onValueChanged.AddListener(v =>
            {
                onChange(v);
                Apply();
            });
        }

        void Hook(Toggle toggle, Action<bool> onChange)
        {
            if (toggle == null) return;
            toggle.onValueChanged.AddListener(v =>
            {
                onChange(v);
                Apply();
            });
        }

        void Apply()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.Apply(dirtyDisplay);
                dirtyDisplay = false;
            }
            else
            {
                GameEvents.RaiseSettingsChanged();
            }
        }

        static void SetLabel(Text t, string value)
        {
            if (t != null) t.text = value;
        }

        /// <summary>Loads current settings into the controls without triggering change events.</summary>
        public void Refresh()
        {
            var s = S;
            SetSlider(sensitivity, s.mouseSensitivity, sensitivityValue, s.mouseSensitivity.ToString("0.00", CultureInfo.InvariantCulture));
            SetSlider(fov, s.fieldOfView, fovValue, Mathf.RoundToInt(s.fieldOfView).ToString());
            SetSlider(masterVolume, s.masterVolume * 100f, masterValue, Mathf.RoundToInt(s.masterVolume * 100f) + "%");
            SetSlider(musicVolume, s.musicVolume * 100f, musicValue, Mathf.RoundToInt(s.musicVolume * 100f) + "%");
            SetSlider(sfxVolume, s.sfxVolume * 100f, sfxValue, Mathf.RoundToInt(s.sfxVolume * 100f) + "%");
            SetToggle(invertY, s.invertY);
            SetToggle(sprint, s.sprintEnabled);
            SetToggle(fullscreen, s.fullscreen);
            SetToggle(vsync, s.vsync);
            SetToggle(cameraShake, s.cameraShake);
            SetToggle(headBob, s.headBob);
            SetToggle(weaponBob, s.weaponBob);
            SetToggle(motionEffects, s.motionEffects);
            SetToggle(speedometer, s.showSpeedometer);
            SetToggle(timerToggle, s.showTimer);
            SetToggle(debugOverlay, s.showDebugOverlay);

            if (resolution != null)
            {
                resolutions = SettingsManager.GetResolutionOptions();
                var labels = new string[resolutions.Count];
                int selected = resolutions.Count - 1;
                for (int i = 0; i < resolutions.Count; i++)
                {
                    labels[i] = resolutions[i].x + " x " + resolutions[i].y;
                    if (resolutions[i].x == s.resolutionWidth && resolutions[i].y == s.resolutionHeight) selected = i;
                }
                resolution.SetOptions(labels, selected);
            }
            if (fpsLimit != null)
            {
                var options = SettingsManager.FpsOptions;
                var labels = new string[options.Length];
                int selected = 0;
                for (int i = 0; i < options.Length; i++)
                {
                    labels[i] = options[i] <= 0 ? "UNLIMITED" : options[i].ToString();
                    if (options[i] == s.fpsLimit) selected = i;
                }
                fpsLimit.SetOptions(labels, selected);
            }
        }

        static void SetSlider(Slider slider, float value, Text label, string text)
        {
            if (slider != null) slider.SetValueWithoutNotify(value);
            SetLabel(label, text);
        }

        static void SetToggle(Toggle toggle, bool value)
        {
            if (toggle != null) toggle.SetIsOnWithoutNotify(value);
        }

        public void Close()
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.Save();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        // ------------------------------------------------------------------ Builder

        /// <summary>Builds the settings panel as a child of parent (stretched to fill it).</summary>
        public static SettingsPanelView Build(Transform parent)
        {
            var panel = UIFactory.CreatePanel("SettingsPanel", parent, UITheme.Panel);
            UIFactory.Stretch(panel.rectTransform);
            var view = panel.gameObject.AddComponent<SettingsPanelView>();

            var title = UIFactory.CreateText("Title", panel.rectTransform, "SETTINGS", UITheme.HeaderSize, TextAnchor.UpperLeft, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(600f, 56f));

            var scroll = UIFactory.CreateScrollView("Scroll", panel.rectTransform, out RectTransform content, 6f);
            UIFactory.Stretch((RectTransform)scroll.transform, 40f, 40f, 100f, 100f);

            Section(content, "CONTROLS");
            view.sensitivity = SliderRow(content, "Mouse Sensitivity", 0.1f, 10f, false, out view.sensitivityValue);
            view.invertY = ToggleRow(content, "Invert Y");
            view.sprint = ToggleRow(content, "Sprint (Left Shift)");

            Section(content, "VIDEO");
            view.fov = SliderRow(content, "Field of View", 60f, 120f, true, out view.fovValue);
            view.fullscreen = ToggleRow(content, "Fullscreen");
            view.resolution = CyclerRow(content, "Resolution");
            view.vsync = ToggleRow(content, "VSync");
            view.fpsLimit = CyclerRow(content, "FPS Limit");

            Section(content, "AUDIO");
            view.masterVolume = SliderRow(content, "Master Volume", 0f, 100f, true, out view.masterValue);
            view.musicVolume = SliderRow(content, "Music Volume", 0f, 100f, true, out view.musicValue);
            view.sfxVolume = SliderRow(content, "SFX Volume", 0f, 100f, true, out view.sfxValue);

            Section(content, "EFFECTS & COMFORT");
            view.cameraShake = ToggleRow(content, "Camera Shake");
            view.headBob = ToggleRow(content, "Head Bob");
            view.weaponBob = ToggleRow(content, "Weapon Bob");
            view.motionEffects = ToggleRow(content, "Motion Effects (FOV kick, tilt, landing)");

            Section(content, "HUD");
            view.speedometer = ToggleRow(content, "Show Speedometer");
            view.timerToggle = ToggleRow(content, "Show Timer");
            view.debugOverlay = ToggleRow(content, "Movement Debug Overlay (F3)");

            var buttons = UIFactory.CreateRect("Buttons", panel.rectTransform);
            buttons.anchorMin = new Vector2(0f, 0f);
            buttons.anchorMax = new Vector2(1f, 0f);
            buttons.pivot = new Vector2(0.5f, 0f);
            buttons.anchoredPosition = new Vector2(0f, 24f);
            buttons.sizeDelta = new Vector2(-80f, 56f);
            UIFactory.AddHorizontal(buttons.gameObject, 16f, null, TextAnchor.MiddleRight);
            view.resetButton = UIFactory.CreateButton("Reset", buttons, "RESET DEFAULTS", 24);
            UIFactory.Size(view.resetButton, 280f, 56f);
            view.backButton = UIFactory.CreateButton("Back", buttons, "BACK", 26);
            UIFactory.Size(view.backButton, 220f, 56f);
            return view;
        }

        static void Section(RectTransform content, string label)
        {
            var t = UIFactory.CreateText("Section_" + label, content, label, 24, TextAnchor.LowerLeft, UITheme.Accent, FontStyle.Bold);
            UIFactory.Size(t, -1f, 46f);
        }

        static RectTransform Row(RectTransform content, string label)
        {
            var row = UIFactory.CreateRect("Row_" + label, content);
            UIFactory.Size(row, -1f, 48f);
            UIFactory.AddHorizontal(row.gameObject, 12f, new RectOffset(8, 8, 2, 2), TextAnchor.MiddleLeft);
            var t = UIFactory.CreateText("Label", row, label, UITheme.BodySize, TextAnchor.MiddleLeft, UITheme.Text);
            UIFactory.Size(t, 520f, 44f);
            return row;
        }

        static Slider SliderRow(RectTransform content, string label, float min, float max, bool whole, out Text valueText)
        {
            var row = Row(content, label);
            var slider = UIFactory.CreateSlider("Slider", row, min, max, whole);
            UIFactory.Size(slider, 420f, 36f);
            valueText = UIFactory.CreateText("Value", row, "", UITheme.BodySize, TextAnchor.MiddleLeft, UITheme.TextDim, FontStyle.Bold);
            UIFactory.Size(valueText, 120f, 44f);
            return slider;
        }

        static Toggle ToggleRow(RectTransform content, string label)
        {
            var row = Row(content, label);
            var toggle = UIFactory.CreateToggle("Toggle", row);
            UIFactory.Size(toggle, 40f, 40f);
            return toggle;
        }

        static ValueCycler CyclerRow(RectTransform content, string label)
        {
            var row = Row(content, label);
            var cycler = ValueCycler.Build(row, "Cycler");
            UIFactory.Size(cycler, 420f, 40f);
            return cycler;
        }
    }
}
