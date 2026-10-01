using System;
using UnityEngine;
using UnityEngine.UI;

namespace Momentum.UI
{
    /// <summary>Left/right option selector ("&lt; 1920 x 1080 &gt;"). Simpler and sturdier than a dropdown.</summary>
    public class ValueCycler : MonoBehaviour
    {
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [SerializeField] Text valueText;

        string[] options = new string[0];
        int index;

        public int Index => index;
        public string Value => options.Length > 0 ? options[index] : "";
        public event Action<int> Changed;

        void Awake()
        {
            if (previousButton != null) previousButton.onClick.AddListener(() => Step(-1));
            if (nextButton != null) nextButton.onClick.AddListener(() => Step(1));
        }

        public void SetOptions(string[] values, int selected)
        {
            options = values ?? new string[0];
            index = options.Length == 0 ? 0 : Mathf.Clamp(selected, 0, options.Length - 1);
            Refresh();
        }

        public void SetIndexWithoutNotify(int value)
        {
            index = options.Length == 0 ? 0 : Mathf.Clamp(value, 0, options.Length - 1);
            Refresh();
        }

        void Step(int delta)
        {
            if (options.Length == 0) return;
            index = (index + delta + options.Length) % options.Length;
            Refresh();
            Changed?.Invoke(index);
        }

        void Refresh()
        {
            if (valueText != null) valueText.text = options.Length > 0 ? options[index] : "-";
        }

        public static ValueCycler Build(Transform parent, string name)
        {
            var root = UIFactory.CreateRect(name, parent);
            UIFactory.AddHorizontal(root.gameObject, 6f, null, TextAnchor.MiddleCenter);
            var cycler = root.gameObject.AddComponent<ValueCycler>();
            cycler.previousButton = UIFactory.CreateButton("Prev", root, "<", 26);
            UIFactory.Size(cycler.previousButton, 46f, 40f);
            var valueBg = UIFactory.CreateImage("ValueBg", root, new Color(0.16f, 0.17f, 0.2f, 1f));
            UIFactory.Size(valueBg, -1f, 40f, 1f);
            cycler.valueText = UIFactory.CreateText("Value", valueBg.rectTransform, "-", 24, TextAnchor.MiddleCenter, UITheme.Text, FontStyle.Bold);
            UIFactory.Stretch(cycler.valueText.rectTransform);
            cycler.nextButton = UIFactory.CreateButton("Next", root, ">", 26);
            UIFactory.Size(cycler.nextButton, 46f, 40f);
            return cycler;
        }
    }
}
