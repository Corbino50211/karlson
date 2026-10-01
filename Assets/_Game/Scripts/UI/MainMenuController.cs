using Momentum.Audio;
using UnityEngine;

namespace Momentum.UI
{
    /// <summary>Main menu scene controller: builds/instantiates the menu and routes its buttons.</summary>
    public class MainMenuController : MonoBehaviour
    {
        MainMenuView view;

        void Awake()
        {
            UIFactory.EnsureEventSystem();
            var registry = PrefabRegistry.Instance;
            if (registry != null && registry.mainMenu != null)
            {
                var go = Instantiate(registry.mainMenu, transform);
                view = go.GetComponent<MainMenuView>();
                if (view == null) Destroy(go);
            }
            if (view == null) view = MainMenuView.Build(transform);

            view.PlayButton.onClick.AddListener(() => OpenLevelSelect(false));
            view.LevelSelectButton.onClick.AddListener(() => OpenLevelSelect(false));
            view.EditorButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.OpenLevelEditor(null);
            });
            view.SettingsButton.onClick.AddListener(() => view.ShowPanel(view.Settings));
            view.CreditsButton.onClick.AddListener(() => view.ShowPanel(view.Credits));
            view.QuitButton.onClick.AddListener(() =>
            {
                view.Dialog.Show("Quit " + GameConfig.Instance.gameTitle + "?", () =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.QuitGame();
                    else Application.Quit();
                }, null, "QUIT", "CANCEL");
            });
        }

        void Start()
        {
            Time.timeScale = 1f;
            GameManager.SetCursorLocked(false);
            AudioManager.PlayMusic(MusicTrack.Menu);
            AudioManager.SetAmbient(false);
            var target = GameManager.Instance != null ? GameManager.Instance.MenuReturnTarget : MenuReturnTarget.Main;
            if (target == MenuReturnTarget.LevelSelect) OpenLevelSelect(false);
            else if (target == MenuReturnTarget.CustomMaps) OpenLevelSelect(true);
            else view.ShowPanel(null);
            if (GameManager.Instance != null) GameManager.Instance.MenuReturnTarget = MenuReturnTarget.Main;
        }

        void OpenLevelSelect(bool custom)
        {
            view.ShowPanel(view.LevelSelect);
            view.LevelSelect.Open(custom);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) view.ShowPanel(null);
        }
    }
}
