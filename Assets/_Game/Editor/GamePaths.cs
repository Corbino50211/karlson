namespace Momentum.EditorTools
{
    /// <summary>Asset paths used by the generators. Change Root to move the whole generated project.</summary>
    public static class GamePaths
    {
        public const string Root = "Assets/_Game";

        public const string Art = Root + "/Art";
        public const string Shaders = Art + "/Shaders";
        public const string Meshes = Art + "/Meshes";
        public const string Textures = Art + "/Textures";
        public const string Audio = Root + "/Audio";
        public const string Materials = Root + "/Materials";
        public const string WeaponMaterials = Materials + "/Weapons";

        public const string Prefabs = Root + "/Prefabs";
        public const string PlayerPrefabs = Prefabs + "/Player";
        public const string WeaponPrefabs = Prefabs + "/Weapons";
        public const string EnemyPrefabs = Prefabs + "/Enemies";
        public const string BossPrefabs = Prefabs + "/Bosses";
        public const string EnvironmentPrefabs = Prefabs + "/Environment";
        public const string PickupPrefabs = Prefabs + "/Pickups";
        public const string UIPrefabs = Prefabs + "/UI";
        public const string EffectPrefabs = Prefabs + "/Effects";
        public const string ProjectilePrefabs = Prefabs + "/Projectiles";

        public const string Scenes = Root + "/Scenes";
        public const string MenuScenes = Scenes + "/Menus";
        public const string LevelScenes = Scenes + "/Levels";

        public const string Resources = Root + "/Resources";
        public const string ScriptableObjects = Root + "/ScriptableObjects";
        public const string WeaponDefinitions = ScriptableObjects + "/Weapons";
        public const string EnemyDefinitions = ScriptableObjects + "/Enemies";
        public const string LevelDefinitions = ScriptableObjects + "/Levels";
        public const string Settings = ScriptableObjects + "/Settings";
        public const string Registries = ScriptableObjects + "/Registries";
        public const string CampaignJson = Root + "/Levels/Campaign";

        public const string GameConfigAsset = Resources + "/GameConfig.asset";
        public const string PrefabRegistryAsset = Registries + "/PrefabRegistry.asset";
        public const string MaterialLibraryAsset = Registries + "/MaterialLibrary.asset";
        public const string LevelRegistryAsset = Registries + "/LevelRegistry.asset";
        public const string AudioLibraryAsset = Registries + "/AudioLibrary.asset";
        public const string MovementSettingsAsset = Settings + "/MovementSettings.asset";
        public const string CameraSettingsAsset = Settings + "/CameraFeelSettings.asset";
        public const string LightingSettingsAsset = Settings + "/RealtimeLighting.lighting";

        public const string MainMenuScene = MenuScenes + "/MainMenu.unity";
        public const string LevelEditorScene = MenuScenes + "/LevelEditor.unity";
        public const string CustomLevelScene = LevelScenes + "/CustomLevel.unity";

        public static readonly string[] AllFolders =
        {
            Art, Shaders, Meshes, Textures, Audio, Materials, WeaponMaterials,
            Prefabs, PlayerPrefabs, WeaponPrefabs, EnemyPrefabs, BossPrefabs, EnvironmentPrefabs, PickupPrefabs, UIPrefabs, EffectPrefabs, ProjectilePrefabs,
            Scenes, MenuScenes, LevelScenes,
            Resources, ScriptableObjects, WeaponDefinitions, EnemyDefinitions, LevelDefinitions, Settings, Registries, CampaignJson
        };
    }
}
