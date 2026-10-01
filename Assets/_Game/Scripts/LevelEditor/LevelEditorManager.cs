using System;
using System.Collections.Generic;
using Momentum.Audio;
using Momentum.Levels;
using Momentum.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Momentum.LevelEditor
{
    public enum EditorMode
    {
        Editing,
        Testing
    }

    /// <summary>
    /// In-game level editor core: owns the LevelData being edited, the inert preview instances, selection,
    /// placement, gizmo drags, copy/paste, undo/redo, save/load (JSON in persistentDataPath/Levels) and
    /// play-testing (builds a playable copy, spawns the player, returns to editing on F5 / Exit Test).
    /// </summary>
    public class LevelEditorManager : MonoBehaviour
    {
        public static LevelEditorManager Instance { get; private set; }

        public static readonly float[] GridSizes = { 0.25f, 0.5f, 1f, 2f, 4f };
        public static readonly float[] RotationSteps = { 5f, 15f, 45f, 90f };

        [SerializeField] Camera editorCamera;
        [SerializeField] EditorCameraController cameraController;
        [SerializeField] LevelManager levelManager;
        [SerializeField] LevelLoader playLoader;
        [SerializeField] UIManager uiManager;
        [SerializeField] Light sun;

        struct DragOriginal
        {
            public int id;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
        }

        readonly Dictionary<int, EditorObject> instances = new Dictionary<int, EditorObject>();
        readonly List<int> selection = new List<int>();
        readonly List<EditorObject> selectedObjects = new List<EditorObject>();
        readonly List<LevelObjectData> clipboard = new List<LevelObjectData>();
        readonly List<DragOriginal> dragOriginals = new List<DragOriginal>();
        readonly EditorHistory history = new EditorHistory(150);

        Transform editRoot;
        Transform ghostRoot;
        GameObject ghost;
        string ghostType;
        TransformGizmo gizmo;
        SelectionHighlighter highlighter;
        EditorGrid grid;
        LevelEditorUI ui;
        bool dragChanged;
        bool gizmoVisible;
        float placementYaw;

        public LevelData Data { get; private set; }
        public string CurrentPath { get; private set; }
        public bool Dirty { get; private set; }
        public EditorMode Mode { get; private set; } = EditorMode.Editing;
        public GizmoMode GizmoMode => gizmo != null ? gizmo.Mode : GizmoMode.Move;
        public bool GridSnap { get; private set; } = true;
        public float GridSize { get; private set; } = 1f;
        public bool RotationSnap { get; private set; } = true;
        public float RotationStep { get; private set; } = 15f;
        public string PlacementType { get; private set; }
        public IReadOnlyList<int> Selection => selection;
        public int SelectionCount => selection.Count;
        public bool CanUndo => history.CanUndo;
        public bool CanRedo => history.CanRedo;
        public Camera EditorCamera => editorCamera;

        public event Action SelectionChanged;
        public event Action LevelChanged;
        public event Action ModeChanged;
        public event Action ToolChanged;
        public event Action<string, bool> StatusChanged;

        public LevelObjectData PrimarySelection
        {
            get
            {
                if (selection.Count == 0) return null;
                return instances.TryGetValue(selection[0], out var eo) ? eo.Data : null;
            }
        }

        // ------------------------------------------------------------------ Lifecycle

        void Awake()
        {
            Instance = this;
            editRoot = new GameObject("EditRoot").transform;
            ghostRoot = new GameObject("PlacementGhost").transform;
            gizmo = new GameObject("TransformGizmo").AddComponent<TransformGizmo>();
            highlighter = new GameObject("SelectionHighlighter").AddComponent<SelectionHighlighter>();
            grid = new GameObject("EditorGrid").AddComponent<EditorGrid>();
            if (editorCamera == null) editorCamera = Camera.main;
            if (cameraController == null && editorCamera != null) cameraController = editorCamera.GetComponent<EditorCameraController>();
            if (cameraController == null && editorCamera != null) cameraController = editorCamera.gameObject.AddComponent<EditorCameraController>();
            if (playLoader == null) playLoader = gameObject.AddComponent<LevelLoader>();
        }

        void Start()
        {
            UIFactory.EnsureEventSystem();
            gizmo.Initialize(this, editorCamera);
            if (editorCamera != null) grid.SetFollowTarget(editorCamera.transform);
            ui = LevelEditorUI.Create(this, transform);

            if (uiManager != null) uiManager.ExitTestRequested += StopTest;
            if (levelManager != null) levelManager.RestartRequested += RestartTest;

            string path = GameManager.Instance != null ? GameManager.Instance.PendingEditorLevelPath : null;
            if (GameManager.Instance != null) GameManager.Instance.PendingEditorLevelPath = null;
            if (string.IsNullOrEmpty(path) || !Load(path)) NewLevel();

            if (cameraController != null) cameraController.SetPose(new Vector3(0f, 18f, -28f), 0f, 32f);
            AudioManager.PlayMusic(MusicTrack.Editor);
            GameManager.SetCursorLocked(false);
            SetStatus("Welcome to the level editor. Press H for help.", false);
        }

        void OnDestroy()
        {
            if (uiManager != null) uiManager.ExitTestRequested -= StopTest;
            if (levelManager != null) levelManager.RestartRequested -= RestartTest;
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ Update

        void Update()
        {
            if (Mode == EditorMode.Testing)
            {
                if (Input.GetKeyDown(KeyCode.F5)) StopTest();
                return;
            }

            bool typing = IsTyping();
            bool modal = ui != null && ui.IsModalOpen;
            if (cameraController != null) cameraController.InputBlocked = typing || modal;
            if (modal) return;

            if (!typing && (cameraController == null || !cameraController.IsFlying)) HandleShortcuts();
            HandleMouse();
        }

        void LateUpdate()
        {
            if (Mode != EditorMode.Editing) return;
            highlighter.Draw(selectedObjects);
            bool show = selection.Count > 0 && PlacementType == null;
            if (show != gizmoVisible)
            {
                gizmoVisible = show;
                gizmo.SetVisible(show);
            }
            if (show)
            {
                var primary = selectedObjects.Count > 0 ? selectedObjects[0] : null;
                gizmo.UpdatePlacement(SelectionPivot(), primary != null ? primary.transform.rotation : Quaternion.identity);
            }
        }

        static bool IsTyping()
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null) return false;
            var input = es.currentSelectedGameObject.GetComponent<InputField>();
            return input != null && input.isFocused;
        }

        void HandleShortcuts()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (ctrl)
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    if (shift) Redo();
                    else Undo();
                }
                if (Input.GetKeyDown(KeyCode.Y)) Redo();
                if (Input.GetKeyDown(KeyCode.C)) CopySelection();
                if (Input.GetKeyDown(KeyCode.V)) Paste();
                if (Input.GetKeyDown(KeyCode.D)) DuplicateSelection();
                if (Input.GetKeyDown(KeyCode.S)) Save();
                if (Input.GetKeyDown(KeyCode.A)) SelectAll();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) DeleteSelection();
            if (Input.GetKeyDown(KeyCode.F5)) StartTest();
            if (Input.GetKeyDown(KeyCode.G)) SetGridSnap(!GridSnap);
            if (Input.GetKeyDown(KeyCode.F)) FocusSelection();
            if (Input.GetKeyDown(KeyCode.H) && ui != null) ui.ToggleHelp();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (PlacementType != null) CancelPlacement();
                else ClearSelection();
            }

            if (PlacementType != null)
            {
                if (Input.GetKeyDown(KeyCode.R)) placementYaw = Mathf.Repeat(placementYaw + 90f, 360f);
                if (Input.GetKeyDown(KeyCode.Q)) placementYaw = Mathf.Repeat(placementYaw - 90f, 360f);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.W)) SetGizmoMode(GizmoMode.Move);
                if (Input.GetKeyDown(KeyCode.E)) SetGizmoMode(GizmoMode.Rotate);
                if (Input.GetKeyDown(KeyCode.R)) SetGizmoMode(GizmoMode.Scale);
            }

            float step = GridSnap ? GridSize : 0.25f;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) NudgeSelection(Vector3.left * step);
            if (Input.GetKeyDown(KeyCode.RightArrow)) NudgeSelection(Vector3.right * step);
            if (Input.GetKeyDown(KeyCode.UpArrow)) NudgeSelection(Vector3.forward * step);
            if (Input.GetKeyDown(KeyCode.DownArrow)) NudgeSelection(Vector3.back * step);
            if (Input.GetKeyDown(KeyCode.PageUp)) NudgeSelection(Vector3.up * step);
            if (Input.GetKeyDown(KeyCode.PageDown)) NudgeSelection(Vector3.down * step);
        }

        void HandleMouse()
        {
            if (editorCamera == null) return;
            if (cameraController != null && cameraController.IsFlying)
            {
                if (ghost != null) ghost.SetActive(false);
                return;
            }
            Ray ray = editorCamera.ScreenPointToRay(Input.mousePosition);
            bool overUI = TransformGizmo.PointerOverUI();

            if (PlacementType != null)
            {
                UpdateGhost(ray, overUI);
                if (!overUI && Input.GetMouseButtonDown(0) && ghost != null && ghost.activeSelf)
                {
                    PlaceObject(PlacementType, ghost.transform.position, placementYaw);
                }
                return;
            }

            if (gizmo.ProcessInput(ray, overUI)) return;

            if (!overUI && Input.GetMouseButtonDown(0))
            {
                bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
                                Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                var hit = RaycastEditObject(ray);
                if (hit != null)
                {
                    if (additive) ToggleSelect(hit.Id);
                    else SelectOnly(hit.Id);
                }
                else if (!additive)
                {
                    ClearSelection();
                }
            }
        }

        // ------------------------------------------------------------------ Picking & placement

        EditorObject RaycastEditObject(Ray ray)
        {
            int mask = Layers.EditorSelectMask & ~(1 << Layers.EditorGizmo);
            var hits = Physics.RaycastAll(ray, 3000f, mask, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            EditorObject fallback = null;
            foreach (var h in hits)
            {
                var eo = h.collider.GetComponentInParent<EditorObject>();
                if (eo == null || !eo.transform.IsChildOf(editRoot)) continue;
                if (h.collider.isTrigger)
                {
                    Vector3 size = h.collider.bounds.size;
                    if (size.x * size.y * size.z > 3000f)
                    {
                        if (fallback == null) fallback = eo;
                        continue;
                    }
                }
                return eo;
            }
            return fallback;
        }

        bool PlacementPoint(Ray ray, LevelObjectDef def, out Vector3 position)
        {
            position = Vector3.zero;
            Vector3 point;
            Vector3 normal;
            int mask = Layers.EditorSelectMask & ~(1 << Layers.EditorGizmo);
            RaycastHit hit = default;
            bool found = false;
            foreach (var h in Physics.RaycastAll(ray, 3000f, mask, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(ghostRoot)) continue;
                if (h.collider.GetComponentInParent<EditorObject>() == null) continue;
                if (!found || h.distance < hit.distance)
                {
                    hit = h;
                    found = true;
                }
            }
            if (found)
            {
                point = hit.point;
                normal = hit.normal;
            }
            else
            {
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (!plane.Raycast(ray, out float enter) || enter > 3000f) return false;
                point = ray.GetPoint(enter);
                normal = Vector3.up;
            }

            Vector3 scale = def.scalable ? def.defaultScale : Vector3.one;
            Vector3 offset;
            if (def.centerPivot)
            {
                float half = (Mathf.Abs(normal.x) * scale.x + Mathf.Abs(normal.y) * scale.y + Mathf.Abs(normal.z) * scale.z) * 0.5f;
                offset = normal * half;
            }
            else
            {
                offset = normal.y > 0.5f ? Vector3.zero : normal * 0.5f;
            }
            position = point + offset;
            if (GridSnap)
            {
                position.x = MathUtil.SnapTo(position.x, GridSize);
                position.z = MathUtil.SnapTo(position.z, GridSize);
                if (normal.y < 0.5f) position.y = MathUtil.SnapTo(position.y, GridSize * 0.5f);
            }
            position.y = Mathf.Round(position.y * 100f) / 100f;
            return true;
        }

        void UpdateGhost(Ray ray, bool overUI)
        {
            EnsureGhost();
            if (ghost == null) return;
            var def = LevelObjectCatalog.Get(PlacementType);
            Vector3 pos = Vector3.zero;
            bool ok = !overUI && def != null && PlacementPoint(ray, def, out pos);
            if (ghost.activeSelf != ok) ghost.SetActive(ok);
            if (!ok) return;
            ghost.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, placementYaw, 0f));
        }

        void EnsureGhost()
        {
            if (ghost != null && ghostType == PlacementType) return;
            DestroyGhost();
            if (PlacementType == null) return;
            var data = LevelObjectCatalog.CreateData(PlacementType, Vector3.zero);
            data.id = -1;
            ghost = LevelObjectFactory.Create(data, ghostRoot, LevelBuildMode.Edit);
            ghostType = PlacementType;
            if (ghost == null) return;
            foreach (var c in ghost.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            var lib = MaterialLibrary.Instance;
            var ghostMat = lib != null ? lib.editorGhost : null;
            if (ghostMat != null)
            {
                foreach (var r in ghost.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = ghostMat;
                    r.sharedMaterials = mats;
                }
            }
        }

        void DestroyGhost()
        {
            if (ghost != null)
            {
                ghost.SetActive(false);
                Destroy(ghost);
            }
            ghost = null;
            ghostType = null;
        }

        public void BeginPlacement(string type)
        {
            if (LevelObjectCatalog.Get(type) == null) return;
            if (PlacementType == type)
            {
                CancelPlacement();
                return;
            }
            PlacementType = type;
            placementYaw = 0f;
            ClearSelection();
            SetStatus("Placing " + LevelObjectCatalog.Get(type).displayName + ": click to place, Q/R rotate, Esc to stop.", false);
            ToolChanged?.Invoke();
        }

        public void CancelPlacement()
        {
            PlacementType = null;
            DestroyGhost();
            ToolChanged?.Invoke();
        }

        // ------------------------------------------------------------------ Level lifecycle

        public void NewLevel()
        {
            Data = CreateDefaultLevel();
            CurrentPath = null;
            history.Clear();
            selection.Clear();
            RebuildAll();
            Dirty = false;
            LevelChanged?.Invoke();
            SelectionChanged?.Invoke();
            SetStatus("New level created.", false);
        }

        static LevelData CreateDefaultLevel()
        {
            var b = new LevelBuilder("", "New Level");
            b.Data.author = "Player";
            b.Environment("Day").KillHeight(-30f).Ranks(20f, 30f, 45f, 70f).StartingWeapons("pistol");
            b.FloorTop(0f, 0f, 0f, 40f, 40f, 1f, "LightGray");
            b.Spawn(new Vector3(0f, 0f, -15f), 0f);
            b.StartGate(new Vector3(0f, 2f, -11f), 0f, new Vector3(8f, 4f, 1f));
            b.Finish(new Vector3(0f, 0f, 15f), 0f, false);
            return b.Data;
        }

        public bool Load(string path)
        {
            var data = CustomLevelStorage.Load(path);
            if (data == null)
            {
                SetStatus("Could not load " + path, true);
                return false;
            }
            Data = data;
            CurrentPath = path;
            history.Clear();
            selection.Clear();
            RebuildAll();
            Dirty = false;
            LevelChanged?.Invoke();
            SelectionChanged?.Invoke();
            SetStatus("Loaded \"" + data.levelName + "\"", false);
            return true;
        }

        public void Save()
        {
            if (Data == null) return;
            if (string.IsNullOrWhiteSpace(Data.levelName)) Data.levelName = "Untitled";
            string path = CustomLevelStorage.Save(Data, CurrentPath);
            if (path == null)
            {
                SetStatus("Save failed!", true);
                return;
            }
            CurrentPath = path;
            Dirty = false;
            LevelChanged?.Invoke();
            SetStatus("Saved to " + path, false);
            AudioManager.PlayUI(SoundId.Checkpoint, 0.5f);
        }

        public void ExitToMenu()
        {
            if (Mode == EditorMode.Testing) StopTest();
            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu(MenuReturnTarget.CustomMaps);
        }

        void RebuildAll()
        {
            DestroyChildren(editRoot);
            instances.Clear();
            foreach (var obj in Data.objects) CreateInstance(obj);
            RefreshSelectionCache();
            EnvironmentApplier.Apply(Data.environment, sun);
        }

        static void DestroyChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        EditorObject CreateInstance(LevelObjectData data)
        {
            var go = LevelObjectFactory.Create(data, editRoot, LevelBuildMode.Edit);
            if (go == null) return null;
            var eo = go.AddComponent<EditorObject>();
            eo.Init(data);
            EnsureSelectable(go);
            instances[data.id] = eo;
            return eo;
        }

        static void EnsureSelectable(GameObject go)
        {
            if (go.GetComponentInChildren<Collider>(true) != null) return;
            var renderers = go.GetComponentsInChildren<Renderer>();
            Bounds b = new Bounds(go.transform.position, Vector3.one);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (i == 0) b = renderers[i].bounds;
                else b.Encapsulate(renderers[i].bounds);
            }
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Vector3 s = go.transform.lossyScale;
            box.center = go.transform.InverseTransformPoint(b.center);
            box.size = new Vector3(b.size.x / Mathf.Max(0.01f, s.x), b.size.y / Mathf.Max(0.01f, s.y), b.size.z / Mathf.Max(0.01f, s.z));
        }

        void RebuildInstance(int id)
        {
            if (instances.TryGetValue(id, out var eo) && eo != null)
            {
                eo.gameObject.SetActive(false);
                Destroy(eo.gameObject);
            }
            instances.Remove(id);
            var data = Data.Find(id);
            if (data != null) CreateInstance(data);
            RefreshSelectionCache();
        }

        void MarkDirty()
        {
            Dirty = true;
        }

        void BeginChange()
        {
            history.Record(Data);
            MarkDirty();
        }

        // ------------------------------------------------------------------ Selection

        public void SelectOnly(int id)
        {
            selection.Clear();
            if (instances.ContainsKey(id)) selection.Add(id);
            RefreshSelectionCache();
            SelectionChanged?.Invoke();
        }

        public void ToggleSelect(int id)
        {
            if (selection.Contains(id)) selection.Remove(id);
            else if (instances.ContainsKey(id)) selection.Add(id);
            RefreshSelectionCache();
            SelectionChanged?.Invoke();
        }

        public void ClearSelection()
        {
            if (selection.Count == 0) return;
            selection.Clear();
            RefreshSelectionCache();
            SelectionChanged?.Invoke();
        }

        public void SelectAll()
        {
            selection.Clear();
            foreach (var obj in Data.objects) selection.Add(obj.id);
            RefreshSelectionCache();
            SelectionChanged?.Invoke();
        }

        void RefreshSelectionCache()
        {
            selection.RemoveAll(id => !instances.ContainsKey(id) || instances[id] == null);
            selectedObjects.Clear();
            foreach (var id in selection) selectedObjects.Add(instances[id]);
        }

        Vector3 SelectionPivot()
        {
            if (selectedObjects.Count == 0) return Vector3.zero;
            if (selectedObjects.Count == 1) return selectedObjects[0].transform.position;
            Vector3 sum = Vector3.zero;
            foreach (var eo in selectedObjects) sum += eo.transform.position;
            return sum / selectedObjects.Count;
        }

        public void FocusSelection()
        {
            if (cameraController == null) return;
            if (selectedObjects.Count == 0)
            {
                cameraController.SetPose(new Vector3(0f, 18f, -28f), 0f, 32f);
                return;
            }
            Bounds b = selectedObjects[0].GetBounds();
            foreach (var eo in selectedObjects) b.Encapsulate(eo.GetBounds());
            cameraController.Focus(b);
        }

        // ------------------------------------------------------------------ Commands

        public void PlaceObject(string type, Vector3 position, float yaw)
        {
            var def = LevelObjectCatalog.Get(type);
            if (def == null) return;
            BeginChange();
            if (def.maxCount > 0)
            {
                var existing = Data.objects.FindAll(o => string.Equals(o.objectType, type, StringComparison.OrdinalIgnoreCase));
                while (existing.Count >= def.maxCount && existing.Count > 0)
                {
                    RemoveObject(existing[0].id);
                    existing.RemoveAt(0);
                }
            }
            var data = LevelObjectCatalog.CreateData(type, position);
            data.rotation = new Vector3(0f, yaw, 0f);
            data.id = Data.NextId();
            if (type == "Checkpoint")
            {
                int maxOrder = 0;
                foreach (var o in Data.objects)
                {
                    if (o.objectType == "Checkpoint") maxOrder = Mathf.Max(maxOrder, o.GetInt("order", 0));
                }
                data.Set("order", maxOrder + 1);
            }
            Data.objects.Add(data);
            CreateInstance(data);
            selection.Clear();
            selection.Add(data.id);
            RefreshSelectionCache();
            SelectionChanged?.Invoke();
            LevelChanged?.Invoke();
            AudioManager.PlayUI(SoundId.UIClick, 0.6f);
        }

        void RemoveObject(int id)
        {
            Data.objects.RemoveAll(o => o.id == id);
            if (instances.TryGetValue(id, out var eo) && eo != null)
            {
                eo.gameObject.SetActive(false);
                Destroy(eo.gameObject);
            }
            instances.Remove(id);
            selection.Remove(id);
        }

        public void DeleteSelection()
        {
            if (selection.Count == 0) return;
            BeginChange();
            foreach (var id in new List<int>(selection)) RemoveObject(id);
            selection.Clear();
            RefreshSelectionCache();
            SelectionChanged?.Invoke();
            LevelChanged?.Invoke();
            SetStatus("Deleted.", false);
        }

        public void DuplicateSelection()
        {
            if (selection.Count == 0) return;
            BeginChange();
            var created = new List<int>();
            Vector3 offset = new Vector3(GridSnap ? GridSize : 1f, 0f, 0f);
            foreach (var id in selection)
            {
                var src = Data.Find(id);
                if (src == null) continue;
                var copy = src.Clone();
                copy.id = Data.NextId();
                copy.position += offset;
                Data.objects.Add(copy);
                CreateInstance(copy);
                created.Add(copy.id);
            }
            selection.Clear();
            selection.AddRange(created);
            RefreshSelectionCache();
            SelectionChanged?.Invoke();
            LevelChanged?.Invoke();
            SetStatus("Duplicated " + created.Count + " object(s).", false);
        }

        public void CopySelection()
        {
            if (selection.Count == 0) return;
            clipboard.Clear();
            foreach (var id in selection)
            {
                var src = Data.Find(id);
                if (src != null) clipboard.Add(src.Clone());
            }
            SetStatus("Copied " + clipboard.Count + " object(s).", false);
        }

        public void Paste()
        {
            if (clipboard.Count == 0) return;
            BeginChange();
            Vector3 centroid = Vector3.zero;
            foreach (var c in clipboard) centroid += c.position;
            centroid /= clipboard.Count;
            Vector3 offset = new Vector3(GridSize * 2f, 0f, GridSize * 2f);
            if (editorCamera != null && PlacementPoint(editorCamera.ScreenPointToRay(Input.mousePosition), LevelObjectCatalog.Get("SpawnPoint"), out Vector3 target))
            {
                offset = target - centroid;
                offset.y = 0f;
                if (GridSnap)
                {
                    offset.x = MathUtil.SnapTo(offset.x, GridSize);
                    offset.z = MathUtil.SnapTo(offset.z, GridSize);
                }
            }
            var created = new List<int>();
            foreach (var c in clipboard)
            {
                var copy = c.Clone();
                copy.id = Data.NextId();
                copy.position += offset;
                Data.objects.Add(copy);
                CreateInstance(copy);
                created.Add(copy.id);
            }
            selection.Clear();
            selection.AddRange(created);
            RefreshSelectionCache();
            SelectionChanged?.Invoke();
            LevelChanged?.Invoke();
            SetStatus("Pasted " + created.Count + " object(s).", false);
        }

        public void NudgeSelection(Vector3 delta)
        {
            if (selection.Count == 0) return;
            BeginChange();
            foreach (var eo in selectedObjects)
            {
                eo.Data.position += delta;
                eo.SyncTransform();
            }
            LevelChanged?.Invoke();
        }

        public void Undo()
        {
            if (!history.CanUndo)
            {
                SetStatus("Nothing to undo.", false);
                return;
            }
            RestoreSnapshot(history.Undo(Data.ToJson(false)));
            SetStatus("Undo", false);
        }

        public void Redo()
        {
            if (!history.CanRedo)
            {
                SetStatus("Nothing to redo.", false);
                return;
            }
            RestoreSnapshot(history.Redo(Data.ToJson(false)));
            SetStatus("Redo", false);
        }

        void RestoreSnapshot(string json)
        {
            var data = LevelData.FromJson(json);
            if (data == null) return;
            var keep = new List<int>(selection);
            Data = data;
            RebuildAll();
            selection.Clear();
            foreach (var id in keep)
            {
                if (instances.ContainsKey(id)) selection.Add(id);
            }
            RefreshSelectionCache();
            MarkDirty();
            SelectionChanged?.Invoke();
            LevelChanged?.Invoke();
        }

        // ------------------------------------------------------------------ Property editing (UI)

        public void SetTransform(int id, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            var data = Data.Find(id);
            if (data == null) return;
            BeginChange();
            data.position = position;
            data.rotation = rotation;
            var def = LevelObjectCatalog.Get(data.objectType);
            if (def != null && def.scalable) data.scale = new Vector3(Mathf.Max(0.05f, scale.x), Mathf.Max(0.05f, scale.y), Mathf.Max(0.05f, scale.z));
            if (instances.TryGetValue(id, out var eo)) eo.SyncTransform();
            LevelChanged?.Invoke();
        }

        public void SetProperty(int id, string key, string value)
        {
            var data = Data.Find(id);
            if (data == null) return;
            if (data.GetString(key, null) == value) return;
            BeginChange();
            data.Set(key, value);
            RebuildInstance(id);
            LevelChanged?.Invoke();
        }

        /// <summary>Applies a change to level-wide settings with undo.</summary>
        public void ModifyLevel(Action<LevelData> change, bool environmentChanged)
        {
            if (change == null) return;
            BeginChange();
            change(Data);
            if (environmentChanged) EnvironmentApplier.Apply(Data.environment, sun);
            LevelChanged?.Invoke();
        }

        // ------------------------------------------------------------------ Tools

        public void SetGizmoMode(GizmoMode mode)
        {
            if (PlacementType != null) CancelPlacement();
            gizmo.SetMode(mode);
            ToolChanged?.Invoke();
        }

        public void SetGridSnap(bool value)
        {
            GridSnap = value;
            ToolChanged?.Invoke();
            SetStatus("Grid snap " + (value ? "ON" : "OFF"), false);
        }

        public void CycleGridSize()
        {
            int i = Array.IndexOf(GridSizes, GridSize);
            GridSize = GridSizes[(i + 1) % GridSizes.Length];
            grid.SetCellSize(GridSize);
            ToolChanged?.Invoke();
        }

        public void SetRotationSnap(bool value)
        {
            RotationSnap = value;
            ToolChanged?.Invoke();
        }

        public void CycleRotationStep()
        {
            int i = Array.IndexOf(RotationSteps, RotationStep);
            RotationStep = RotationSteps[(i + 1) % RotationSteps.Length];
            ToolChanged?.Invoke();
        }

        // ------------------------------------------------------------------ Gizmo drag API

        public void BeginTransformDrag()
        {
            history.Record(Data);
            dragChanged = false;
            dragOriginals.Clear();
            foreach (var eo in selectedObjects)
            {
                dragOriginals.Add(new DragOriginal { id = eo.Id, position = eo.Data.position, rotation = eo.Data.rotation, scale = eo.Data.scale });
            }
        }

        public void DragMove(Vector3 delta)
        {
            if (dragOriginals.Count == 0) return;
            Vector3 p0 = dragOriginals[0].position;
            Vector3 snapped = delta;
            if (GridSnap)
            {
                if (Mathf.Abs(delta.x) > 0.0001f) snapped.x = MathUtil.SnapTo(p0.x + delta.x, GridSize) - p0.x;
                if (Mathf.Abs(delta.y) > 0.0001f) snapped.y = MathUtil.SnapTo(p0.y + delta.y, GridSize * 0.5f) - p0.y;
                if (Mathf.Abs(delta.z) > 0.0001f) snapped.z = MathUtil.SnapTo(p0.z + delta.z, GridSize) - p0.z;
            }
            foreach (var o in dragOriginals)
            {
                if (!instances.TryGetValue(o.id, out var eo)) continue;
                eo.Data.position = o.position + snapped;
                eo.SyncTransform();
            }
            if (snapped.sqrMagnitude > 0.000001f) dragChanged = true;
        }

        public void DragRotate(Vector3 axis, float angle, Vector3 pivot)
        {
            if (RotationSnap) angle = MathUtil.SnapTo(angle, RotationStep);
            var q = Quaternion.AngleAxis(angle, axis);
            foreach (var o in dragOriginals)
            {
                if (!instances.TryGetValue(o.id, out var eo)) continue;
                var rot = q * Quaternion.Euler(o.rotation);
                Vector3 e = rot.eulerAngles;
                eo.Data.rotation = new Vector3(Mathf.Round(e.x * 100f) / 100f, Mathf.Round(e.y * 100f) / 100f, Mathf.Round(e.z * 100f) / 100f);
                eo.Data.position = dragOriginals.Count > 1 ? pivot + q * (o.position - pivot) : o.position;
                eo.SyncTransform();
            }
            if (Mathf.Abs(angle) > 0.001f) dragChanged = true;
        }

        public void DragScale(int axis, float factor)
        {
            foreach (var o in dragOriginals)
            {
                if (!instances.TryGetValue(o.id, out var eo)) continue;
                if (eo.Definition == null || !eo.Definition.scalable) continue;
                Vector3 s = o.scale;
                if (axis < 0) s *= factor;
                else s[axis] *= factor;
                if (GridSnap)
                {
                    float step = 0.25f;
                    s = new Vector3(Mathf.Max(step, MathUtil.SnapTo(s.x, step)), Mathf.Max(step, MathUtil.SnapTo(s.y, step)), Mathf.Max(step, MathUtil.SnapTo(s.z, step)));
                }
                s = new Vector3(Mathf.Max(0.05f, s.x), Mathf.Max(0.05f, s.y), Mathf.Max(0.05f, s.z));
                eo.Data.scale = s;
                eo.SyncTransform();
                if ((s - o.scale).sqrMagnitude > 0.000001f) dragChanged = true;
            }
        }

        public void EndTransformDrag()
        {
            if (!dragChanged) history.DiscardLast();
            else
            {
                MarkDirty();
                // Rebuild so components depending on transform (e.g. door panels, hazard triggers) refresh.
                foreach (var o in dragOriginals) RebuildInstance(o.id);
                LevelChanged?.Invoke();
            }
            dragOriginals.Clear();
        }

        // ------------------------------------------------------------------ Testing

        public void StartTest()
        {
            if (Mode == EditorMode.Testing) return;
            if (Data.Count("SpawnPoint") == 0)
            {
                SetStatus("Add a Spawn Point (Gameplay category) before testing.", true);
                return;
            }
            if (levelManager == null || playLoader == null || uiManager == null)
            {
                SetStatus("Editor scene is missing LevelManager / LevelLoader / UIManager. Run the setup tool.", true);
                return;
            }
            CancelPlacement();
            ClearSelection();
            Mode = EditorMode.Testing;
            editRoot.gameObject.SetActive(false);
            ghostRoot.gameObject.SetActive(false);
            gizmo.SetVisible(false);
            gizmoVisible = false;
            highlighter.Draw(null);
            grid.gameObject.SetActive(false);
            if (ui != null) ui.SetVisible(false);
            if (editorCamera != null) editorCamera.gameObject.SetActive(false);

            var testData = Data.DeepClone();
            var root = playLoader.Build(testData, LevelBuildMode.Play, "PlayRoot");
            uiManager.SetGameplayActive(true);
            levelManager.BeginEditorTest(testData, root);
            ModeChanged?.Invoke();
            if (Data.Count("FinishTrigger") == 0) GameEvents.RaiseNotification("NO FINISH IN THIS LEVEL - PRESS F5 TO STOP", Color.yellow);
            else GameEvents.RaiseNotification("TEST MODE - F5 TO RETURN TO EDITOR", UITheme.Secondary);
        }

        public void StopTest()
        {
            if (Mode != EditorMode.Testing) return;
            levelManager.EndEditorTest();
            playLoader.Clear();
            uiManager.SetGameplayActive(false);
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            Mode = EditorMode.Editing;
            editRoot.gameObject.SetActive(true);
            ghostRoot.gameObject.SetActive(true);
            grid.gameObject.SetActive(true);
            if (editorCamera != null) editorCamera.gameObject.SetActive(true);
            if (ui != null) ui.SetVisible(true);
            GameManager.SetCursorLocked(false);
            AudioManager.PlayMusic(MusicTrack.Editor);
            EnvironmentApplier.Apply(Data.environment, sun);
            ModeChanged?.Invoke();
            SetStatus("Back to editing.", false);
        }

        void RestartTest()
        {
            StopTest();
            StartTest();
        }

        public void SetStatus(string message, bool error)
        {
            StatusChanged?.Invoke(message, error);
            if (error) AudioManager.PlayUI(SoundId.UIBack, 0.8f);
        }
    }
}
