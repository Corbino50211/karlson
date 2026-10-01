using System;
using Momentum.Levels;
using Momentum.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Momentum.LevelEditor
{
    /// <summary>Modal list of saved custom levels with Load and Delete.</summary>
    public class LevelBrowserDialog : MonoBehaviour
    {
        [SerializeField] RectTransform listContent;
        [SerializeField] Text emptyText;
        [SerializeField] Button cancelButton;

        Action<string> onLoad;
        ConfirmDialog confirm;

        public bool IsOpen => gameObject.activeSelf;

        void Awake()
        {
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
        }

        public void Open(Action<string> loadCallback, ConfirmDialog confirmDialog)
        {
            onLoad = loadCallback;
            confirm = confirmDialog;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        void Refresh()
        {
            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                var child = listContent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            var levels = CustomLevelStorage.ListLevels();
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(levels.Count == 0);
                emptyText.text = "No saved levels yet.\n" + CustomLevelStorage.LevelsDirectory;
            }
            foreach (var info in levels)
            {
                var row = UIFactory.CreatePanel("Row", listContent, UITheme.PanelLight);
                UIFactory.Size(row, -1f, 70f);
                UIFactory.AddHorizontal(row.gameObject, 12f, new RectOffset(16, 12, 8, 8), TextAnchor.MiddleLeft);
                var label = UIFactory.CreateText("Name", row.rectTransform,
                    info.levelName + "\n<size=16><color=#9AA0AA>" + info.objectCount + " objects   " + info.modified.ToString("yyyy-MM-dd HH:mm") + "</color></size>",
                    22, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold);
                UIFactory.Size(label, -1f, 54f, 1f);
                string path = info.path;
                string levelName = info.levelName;
                var load = UIFactory.CreateButton("Load", row.rectTransform, "LOAD", 20);
                UIFactory.Size(load, 110f, 50f);
                load.onClick.AddListener(() =>
                {
                    Close();
                    onLoad?.Invoke(path);
                });
                var delete = UIFactory.CreateButton("Delete", row.rectTransform, "DELETE", 20);
                UIFactory.Size(delete, 120f, 50f);
                delete.colors = UIFactory.ButtonColors(UITheme.Button, UITheme.Danger);
                delete.onClick.AddListener(() =>
                {
                    if (confirm != null)
                    {
                        confirm.Show("Delete \"" + levelName + "\"?", () =>
                        {
                            CustomLevelStorage.Delete(path);
                            Refresh();
                        }, null, "DELETE", "CANCEL");
                    }
                    else
                    {
                        CustomLevelStorage.Delete(path);
                        Refresh();
                    }
                });
            }
        }

        public static LevelBrowserDialog Build(Transform canvasRoot)
        {
            var overlay = UIFactory.CreatePanel("LoadDialog", canvasRoot, UITheme.Overlay);
            UIFactory.Stretch(overlay.rectTransform);
            var dialog = overlay.gameObject.AddComponent<LevelBrowserDialog>();
            var panel = UIFactory.CreatePanel("Panel", overlay.rectTransform, UITheme.Background);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 720f));
            var title = UIFactory.CreateText("Title", panel.rectTransform, "LOAD LEVEL", 36, TextAnchor.UpperLeft, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(600f, 50f));
            var scroll = UIFactory.CreateScrollView("List", panel.rectTransform, out RectTransform content, 8f);
            UIFactory.Stretch((RectTransform)scroll.transform, 30f, 30f, 90f, 100f);
            dialog.listContent = content;
            dialog.emptyText = UIFactory.CreateText("Empty", panel.rectTransform, "", 22, TextAnchor.MiddleCenter, UITheme.TextDim);
            UIFactory.Stretch(dialog.emptyText.rectTransform, 30f, 30f, 90f, 100f);
            dialog.emptyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            dialog.cancelButton = UIFactory.CreateButton("Cancel", panel.rectTransform, "CANCEL", 24);
            UIFactory.Place((RectTransform)dialog.cancelButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(200f, 56f));
            overlay.gameObject.SetActive(false);
            return dialog;
        }
    }
}
