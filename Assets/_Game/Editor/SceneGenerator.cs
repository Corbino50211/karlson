using System.Collections.Generic;
using Momentum.LevelEditor;
using Momentum.Levels;
using Momentum.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Generates the menu scene, the level editor scene, the custom level scene and (through
    /// DemoLevelGenerator) the campaign scenes, configures realtime lighting and writes Build Settings.
    /// Scenes are regenerated from scratch, so rerunning never duplicates objects.
    /// </summary>
    public static class SceneGenerator
    {
        public const string MainMenuSceneName = "MainMenu";
        public const string LevelEditorSceneName = "LevelEditor";
        public const string CustomLevelSceneName = "CustomLevel";

        // ------------------------------------------------------------------ Shared helpers

        public static LightingSettings LightingAsset()
        {
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(GamePaths.LightingSettingsAsset);
            if (ls == null)
            {
                EditorUtil.EnsureFolder(GamePaths.Settings);
                ls = new LightingSettings { name = "RealtimeLighting" };
                AssetDatabase.CreateAsset(ls, GamePaths.LightingSettingsAsset);
            }
            ls.bakedGI = false;
            ls.realtimeGI = false;
            // Disable automatic lighting generation (GIWorkflowMode.OnDemand = 1).
            var so = new SerializedObject(ls);
            var workflow = so.FindProperty("m_GIWorkflowMode");
            if (workflow != null)
            {
                workflow.intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(ls);
            return ls;
        }

        /// <summary>Opens a fresh empty scene with realtime-only lighting.</summary>
        public static Scene NewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Lightmapping.lightingSettings = LightingAsset();
            return scene;
        }

        public static void Save(Scene scene, string path)
        {
            EditorUtil.EnsureFolder(System.IO.Path.GetDirectoryName(path));
            if (!EditorSceneManager.SaveScene(scene, path)) Debug.LogError("[Momentum Setup] Could not save scene " + path);
        }

        public static Light CreateSun()
        {
            var go = new GameObject("Sun");
            var sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.shadowBias = 0.04f;
            sun.shadowNormalBias = 0.3f;
            RenderSettings.sun = sun;
            return sun;
        }

        public static void ApplyEnvironment(LevelEnvironmentSettings env, Light sun)
        {
            EnvironmentApplier.Apply(env, sun);
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        }

        public static UIManager CreateUI(bool editorTestMode)
        {
            var go = new GameObject("[UI]");
            var ui = go.AddComponent<UIManager>();
            EditorUtil.Set(ui, "editorTestMode", editorTestMode);
            return ui;
        }

        /// <summary>Creates the run controller object (LevelManager + timer + checkpoints + NavMesh baker).</summary>
        public static LevelManager CreateLevelManager(LevelMode mode, bool autoBegin, out NavMeshBaker baker)
        {
            var go = new GameObject("[Level Manager]");
            go.AddComponent<LevelTimer>();
            go.AddComponent<CheckpointManager>();
            baker = go.AddComponent<NavMeshBaker>();
            var lm = go.AddComponent<LevelManager>();
            EditorUtil.Set(lm, "mode", (int)mode);
            EditorUtil.Set(lm, "autoBegin", autoBegin);
            EditorUtil.Set(lm, "navMeshBaker", baker);
            return lm;
        }

        /// <summary>Instantiates level data as prefab instances (so scenes stay linked to the generated prefabs).</summary>
        public static Transform BuildLevelObjects(LevelData data, string rootName)
        {
            var root = new GameObject(rootName).transform;
            LevelObjectFactory.InstantiateOverride = (prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            try
            {
                data.Sanitize();
                foreach (var obj in data.objects)
                {
                    var go = LevelObjectFactory.Create(obj, root, LevelBuildMode.Play);
                    if (go != null) RecordInstanceChanges(go);
                }
            }
            finally
            {
                LevelObjectFactory.InstantiateOverride = null;
            }
            return root;
        }

        /// <summary>Marks script-made changes on prefab instances as overrides so they are saved with the scene.</summary>
        static void RecordInstanceChanges(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) continue;
                PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c != null) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                }
            }
        }

        // ------------------------------------------------------------------ Main menu

        public static void GenerateMainMenu()
        {
            var scene = NewScene();
            var sun = CreateSun();
            ApplyEnvironment(EnvironmentApplier.Preset("Dusk"), sun);

            BuildLevelObjects(MenuDiorama(), "MenuDiorama");

            var camGo = new GameObject("MenuCamera");
            camGo.tag = Tags.MainCamera;
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1500f;
            cam.cullingMask = ~((1 << Layers.ViewModel) | (1 << Layers.EditorGizmo));
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 14f, -38f);
            camGo.transform.LookAt(new Vector3(0f, 6f, 0f));
            var orbit = camGo.AddComponent<MenuCameraOrbit>();
            EditorUtil.Set(orbit, "center", new Vector3(0f, 5f, 0f));
            EditorUtil.Set(orbit, "radius", 40f);
            EditorUtil.Set(orbit, "height", 13f);
            EditorUtil.Set(orbit, "speed", 4f);

            new GameObject("[Main Menu]").AddComponent<MainMenuController>();
            Save(scene, GamePaths.MainMenuScene);
        }

        /// <summary>Small original set piece shown behind the main menu.</summary>
        static LevelData MenuDiorama()
        {
            var b = new LevelBuilder("menu", "Menu Diorama");
            b.FloorTop(0f, 0f, 0f, 64f, 64f, 1f, "LightGray");
            b.Box(new Vector3(-14f, 3f, 6f), new Vector3(6f, 6f, 6f), "White");
            b.Box(new Vector3(-14f, 7f, 6f), new Vector3(3f, 2f, 3f), "Accent");
            b.Box(new Vector3(16f, 5f, -6f), new Vector3(4f, 10f, 4f), "White");
            b.Box(new Vector3(8f, 1f, 14f), new Vector3(10f, 2f, 3f), "Gray");
            b.Ramp(new Vector3(-2f, 0f, -12f), 0f, new Vector3(6f, 3f, 8f), "Accent");
            b.FloorTop(-2f, 3f, -4f, 6f, 8f, 3f, "White");
            b.Wall(new Vector3(4f, 5f, 4f), new Vector3(0.6f, 10f, 16f), "AccentBlue", 20f);
            b.Pillar(new Vector3(20f, 0f, 16f), 2f, 14f, "White");
            b.Pillar(new Vector3(-22f, 0f, -16f), 2f, 9f, "Gray");
            b.Pillar(new Vector3(-8f, 0f, 20f), 1.5f, 11f, "White");
            b.Platform(new Vector3(-4f, 9f, 18f), new Vector3(8f, 0.4f, 4f), "Gray");
            b.MovingPlatform(new Vector3(10f, 6f, -14f), new Vector3(4f, 0.4f, 4f), new Vector3(0f, 6f, 0f), 2f, 1.5f);
            b.GrapplePoint(new Vector3(0f, 14f, 6f));
            b.GrapplePoint(new Vector3(-10f, 12f, -8f));
            b.LaunchPad(new Vector3(8f, 0f, 2f), 0f);
            b.SpeedPad(new Vector3(-6f, 0f, -24f), 90f);
            b.Light(new Vector3(-14f, 9f, 6f), new Color(1f, 0.6f, 0.3f), 18f, 2f, true);
            b.Light(new Vector3(16f, 11f, -6f), new Color(0.4f, 0.8f, 1f), 18f, 2f, true);
            return b.Data;
        }

        // ------------------------------------------------------------------ Level editor & custom level scenes

        public static void GenerateLevelEditor()
        {
            var scene = NewScene();
            var sun = CreateSun();
            ApplyEnvironment(EnvironmentApplier.Preset("Day"), sun);

            var camGo = new GameObject("EditorCamera");
            camGo.tag = Tags.MainCamera;
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 3000f;
            cam.cullingMask = ~(1 << Layers.ViewModel);
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 18f, -28f);
            camGo.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
            var controller = camGo.AddComponent<EditorCameraController>();

            var lm = CreateLevelManager(LevelMode.EditorTest, false, out _);
            var ui = CreateUI(true);

            var editorGo = new GameObject("[Level Editor]");
            var loader = editorGo.AddComponent<LevelLoader>();
            EditorUtil.Set(loader, "sun", sun);
            var manager = editorGo.AddComponent<LevelEditorManager>();
            EditorUtil.Set(manager, "editorCamera", cam);
            EditorUtil.Set(manager, "cameraController", controller);
            EditorUtil.Set(manager, "levelManager", lm);
            EditorUtil.Set(manager, "playLoader", loader);
            EditorUtil.Set(manager, "uiManager", ui);
            EditorUtil.Set(manager, "sun", sun);
            Save(scene, GamePaths.LevelEditorScene);
        }

        public static void GenerateCustomLevel()
        {
            var scene = NewScene();
            var sun = CreateSun();
            ApplyEnvironment(EnvironmentApplier.Preset("Day"), sun);
            var lm = CreateLevelManager(LevelMode.Custom, true, out _);
            var loader = lm.gameObject.AddComponent<LevelLoader>();
            EditorUtil.Set(loader, "sun", sun);
            EditorUtil.Set(lm, "loader", loader);
            CreateUI(false);
            Save(scene, GamePaths.CustomLevelScene);
        }

        // ------------------------------------------------------------------ Campaign scene

        public static void GenerateCampaignScene(CampaignLevels.Entry entry, LevelData data, LevelDefinition definition, PrefabRegistry registry)
        {
            var scene = NewScene();
            var sun = CreateSun();
            ApplyEnvironment(data.environment, sun);

            var lm = CreateLevelManager(LevelMode.Campaign, true, out _);
            var root = BuildLevelObjects(data, "Level");
            EditorUtil.Set(lm, "definition", definition);
            EditorUtil.Set(lm, "levelRoot", root);
            EditorUtil.Set(lm, "killHeight", data.killHeight);
            EditorUtil.Set(lm, "music", (int)entry.music);
            var weapons = new List<Object>();
            foreach (var id in data.startingWeapons)
            {
                var def = registry.GetWeapon(id);
                if (def != null) weapons.Add(def);
                else Debug.LogWarning($"[Momentum Setup] {entry.scene}: unknown starting weapon '{id}'.");
            }
            EditorUtil.SetArray(lm, "startingWeapons", weapons.ToArray());
            CreateUI(false);
            Save(scene, CampaignScenePath(entry));
        }

        public static string CampaignScenePath(CampaignLevels.Entry entry)
        {
            return GamePaths.LevelScenes + "/" + entry.scene + ".unity";
        }

        // ------------------------------------------------------------------ Build settings

        public static void WriteBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(GamePaths.MainMenuScene, true),
                new EditorBuildSettingsScene(GamePaths.LevelEditorScene, true),
                new EditorBuildSettingsScene(GamePaths.CustomLevelScene, true)
            };
            foreach (var e in CampaignLevels.All)
            {
                string path = CampaignScenePath(e);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
