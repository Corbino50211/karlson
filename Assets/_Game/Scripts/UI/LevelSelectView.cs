using System;
using Momentum.Levels;
using Momentum.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>
    /// Level selection with two tabs: CAMPAIGN (cards with best time, rank and completion; boss levels
    /// styled differently) and CUSTOM MAPS (saved JSON levels with Play / Edit / Delete).
    /// </summary>
    public class LevelSelectView : MonoBehaviour
    {
        [SerializeField] Button campaignTab;
        [SerializeField] Button customTab;
        [SerializeField] Button continueButton;
        [SerializeField] Button backButton;
        [SerializeField] RectTransform campaignPage;
        [SerializeField] RectTransform campaignGrid;
        [SerializeField] RectTransform customPage;
        [SerializeField] RectTransform customList;
        [SerializeField] Button createLevelButton;
        [SerializeField] Button openFolderButton;
        [SerializeField] Text customEmptyText;
        [SerializeField] Text footerText;

        bool showingCustom;

        public Button BackButton => backButton;
        public ConfirmDialog Dialog { get; set; }
        public event Action Closed;

        void Awake()
        {
            if (campaignTab != null) campaignTab.onClick.AddListener(() => ShowTab(false));
            if (customTab != null) customTab.onClick.AddListener(() => ShowTab(true));
            if (backButton != null) backButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                Closed?.Invoke();
            });
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
            if (createLevelButton != null) createLevelButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.OpenLevelEditor(null);
            });
            if (openFolderButton != null) openFolderButton.onClick.AddListener(() =>
            {
                Application.OpenURL("file://" + CustomLevelStorage.LevelsDirectory);
            });
        }

        void OnEnable()
        {
            ShowTab(showingCustom);
        }

        public void Open(bool customTabSelected)
        {
            showingCustom = customTabSelected;
            gameObject.SetActive(true);
            ShowTab(customTabSelected);
        }

        public void ShowTab(bool custom)
        {
            showingCustom = custom;
            if (campaignPage != null) campaignPage.gameObject.SetActive(!custom);
            if (customPage != null) customPage.gameObject.SetActive(custom);
            if (campaignTab != null) campaignTab.colors = UIFactory.ButtonColors(custom ? UITheme.Button : UITheme.Accent, UITheme.Accent);
            if (customTab != null) customTab.colors = UIFactory.ButtonColors(custom ? UITheme.Accent : UITheme.Button, UITheme.Accent);
            if (continueButton != null) continueButton.gameObject.SetActive(!custom);
            if (custom) BuildCustomList();
            else BuildCampaign();
        }

        // ------------------------------------------------------------------ Campaign

        static LevelDefinition FindContinueLevel()
        {
            var registry = GameConfig.Instance.levelRegistry;
            var save = SaveManager.Instance;
            if (registry == null || registry.Count == 0) return null;
            for (int i = 0; i < registry.Count; i++)
            {
                var def = registry.GetByIndex(i);
                if (def == null) continue;
                bool unlocked = save == null || save.IsUnlocked(def);
                bool completed = save != null && save.IsCompleted(def.id);
                if (unlocked && !completed) return def;
            }
            return registry.GetByIndex(0);
        }

        void Continue()
        {
            var def = FindContinueLevel();
            if (def != null && GameManager.Instance != null) GameManager.Instance.LoadLevel(def);
        }

        void BuildCampaign()
        {
            if (campaignGrid == null) return;
            Clear(campaignGrid);
            var registry = GameConfig.Instance.levelRegistry;
            var save = SaveManager.Instance;
            if (registry == null || registry.Count == 0)
            {
                var t = UIFactory.CreateText("Empty", campaignGrid, "No campaign levels found.\nRun Tools > Parkour FPS > Setup Complete Game.", 26, TextAnchor.MiddleCenter, UITheme.TextDim);
                return;
            }

            var next = FindContinueLevel();
            if (continueButton != null)
            {
                var label = UIFactory.GetLabel(continueButton);
                if (label != null) label.text = next != null ? "CONTINUE  -  " + next.DisplayNumber + " " + next.displayName.ToUpperInvariant() : "CONTINUE";
            }

            int completedCount = 0;
            for (int i = 0; i < registry.Count; i++)
            {
                var def = registry.GetByIndex(i);
                if (def == null) continue;
                bool unlocked = save == null || save.IsUnlocked(def);
                var record = save != null ? save.GetRecord(def.id) : null;
                bool completed = record != null && record.completed;
                if (completed) completedCount++;
                BuildCard(def, unlocked, record);
            }
            if (footerText != null) footerText.text = "COMPLETED  " + completedCount + " / " + registry.Count;
        }

        void BuildCard(LevelDefinition def, bool unlocked, LevelRecord record)
        {
            bool boss = def.isBossLevel;
            var button = UIFactory.CreateButton("Card_" + def.id, campaignGrid, "", 20);
            Color baseColor = boss ? new Color(0.28f, 0.08f, 0.1f, 1f) : UITheme.PanelLight;
            Color hover = boss ? UITheme.Boss : UITheme.Accent;
            button.colors = UIFactory.ButtonColors(baseColor, hover);
            button.interactable = unlocked;
            var rt = (RectTransform)button.transform;
            var label = UIFactory.GetLabel(button);
            if (label != null) Destroy(label.gameObject);

            var stripe = UIFactory.CreateImage("Stripe", rt, unlocked ? def.accentColor : new Color(0.3f, 0.3f, 0.3f));
            UIFactory.Place(stripe.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(8f, 400f));
            stripe.rectTransform.anchorMin = new Vector2(0f, 0f);
            stripe.rectTransform.anchorMax = new Vector2(0f, 1f);
            stripe.rectTransform.sizeDelta = new Vector2(8f, 0f);

            var number = UIFactory.CreateText("Number", rt, def.DisplayNumber, 44, TextAnchor.UpperLeft, unlocked ? UITheme.Text : UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(number.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -10f), new Vector2(120f, 54f));

            if (boss)
            {
                var tag = UIFactory.CreateText("BossTag", rt, "BOSS", 20, TextAnchor.UpperRight, UITheme.Boss, FontStyle.Bold);
                UIFactory.Place(tag.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -14f), new Vector2(120f, 26f));
            }

            var name = UIFactory.CreateText("Name", rt, def.displayName.ToUpperInvariant(), 22, TextAnchor.UpperLeft, unlocked ? UITheme.Text : UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -66f), new Vector2(240f, 56f));
            name.horizontalOverflow = HorizontalWrapMode.Wrap;

            string status;
            Color statusColor;
            if (!unlocked)
            {
                status = "LOCKED";
                statusColor = UITheme.TextDim;
            }
            else if (record != null && record.completed)
            {
                status = "COMPLETED";
                statusColor = UITheme.Good;
            }
            else
            {
                status = "NEW";
                statusColor = UITheme.Accent;
            }
            var statusText = UIFactory.CreateText("Status", rt, status, 18, TextAnchor.LowerLeft, statusColor, FontStyle.Bold);
            UIFactory.Place(statusText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 40f), new Vector2(200f, 24f));

            string best = record != null && record.HasBestTime ? TimeFormat.Format(record.bestTime) : "--:--.--";
            var bestText = UIFactory.CreateText("Best", rt, "BEST  " + best, 18, TextAnchor.LowerLeft, UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(bestText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 14f), new Vector2(220f, 24f));

            string rank = record != null && !string.IsNullOrEmpty(record.bestRank) ? record.bestRank : "-";
            var rankText = UIFactory.CreateText("Rank", rt, rank, 56, TextAnchor.LowerRight, RankCalculator.GetColor(rank), FontStyle.Bold);
            UIFactory.Place(rankText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 6f), new Vector2(80f, 70f));

            if (unlocked)
            {
                var captured = def;
                button.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.LoadLevel(captured);
                });
            }
        }

        // ------------------------------------------------------------------ Custom maps

        void BuildCustomList()
        {
            if (customList == null) return;
            Clear(customList);
            var levels = CustomLevelStorage.ListLevels();
            if (customEmptyText != null)
            {
                customEmptyText.gameObject.SetActive(levels.Count == 0);
                customEmptyText.text = "No custom maps yet.\nCreate one in the LEVEL EDITOR.\n\nFolder: " + CustomLevelStorage.LevelsDirectory;
            }
            if (footerText != null) footerText.text = levels.Count + " CUSTOM MAP" + (levels.Count == 1 ? "" : "S");
            var save = SaveManager.Instance;
            foreach (var info in levels)
            {
                var row = UIFactory.CreatePanel("Row_" + info.fileName, customList, UITheme.PanelLight);
                UIFactory.Size(row, -1f, 92f);
                UIFactory.AddHorizontal(row.gameObject, 14f, new RectOffset(20, 16, 12, 12), TextAnchor.MiddleLeft);

                var textCol = UIFactory.CreateRect("Info", row.rectTransform);
                UIFactory.Size(textCol, -1f, 68f, 1f);
                var title = UIFactory.CreateText("Name", textCol, info.levelName.ToUpperInvariant(), 26, TextAnchor.UpperLeft, UITheme.Text, FontStyle.Bold);
                UIFactory.Stretch(title.rectTransform, 0f, 0f, 0f, 34f);
                float best = save != null ? save.GetBestTime("custom:" + info.levelId) : -1f;
                string details = (string.IsNullOrEmpty(info.author) ? "" : "BY " + info.author.ToUpperInvariant() + "   ") +
                                 info.objectCount + " OBJECTS   BEST " + TimeFormat.Format(best) + "   " + info.modified.ToString("yyyy-MM-dd HH:mm");
                var sub = UIFactory.CreateText("Details", textCol, details, 18, TextAnchor.LowerLeft, UITheme.TextDim);
                UIFactory.Stretch(sub.rectTransform, 0f, 0f, 36f, 0f);

                var path = info.path;
                var play = UIFactory.CreateButton("Play", row.rectTransform, "PLAY", 22);
                UIFactory.Size(play, 120f, 60f);
                play.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.PlayCustomLevel(path); });
                var edit = UIFactory.CreateButton("Edit", row.rectTransform, "EDIT", 22);
                UIFactory.Size(edit, 120f, 60f);
                edit.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.OpenLevelEditor(path); });
                var delete = UIFactory.CreateButton("Delete", row.rectTransform, "DELETE", 22);
                UIFactory.Size(delete, 130f, 60f);
                delete.colors = UIFactory.ButtonColors(UITheme.Button, UITheme.Danger);
                string levelName = info.levelName;
                delete.onClick.AddListener(() => ConfirmDelete(path, levelName));
            }
        }

        void ConfirmDelete(string path, string levelName)
        {
            void DoDelete()
            {
                CustomLevelStorage.Delete(path);
                BuildCustomList();
            }

            if (Dialog != null) Dialog.Show("Delete \"" + levelName + "\"?\nThis cannot be undone.", DoDelete, null, "DELETE", "CANCEL");
            else DoDelete();
        }

        static void Clear(RectTransform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        // ------------------------------------------------------------------ Builder

        public static LevelSelectView Build(Transform parent)
        {
            var panel = UIFactory.CreatePanel("LevelSelect", parent, UITheme.Panel);
            UIFactory.Stretch(panel.rectTransform);
            var view = panel.gameObject.AddComponent<LevelSelectView>();
            var p = panel.rectTransform;

            var tabs = UIFactory.CreateRect("Tabs", p);
            UIFactory.Place(tabs, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -28f), new Vector2(560f, 58f));
            UIFactory.AddHorizontal(tabs.gameObject, 12f, null, TextAnchor.MiddleLeft, true);
            view.campaignTab = UIFactory.CreateButton("CampaignTab", tabs, "CAMPAIGN", 26);
            view.customTab = UIFactory.CreateButton("CustomTab", tabs, "CUSTOM MAPS", 26);

            view.continueButton = UIFactory.CreateButton("Continue", p, "CONTINUE", 22);
            UIFactory.Place((RectTransform)view.continueButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -28f), new Vector2(480f, 58f));
            view.continueButton.colors = UIFactory.ButtonColors(new Color(0.35f, 0.2f, 0.08f, 1f), UITheme.Accent);

            // Campaign page
            view.campaignPage = UIFactory.CreateRect("CampaignPage", p);
            UIFactory.Stretch(view.campaignPage, 36f, 36f, 110f, 100f);
            view.campaignGrid = UIFactory.CreateRect("Grid", view.campaignPage);
            UIFactory.Stretch(view.campaignGrid);
            var grid = view.campaignGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(262f, 190f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperLeft;

            // Custom page
            view.customPage = UIFactory.CreateRect("CustomPage", p);
            UIFactory.Stretch(view.customPage, 36f, 36f, 110f, 100f);
            var scroll = UIFactory.CreateScrollView("List", view.customPage, out RectTransform content, 10f);
            UIFactory.Stretch((RectTransform)scroll.transform, 0f, 0f, 0f, 80f);
            view.customList = content;
            view.customEmptyText = UIFactory.CreateText("Empty", view.customPage, "", 24, TextAnchor.MiddleCenter, UITheme.TextDim);
            UIFactory.Stretch(view.customEmptyText.rectTransform, 0f, 0f, 0f, 80f);
            view.customEmptyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var customButtons = UIFactory.CreateRect("CustomButtons", view.customPage);
            customButtons.anchorMin = new Vector2(0f, 0f);
            customButtons.anchorMax = new Vector2(1f, 0f);
            customButtons.pivot = new Vector2(0.5f, 0f);
            customButtons.anchoredPosition = Vector2.zero;
            customButtons.sizeDelta = new Vector2(0f, 64f);
            UIFactory.AddHorizontal(customButtons.gameObject, 14f, null, TextAnchor.MiddleLeft);
            view.createLevelButton = UIFactory.CreateButton("Create", customButtons, "CREATE NEW MAP", 24);
            UIFactory.Size(view.createLevelButton, 320f, 60f);
            view.openFolderButton = UIFactory.CreateButton("OpenFolder", customButtons, "OPEN MAPS FOLDER", 24);
            UIFactory.Size(view.openFolderButton, 320f, 60f);
            view.customPage.gameObject.SetActive(false);

            view.footerText = UIFactory.CreateText("Footer", p, "", 22, TextAnchor.LowerLeft, UITheme.TextDim, FontStyle.Bold);
            UIFactory.Place(view.footerText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 36f), new Vector2(600f, 40f));

            view.backButton = UIFactory.CreateButton("Back", p, "BACK", 26);
            UIFactory.Place((RectTransform)view.backButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36f, 24f), new Vector2(220f, 58f));
            var sounds = view.backButton.GetComponent<UIButtonSounds>();
            if (sounds != null) sounds.SetBackButton(true);
            return view;
        }
    }
}
