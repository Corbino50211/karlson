using System;
using Momentum.Levels;
using Momentum.PlayerSystems;
using UnityEngine;

namespace Momentum.UI
{
    /// <summary>
    /// Gameplay UI coordinator for a scene: creates the HUD, pause menu, results screen and debug overlay
    /// (from generated prefabs when available, otherwise built in code), handles ESC/pause and routes
    /// menu buttons to LevelManager / GameManager.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Tooltip("Level editor scene: gameplay UI only exists while testing, and menus return to the editor.")]
        [SerializeField] bool editorTestMode;

        HUDView hud;
        PauseMenuView pause;
        ResultsView results;
        DebugOverlay debugOverlay;
        bool gameplayActive;
        bool showingResults;

        /// <summary>Raised when the player chooses "Exit Test" / "Back to Editor" (editor scene only).</summary>
        public event Action ExitTestRequested;

        public bool IsPaused => pause != null && pause.gameObject.activeSelf;
        public bool ShowingResults => showingResults;

        void Awake()
        {
            Instance = this;
            UIFactory.EnsureEventSystem();
            var registry = PrefabRegistry.Instance;

            hud = Create(registry != null ? registry.hud : null, HUDView.Build);
            pause = Create(registry != null ? registry.pauseMenu : null, PauseMenuView.Build);
            results = Create(registry != null ? registry.resultsScreen : null, ResultsView.Build);
            debugOverlay = DebugOverlay.Build(transform);

            pause.gameObject.SetActive(false);
            results.gameObject.SetActive(false);
            pause.SetEditorTestMode(editorTestMode);

            pause.ResumeButton.onClick.AddListener(Resume);
            pause.RestartCheckpointButton.onClick.AddListener(() =>
            {
                Resume();
                if (LevelManager.Instance != null) LevelManager.Instance.RestartFromCheckpoint();
            });
            pause.RestartLevelButton.onClick.AddListener(() =>
            {
                Resume();
                if (LevelManager.Instance != null) LevelManager.Instance.RestartLevel();
            });
            pause.MainMenuButton.onClick.AddListener(() =>
            {
                if (editorTestMode)
                {
                    Resume();
                    ExitTestRequested?.Invoke();
                }
                else if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoadMainMenu(MenuReturnTarget.LevelSelect);
                }
            });

            results.RetryButton.onClick.AddListener(() =>
            {
                HideResults();
                if (LevelManager.Instance != null) LevelManager.Instance.RestartLevel();
            });
            results.NextButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null && !GameManager.Instance.LoadNextLevel()) GameManager.Instance.LoadMainMenu(MenuReturnTarget.LevelSelect);
            });
            results.LevelSelectButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance == null) return;
                bool custom = LevelManager.Instance != null && LevelManager.Instance.Mode == LevelMode.Custom;
                GameManager.Instance.LoadMainMenu(custom ? MenuReturnTarget.CustomMaps : MenuReturnTarget.LevelSelect);
            });
            results.MainMenuButton.onClick.AddListener(() =>
            {
                if (editorTestMode)
                {
                    HideResults();
                    ExitTestRequested?.Invoke();
                }
                else if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoadMainMenu(MenuReturnTarget.Main);
                }
            });

            SetGameplayActive(!editorTestMode);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        T Create<T>(GameObject prefab, Func<Transform, T> build) where T : Component
        {
            if (prefab != null)
            {
                var go = Instantiate(prefab, transform);
                var view = go.GetComponent<T>();
                if (view != null) return view;
                Destroy(go);
            }
            return build(transform);
        }

        void OnEnable()
        {
            GameEvents.RunFinished += OnRunFinished;
        }

        void OnDisable()
        {
            GameEvents.RunFinished -= OnRunFinished;
        }

        /// <summary>Shows/hides gameplay UI (used by the level editor when entering/leaving test mode).</summary>
        public void SetGameplayActive(bool active)
        {
            gameplayActive = active;
            if (hud != null) hud.gameObject.SetActive(active);
            if (debugOverlay != null) debugOverlay.gameObject.SetActive(active);
            if (!active)
            {
                if (pause != null) pause.Hide();
                HideResults();
            }
        }

        void Update()
        {
            if (!gameplayActive || showingResults) return;
            var player = PlayerController.Current;
            KeyCode pauseKey = player != null && player.InputHandler != null ? player.InputHandler.Bindings.pause : KeyCode.Escape;
            if (Input.GetKeyDown(pauseKey))
            {
                if (IsPaused)
                {
                    if (pause.SettingsOpen) pause.CloseSettings();
                    else Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        public void Pause()
        {
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(true);
            pause.Show();
            GameManager.SetCursorLocked(false);
        }

        public void Resume()
        {
            pause.Hide();
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            if (!showingResults) GameManager.SetCursorLocked(true);
        }

        void OnRunFinished(LevelResult result)
        {
            if (!gameplayActive) return;
            showingResults = true;
            if (pause != null) pause.Hide();
            results.Show(result);
            GameManager.SetCursorLocked(false);
        }

        public void HideResults()
        {
            showingResults = false;
            if (results != null) results.Hide();
        }
    }
}
