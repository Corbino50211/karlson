using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>Configures project settings: folders, tags, layers, physics, input axes and player settings.</summary>
    public static class ProjectConfigurator
    {
        public static void CreateFolders()
        {
            foreach (var folder in GamePaths.AllFolders) EditorUtil.EnsureFolder(folder);
        }

        // ------------------------------------------------------------------ Tags & layers

        public static void ConfigureTagsAndLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[Momentum Setup] Could not open ProjectSettings/TagManager.asset");
                return;
            }
            var so = new SerializedObject(assets[0]);

            var tags = so.FindProperty("tags");
            foreach (var tag in Layers.CustomTags)
            {
                bool found = false;
                for (int i = 0; i < tags.arraySize; i++)
                {
                    if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) continue;
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            }

            var layers = so.FindProperty("layers");
            for (int i = 0; i < Layers.CustomLayerIndices.Length; i++)
            {
                int index = Layers.CustomLayerIndices[i];
                string name = Layers.CustomLayerNames[i];
                var element = layers.GetArrayElementAtIndex(index);
                if (element.stringValue == name) continue;
                if (!string.IsNullOrEmpty(element.stringValue))
                {
                    Debug.LogWarning($"[Momentum Setup] Layer {index} was '{element.stringValue}', renaming to '{name}'.");
                }
                element.stringValue = name;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Physics & time

        public static void ConfigurePhysics()
        {
            Physics.gravity = new Vector3(0f, -25f, 0f);
            Physics.queriesHitTriggers = false;
            Physics.defaultSolverIterations = 8;
            Physics.defaultSolverVelocityIterations = 2;
            Physics.bounceThreshold = 2f;
            Physics.sleepThreshold = 0.005f;
            Physics.defaultContactOffset = 0.01f;
            Physics.autoSyncTransforms = false;
            Layers.ApplyCollisionMatrix();

            // Persist into the DynamicsManager asset as well (the API above already updates it in memory).
            var dynamics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset");
            if (dynamics != null && dynamics.Length > 0) EditorUtility.SetDirty(dynamics[0]);

            Time.fixedDeltaTime = 0.01f;
            Time.maximumDeltaTime = 0.1f;
            var time = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset");
            if (time != null && time.Length > 0) EditorUtility.SetDirty(time[0]);
        }

        // ------------------------------------------------------------------ Input

        struct Axis
        {
            public string name;
            public string negative;
            public string positive;
            public string altNegative;
            public string altPositive;
            public float gravity;
            public float dead;
            public float sensitivity;
            public bool snap;
            public int type;
            public int axis;
        }

        static readonly Axis[] RequiredAxes =
        {
            new Axis { name = "Horizontal", negative = "left", positive = "right", altNegative = "a", altPositive = "d", gravity = 3f, dead = 0.001f, sensitivity = 3f, snap = true },
            new Axis { name = "Vertical", negative = "down", positive = "up", altNegative = "s", altPositive = "w", gravity = 3f, dead = 0.001f, sensitivity = 3f, snap = true },
            new Axis { name = "Mouse X", sensitivity = 0.1f, type = 1, axis = 0 },
            new Axis { name = "Mouse Y", sensitivity = 0.1f, type = 1, axis = 1 },
            new Axis { name = "Mouse ScrollWheel", sensitivity = 0.1f, type = 1, axis = 2 },
            new Axis { name = "Submit", positive = "return", altPositive = "joystick button 0", gravity = 1000f, dead = 0.001f, sensitivity = 1000f },
            new Axis { name = "Cancel", positive = "escape", altPositive = "joystick button 1", gravity = 1000f, dead = 0.001f, sensitivity = 1000f },
            new Axis { name = "Fire1", positive = "left ctrl", altPositive = "mouse 0", gravity = 1000f, dead = 0.001f, sensitivity = 1000f },
            new Axis { name = "Jump", positive = "space", gravity = 1000f, dead = 0.001f, sensitivity = 1000f }
        };

        /// <summary>Makes sure the legacy Input Manager has the axes the game and uGUI need.</summary>
        public static void ConfigureInput()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[Momentum Setup] Could not open ProjectSettings/InputManager.asset");
                return;
            }
            var so = new SerializedObject(assets[0]);
            var axes = so.FindProperty("m_Axes");
            var existing = new HashSet<string>();
            for (int i = 0; i < axes.arraySize; i++)
            {
                existing.Add(axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue);
            }

            int added = 0;
            foreach (var a in RequiredAxes)
            {
                if (existing.Contains(a.name)) continue;
                axes.InsertArrayElementAtIndex(axes.arraySize);
                var e = axes.GetArrayElementAtIndex(axes.arraySize - 1);
                e.FindPropertyRelative("m_Name").stringValue = a.name;
                e.FindPropertyRelative("descriptiveName").stringValue = "";
                e.FindPropertyRelative("descriptiveNegativeName").stringValue = "";
                e.FindPropertyRelative("negativeButton").stringValue = a.negative ?? "";
                e.FindPropertyRelative("positiveButton").stringValue = a.positive ?? "";
                e.FindPropertyRelative("altNegativeButton").stringValue = a.altNegative ?? "";
                e.FindPropertyRelative("altPositiveButton").stringValue = a.altPositive ?? "";
                e.FindPropertyRelative("gravity").floatValue = a.gravity;
                e.FindPropertyRelative("dead").floatValue = a.dead;
                e.FindPropertyRelative("sensitivity").floatValue = a.sensitivity;
                e.FindPropertyRelative("snap").boolValue = a.snap;
                e.FindPropertyRelative("invert").boolValue = false;
                e.FindPropertyRelative("type").intValue = a.type;
                e.FindPropertyRelative("axis").intValue = a.axis;
                e.FindPropertyRelative("joyNum").intValue = 0;
                added++;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            if (added > 0) Debug.Log($"[Momentum Setup] Added {added} missing input axes.");
        }

        public static bool HasInputAxis(string axisName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");
            if (assets == null || assets.Length == 0) return false;
            var axes = new SerializedObject(assets[0]).FindProperty("m_Axes");
            for (int i = 0; i < axes.arraySize; i++)
            {
                if (axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue == axisName) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ Player settings

        public static void ConfigurePlayerSettings(GameConfig config)
        {
            PlayerSettings.productName = config != null ? config.gameTitle : "MOMENTUM";
            if (string.IsNullOrEmpty(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany")
            {
                PlayerSettings.companyName = config != null ? config.studioName : "Independent";
            }
            PlayerSettings.bundleVersion = config != null ? config.version : "0.1.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = false;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadowDistance = 120f;
        }
    }
}
