using UnityEngine;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>ESC menu: Resume, Restart Checkpoint, Restart Level, Settings, Main Menu (or Exit Test).</summary>
    public class PauseMenuView : MonoBehaviour
    {
        [SerializeField] RectTransform menuPanel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button restartCheckpointButton;
        [SerializeField] Button restartLevelButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button mainMenuButton;
        [SerializeField] SettingsPanelView settingsPanel;
        [SerializeField] Text titleText;

        public Button ResumeButton => resumeButton;
        public Button RestartCheckpointButton => restartCheckpointButton;
        public Button RestartLevelButton => restartLevelButton;
        public Button MainMenuButton => mainMenuButton;
        public SettingsPanelView Settings => settingsPanel;
        public bool SettingsOpen => settingsPanel != null && settingsPanel.gameObject.activeSelf;

        void Awake()
        {
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (settingsPanel != null)
            {
                settingsPanel.Closed += () => { if (menuPanel != null) menuPanel.gameObject.SetActive(true); };
                settingsPanel.gameObject.SetActive(false);
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (menuPanel != null) menuPanel.gameObject.SetActive(true);
            if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
        }

        public void Hide()
        {
            if (settingsPanel != null && settingsPanel.gameObject.activeSelf) settingsPanel.Close();
            gameObject.SetActive(false);
        }

        public void OpenSettings()
        {
            if (menuPanel != null) menuPanel.gameObject.SetActive(false);
            if (settingsPanel != null) settingsPanel.gameObject.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.Close();
        }

        public void SetEditorTestMode(bool editorTest)
        {
            var label = UIFactory.GetLabel(mainMenuButton);
            if (label != null) label.text = editorTest ? "EXIT TEST (BACK TO EDITOR)" : "MAIN MENU";
            if (titleText != null) titleText.text = editorTest ? "TEST PAUSED" : "PAUSED";
        }

        public static PauseMenuView Build(Transform parent)
        {
            var canvas = UIFactory.CreateCanvas("PauseMenu", 50, parent);
            var view = canvas.gameObject.AddComponent<PauseMenuView>();
            var overlay = UIFactory.CreatePanel("Overlay", canvas.transform, UITheme.Overlay);
            UIFactory.Stretch(overlay.rectTransform);

            var panel = UIFactory.CreatePanel("Menu", canvas.transform, UITheme.Background);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 620f));
            view.menuPanel = panel.rectTransform;
            UIFactory.AddVertical(panel.gameObject, 14f, new RectOffset(40, 40, 36, 36), TextAnchor.UpperCenter);

            view.titleText = UIFactory.CreateText("Title", panel.rectTransform, "PAUSED", 52, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold);
            UIFactory.Size(view.titleText, -1f, 80f);
            view.resumeButton = MenuButton(panel.rectTransform, "RESUME");
            view.restartCheckpointButton = MenuButton(panel.rectTransform, "RESTART CHECKPOINT");
            view.restartLevelButton = MenuButton(panel.rectTransform, "RESTART LEVEL");
            view.settingsButton = MenuButton(panel.rectTransform, "SETTINGS");
            view.mainMenuButton = MenuButton(panel.rectTransform, "MAIN MENU");

            var settingsHolder = UIFactory.CreateRect("SettingsHolder", canvas.transform);
            UIFactory.Place(settingsHolder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 900f));
            view.settingsPanel = SettingsPanelView.Build(settingsHolder);
            view.settingsPanel.gameObject.SetActive(false);
            canvas.gameObject.SetActive(false);
            return view;
        }

        static Button MenuButton(RectTransform parent, string label)
        {
            var b = UIFactory.CreateButton(label, parent, label, 28);
            UIFactory.Size(b, -1f, 68f);
            return b;
        }
    }
}
