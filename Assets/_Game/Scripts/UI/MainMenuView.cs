using UnityEngine;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>Main menu layout: title, menu buttons (PLAY, LEVEL SELECT, LEVEL EDITOR, SETTINGS, CREDITS, QUIT) and content panels.</summary>
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] Text titleText;
        [SerializeField] Text subtitleText;
        [SerializeField] Text versionText;
        [SerializeField] Button playButton;
        [SerializeField] Button levelSelectButton;
        [SerializeField] Button editorButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button creditsButton;
        [SerializeField] Button quitButton;
        [SerializeField] LevelSelectView levelSelect;
        [SerializeField] SettingsPanelView settings;
        [SerializeField] CreditsView credits;
        [SerializeField] ConfirmDialog dialog;

        public Button PlayButton => playButton;
        public Button LevelSelectButton => levelSelectButton;
        public Button EditorButton => editorButton;
        public Button SettingsButton => settingsButton;
        public Button CreditsButton => creditsButton;
        public Button QuitButton => quitButton;
        public LevelSelectView LevelSelect => levelSelect;
        public SettingsPanelView Settings => settings;
        public CreditsView Credits => credits;
        public ConfirmDialog Dialog => dialog;

        void Awake()
        {
            var config = GameConfig.Instance;
            if (titleText != null) titleText.text = config.gameTitle;
            if (subtitleText != null) subtitleText.text = config.subtitle.ToUpperInvariant();
            if (versionText != null) versionText.text = "v" + config.version + "   " + config.studioName;
            if (levelSelect != null) levelSelect.Dialog = dialog;
        }

        public void ShowPanel(Component panel)
        {
            if (levelSelect != null) levelSelect.gameObject.SetActive(panel == levelSelect);
            if (settings != null) settings.gameObject.SetActive(panel == settings);
            if (credits != null) credits.gameObject.SetActive(panel == credits);
        }

        public static MainMenuView Build(Transform parent)
        {
            var canvas = UIFactory.CreateCanvas("MainMenu", 10, parent);
            var view = canvas.gameObject.AddComponent<MainMenuView>();
            var root = canvas.transform;

            var side = UIFactory.CreatePanel("Side", root, new Color(0.04f, 0.045f, 0.06f, 0.88f));
            side.rectTransform.anchorMin = new Vector2(0f, 0f);
            side.rectTransform.anchorMax = new Vector2(0f, 1f);
            side.rectTransform.pivot = new Vector2(0f, 0.5f);
            side.rectTransform.sizeDelta = new Vector2(620f, 0f);
            side.rectTransform.anchoredPosition = Vector2.zero;

            var accentBar = UIFactory.CreateImage("AccentBar", side.rectTransform, UITheme.Accent);
            accentBar.rectTransform.anchorMin = new Vector2(1f, 0f);
            accentBar.rectTransform.anchorMax = new Vector2(1f, 1f);
            accentBar.rectTransform.pivot = new Vector2(1f, 0.5f);
            accentBar.rectTransform.sizeDelta = new Vector2(6f, 0f);

            view.titleText = UIFactory.CreateText("Title", side.rectTransform, "MOMENTUM", 104, TextAnchor.UpperLeft, UITheme.Accent, FontStyle.Bold);
            UIFactory.Place(view.titleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -90f), new Vector2(560f, 120f));
            view.subtitleText = UIFactory.CreateText("Subtitle", side.rectTransform, "", 24, TextAnchor.UpperLeft, UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(view.subtitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -212f), new Vector2(540f, 40f));

            var buttons = UIFactory.CreateRect("Buttons", side.rectTransform);
            UIFactory.Place(buttons, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -320f), new Vector2(480f, 560f));
            UIFactory.AddVertical(buttons.gameObject, 14f, null, TextAnchor.UpperLeft);
            view.playButton = MenuButton(buttons, "PLAY");
            view.playButton.colors = UIFactory.ButtonColors(new Color(0.35f, 0.18f, 0.06f, 1f), UITheme.Accent);
            view.levelSelectButton = MenuButton(buttons, "LEVEL SELECT");
            view.editorButton = MenuButton(buttons, "LEVEL EDITOR");
            view.settingsButton = MenuButton(buttons, "SETTINGS");
            view.creditsButton = MenuButton(buttons, "CREDITS");
            view.quitButton = MenuButton(buttons, "QUIT");

            view.versionText = UIFactory.CreateText("Version", side.rectTransform, "", 18, TextAnchor.LowerLeft, UITheme.TextDim);
            UIFactory.Place(view.versionText.rectTransform, Vector2.zero, Vector2.zero, new Vector2(60f, 30f), new Vector2(520f, 30f));

            var content = UIFactory.CreateRect("Content", root);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 1f);
            content.offsetMin = new Vector2(680f, 70f);
            content.offsetMax = new Vector2(-60f, -70f);

            view.levelSelect = LevelSelectView.Build(content);
            view.levelSelect.gameObject.SetActive(false);
            view.settings = SettingsPanelView.Build(content);
            view.settings.gameObject.SetActive(false);
            view.credits = CreditsView.Build(content);
            view.credits.gameObject.SetActive(false);
            view.dialog = ConfirmDialog.Build(root);
            return view;
        }

        static Button MenuButton(RectTransform parent, string label)
        {
            var b = UIFactory.CreateButton(label, parent, label, 34, TextAnchor.MiddleLeft);
            UIFactory.Size(b, -1f, 74f);
            return b;
        }
    }
}
