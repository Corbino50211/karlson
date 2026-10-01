using System;
using UnityEngine;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>Simple modal Yes/No dialog.</summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] Text messageText;
        [SerializeField] Button yesButton;
        [SerializeField] Button noButton;

        Action onYes;
        Action onNo;

        void Awake()
        {
            if (yesButton != null) yesButton.onClick.AddListener(() => Close(true));
            if (noButton != null) noButton.onClick.AddListener(() => Close(false));
        }

        public void Show(string message, Action yes, Action no = null, string yesLabel = "YES", string noLabel = "NO")
        {
            onYes = yes;
            onNo = no;
            if (messageText != null) messageText.text = message;
            var yl = UIFactory.GetLabel(yesButton);
            if (yl != null) yl.text = yesLabel;
            var nl = UIFactory.GetLabel(noButton);
            if (nl != null) nl.text = noLabel;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        void Close(bool confirmed)
        {
            gameObject.SetActive(false);
            var callback = confirmed ? onYes : onNo;
            onYes = null;
            onNo = null;
            callback?.Invoke();
        }

        /// <summary>Builds the dialog as a full-screen child of a canvas.</summary>
        public static ConfirmDialog Build(Transform canvasRoot)
        {
            var overlay = UIFactory.CreatePanel("ConfirmDialog", canvasRoot, UITheme.Overlay);
            UIFactory.Stretch(overlay.rectTransform);
            var dialog = overlay.gameObject.AddComponent<ConfirmDialog>();
            var panel = UIFactory.CreatePanel("Panel", overlay.rectTransform, UITheme.PanelLight);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 320f));
            dialog.messageText = UIFactory.CreateText("Message", panel.rectTransform, "Are you sure?", 30, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold);
            UIFactory.Stretch(dialog.messageText.rectTransform, 30f, 30f, 30f, 110f);
            dialog.messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var buttons = UIFactory.CreateRect("Buttons", panel.rectTransform);
            buttons.anchorMin = new Vector2(0f, 0f);
            buttons.anchorMax = new Vector2(1f, 0f);
            buttons.pivot = new Vector2(0.5f, 0f);
            buttons.anchoredPosition = new Vector2(0f, 28f);
            buttons.sizeDelta = new Vector2(-60f, 64f);
            UIFactory.AddHorizontal(buttons.gameObject, 20f, null, TextAnchor.MiddleCenter, true);
            dialog.yesButton = UIFactory.CreateButton("Yes", buttons, "YES", 28);
            dialog.noButton = UIFactory.CreateButton("No", buttons, "NO", 28);
            overlay.gameObject.SetActive(false);
            return dialog;
        }
    }
}
