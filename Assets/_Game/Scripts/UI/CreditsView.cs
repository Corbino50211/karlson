using System;
using UnityEngine;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>Credits panel (text comes from GameConfig.credits).</summary>
    public class CreditsView : MonoBehaviour
    {
        [SerializeField] Text bodyText;
        [SerializeField] Button backButton;

        public event Action Closed;

        void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                Closed?.Invoke();
            });
        }

        void OnEnable()
        {
            var config = GameConfig.Instance;
            if (bodyText != null) bodyText.text = config.credits + "\n\nVersion " + config.version;
        }

        public static CreditsView Build(Transform parent)
        {
            var panel = UIFactory.CreatePanel("Credits", parent, UITheme.Panel);
            UIFactory.Stretch(panel.rectTransform);
            var view = panel.gameObject.AddComponent<CreditsView>();
            var title = UIFactory.CreateText("Title", panel.rectTransform, "CREDITS", UITheme.HeaderSize, TextAnchor.UpperLeft, UITheme.Text, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(600f, 56f));
            view.bodyText = UIFactory.CreateText("Body", panel.rectTransform, "", 26, TextAnchor.UpperLeft, UITheme.Text);
            UIFactory.Stretch(view.bodyText.rectTransform, 40f, 40f, 110f, 100f);
            view.bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            view.backButton = UIFactory.CreateButton("Back", panel.rectTransform, "BACK", 26);
            UIFactory.Place((RectTransform)view.backButton.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36f, 24f), new Vector2(220f, 58f));
            return view;
        }
    }
}
