using Momentum.Levels;
using Momentum.SaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Momentum
{
    public enum GameState
    {
        Boot,
        MainMenu,
        Playing,
        Editor,
        Loading
    }

    /// <summary>Which main-menu panel to show when returning from a level.</summary>
    public enum MenuReturnTarget
    {
        Main,
        LevelSelect,
        CustomMaps
    }

    /// <summary>
    /// Persistent high-level game flow: scene transitions, pause state and cursor handling.
    /// Gameplay rules live in LevelManager; saving lives in SaveManager; this class stays small on purpose.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Boot;
        public bool IsPaused { get; private set; }

        /// <summary>The campaign level currently being played (null for custom levels / menus).</summary>
        public LevelDefinition CurrentLevel { get; private set; }

        /// <summary>Path of the custom JSON level the CustomLevel scene should load.</summary>
        public string PendingCustomLevelPath { get; set; }

        /// <summary>Path of the custom JSON level the level editor should open (null = new level).</summary>
        public string PendingEditorLevelPath { get; set; }

        public MenuReturnTarget MenuReturnTarget { get; set; } = MenuReturnTarget.Main;

        GameConfig config;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            config = GameConfig.Instance;
            SceneManager.sceneLoaded += OnSceneLoaded;
            DetectState(SceneManager.GetActiveScene());
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            SetPaused(false);
            DetectState(scene);
        }

        void DetectState(Scene scene)
        {
            if (scene.name == config.mainMenuScene)
            {
                State = GameState.MainMenu;
                CurrentLevel = null;
                SetCursorLocked(false);
            }
            else if (scene.name == config.levelEditorScene)
            {
                State = GameState.Editor;
                CurrentLevel = null;
                SetCursorLocked(false);
            }
            else
            {
                State = GameState.Playing;
                if (scene.name != config.customLevelScene && config.levelRegistry != null)
                {
                    var def = config.levelRegistry.FindByScene(scene.name);
                    if (def != null) CurrentLevel = def;
                }
                else if (scene.name == config.customLevelScene)
                {
                    CurrentLevel = null;
                }
            }
        }

        // ------------------------------------------------------------------ Scene flow

        public void LoadMainMenu(MenuReturnTarget target = MenuReturnTarget.Main)
        {
            MenuReturnTarget = target;
            CurrentLevel = null;
            SceneLoader.Load(config.mainMenuScene);
        }

        public void LoadLevel(LevelDefinition level)
        {
            if (level == null)
            {
                Debug.LogError("[Momentum] Tried to load a null level.");
                return;
            }
            CurrentLevel = level;
            if (SaveManager.Instance != null) SaveManager.Instance.SetLastPlayed(level.id);
            SceneLoader.Load(level.sceneName);
        }

        /// <summary>Loads the campaign level after the current one. Returns false if there is none or it is locked.</summary>
        public bool LoadNextLevel()
        {
            if (CurrentLevel == null || config.levelRegistry == null) return false;
            var next = config.levelRegistry.GetNext(CurrentLevel);
            if (next == null) return false;
            if (SaveManager.Instance != null && !SaveManager.Instance.IsUnlocked(next)) return false;
            LoadLevel(next);
            return true;
        }

        public bool HasNextLevel()
        {
            if (CurrentLevel == null || config.levelRegistry == null) return false;
            var next = config.levelRegistry.GetNext(CurrentLevel);
            return next != null && (SaveManager.Instance == null || SaveManager.Instance.IsUnlocked(next));
        }

        public void RestartScene()
        {
            SceneLoader.Load(SceneManager.GetActiveScene().name);
        }

        public void PlayCustomLevel(string path)
        {
            PendingCustomLevelPath = path;
            CurrentLevel = null;
            SceneLoader.Load(config.customLevelScene);
        }

        public void OpenLevelEditor(string pathOrNull)
        {
            PendingEditorLevelPath = pathOrNull;
            CurrentLevel = null;
            SceneLoader.Load(config.levelEditorScene);
        }

        // ------------------------------------------------------------------ Pause

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            GameEvents.RaisePauseChanged(paused);
        }

        public void QuitGame()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
