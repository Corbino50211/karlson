using System;
using System.Collections.Generic;
using System.Globalization;
using Momentum.Levels;
using Momentum.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Momentum.LevelEditor
{
    /// <summary>
    /// Level editor interface. Top: file/test/undo/tool toolbar. Left: object browser by category.
    /// Right: properties of the selection (position/rotation/scale + type-specific settings) or level
    /// settings when nothing is selected. Bottom: status/hints. Plus load dialog, confirm dialog and help.
    /// </summary>
    public class LevelEditorUI : MonoBehaviour
    {
        [SerializeField] Button newButton;
        [SerializeField] Button saveButton;
        [SerializeField] Button loadButton;
        [SerializeField] Button testButton;
        [SerializeField] Button undoButton;
        [SerializeField] Button redoButton;
        [SerializeField] Button moveButton;
        [SerializeField] Button rotateButton;
        [SerializeField] Button scaleButton;
        [SerializeField] Button snapButton;
        [SerializeField] Button gridButton;
        [SerializeField] Button rotStepButton;
        [SerializeField] Button helpButton;
        [SerializeField] Button menuButton;
        [SerializeField] InputField nameField;
        [SerializeField] RectTransform browserContent;
        [SerializeField] RectTransform propertiesContent;
        [SerializeField] Text statusText;
        [SerializeField] LevelBrowserDialog loadDialog;
        [SerializeField] ConfirmDialog confirmDialog;
        [SerializeField] GameObject helpPanel;

        const string Hint = "RMB + WASD/QE fly  |  LMB select (Shift = multi)  |  W/E/R move/rotate/scale  |  Ctrl+Z/Y undo/redo  |  Ctrl+C/V/D  |  Del delete  |  F focus  |  F5 test  |  H help";

        LevelEditorManager manager;
        readonly Dictionary<string, Button> browserButtons = new Dictionary<string, Button>();
        float statusTimer;
        bool bound;

        public bool IsModalOpen =>
            (loadDialog != null && loadDialog.IsOpen) ||
            (confirmDialog != null && confirmDialog.gameObject.activeSelf) ||
            (helpPanel != null && helpPanel.activeSelf);

        public static LevelEditorUI Create(LevelEditorManager manager, Transform parent)
        {
            LevelEditorUI ui = null;
            var registry = PrefabRegistry.Instance;
            if (registry != null && registry.levelEditorUI != null)
            {
                var go = Instantiate(registry.levelEditorUI, parent);
                ui = go.GetComponent<LevelEditorUI>();
                if (ui == null) Destroy(go);
            }
            if (ui == null) ui = Build(parent);
            ui.Bind(manager);
            return ui;
        }

        void OnDestroy()
        {
            if (manager == null) return;
            manager.SelectionChanged -= RebuildProperties;
            manager.LevelChanged -= OnLevelChanged;
            manager.ToolChanged -= RefreshToolbar;
            manager.ModeChanged -= RefreshToolbar;
            manager.StatusChanged -= ShowStatus;
        }

        void Bind(LevelEditorManager m)
        {
            if (bound) return;
            bound = true;
            manager = m;

            newButton.onClick.AddListener(() => ConfirmIfDirty("Discard unsaved changes and start a new level?", manager.NewLevel));
            saveButton.onClick.AddListener(manager.Save);
            loadButton.onClick.AddListener(() => ConfirmIfDirty("Discard unsaved changes and load another level?", () => loadDialog.Open(path => manager.Load(path), confirmDialog)));
            testButton.onClick.AddListener(manager.StartTest);
            undoButton.onClick.AddListener(manager.Undo);
            redoButton.onClick.AddListener(manager.Redo);
            moveButton.onClick.AddListener(() => manager.SetGizmoMode(GizmoMode.Move));
            rotateButton.onClick.AddListener(() => manager.SetGizmoMode(GizmoMode.Rotate));
            scaleButton.onClick.AddListener(() => manager.SetGizmoMode(GizmoMode.Scale));
            snapButton.onClick.AddListener(() => manager.SetGridSnap(!manager.GridSnap));
            gridButton.onClick.AddListener(manager.CycleGridSize);
            rotStepButton.onClick.AddListener(manager.CycleRotationStep);
            helpButton.onClick.AddListener(ToggleHelp);
            menuButton.onClick.AddListener(() => ConfirmIfDirty("Leave the editor? Unsaved changes will be lost.", manager.ExitToMenu));
            nameField.onEndEdit.AddListener(value =>
            {
                if (manager.Data == null || value == manager.Data.levelName) return;
                manager.ModifyLevel(d => d.levelName = string.IsNullOrWhiteSpace(value) ? "Untitled" : value.Trim(), false);
            });
            if (helpPanel != null)
            {
                var close = helpPanel.GetComponentInChildren<Button>(true);
                if (close != null) close.onClick.AddListener(ToggleHelp);
                helpPanel.SetActive(false);
            }

            manager.SelectionChanged += RebuildProperties;
            manager.LevelChanged += OnLevelChanged;
            manager.ToolChanged += RefreshToolbar;
            manager.ModeChanged += RefreshToolbar;
            manager.StatusChanged += ShowStatus;

            BuildBrowser();
            RefreshToolbar();
            RebuildProperties();
            ShowStatus(Hint, false);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void ToggleHelp()
        {
            if (helpPanel != null) helpPanel.SetActive(!helpPanel.activeSelf);
        }

        void ConfirmIfDirty(string message, Action action)
        {
            if (manager.Dirty && confirmDialog != null) confirmDialog.Show(message, action, null, "CONTINUE", "CANCEL");
            else action();
        }

        void Update()
        {
            if (statusTimer > 0f)
            {
                statusTimer -= Time.unscaledDeltaTime;
                if (statusTimer <= 0f && statusText != null)
                {
                    statusText.text = Hint;
                    statusText.color = UITheme.TextDim;
                }
            }
        }

        void ShowStatus(string message, bool error)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = error ? UITheme.Danger : UITheme.Text;
            statusTimer = message == Hint ? 0f : 4f;
        }

        void OnLevelChanged()
        {
            RefreshToolbar();
            RebuildProperties();
        }

        // ------------------------------------------------------------------ Toolbar

        void RefreshToolbar()
        {
            if (manager == null) return;
            SetActiveColor(moveButton, manager.GizmoMode == GizmoMode.Move && manager.PlacementType == null);
            SetActiveColor(rotateButton, manager.GizmoMode == GizmoMode.Rotate && manager.PlacementType == null);
            SetActiveColor(scaleButton, manager.GizmoMode == GizmoMode.Scale && manager.PlacementType == null);
            SetActiveColor(snapButton, manager.GridSnap);
            SetLabel(snapButton, manager.GridSnap ? "SNAP ON" : "SNAP OFF");
            SetLabel(gridButton, "GRID " + manager.GridSize.ToString("0.##", CultureInfo.InvariantCulture));
            SetLabel(rotStepButton, "ROT " + manager.RotationStep.ToString("0", CultureInfo.InvariantCulture));
            undoButton.interactable = manager.CanUndo;
            redoButton.interactable = manager.CanRedo;
            SetLabel(saveButton, manager.Dirty ? "SAVE*" : "SAVE");
            if (manager.Data != null && !nameField.isFocused) nameField.SetTextWithoutNotify(manager.Data.levelName);
            foreach (var pair in browserButtons) SetActiveColor(pair.Value, pair.Key == manager.PlacementType);
        }

        static void SetActiveColor(Button b, bool active)
        {
            if (b == null) return;
            b.colors = UIFactory.ButtonColors(active ? UITheme.Accent : UITheme.Button, active ? UITheme.Accent : Color.Lerp(UITheme.Button, UITheme.Accent, 0.6f));
        }

        static void SetLabel(Button b, string text)
        {
            var label = UIFactory.GetLabel(b);
            if (label != null) label.text = text;
        }

        // ------------------------------------------------------------------ Object browser

        void BuildBrowser()
        {
            Clear(browserContent);
            browserButtons.Clear();
            foreach (var category in LevelObjectCatalog.Categories)
            {
                var header = UIFactory.CreateText("Header_" + category, browserContent, category.ToUpperInvariant(), 20, TextAnchor.LowerLeft, UITheme.Accent, FontStyle.Bold);
                UIFactory.Size(header, -1f, 34f);
                foreach (var def in LevelObjectCatalog.InCategory(category))
                {
                    var button = UIFactory.CreateButton(def.type, browserContent, def.displayName, 20, TextAnchor.MiddleLeft);
                    UIFactory.Size(button, -1f, 40f);
                    string type = def.type;
                    button.onClick.AddListener(() => manager.BeginPlacement(type));
                    browserButtons[type] = button;
                }
            }
        }

        // ------------------------------------------------------------------ Properties

        void RebuildProperties()
        {
            if (manager == null || propertiesContent == null) return;
            Clear(propertiesContent);
            var selection = manager.Selection;
            if (selection.Count == 0) BuildLevelSettings();
            else if (selection.Count == 1 && manager.PrimarySelection != null) BuildObjectProperties(manager.PrimarySelection);
            else BuildMultiSelection(selection.Count);
            RefreshToolbar();
        }

        void BuildLevelSettings()
        {
            var data = manager.Data;
            if (data == null) return;
            Header("LEVEL SETTINGS");
            StringRow("Name", data.levelName, v => manager.ModifyLevel(d => d.levelName = string.IsNullOrWhiteSpace(v) ? "Untitled" : v.Trim(), false));
            StringRow("Author", data.author, v => manager.ModifyLevel(d => d.author = v, false));
            StringRow("Description", data.description, v => manager.ModifyLevel(d => d.description = v, false));
            FloatRow("Kill Height", data.killHeight, v => manager.ModifyLevel(d => d.killHeight = v, false));

            Header("RANK TIMES (SECONDS)");
            FloatRow("S rank", data.rankTimes.s, v => manager.ModifyLevel(d => d.rankTimes.s = Mathf.Max(1f, v), false));
            FloatRow("A rank", data.rankTimes.a, v => manager.ModifyLevel(d => d.rankTimes.a = Mathf.Max(1f, v), false));
            FloatRow("B rank", data.rankTimes.b, v => manager.ModifyLevel(d => d.rankTimes.b = Mathf.Max(1f, v), false));
            FloatRow("C rank", data.rankTimes.c, v => manager.ModifyLevel(d => d.rankTimes.c = Mathf.Max(1f, v), false));

            Header("ENVIRONMENT");
            ChoiceRow("Lighting", EnvironmentApplier.PresetNames, data.environment.preset, v => manager.ModifyLevel(d => d.environment = EnvironmentApplier.Preset(v), true));
            ChoiceRow("Music", new[] { "Level", "Intense", "Boss", "Menu" }, data.music, v => manager.ModifyLevel(d => d.music = v, false));

            Header("STARTING WEAPONS");
            foreach (var id in LevelObjectCatalog.WeaponIds)
            {
                string weaponId = id;
                BoolRow(id.ToUpperInvariant(), data.startingWeapons.Contains(id), on => manager.ModifyLevel(d =>
                {
                    d.startingWeapons.Remove(weaponId);
                    if (on) d.startingWeapons.Add(weaponId);
                }, false));
            }

            Header("INFO");
            Info("Objects: " + data.objects.Count);
            Info("Spawn points: " + data.Count("SpawnPoint") + "   Finish: " + data.Count("FinishTrigger") + "   Checkpoints: " + data.Count("Checkpoint"));
            Info(string.IsNullOrEmpty(manager.CurrentPath) ? "Not saved yet" : manager.CurrentPath);
        }

        void BuildObjectProperties(LevelObjectData data)
        {
            var def = LevelObjectCatalog.Get(data.objectType);
            Header(def != null ? def.displayName.ToUpperInvariant() : data.objectType);
            if (def != null && !string.IsNullOrEmpty(def.description)) Info(def.description);
            int id = data.id;

            Vector3Row("Position", data.position, v =>
            {
                var d = manager.Data.Find(id);
                if (d != null) manager.SetTransform(id, v, d.rotation, d.scale);
            });
            Vector3Row("Rotation", data.rotation, v =>
            {
                var d = manager.Data.Find(id);
                if (d != null) manager.SetTransform(id, d.position, v, d.scale);
            });
            if (def == null || def.scalable)
            {
                Vector3Row("Scale", data.scale, v =>
                {
                    var d = manager.Data.Find(id);
                    if (d != null) manager.SetTransform(id, d.position, d.rotation, v);
                });
            }

            if (def != null && def.properties.Count > 0)
            {
                Header("PROPERTIES");
                foreach (var p in def.properties)
                {
                    string key = p.key;
                    string current = data.GetString(key, p.defaultValue);
                    switch (p.kind)
                    {
                        case PropertyKind.Bool:
                            BoolRow(p.label, data.GetBool(key, current == "true"), v => manager.SetProperty(id, key, v ? "true" : "false"));
                            break;
                        case PropertyKind.Choice:
                            ChoiceRow(p.label, p.options, current, v => manager.SetProperty(id, key, v));
                            break;
                        case PropertyKind.Vector3:
                            Vector3Row(p.label, LevelObjectData.ParseVector3(current, Vector3.zero), v => manager.SetProperty(id, key, LevelObjectData.FormatVector3(v)));
                            break;
                        case PropertyKind.Float:
                        case PropertyKind.Int:
                            {
                                var def2 = p;
                                FloatRow(p.label, data.GetFloat(key, 0f), v =>
                                {
                                    v = Mathf.Clamp(v, def2.min, def2.max);
                                    string s = def2.kind == PropertyKind.Int ? Mathf.RoundToInt(v).ToString(CultureInfo.InvariantCulture) : v.ToString("0.###", CultureInfo.InvariantCulture);
                                    manager.SetProperty(id, key, s);
                                });
                            }
                            break;
                        default:
                            StringRow(p.label, current, v => manager.SetProperty(id, key, v));
                            break;
                    }
                }
            }

            Header("ACTIONS");
            ButtonRow(("DUPLICATE", manager.DuplicateSelection), ("FOCUS", manager.FocusSelection), ("DELETE", manager.DeleteSelection));
            Info("Object id: " + id);
        }

        void BuildMultiSelection(int count)
        {
            Header(count + " OBJECTS SELECTED");
            Info("Use the gizmo to move/rotate/scale them together.");
            ButtonRow(("DUPLICATE", manager.DuplicateSelection), ("DELETE", manager.DeleteSelection));
        }

        // ------------------------------------------------------------------ Row helpers

        void Header(string text)
        {
            var t = UIFactory.CreateText("Header", propertiesContent, text, 22, TextAnchor.LowerLeft, UITheme.Accent, FontStyle.Bold);
            UIFactory.Size(t, -1f, 40f);
        }

        void Info(string text)
        {
            var t = UIFactory.CreateText("Info", propertiesContent, text, 17, TextAnchor.UpperLeft, UITheme.TextDim);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.Size(t, -1f, 46f);
        }

        RectTransform Row(string label)
        {
            var row = UIFactory.CreateRect("Row_" + label, propertiesContent);
            UIFactory.Size(row, -1f, 40f);
            UIFactory.AddHorizontal(row.gameObject, 6f, new RectOffset(2, 2, 2, 2), TextAnchor.MiddleLeft);
            var t = UIFactory.CreateText("Label", row, label, 18, TextAnchor.MiddleLeft, UITheme.Text);
            UIFactory.Size(t, 120f, 36f);
            return row;
        }

        InputField Field(RectTransform row, string value, float width = -1f)
        {
            var f = UIFactory.CreateInputField("Field", row, "", 18);
            f.SetTextWithoutNotify(value);
            if (width > 0f) UIFactory.Size(f, width, 36f);
            else UIFactory.Size(f, -1f, 36f, 1f);
            return f;
        }

        void StringRow(string label, string value, Action<string> commit)
        {
            var row = Row(label);
            var f = Field(row, value ?? "");
            f.onEndEdit.AddListener(v => commit(v));
        }

        void FloatRow(string label, float value, Action<float> commit)
        {
            var row = Row(label);
            var f = Field(row, value.ToString("0.###", CultureInfo.InvariantCulture));
            f.contentType = InputField.ContentType.DecimalNumber;
            f.onEndEdit.AddListener(v =>
            {
                if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) && Mathf.Abs(parsed - value) > 0.0001f) commit(parsed);
            });
        }

        void BoolRow(string label, bool value, Action<bool> commit)
        {
            var row = Row(label);
            var toggle = UIFactory.CreateToggle("Toggle", row);
            UIFactory.Size(toggle, 34f, 34f);
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(v => commit(v));
        }

        void ChoiceRow(string label, string[] options, string current, Action<string> commit)
        {
            var row = Row(label);
            var cycler = ValueCycler.Build(row, "Choice");
            UIFactory.Size(cycler, -1f, 36f, 1f);
            int index = Math.Max(0, Array.FindIndex(options, o => string.Equals(o, current, StringComparison.OrdinalIgnoreCase)));
            cycler.SetOptions(options, index);
            cycler.Changed += i => commit(options[i]);
        }

        void Vector3Row(string label, Vector3 value, Action<Vector3> commit)
        {
            var row = Row(label);
            var fx = Field(row, value.x.ToString("0.##", CultureInfo.InvariantCulture));
            var fy = Field(row, value.y.ToString("0.##", CultureInfo.InvariantCulture));
            var fz = Field(row, value.z.ToString("0.##", CultureInfo.InvariantCulture));
            foreach (var f in new[] { fx, fy, fz }) f.contentType = InputField.ContentType.DecimalNumber;
            void Commit(string _)
            {
                var v = new Vector3(Parse(fx.text, value.x), Parse(fy.text, value.y), Parse(fz.text, value.z));
                if ((v - value).sqrMagnitude > 0.000001f) commit(v);
            }
            fx.onEndEdit.AddListener(Commit);
            fy.onEndEdit.AddListener(Commit);
            fz.onEndEdit.AddListener(Commit);
        }

        void ButtonRow(params (string label, Action action)[] buttons)
        {
            var row = UIFactory.CreateRect("Buttons", propertiesContent);
            UIFactory.Size(row, -1f, 44f);
            UIFactory.AddHorizontal(row.gameObject, 6f, null, TextAnchor.MiddleLeft, true);
            foreach (var (label, action) in buttons)
            {
                var b = UIFactory.CreateButton(label, row, label, 18);
                var a = action;
                b.onClick.AddListener(() => a());
                if (label == "DELETE") b.colors = UIFactory.ButtonColors(UITheme.Button, UITheme.Danger);
            }
        }

        static float Parse(string s, float fallback)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
        }

        static void Clear(RectTransform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        // ------------------------------------------------------------------ Builder

        public static LevelEditorUI Build(Transform parent)
        {
            var canvas = UIFactory.CreateCanvas("LevelEditorUI", 20, parent);
            var ui = canvas.gameObject.AddComponent<LevelEditorUI>();
            var root = canvas.transform;

            // Toolbar
            var toolbar = UIFactory.CreatePanel("Toolbar", root, UITheme.Background);
            toolbar.rectTransform.anchorMin = new Vector2(0f, 1f);
            toolbar.rectTransform.anchorMax = new Vector2(1f, 1f);
            toolbar.rectTransform.pivot = new Vector2(0.5f, 1f);
            toolbar.rectTransform.sizeDelta = new Vector2(0f, 60f);
            toolbar.rectTransform.anchoredPosition = Vector2.zero;
            UIFactory.AddHorizontal(toolbar.gameObject, 6f, new RectOffset(10, 10, 8, 8), TextAnchor.MiddleLeft);
            ui.newButton = ToolButton(toolbar.rectTransform, "NEW", 80f);
            ui.saveButton = ToolButton(toolbar.rectTransform, "SAVE", 90f);
            ui.loadButton = ToolButton(toolbar.rectTransform, "LOAD", 80f);
            ui.testButton = ToolButton(toolbar.rectTransform, "TEST (F5)", 130f);
            ui.testButton.colors = UIFactory.ButtonColors(new Color(0.12f, 0.35f, 0.18f, 1f), UITheme.Good);
            Spacer(toolbar.rectTransform, 10f);
            ui.undoButton = ToolButton(toolbar.rectTransform, "UNDO", 80f);
            ui.redoButton = ToolButton(toolbar.rectTransform, "REDO", 80f);
            Spacer(toolbar.rectTransform, 10f);
            ui.moveButton = ToolButton(toolbar.rectTransform, "MOVE", 84f);
            ui.rotateButton = ToolButton(toolbar.rectTransform, "ROTATE", 96f);
            ui.scaleButton = ToolButton(toolbar.rectTransform, "SCALE", 84f);
            Spacer(toolbar.rectTransform, 10f);
            ui.snapButton = ToolButton(toolbar.rectTransform, "SNAP ON", 110f);
            ui.gridButton = ToolButton(toolbar.rectTransform, "GRID 1", 100f);
            ui.rotStepButton = ToolButton(toolbar.rectTransform, "ROT 15", 96f);
            Spacer(toolbar.rectTransform, 10f);
            ui.nameField = UIFactory.CreateInputField("LevelName", toolbar.rectTransform, "Level name", 20);
            UIFactory.Size(ui.nameField, 260f, 44f, 1f);
            ui.helpButton = ToolButton(toolbar.rectTransform, "HELP", 80f);
            ui.menuButton = ToolButton(toolbar.rectTransform, "MENU", 86f);

            // Left: object browser
            var left = UIFactory.CreatePanel("Browser", root, UITheme.Panel);
            left.rectTransform.anchorMin = new Vector2(0f, 0f);
            left.rectTransform.anchorMax = new Vector2(0f, 1f);
            left.rectTransform.pivot = new Vector2(0f, 0.5f);
            left.rectTransform.offsetMin = new Vector2(0f, 40f);
            left.rectTransform.offsetMax = new Vector2(280f, -60f);
            var leftTitle = UIFactory.CreateText("Title", left.rectTransform, "OBJECTS", 24, TextAnchor.UpperLeft, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(leftTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(240f, 32f));
            var leftScroll = UIFactory.CreateScrollView("List", left.rectTransform, out RectTransform browserContent, 4f);
            UIFactory.Stretch((RectTransform)leftScroll.transform, 10f, 6f, 48f, 10f);
            ui.browserContent = browserContent;

            // Right: properties
            var right = UIFactory.CreatePanel("Properties", root, UITheme.Panel);
            right.rectTransform.anchorMin = new Vector2(1f, 0f);
            right.rectTransform.anchorMax = new Vector2(1f, 1f);
            right.rectTransform.pivot = new Vector2(1f, 0.5f);
            right.rectTransform.offsetMin = new Vector2(-400f, 40f);
            right.rectTransform.offsetMax = new Vector2(0f, -60f);
            var rightTitle = UIFactory.CreateText("Title", right.rectTransform, "PROPERTIES", 24, TextAnchor.UpperLeft, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(rightTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(360f, 32f));
            var rightScroll = UIFactory.CreateScrollView("Props", right.rectTransform, out RectTransform propsContent, 4f);
            UIFactory.Stretch((RectTransform)rightScroll.transform, 12f, 6f, 48f, 10f);
            ui.propertiesContent = propsContent;

            // Bottom: status
            var status = UIFactory.CreatePanel("Status", root, UITheme.Background);
            status.rectTransform.anchorMin = new Vector2(0f, 0f);
            status.rectTransform.anchorMax = new Vector2(1f, 0f);
            status.rectTransform.pivot = new Vector2(0.5f, 0f);
            status.rectTransform.sizeDelta = new Vector2(0f, 40f);
            status.rectTransform.anchoredPosition = Vector2.zero;
            ui.statusText = UIFactory.CreateText("Text", status.rectTransform, Hint, 18, TextAnchor.MiddleLeft, UITheme.TextDim);
            UIFactory.Stretch(ui.statusText.rectTransform, 16f, 16f, 0f, 0f);

            // Dialogs
            ui.loadDialog = LevelBrowserDialog.Build(root);
            ui.confirmDialog = ConfirmDialog.Build(root);
            ui.helpPanel = BuildHelp(root);
            return ui;
        }

        static Button ToolButton(RectTransform parent, string label, float width)
        {
            var b = UIFactory.CreateButton(label, parent, label, 18);
            UIFactory.Size(b, width, 44f);
            return b;
        }

        static void Spacer(RectTransform parent, float width)
        {
            var s = UIFactory.CreateRect("Spacer", parent);
            UIFactory.Size(s, width, 10f);
        }

        static GameObject BuildHelp(Transform root)
        {
            var overlay = UIFactory.CreatePanel("Help", root, UITheme.Overlay);
            UIFactory.Stretch(overlay.rectTransform);
            var panel = UIFactory.CreatePanel("Panel", overlay.rectTransform, UITheme.Background);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 760f));
            var text = UIFactory.CreateText("Text", panel.rectTransform,
                "<b>LEVEL EDITOR CONTROLS</b>\n\n" +
                "Hold Right Mouse + WASD / Q E    Fly camera (Shift = fast, wheel = speed)\n" +
                "Middle Mouse drag                Pan        Mouse wheel   Zoom\n" +
                "Left Click                       Select     Shift/Ctrl + Click   Multi-select\n" +
                "W / E / R                        Move / Rotate / Scale gizmo\n" +
                "G                                Toggle grid snap (GRID / ROT buttons change step)\n" +
                "Arrow keys / PgUp / PgDn         Nudge selection\n" +
                "Ctrl+Z / Ctrl+Y                  Undo / Redo\n" +
                "Ctrl+C / Ctrl+V / Ctrl+D         Copy / Paste (at mouse) / Duplicate\n" +
                "Delete                           Delete selection\n" +
                "F                                Focus selection\n" +
                "Ctrl+S                           Save\n" +
                "F5                               Test the level / return to editor\n\n" +
                "<b>PLACING OBJECTS</b>\nPick an object on the left, click in the world to place it.\nQ / R rotate the preview, Esc stops placing.\n\n" +
                "<b>A PLAYABLE LEVEL NEEDS</b>\nA Spawn Point, ideally a Start Gate, Checkpoints and a Finish.\n" +
                "Levels are saved as JSON in the game's Levels folder.",
                22, TextAnchor.UpperLeft, UITheme.Text);
            UIFactory.Stretch(text.rectTransform, 40f, 40f, 30f, 90f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            var close = UIFactory.CreateButton("Close", panel.rectTransform, "CLOSE", 24);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(200f, 56f));
            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }
    }
}
