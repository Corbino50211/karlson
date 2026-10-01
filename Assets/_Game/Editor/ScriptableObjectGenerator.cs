using System.Collections.Generic;
using Momentum.Audio;
using Momentum.Levels;
using Momentum.PlayerSystems;
using UnityEditor;
using UnityEngine;

namespace Momentum.EditorTools
{
    /// <summary>
    /// Creates (or loads) every ScriptableObject the game uses: GameConfig (in Resources so the runtime can
    /// find it), the prefab registry, material library, audio library, movement and camera settings, the
    /// level registry and one LevelDefinition per campaign level. Existing assets keep their tuned values.
    /// </summary>
    public static class ScriptableObjectGenerator
    {
        public class Assets
        {
            public GameConfig config;
            public PrefabRegistry prefabs;
            public MaterialLibrary materials;
            public AudioLibrary audio;
            public MovementSettings movement;
            public CameraFeelSettings camera;
            public LevelRegistry levels;
        }

        /// <summary>Creates/loads the core assets and links them into GameConfig.</summary>
        public static Assets CreateCoreAssets()
        {
            foreach (var folder in GamePaths.AllFolders) EditorUtil.EnsureFolder(folder);

            var a = new Assets
            {
                config = EditorUtil.LoadOrCreate<GameConfig>(GamePaths.GameConfigAsset),
                prefabs = EditorUtil.LoadOrCreate<PrefabRegistry>(GamePaths.PrefabRegistryAsset),
                materials = EditorUtil.LoadOrCreate<MaterialLibrary>(GamePaths.MaterialLibraryAsset),
                audio = EditorUtil.LoadOrCreate<AudioLibrary>(GamePaths.AudioLibraryAsset),
                movement = EditorUtil.LoadOrCreate<MovementSettings>(GamePaths.MovementSettingsAsset),
                camera = EditorUtil.LoadOrCreate<CameraFeelSettings>(GamePaths.CameraSettingsAsset),
                levels = EditorUtil.LoadOrCreate<LevelRegistry>(GamePaths.LevelRegistryAsset)
            };

            a.audio.EnsureAllEntries();

            a.config.prefabRegistry = a.prefabs;
            a.config.materialLibrary = a.materials;
            a.config.audioLibrary = a.audio;
            a.config.defaultMovementSettings = a.movement;
            a.config.defaultCameraSettings = a.camera;
            a.config.levelRegistry = a.levels;
            a.config.mainMenuScene = SceneGenerator.MainMenuSceneName;
            a.config.levelEditorScene = SceneGenerator.LevelEditorSceneName;
            a.config.customLevelScene = SceneGenerator.CustomLevelSceneName;

            foreach (var o in new Object[] { a.config, a.prefabs, a.materials, a.audio, a.movement, a.camera, a.levels }) EditorUtility.SetDirty(o);
            AssetDatabase.SaveAssets();
            GameConfig.ClearCache();
            return a;
        }

        /// <summary>Creates one LevelDefinition per campaign level and fills the level registry in order.</summary>
        public static List<LevelDefinition> CreateLevelDefinitions(LevelRegistry registry)
        {
            var result = new List<LevelDefinition>();
            foreach (var e in CampaignLevels.All)
            {
                string path = GamePaths.LevelDefinitions + "/Level" + e.number.ToString("00") + "_" + e.name.Replace(" ", "") + ".asset";
                var def = EditorUtil.LoadOrCreate<LevelDefinition>(path, out bool created);
                // Identity always follows the generator so scenes and save keys stay consistent.
                def.id = e.id;
                def.number = e.number;
                def.sceneName = e.scene;
                def.isBossLevel = e.boss;
                def.bossName = e.bossName;
                if (created)
                {
                    def.displayName = e.name;
                    def.description = e.description;
                    def.accentColor = e.accent;
                    def.music = e.music;
                    def.rankTimes = e.ranks;
                }
                EditorUtility.SetDirty(def);
                result.Add(def);
            }
            registry.campaign = result;
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            return result;
        }
    }
}
