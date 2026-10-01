using Momentum.Audio;
using Momentum.Levels;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum
{
    /// <summary>
    /// Global game configuration. Lives in a Resources folder so it can be loaded from anywhere
    /// (Resources/GameConfig.asset). Change the game title, scene names and developer options here.
    /// </summary>
    [CreateAssetMenu(menuName = "Momentum/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public const string ResourcePath = "GameConfig";

        [Header("Branding")]
        [Tooltip("Displayed on the main menu, window title and credits. Change this to rename the game.")]
        public string gameTitle = "MOMENTUM";
        public string subtitle = "A first-person movement shooter";
        public string version = "0.1.0";
        public string studioName = "Independent";
        [TextArea(6, 20)]
        public string credits =
            "MOMENTUM\n\n" +
            "Design, code and procedural assets generated for this project.\n" +
            "All geometry, sounds and music are created procedurally at setup/runtime.\n\n" +
            "Inspired by the broad feel of fast arcade movement shooters.\n" +
            "No third-party assets were used.\n\n" +
            "Built with Unity (Built-in Render Pipeline).";

        [Header("Developer")]
        [Tooltip("When enabled every campaign level is selectable regardless of save progress.")]
        public bool unlockAllLevels;
        [Tooltip("Enables extra logging and the developer shortcuts (F5 quick-restart, F6 skip level).")]
        public bool developerMode;

        [Header("Scenes (names as they appear in Build Settings)")]
        public string mainMenuScene = "MainMenu";
        public string levelEditorScene = "LevelEditor";
        public string customLevelScene = "CustomLevel";

        [Header("Registries")]
        public LevelRegistry levelRegistry;
        public PrefabRegistry prefabRegistry;
        public MaterialLibrary materialLibrary;
        public AudioLibrary audioLibrary;

        [Header("Default Settings Assets")]
        public MovementSettings defaultMovementSettings;
        public CameraFeelSettings defaultCameraSettings;

        [Header("Simulation")]
        [Tooltip("Physics step. 0.01 (100 Hz) gives very responsive movement.")]
        public float fixedTimestep = 0.01f;
        public float defaultKillHeight = -40f;
        public float respawnDelay = 1.25f;
        public float fallRespawnDelay = 0.35f;

        [Header("Theme")]
        public Color accentColor = new Color(1f, 0.45f, 0.1f);
        public Color secondaryColor = new Color(0.15f, 0.85f, 1f);
        public Color bossColor = new Color(0.9f, 0.12f, 0.2f);
        public Color successColor = new Color(0.3f, 1f, 0.45f);

        [Header("Custom Levels")]
        [Tooltip("Folder (inside Application.persistentDataPath) used for custom JSON maps.")]
        public string customLevelsFolder = "Levels";

        static GameConfig instance;

        /// <summary>Loads the config from Resources, falling back to an in-memory default.</summary>
        public static GameConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameConfig>(ResourcePath);
                    if (instance == null)
                    {
                        instance = CreateInstance<GameConfig>();
                        instance.name = "GameConfig (runtime default)";
                        Debug.LogWarning("[Momentum] GameConfig not found in Resources. Run Tools > Parkour FPS > Setup Complete Game.");
                    }
                }
                return instance;
            }
        }

        /// <summary>Clears the cached instance (used by editor tooling after regenerating assets).</summary>
        public static void ClearCache()
        {
            instance = null;
        }
    }
}
