using System;
using Momentum.SaveSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Tools > Parkour FPS menu. "Setup Complete Game" builds the entire project (settings, materials, meshes,
    /// prefabs, ScriptableObjects, UI, scenes, campaign levels, Build Settings) and is safe to run again:
    /// assets are overwritten in place (GUIDs and tuned values are kept) and scenes are regenerated.
    /// </summary>
    public static class SetupMenu
    {
        const string Menu = "Tools/Parkour FPS/";

        [MenuItem(Menu + "Setup Complete Game", false, 0)]
        public static void SetupCompleteGame()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("MOMENTUM Setup", "Exit Play Mode before running the setup.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var started = DateTime.Now;
            try
            {
                Step("Configuring project settings", 0.02f);
                ProjectConfigurator.CreateFolders();
                ProjectConfigurator.ConfigureTagsAndLayers();
                ProjectConfigurator.ConfigurePhysics();
                ProjectConfigurator.ConfigureInput();

                Step("Creating ScriptableObjects", 0.06f);
                var assets = ScriptableObjectGenerator.CreateCoreAssets();
                ProjectConfigurator.ConfigurePlayerSettings(assets.config);

                // Prefabs are assembled in a throwaway scene.
                SceneGenerator.NewScene();

                Step("Generating materials and textures", 0.1f);
                MaterialGenerator.Generate(assets.materials);
                EditorUtility.SetDirty(assets.materials);

                Step("Generating meshes", 0.14f);
                MeshAssetGenerator.Generate();

                Step("Generating effects and projectiles", 0.18f);
                EffectPrefabGenerator.Generate(assets.prefabs, assets.materials);

                Step("Generating weapons", 0.24f);
                WeaponPrefabGenerator.Generate(assets.prefabs, assets.materials);
                assets.prefabs.InvalidateCaches();

                Step("Generating level objects and pickups", 0.3f);
                EnvironmentPrefabGenerator.Generate(assets.prefabs, assets.materials);

                Step("Generating enemies", 0.36f);
                EnemyPrefabGenerator.Generate(assets.prefabs, assets.materials);

                Step("Generating bosses", 0.42f);
                BossPrefabGenerator.Generate(assets.prefabs, assets.materials);

                Step("Generating the player", 0.48f);
                PlayerPrefabGenerator.Generate(assets.prefabs, assets.materials, assets.movement, assets.camera);

                Step("Generating UI", 0.52f);
                UIGenerator.Generate(assets.prefabs);

                assets.prefabs.InvalidateCaches();
                EditorUtility.SetDirty(assets.prefabs);
                AssetDatabase.SaveAssets();
                GameConfig.ClearCache();

                Step("Creating level definitions", 0.56f);
                var definitions = ScriptableObjectGenerator.CreateLevelDefinitions(assets.levels);

                Step("Building menu, editor and custom level scenes", 0.6f);
                SceneGenerator.GenerateMainMenu();
                SceneGenerator.GenerateLevelEditor();
                SceneGenerator.GenerateCustomLevel();

                Step("Building campaign levels", 0.65f);
                DemoLevelGenerator.Generate(assets, definitions);

                Step("Writing Build Settings", 0.97f);
                SceneGenerator.WriteBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(e);
                EditorUtility.DisplayDialog("MOMENTUM Setup", "Setup failed: " + e.Message + "\n\nSee the Console for details.", "OK");
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorSceneManager.OpenScene(GamePaths.MainMenuScene);
            int errors = ProjectValidator.Validate(false);
            double seconds = (DateTime.Now - started).TotalSeconds;
            Debug.Log($"[Momentum Setup] Setup complete in {seconds:0.0}s. Open scene: MainMenu. Press Play to start.");
            EditorUtility.DisplayDialog("MOMENTUM Setup",
                "Setup complete!\n\nThe MainMenu scene is open - press Play.\n\n" +
                (errors == 0 ? "Validation passed." : errors + " validation error(s) - see the Console."),
                "Let's go");
        }

        [MenuItem(Menu + "Rebuild Demo Levels", false, 20)]
        public static void RebuildDemoLevels()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(GamePaths.GameConfigAsset);
            if (config == null || config.prefabRegistry == null || config.prefabRegistry.player == null)
            {
                if (EditorUtility.DisplayDialog("MOMENTUM", "The project has not been set up yet. Run the full setup now?", "Run Setup", "Cancel")) SetupCompleteGame();
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                var assets = ScriptableObjectGenerator.CreateCoreAssets();
                var definitions = ScriptableObjectGenerator.CreateLevelDefinitions(assets.levels);
                SceneGenerator.GenerateMainMenu();
                DemoLevelGenerator.Generate(assets, definitions);
                SceneGenerator.WriteBuildSettings();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("MOMENTUM", "Rebuilding levels failed: " + e.Message, "OK");
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            EditorSceneManager.OpenScene(GamePaths.MainMenuScene);
            Debug.Log("[Momentum Setup] Demo levels rebuilt.");
        }

        [MenuItem(Menu + "Validate Project", false, 21)]
        public static void ValidateProject()
        {
            ProjectValidator.Validate(true);
        }

        [MenuItem(Menu + "Unlock All Levels", false, 40)]
        public static void UnlockAllLevels()
        {
            SaveManager.WriteUnlockAll();
            Debug.Log("[Momentum] All levels unlocked (" + SaveManager.SaveFilePath + ").");
            EditorUtility.DisplayDialog("MOMENTUM", "All campaign levels are now unlocked.", "OK");
        }

        [MenuItem(Menu + "Clear Local Save", false, 41)]
        public static void ClearLocalSave()
        {
            int choice = EditorUtility.DisplayDialogComplex("MOMENTUM - Clear Local Save",
                "Delete the local save (best times, unlocks, splits)?\nCustom levels in " + SaveManager.CustomLevelsDirectory + " are kept.",
                "Delete Save", "Cancel", "Delete Save + Settings");
            if (choice == 1) return;
            SaveManager.DeleteSaveFiles(choice == 2);
            Debug.Log("[Momentum] Local save deleted" + (choice == 2 ? " (settings too)." : "."));
        }

        [MenuItem(Menu + "Open Main Menu Scene", false, 60)]
        public static void OpenMainMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GamePaths.MainMenuScene) == null)
            {
                EditorUtility.DisplayDialog("MOMENTUM", "The MainMenu scene does not exist yet. Run Setup Complete Game first.", "OK");
                return;
            }
            EditorSceneManager.OpenScene(GamePaths.MainMenuScene);
        }

        [MenuItem(Menu + "Open Save Folder", false, 61)]
        public static void OpenSaveFolder()
        {
            EditorUtility.RevealInFinder(SaveManager.SaveDirectory);
        }

        static void Step(string message, float progress)
        {
            EditorUtility.DisplayProgressBar("MOMENTUM Setup", message, progress);
        }
    }
}
